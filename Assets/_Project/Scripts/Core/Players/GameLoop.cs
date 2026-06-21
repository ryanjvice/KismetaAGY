using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Phases;
using Kismeta.Core.Rules;
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
        private CeremonyGate? _ceremonyGate;

        /// <summary>Set while the loop is awaiting a human controller's input.</summary>
        public HotSeatController? PendingHumanController { get; private set; }

        /// <summary>The action context hint that was provided with the current pending request.</summary>
        public ActionHint PendingHint { get; private set; }

        /// <summary>0-based index of the player the loop is currently asking to act.</summary>
        public int ActivePlayerId { get; private set; } = -1;

        /// <summary>
        /// When PendingHint is AdeptDecision or a Fate decision, this holds the card instance ID
        /// the player must decide on.
        /// </summary>
        public string? PendingCardId { get; private set; }

        /// <summary>Fired on every log-worthy event (phase changes, errors, etc.).</summary>
        public event Action<string>? OnLog;

        public GameLoop(GameSession session, IReadOnlyList<IPlayerController> controllers)
        {
            _session     = session;
            _controllers = controllers;
        }

        public void BindCeremonyGate(CeremonyGate gate) => _ceremonyGate = gate;

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

        // ─── Phase helper ─────────────────────────────────────────────────────────

        /// <summary>
        /// Jumps the phase controller to the first step of the given season and fires
        /// a <see cref="PhaseChangedEvent"/> so the UI left-column label stays in sync.
        /// </summary>
        private void SetPhase(Season season)
        {
            _session.Phase.SetSeason(season);
            _session.EmitEvent(new PhaseChangedEvent(season, 0));
        }

        // ─── Spring ───────────────────────────────────────────────────────────────

        private async Task RunSpringAsync(CancellationToken ct)
        {
            SetPhase(Season.Spring);

            await RunRoundOpenCeremonyAsync(ct);
            if (_session.IsOver || ct.IsCancellationRequested) return;

            await WaitCeremonyUiAsync(CeremonyStep.AgeOpening, ct);
            if (_session.IsOver || ct.IsCancellationRequested) return;

            await WaitCeremonyUiAsync(CeremonyStep.SpringIntro, ct);
            if (_session.IsOver || ct.IsCancellationRequested) return;

            Log("Spring — Step 2: Zodiac rolls");
            foreach (var player in _session.Players)
            {
                var rollCmd = await RequestAsync(player.PlayerId, ActionHint.RollZodiac, ct);
                Apply(rollCmd);

                if (_controllers[player.PlayerId] is HotSeatController)
                {
                    var ackCmd = await RequestAsync(player.PlayerId, ActionHint.AcknowledgeSign, ct);
                    Apply(ackCmd);
                }
            }

            Log("Spring — Step 3: Harvest");
            foreach (var player in _session.Players)
                Apply(new HarvestCommand(player.PlayerId, 0));

            // Resolve Fate cards that were drawn (auto + async)
            await ResolvePendingFatesAsync(ct);
            if (_session.IsOver || ct.IsCancellationRequested) return;

            // Resolve Adept purchase decisions queued during harvest
            await ResolvePendingAdeptsAsync(ct);
            if (_session.IsOver || ct.IsCancellationRequested) return;

            Log("Spring — Step 4: Commune");
            foreach (var player in _session.Players)
            {
                var cmd = await RequestAsync(player.PlayerId,
                    ActionHint.Commune, ct);
                Apply(cmd);
            }

            Log("Spring — Step 5: Card Lock");
            Apply(new SetCardLockCommand(true));
        }

        // ─── Fate card resolution ─────────────────────────────────────────────────

        private async Task ResolvePendingFatesAsync(CancellationToken ct)
        {
            var pending = _session.Board.PendingFateDecisions;
            if (pending.Count == 0) return;

            var snapshot = new List<(int PlayerId, string FateCardId, int ArcanaNumber)>(pending);
            pending.Clear();

            foreach (var (playerId, fateCardId, arcanaNum) in snapshot)
            {
                if (ct.IsCancellationRequested) break;

                // Auto-resolve inline (returns true) — nothing further needed
                if (_session.Rules?.FateResolver?.Resolve(_session, playerId, fateCardId, arcanaNum) ?? false)
                    continue;

                // Async fates require player input
                switch (arcanaNum)
                {
                    case 18: // Moon: draw 4 cards, then player keeps 2
                        Log($"Spring — Fate: The Moon — P{playerId} draws 4, keeps 2");
                        _session.Board.FateMoonDrawnCardIds.Clear();
                        DrawCards(playerId, 4, _session.Board.FateMoonDrawnCardIds);
                        var moonCmd = await RequestAsync(playerId, ActionHint.FateMoonDecision, ct, fateCardId);
                        Apply(moonCmd);
                        _session.Board.FateMoonDrawnCardIds.Clear();
                        break;

                    case 0: // Fool: drawer draws 2; each opponent picks 1 Reagent
                        Log($"Spring — Fate: The Fool — P{playerId} draws 2");
                        DrawCards(playerId, 2);
                        foreach (var opp in _session.Players)
                        {
                            if (opp.PlayerId == playerId || ct.IsCancellationRequested) continue;
                            Log($"Spring — Fate: The Fool — P{opp.PlayerId} picks a Reagent");
                            var reagentCmd = await RequestAsync(opp.PlayerId, ActionHint.FateReagentChoice, ct, fateCardId);
                            Apply(reagentCmd);
                        }
                        break;

                    case 6: // Lovers: drawer picks a target; that target chooses the reward for the drawer only
                        Log($"Spring — Fate: The Lovers — P{playerId} picks a target to choose their reward");
                        var loversTargetCmd = await RequestAsync(playerId, ActionHint.FateLoversTargetPick, ct, fateCardId);
                        int targetId = (playerId + 1) % _session.Players.Count;
                        if (loversTargetCmd is FateLoversTargetCommand lt && lt.ChosenTargetId != playerId)
                            targetId = lt.ChosenTargetId;
                        Log($"Spring — Fate: The Lovers — P{targetId} now chooses the reward for P{playerId}");
                        var loversRewardCmd = await RequestAsync(targetId, ActionHint.FateLoversChoice, ct, fateCardId);
                        if (loversRewardCmd is FateLoversChoiceCommand lc)
                        {
                            // Only the drawer receives the reward (the target is the chooser, not the recipient)
                            Apply(new FateLoversChoiceCommand(playerId, lc.DrawCards, lc.ChosenReagent));
                        }
                        break;
                }
            }
        }

        // ─── Adept decision resolution ────────────────────────────────────────────

        private async Task ResolvePendingAdeptsAsync(CancellationToken ct)
        {
            var pending = _session.Board.PendingAdeptDecisions;
            if (pending.Count == 0) return;

            // Process a snapshot; new entries won't be added mid-resolution
            var decisions = new List<(int PlayerId, string AdeptCardId)>(pending);
            pending.Clear();

            foreach (var (playerId, adeptCardId) in decisions)
            {
                if (ct.IsCancellationRequested) break;
                Log($"Spring — Adept decision: P{playerId} offered {adeptCardId}");
                var cmd = await RequestAsync(playerId, ActionHint.AdeptDecision, ct, adeptCardId);
                Apply(cmd);
            }
        }

        // ─── Summer ───────────────────────────────────────────────────────────────

        private async Task RunSummerAsync(CancellationToken ct)
        {
            SetPhase(Season.Summer);
            await WaitCeremonyUiAsync(CeremonyStep.SummerIntro, ct);
            if (_session.IsOver || ct.IsCancellationRequested) return;

            Log("Summer — Free-Action Pool");
            await RunFreeActionPool(ActionHint.SummerAction, ct);
        }

        // ─── Autumn ───────────────────────────────────────────────────────────────

        private async Task RunAutumnAsync(CancellationToken ct)
        {
            SetPhase(Season.Autumn);
            await WaitCeremonyUiAsync(CeremonyStep.AutumnIntro, ct);
            if (_session.IsOver || ct.IsCancellationRequested) return;

            Log("Autumn — Free-Action Pool");
            await RunFreeActionPool(ActionHint.AutumnAction, ct);
        }

        // ─── Winter ───────────────────────────────────────────────────────────────

        private async Task RunWinterAsync(CancellationToken ct)
        {
            SetPhase(Season.Winter);
            await WaitCeremonyUiAsync(CeremonyStep.WinterIntro, ct);
            if (_session.IsOver || ct.IsCancellationRequested) return;

            Log("Winter — Step 1: Card Unlock");
            Apply(new SetCardLockCommand(false));

            Log("Winter — Step 2: Activities");
            await RunFreeActionPool(ActionHint.WinterAction, ct);

            Log("Winter — Step 3: Enforce Card Limits");
            await RunWinterDiscardAsync(ct);

            Log("Winter — Step 4: Transit Age");
            await RunAgeClosingCeremonyAsync(ct);
        }

        private async Task RunRoundOpenCeremonyAsync(CancellationToken ct)
        {
            int keeperId = FindAgekeeperId();
            Log("Spring — Step 1: Cosmic Age roll");

            if (IsHumanPlayer(keeperId) && _ceremonyGate != null)
            {
                var result = await _ceremonyGate.WaitAsync(CeremonyStep.RoundOpen, ct);
                if (result.Command is RollCosmicAgeCommand roll)
                    Apply(roll);
                else
                    Apply(new RollCosmicAgeCommand(keeperId));
            }
            else
            {
                Apply(new RollCosmicAgeCommand(keeperId));
            }
        }

        private async Task RunAgeClosingCeremonyAsync(CancellationToken ct)
        {
            if (_ceremonyGate != null && HasAnyHumanPlayer())
            {
                var result = await _ceremonyGate.WaitAsync(CeremonyStep.AgeClosing, ct);
                if (result.Command is TransitAgeCommand)
                    Apply(result.Command);
                else
                    Apply(new TransitAgeCommand());
            }
            else
            {
                Apply(new TransitAgeCommand());
            }
        }

        private async Task WaitCeremonyUiAsync(CeremonyStep step, CancellationToken ct)
        {
            if (_ceremonyGate == null || !HasAnyHumanPlayer())
                return;

            await _ceremonyGate.WaitAsync(step, ct);
        }

        private bool HasAnyHumanPlayer()
        {
            foreach (var c in _controllers)
            {
                if (c is HotSeatController)
                    return true;
            }
            return false;
        }

        private bool IsHumanPlayer(int playerId) =>
            playerId >= 0 && playerId < _controllers.Count && _controllers[playerId] is HotSeatController;

        /// <summary>
        /// For each player: if over Hand or Spread limits, ask them to choose discards.
        /// Always clear Fate cards from Arcanum (automated cleanup for every player).
        /// </summary>
        private async Task RunWinterDiscardAsync(CancellationToken ct)
        {
            _session.Rules?.Winter?.ClearFateCardsFromArcanum(_session);

            foreach (var player in _session.Players)
            {
                bool overSpread = player.Spread.Count > WinterRules.SpreadLimit;
                bool overHand   = player.Hand.Count   > WinterRules.HandLimit;
                if (!overSpread && !overHand) continue;

                var cmd = await RequestAsync(player.PlayerId, ActionHint.DiscardToLimit, ct);
                Apply(cmd);
            }
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
                var result   = Apply(cmd);

                // A player is considered to have "given up their turn" if they:
                //   (a) explicitly passed, OR
                //   (b) submitted a command that the rule engine rejected (nothing changed).
                // Only a successfully applied non-pass action resets the streak.
                bool passed = cmd is PassActionCommand or PassCrucibleActionCommand;
                bool acted  = !passed && result.IsOk;
                consecutivePasses = acted ? 0 : consecutivePasses + 1;

                currentIdx = (currentIdx + 1) % playerCount;
            }
        }

        // ─── Controller dispatch ──────────────────────────────────────────────────

        private async Task<IGameCommand> RequestAsync(int playerId, ActionHint hint,
            CancellationToken ct, string? pendingCardId = null)
        {
            ActivePlayerId = playerId;
            PendingCardId  = pendingCardId;
            var controller = _controllers[playerId];

            var pubView  = GamePublicView.From(_session);
            var privView = PlayerPrivateView.From(_session, playerId);

            // For The Moon decision, pass the 4 drawn card IDs so controllers can validate
            IReadOnlyList<string>? moonDrawnIds = hint == ActionHint.FateMoonDecision
                ? _session.Board.FateMoonDrawnCardIds
                : null;

            // For Adept decisions, pass which Arcanum slots are already Adepts (vs Fate cards)
            IReadOnlyList<string>? arcanumAdeptIds = null;
            if (hint == ActionHint.AdeptDecision)
            {
                var player = _session.Players[playerId];
                var adepts = new List<string>();
                foreach (var id in player.Arcanum)
                {
                    var inst = _session.GetCard(id);
                    var def  = inst != null ? _session.Rules?.CardDatabase.GetById(inst.DefinitionId) : null;
                    if (def?.MajorArcanaType == Domain.MajorArcanaType.Adept)
                        adepts.Add(id);
                }
                arcanumAdeptIds = adepts;
            }

            var ctx = new GameContext(pubView, privView, playerId, hint, pendingCardId,
                moonDrawnIds, arcanumAdeptIds,
                _session.Rules?.CodexDatabase,
                _session.Rules?.CardDatabase,
                _session.Rules?.AlchemicalValidator);

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
                PendingHint            = ActionHint.None;
                PendingCardId          = null;
                ActivePlayerId         = -1;
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

        /// <summary>
        /// Draw <paramref name="count"/> cards from CommonDeck, routing each through
        /// <see cref="IHarvestService.RouteDrawnCard"/> so Major Arcana never land in Hand.
        /// Minor Arcana card ids are appended to <paramref name="minorPool"/> when provided
        /// (used by the Moon fate to build the player's "keep-2" pick list).
        /// </summary>
        private void DrawCards(int playerId, int count, List<string>? minorPool = null)
        {
            var harvest = _session.Rules?.Harvest;
            for (int i = 0; i < count; i++)
            {
                if (_session.Board.CommonDeck.Count == 0)
                    SpringRules.ReshuffleDiscardStatic(_session);
                if (_session.Board.CommonDeck.Count == 0) break;

                var id = _session.Board.CommonDeck.Pop();
                if (harvest != null)
                    harvest.RouteDrawnCard(_session, playerId, id, minorPool);
                else
                {
                    // Fallback: plain hand add (tests without rules wired)
                    _session.GetCard(id)?.MoveTo(CardZone.Hand, playerId);
                    _session.Players[playerId].Hand.Add(id);
                    minorPool?.Add(id);
                }
            }
        }

        private void Log(string msg) => OnLog?.Invoke(msg);
    }

    /// <summary>Hints passed to a controller indicating what kind of action is expected.</summary>
    public enum ActionHint
    {
        None,
        /// <summary>Player rolls their Zodiac Die to determine their sign for the round.</summary>
        RollZodiac,
        /// <summary>Player reviews their rolled sign before harvest continues.</summary>
        AcknowledgeSign,
        Commune,
        SummerAction,
        AutumnAction,
        /// <summary>Player must choose to Buy or Decline an Adept card just drawn in Harvest.</summary>
        AdeptDecision,
        /// <summary>The Moon Fate: player keeps 2 of 4 drawn cards, returns the rest.</summary>
        FateMoonDecision,
        /// <summary>The Fool/Lovers Fate: player picks one Reagent from the supply.</summary>
        FateReagentChoice,
        /// <summary>The Lovers Fate: target player chooses the drawer's reward.</summary>
        FateLoversChoice,
        /// <summary>The Lovers Fate: drawer picks which opponent will choose the reward.</summary>
        FateLoversTargetPick,
        /// <summary>Winter Activities free-action pool: move cards Hand↔Spread, craft, or place Fateful Wager.</summary>
        WinterAction,
        /// <summary>Player must choose which cards to discard because they are over the zone limit.</summary>
        DiscardToLimit,
    }
}
