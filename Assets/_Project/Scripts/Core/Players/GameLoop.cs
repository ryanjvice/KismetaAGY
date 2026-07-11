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
    /// Season action phases use a single clockwise lap from the Agekeeper;
    /// each player takes multiple actions until they End Turn (Pass).
    /// Contests pause the active turn until the defender responds.
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

        /// <summary>Player whose proactive turn is active during a season action phase.</summary>
        public int TurnPlayerId { get; private set; } = -1;

        /// <summary>First hot-seat player index, or 0 when no human is seated.</summary>
        public int LocalHumanPlayerId
        {
            get
            {
                for (int i = 0; i < _controllers.Count; i++)
                {
                    if (_controllers[i] is HotSeatController)
                        return i;
                }
                return 0;
            }
        }

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

        /// <summary>
        /// When set, invoked after a contest resolves so the UI can block until seated humans
        /// acknowledge any exchange summary they participated in.
        /// </summary>
        public Func<CancellationToken, Task>? WaitForPendingExchangesAsync { get; set; }

        /// <summary>
        /// When set, invoked after each harvest card is dealt so the UI can animate before the next step.
        /// </summary>
        public Func<CancellationToken, Task>? WaitForHarvestCardUiAsync { get; set; }

        /// <summary>
        /// When set, invoked after a human begins dealing so the UI can show the tableau.
        /// </summary>
        public Func<CancellationToken, Task>? WaitForHarvestDealStartUiAsync { get; set; }

        /// <summary>True when the local human is not the active turn player during a season action phase.</summary>
        public bool IsLocalHumanWaitingForTurn =>
            TurnPlayerId >= 0 && TurnPlayerId != LocalHumanPlayerId;

        // ─── Entry point ──────────────────────────────────────────────────────────

        /// <summary>Run the complete game, resolving each round until IsOver or cancelled.</summary>
        public async Task RunAsync(CancellationToken ct = default)
        {
            Log("=== Game Loop Started ===");

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

        private void SetPhase(Season season)
        {
            _session.Phase.SetSeason(season);
            _session.EmitEvent(new PhaseChangedEvent(season, 0));
        }

        // ─── Spring ───────────────────────────────────────────────────────────────

        private async Task RunSpringAsync(CancellationToken ct)
        {
            SetPhase(Season.Spring);

            await WaitCeremonyUiAsync(CeremonyStep.SpringIntro, ct);
            if (_session.IsOver || ct.IsCancellationRequested) return;

            await RunAgeOpenCeremonyAsync(ct);
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
            {
                await RunPlayerHarvestAsync(player.PlayerId, ct);
                if (_session.IsOver || ct.IsCancellationRequested) return;
            }

            await ResolvePendingPriestessReturnsAsync(ct);
            AuditInventory("after ResolvePendingPriestessReturns");
            if (_session.IsOver || ct.IsCancellationRequested) return;

            await ResolvePendingFatesAsync(ct);
            AuditInventory("after ResolvePendingFates");
            if (_session.IsOver || ct.IsCancellationRequested) return;

            await ResolvePendingAdeptsAsync(ct);
            AuditInventory("after ResolvePendingAdepts");
            if (_session.IsOver || ct.IsCancellationRequested) return;

            Log("Spring — Step 4: Spring Hub (Commune / Build a House)");
            await RunSpringHubAsync(ct);
            if (_session.IsOver || ct.IsCancellationRequested) return;

            Log("Spring — Step 5: Card Lock");
            Apply(new SetCardLockCommand(true));
            AuditInventory("end Spring");
        }

        // ─── Per-player harvest ───────────────────────────────────────────────────

        private async Task RunPlayerHarvestAsync(int playerId, CancellationToken ct)
        {
            var cmd = await RequestAsync(playerId, ActionHint.ConfirmHarvest, ct);

            if (cmd is HarvestCommand batch)
            {
                Apply(batch);
                return;
            }

            if (cmd is not BeginHarvestCommand begin || begin.PlayerId != playerId)
            {
                Apply(cmd);
                return;
            }

            Apply(begin);

            if (WaitForHarvestDealStartUiAsync != null)
                await WaitForHarvestDealStartUiAsync(ct);

            while (!ct.IsCancellationRequested)
            {
                var deal = _session.Board.ActiveHarvestDeal;
                if (deal == null || deal.PlayerId != playerId)
                    break;

                Apply(new HarvestDealStepCommand(playerId));

                if (WaitForHarvestCardUiAsync != null)
                    await WaitForHarvestCardUiAsync(ct);

                await ResolveInlineAdeptsForPlayerAsync(playerId, ct);
                await ResolveInlineFatesForPlayerAsync(playerId, ct);

                if (WaitForPendingExchangesAsync != null)
                    await WaitForPendingExchangesAsync(ct);

                if (_session.Board.ActiveHarvestDeal == null)
                    break;
            }

            if (_session.Board.PendingPriestessReturns.Contains(playerId))
            {
                Log($"Spring — Priestess: P{playerId} returns 2 cards to deck");
                var priestessCmd = await RequestAsync(playerId, ActionHint.PriestessHarvestReturn, ct);
                Apply(priestessCmd);
                _session.Board.PendingPriestessReturns.Remove(playerId);
            }
        }

        /// <summary>
        /// Step 4: each player communes on Spring Hub, then takes Spring Action turns until pass.
        /// </summary>
        private async Task RunSpringHubAsync(CancellationToken ct)
        {
            int playerCount = _session.Players.Count;
            int startIdx    = FindAgekeeperIndex();

            for (int i = 0; i < playerCount; i++)
            {
                if (_session.IsOver || ct.IsCancellationRequested) return;

                int playerId = _session.Players[(startIdx + i) % playerCount].PlayerId;
                Log($"Spring — Commune: P{playerId}");
                var communeCmd = await RequestAsync(playerId, ActionHint.Commune, ct);
                Apply(communeCmd);
                if (_session.IsOver || ct.IsCancellationRequested) return;

                await RunPlayerTurnAsync(playerId, ActionHint.SpringAction, ct);
            }
        }

        private async Task ResolveInlineAdeptsForPlayerAsync(int playerId, CancellationToken ct)
        {
            var pending = _session.Board.PendingAdeptDecisions;
            if (pending.Count == 0) return;

            var decisions = new List<(int PlayerId, string AdeptCardId)>();
            foreach (var (pid, adeptCardId) in pending)
            {
                if (pid != playerId) continue;
                if (!ShouldOfferAdeptDecision(playerId, adeptCardId)) continue;
                decisions.Add((pid, adeptCardId));
            }

            foreach (var (pid, adeptCardId) in decisions)
            {
                if (ct.IsCancellationRequested) break;
                pending.Remove((pid, adeptCardId));
                Log($"Spring — Adept decision: P{pid} offered {adeptCardId}");
                var cmd = await RequestAsync(pid, ActionHint.AdeptDecision, ct, adeptCardId);
                var result = Apply(cmd);
                if (!result.IsOk)
                {
                    pending.Add((pid, adeptCardId));
                    Log($"[WARN] Adept decision rejected, re-queued: {result.Message}");
                }
            }
        }

        private async Task ResolveInlineFatesForPlayerAsync(int playerId, CancellationToken ct)
        {
            var pending = _session.Board.PendingFateDecisions;
            if (pending.Count == 0) return;

            var snapshot = new List<(int PlayerId, string FateCardId, int ArcanaNumber)>();
            foreach (var entry in pending)
            {
                if (entry.PlayerId == playerId)
                    snapshot.Add(entry);
            }

            foreach (var (pid, fateCardId, arcanaNum) in snapshot)
            {
                if (ct.IsCancellationRequested) break;
                pending.Remove((pid, fateCardId, arcanaNum));

                if (_session.Rules?.FateResolver?.Resolve(_session, pid, fateCardId, arcanaNum) ?? false)
                    continue;

                switch (arcanaNum)
                {
                    case 18:
                        Log($"Spring — Fate: The Moon — P{pid} draws 4, keeps 2");
                        _session.Board.FateMoonDrawnCardIds.Clear();
                        DrawCards(pid, 4, _session.Board.FateMoonDrawnCardIds);
                        var moonCmd = await RequestAsync(pid, ActionHint.FateMoonDecision, ct, fateCardId);
                        var moonResult = Apply(moonCmd);
                        if (moonResult.IsOk)
                            _session.Board.FateMoonDrawnCardIds.Clear();
                        else
                        {
                            pending.Add((pid, fateCardId, arcanaNum));
                            Log($"[WARN] Moon decision rejected: {moonResult.Message}");
                        }
                        break;

                    case 0:
                        Log($"Spring — Fate: The Fool — P{pid} draws 2");
                        DrawCards(pid, 2);
                        foreach (var opp in _session.Players)
                        {
                            if (opp.PlayerId == pid || ct.IsCancellationRequested) continue;
                            Log($"Spring — Fate: The Fool — P{opp.PlayerId} picks a Reagent");
                            var reagentCmd = await RequestAsync(opp.PlayerId, ActionHint.FateReagentChoice, ct, fateCardId);
                            Apply(reagentCmd);
                        }
                        break;

                    case 6:
                        Log($"Spring — Fate: The Lovers — P{pid} picks a target to choose their reward");
                        var loversTargetCmd = await RequestAsync(pid, ActionHint.FateLoversTargetPick, ct, fateCardId);
                        int targetId = (pid + 1) % _session.Players.Count;
                        if (loversTargetCmd is FateLoversTargetCommand lt && lt.ChosenTargetId != pid)
                            targetId = lt.ChosenTargetId;
                        Log($"Spring — Fate: The Lovers — P{targetId} now chooses the reward for P{pid}");
                        var loversRewardCmd = await RequestAsync(targetId, ActionHint.FateLoversChoice, ct, fateCardId);
                        if (loversRewardCmd is FateLoversChoiceCommand lc)
                            Apply(new FateLoversChoiceCommand(pid, lc.DrawCards, lc.ChosenReagent, targetId));
                        break;

                    default:
                        pending.Add((pid, fateCardId, arcanaNum));
                        break;
                }

                if (WaitForPendingExchangesAsync != null)
                    await WaitForPendingExchangesAsync(ct);
            }
        }

        // ─── Fate card resolution ─────────────────────────────────────────────────

        private async Task ResolvePendingPriestessReturnsAsync(CancellationToken ct)
        {
            var pending = _session.Board.PendingPriestessReturns;
            if (pending.Count == 0) return;

            var snapshot = new List<int>(pending);
            foreach (var playerId in snapshot)
            {
                if (ct.IsCancellationRequested) break;
                if (!pending.Contains(playerId)) continue;

                Log($"Spring — Priestess: P{playerId} returns 2 cards to deck");
                var cmd = await RequestAsync(playerId, ActionHint.PriestessHarvestReturn, ct);
                Apply(cmd);
            }
        }

        private async Task ResolvePendingFatesAsync(CancellationToken ct)
        {
            var pending = _session.Board.PendingFateDecisions;
            if (pending.Count == 0) return;

            var snapshot = new List<(int PlayerId, string FateCardId, int ArcanaNumber)>(pending);
            pending.Clear();

            foreach (var (playerId, fateCardId, arcanaNum) in snapshot)
            {
                if (ct.IsCancellationRequested) break;

                if (_session.Rules?.FateResolver?.Resolve(_session, playerId, fateCardId, arcanaNum) ?? false)
                    continue;

                switch (arcanaNum)
                {
                    case 18:
                        Log($"Spring — Fate: The Moon — P{playerId} draws 4, keeps 2");
                        _session.Board.FateMoonDrawnCardIds.Clear();
                        DrawCards(playerId, 4, _session.Board.FateMoonDrawnCardIds);
                        var moonCmd = await RequestAsync(playerId, ActionHint.FateMoonDecision, ct, fateCardId);
                        var moonResult = Apply(moonCmd);
                        if (moonResult.IsOk)
                            _session.Board.FateMoonDrawnCardIds.Clear();
                        else
                            Log($"[WARN] Moon decision rejected: {moonResult.Message}");
                        break;

                    case 0:
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

                    case 6:
                        Log($"Spring — Fate: The Lovers — P{playerId} picks a target to choose their reward");
                        var loversTargetCmd = await RequestAsync(playerId, ActionHint.FateLoversTargetPick, ct, fateCardId);
                        int targetId = (playerId + 1) % _session.Players.Count;
                        if (loversTargetCmd is FateLoversTargetCommand lt && lt.ChosenTargetId != playerId)
                            targetId = lt.ChosenTargetId;
                        Log($"Spring — Fate: The Lovers — P{targetId} now chooses the reward for P{playerId}");
                        var loversRewardCmd = await RequestAsync(targetId, ActionHint.FateLoversChoice, ct, fateCardId);
                        if (loversRewardCmd is FateLoversChoiceCommand lc)
                            Apply(new FateLoversChoiceCommand(playerId, lc.DrawCards, lc.ChosenReagent, targetId));
                        break;
                }
            }
        }

        // ─── Adept decision resolution ────────────────────────────────────────────

        private async Task ResolvePendingAdeptsAsync(CancellationToken ct)
        {
            var pending = _session.Board.PendingAdeptDecisions;
            if (pending.Count == 0) return;

            var decisions = new List<(int PlayerId, string AdeptCardId)>();
            var seen = new HashSet<string>();
            foreach (var (playerId, adeptCardId) in pending)
            {
                if (!seen.Add(adeptCardId)) continue;
                if (!ShouldOfferAdeptDecision(playerId, adeptCardId)) continue;
                decisions.Add((playerId, adeptCardId));
            }
            pending.Clear();

            foreach (var (playerId, adeptCardId) in decisions)
            {
                if (ct.IsCancellationRequested) break;
                Log($"Spring — Adept decision: P{playerId} offered {adeptCardId}");
                var cmd = await RequestAsync(playerId, ActionHint.AdeptDecision, ct, adeptCardId);
                var result = Apply(cmd);
                if (!result.IsOk)
                {
                    pending.Add((playerId, adeptCardId));
                    Log($"[WARN] Adept decision rejected, re-queued: {result.Message}");
                }
            }
        }

        private bool ShouldOfferAdeptDecision(int playerId, string adeptCardId)
        {
            var player = _session.Players[playerId];
            if (player.Arcanum.Contains(adeptCardId))
                return false;

            var inst = _session.GetCard(adeptCardId);
            return inst?.Zone != CardZone.Discard;
        }

        // ─── Summer ───────────────────────────────────────────────────────────────

        private async Task RunSummerAsync(CancellationToken ct)
        {
            SetPhase(Season.Summer);
            await WaitCeremonyUiAsync(CeremonyStep.SummerIntro, ct);
            if (_session.IsOver || ct.IsCancellationRequested) return;

            Log("Summer — Turn-based actions (Trade / Duel / Gambit / Opposition)");
            await RunSeasonTurnsAsync(ActionHint.SummerAction, ct);
            AuditInventory("end Summer");
        }

        // ─── Autumn ───────────────────────────────────────────────────────────────

        private async Task RunAutumnAsync(CancellationToken ct)
        {
            SetPhase(Season.Autumn);
            await WaitCeremonyUiAsync(CeremonyStep.AutumnIntro, ct);
            if (_session.IsOver || ct.IsCancellationRequested) return;

            Log("Autumn — Turn-based actions (Craft / Activate / Forge)");
            await RunSeasonTurnsAsync(ActionHint.AutumnAction, ct);
            AuditInventory("end Autumn");
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
            await RunSeasonTurnsAsync(ActionHint.WinterAction, ct);

            Log("Winter — Step 3: Enforce Card Limits");
            await RunWinterDiscardAsync(ct);

            Log("Winter — Step 4: Transit Age");
            await RunAgeClosingCeremonyAsync(ct);
            AuditInventory("end Winter");
        }

        private async Task RunAgeOpenCeremonyAsync(CancellationToken ct)
        {
            int keeperId = FindAgekeeperId();
            Log("Spring — Step 1: Cosmic Age");

            if (!IsHumanPlayer(keeperId))
                Apply(new RollCosmicAgeCommand(keeperId));

            if (_ceremonyGate != null && HasAnyHumanPlayer())
            {
                var result = await _ceremonyGate.WaitAsync(CeremonyStep.RoundOpen, ct);
                if (result.Command is RollCosmicAgeCommand roll)
                    Apply(roll);
                else if (_session.Board.CosmicAgeSign == ZodiacSign.None)
                    Apply(new RollCosmicAgeCommand(keeperId));
            }
            else if (_session.Board.CosmicAgeSign == ZodiacSign.None)
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

        public bool HasAnyHumanPlayer()
        {
            foreach (var c in _controllers)
            {
                if (c is HotSeatController)
                    return true;
            }
            return false;
        }

        public bool IsHumanPlayer(int playerId) =>
            playerId >= 0 && playerId < _controllers.Count && _controllers[playerId] is HotSeatController;

        private async Task RunWinterDiscardAsync(CancellationToken ct)
        {
            _session.Rules?.Winter?.ClearFateCardsFromArcanum(_session);

            foreach (var player in _session.Players)
            {
                bool overSpread = player.Spread.Count > PlayerLimitService.GetSpreadLimit(_session, player);
                bool overHand   = player.Hand.Count > PlayerLimitService.GetHandLimit(_session, player);
                if (!overSpread && !overHand) continue;

                var cmd = await RequestAsync(player.PlayerId, ActionHint.DiscardToLimit, ct);
                Apply(cmd);
            }
        }

        // ─── Turn-based season actions ────────────────────────────────────────────

        /// <summary>
        /// Single clockwise lap starting from the Agekeeper. Each player takes a proactive turn
        /// until they End Turn (Pass).
        /// </summary>
        private async Task RunSeasonTurnsAsync(ActionHint hint, CancellationToken ct)
        {
            int playerCount = _session.Players.Count;
            int startIdx    = FindAgekeeperIndex();

            for (int i = 0; i < playerCount; i++)
            {
                if (_session.IsOver || ct.IsCancellationRequested) return;

                int idx      = (startIdx + i) % playerCount;
                int playerId = _session.Players[idx].PlayerId;
                await RunPlayerTurnAsync(playerId, hint, ct);
            }
        }

        /// <summary>
        /// One player's proactive turn: prompt repeatedly until they End Turn or the game ends.
        /// </summary>
        private async Task RunPlayerTurnAsync(int playerId, ActionHint hint, CancellationToken ct)
        {
            TurnPlayerId = playerId;
            _session.CurrentTurnPlayerId = playerId;
            Log($"Turn — P{playerId} ({hint})");

            try
            {
                while (!_session.IsOver && !ct.IsCancellationRequested)
                {
                    var cmd    = await RequestAsync(playerId, hint, ct);
                    var result = Apply(cmd);

                    if (IsEndTurnCommand(cmd))
                        break;

                    if (result.IsOk)
                        await ResolvePendingContestAsync(ct);
                }
            }
            finally
            {
                TurnPlayerId = -1;
                _session.CurrentTurnPlayerId = null;
            }
        }

        private async Task ResolvePendingContestAsync(CancellationToken ct)
        {
            var pending = _session.Board.PendingContest;
            if (pending == null || ct.IsCancellationRequested) return;

            var responseHint = pending.Kind switch
            {
                ContestKind.Trade      => ActionHint.TradeResponse,
                ContestKind.Duel       => ActionHint.DuelResponse,
                ContestKind.Gambit     => ActionHint.GambitResponse,
                ContestKind.Opposition => ActionHint.OppositionResponse,
                _                      => ActionHint.None
            };

            if (responseHint == ActionHint.None) return;

            Log($"Contest — P{pending.DefenderId} must respond to {pending.Kind} from P{pending.AttackerId}");
            var responseCmd = await RequestAsync(pending.DefenderId, responseHint, ct);
            var result = Apply(responseCmd);
            if (result.IsOk && WaitForPendingExchangesAsync != null)
                await WaitForPendingExchangesAsync(ct);
        }

        static bool IsEndTurnCommand(IGameCommand cmd) =>
            cmd is PassActionCommand or PassCrucibleActionCommand;

        // ─── Controller dispatch ──────────────────────────────────────────────────

        private async Task<IGameCommand> RequestAsync(int playerId, ActionHint hint,
            CancellationToken ct, string? pendingCardId = null)
        {
            ActivePlayerId = playerId;
            PendingCardId  = pendingCardId;
            var controller = _controllers[playerId];

            var pubView  = GamePublicView.From(_session);
            var privView = PlayerPrivateView.From(_session, playerId);

            IReadOnlyList<string>? moonDrawnIds = hint == ActionHint.FateMoonDecision
                ? _session.Board.FateMoonDrawnCardIds
                : null;

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
                _session.Rules?.AlchemicalValidator,
                _session);

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

        public CommandResult ApplySideEffect(IGameCommand cmd) => Apply(cmd);

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
                    _session.GetCard(id)?.MoveTo(CardZone.Hand, playerId);
                    _session.Players[playerId].Hand.Add(id);
                    minorPool?.Add(id);
                }
            }
        }

        private void Log(string msg) => OnLog?.Invoke(msg);

        private void AuditInventory(string context)
        {
            var result = SessionInventoryAudit.Audit(_session, context);
            if (!result.IsConsistent)
            {
                foreach (var v in result.Violations)
                    Log($"[InventoryAudit/{context}] {v}");
            }
        }
    }

    /// <summary>Hints passed to a controller indicating what kind of action is expected.</summary>
    public enum ActionHint
    {
        None,
        RollZodiac,
        AcknowledgeSign,
        ConfirmHarvest,
        /// <summary>Mandatory commune on Spring Hub after all players harvest.</summary>
        Commune,
        /// <summary>Spring Hub turn: Commune, Build a House, or End Turn.</summary>
        SpringAction,
        /// <summary>Summer turn: Trade, Duel, Gambit, Opposition, or End Turn.</summary>
        SummerAction,
        /// <summary>Autumn turn: Craft, Activate, Fire, Temper, Leave Stasis, or End Turn.</summary>
        AutumnAction,
        /// <summary>Defender responds to a pending trade offer.</summary>
        TradeResponse,
        /// <summary>Defender responds to a pending duel.</summary>
        DuelResponse,
        /// <summary>Defender responds to a pending gambit.</summary>
        GambitResponse,
        /// <summary>Defender responds to a pending opposition.</summary>
        OppositionResponse,
        /// <summary>Future: player responds during Spring hub.</summary>
        SpringHubResponse,
        /// <summary>Legacy alias kept for UI routing compatibility.</summary>
        SummerContestResponse,
        /// <summary>Future: defender responds to an Autumn forge action.</summary>
        AutumnForgeResponse,
        AdeptDecision,
        FateMoonDecision,
        FateReagentChoice,
        FateLoversChoice,
        FateLoversTargetPick,
        PriestessHarvestReturn,
        /// <summary>Winter Activities turn: move cards, craft, wager, or End Turn.</summary>
        WinterAction,
        DiscardToLimit,
    }
}
