using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Views;

namespace Kismeta.Core.Players
{
    /// <summary>
    /// Drives one complete game of Kismeta from Setup through end-of-game.
    /// Calls <see cref="IPlayerController.RequestActionAsync"/> for every decision point,
    /// applying the returned command to the session and advancing the phase machine.
    ///
    /// Free-action phases (Summer and Autumn) use a consecutive-pass pool:
    /// the pool ends when all players pass without anyone acting in between.
    ///
    /// This class has no Unity dependency; it can be unit-tested with seeded AI controllers.
    /// </summary>
    public sealed class GameLoop
    {
        private readonly GameSession _session;
        private readonly IReadOnlyList<IPlayerController> _controllers;

        /// <summary>Set while the loop is awaiting a human controller's input.</summary>
        public HotSeatController? PendingHumanController { get; private set; }

        /// <summary>The action context hint that was provided with the current pending request.</summary>
        public ActionHint PendingHint { get; private set; }

        /// <summary>0-based index of the player the loop is currently asking to act.</summary>
        public int ActivePlayerId { get; private set; } = -1;

        /// <summary>Fired on every log-worthy event (phase changes, errors, etc.).</summary>
        public event Action<string>? OnLog;

        public GameLoop(GameSession session, IReadOnlyList<IPlayerController> controllers)
        {
            _session     = session;
            _controllers = controllers;
        }

        // ─── Entry point ──────────────────────────────────────────────────────────

        /// <summary>Run the complete game, resolving each round until IsOver or cancelled.</summary>
        public async Task RunAsync(CancellationToken ct = default)
        {
            Log("=== Game Loop Started ===");

            // One-time setup
            Apply(new SetupGameCommand());
            if (_session.IsOver) return;

            while (!_session.IsOver && !ct.IsCancellationRequested)
            {
                Log($"─── Round {_session.Board.RoundNumber} ───");
                await RunRoundAsync(ct);
            }

            if (_session.IsOver)
                Log($"=== Game Over — Winner: Player {_session.WinnerPlayerId} ===");
        }

        // ─── Round ────────────────────────────────────────────────────────────────

        private async Task RunRoundAsync(CancellationToken ct)
        {
            await RunSpringAsync(ct);   if (_session.IsOver || ct.IsCancellationRequested) return;
            await RunSummerAsync(ct);   if (_session.IsOver || ct.IsCancellationRequested) return;
            await RunAutumnAsync(ct);   if (_session.IsOver || ct.IsCancellationRequested) return;
            await RunWinterAsync(ct);
        }

        // ─── Spring ───────────────────────────────────────────────────────────────

        private async Task RunSpringAsync(CancellationToken ct)
        {
            Log("Spring — Step 1: Cosmic Age roll");
            Apply(new RollCosmicAgeCommand(FindAgekeeperId()));

            Log("Spring — Step 2: Zodiac rolls");
            foreach (var player in _session.Players)
                Apply(new RollZodiacCommand(player.PlayerId));

            Log("Spring — Step 3: Harvest");
            foreach (var player in _session.Players)
                Apply(new HarvestCommand(player.PlayerId, 0));

            Log("Spring — Step 4: Commune");
            foreach (var player in _session.Players)
            {
                var cmd = await RequestAsync(player.PlayerId,
                    ActionHint.Commune, ct);
                Apply(cmd);
            }

            Log("Spring — Step 5: Card Lock");
            Apply(new SetCardLockCommand(true));

            Apply(new AdvancePhaseCommand());
        }

        // ─── Summer ───────────────────────────────────────────────────────────────

        private async Task RunSummerAsync(CancellationToken ct)
        {
            Log("Summer — Free-Action Pool");
            await RunFreeActionPool(ActionHint.SummerAction, ct);
            Apply(new AdvancePhaseCommand());
        }

        // ─── Autumn ───────────────────────────────────────────────────────────────

        private async Task RunAutumnAsync(CancellationToken ct)
        {
            Log("Autumn — Free-Action Pool");
            await RunFreeActionPool(ActionHint.AutumnAction, ct);
            Apply(new AdvancePhaseCommand());
        }

        // ─── Winter ───────────────────────────────────────────────────────────────

        private async Task RunWinterAsync(CancellationToken ct)
        {
            Log("Winter — Step 1: Card Unlock");
            Apply(new SetCardLockCommand(false));

            Log("Winter — Step 3: Enforce Card Limits");
            Apply(new EnforceCardLimitsCommand());

            Log("Winter — Step 4: Transit Age");
            Apply(new TransitAgeCommand());

            Apply(new AdvancePhaseCommand());
        }

        // ─── Free-action pool (shared by Summer + Autumn) ────────────────────────

        /// <summary>
        /// Round-robin, starting from the Agekeeper, until all players pass consecutively.
        /// </summary>
        private async Task RunFreeActionPool(ActionHint hint, CancellationToken ct)
        {
            int playerCount       = _session.Players.Count;
            int startIdx          = FindAgekeeperIndex();
            int consecutivePasses = 0;
            int currentIdx        = startIdx;

            while (consecutivePasses < playerCount && !_session.IsOver && !ct.IsCancellationRequested)
            {
                int playerId = _session.Players[currentIdx].PlayerId;
                var cmd      = await RequestAsync(playerId, hint, ct);
                Apply(cmd);

                bool passed = cmd is PassActionCommand or PassCrucibleActionCommand;
                consecutivePasses = passed ? consecutivePasses + 1 : 0;

                currentIdx = (currentIdx + 1) % playerCount;
            }
        }

        // ─── Controller dispatch ──────────────────────────────────────────────────

        private async Task<IGameCommand> RequestAsync(int playerId, ActionHint hint, CancellationToken ct)
        {
            ActivePlayerId = playerId;
            var controller = _controllers[playerId];

            var pubView  = GamePublicView.From(_session);
            var privView = PlayerPrivateView.From(_session, playerId);
            var ctx      = new GameContext(pubView, privView, playerId, hint);

            if (controller is HotSeatController hs)
            {
                PendingHumanController = hs;
                PendingHint            = hint;
            }

            IGameCommand command;
            try
            {
                command = await controller.RequestActionAsync(ctx, ct);
            }
            finally
            {
                PendingHumanController = null;
                ActivePlayerId = -1;
            }

            return command;
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private CommandResult Apply(IGameCommand cmd)
        {
            var result = _session.Apply(cmd);
            if (!result.IsOk)
                Log($"[WARN] {cmd.GetType().Name} → {result.Status}: {result.Message}");
            return result;
        }

        private int FindAgekeeperId()
        {
            foreach (var p in _session.Players)
                if (p.IsAgekeeper) return p.PlayerId;
            return _session.Players[0].PlayerId;
        }

        private int FindAgekeeperIndex()
        {
            for (int i = 0; i < _session.Players.Count; i++)
                if (_session.Players[i].IsAgekeeper) return i;
            return 0;
        }

        private void Log(string msg) => OnLog?.Invoke(msg);
    }

    /// <summary>Hints passed to a controller indicating what kind of action is expected.</summary>
    public enum ActionHint
    {
        None,
        Commune,
        SummerAction,
        AutumnAction
    }
}
