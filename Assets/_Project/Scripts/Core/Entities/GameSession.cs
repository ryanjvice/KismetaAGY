using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Phases;
using Kismeta.Core.Rules;

namespace Kismeta.Core.Entities
{
    /// <summary>
    /// Root aggregate for a single game of Kismeta. Holds all player states, the board,
    /// the phase controller, and the card-instance registry. External code mutates the
    /// session only via <see cref="Apply"/>.
    ///
    /// Pass a <see cref="GameRuleSet"/> to enable full rule evaluation.
    /// Without rules the session can still track state and phase transitions,
    /// which is useful for unit-test fixtures.
    /// </summary>
    public sealed class GameSession
    {
        public string  SessionId { get; }
        public GameMode Mode     { get; }
        public CrucibleBuildMode CrucibleBuild { get; }
        /// <summary>Set before <see cref="SetupGameCommand"/>; defaults to player 0.</summary>
        public int FirstAgekeeperPlayerId { get; set; } = -1;
        public bool IsOver       { get; private set; }
        public int? WinnerPlayerId { get; private set; }
        public bool CardLockActive { get; private set; }

        /// <summary>Player whose proactive turn is active during season action phases. Set by GameLoop.</summary>
        public int? CurrentTurnPlayerId { get; set; }

        private readonly List<PlayerState> _players;
        public ReadOnlyCollection<PlayerState> Players { get; }

        public BoardState     Board { get; }
        public PhaseController Phase { get; }

        private readonly Dictionary<string, CardInstance> _cards = new();
        public IReadOnlyDictionary<string, CardInstance> Cards => _cards;

        private readonly GameRuleSet? _rules;
        /// <summary>Exposes the rule set for GameLoop to call specialized service methods.</summary>
        public GameRuleSet? Rules => _rules;

        public event Action<IGameEvent>? OnEvent;

        public GameSession(string sessionId, GameMode mode,
            IReadOnlyList<PlayerState> players, GameRuleSet? rules = null,
            CrucibleBuildMode crucibleBuild = CrucibleBuildMode.Curated)
        {
            if (players.Count < 2 || players.Count > 4)
                throw new ArgumentOutOfRangeException(nameof(players), "Kismeta requires 2–4 players.");

            SessionId = sessionId;
            Mode      = mode;
            CrucibleBuild = crucibleBuild;
            _rules    = rules;

            _players = new List<PlayerState>(players);
            Players  = _players.AsReadOnly();
            Board    = new BoardState();
            Phase    = new PhaseController();
        }

        // ─── Card registry ────────────────────────────────────────────────────────

        public void RegisterCard(CardInstance card) => _cards[card.InstanceId] = card;

        public CardInstance? GetCard(string? instanceId) =>
            string.IsNullOrEmpty(instanceId) || !_cards.TryGetValue(instanceId, out var card)
                ? null
                : card;

        public CardDefinition? GetDefinition(string instanceId, Func<string, CardDefinition?> resolver) =>
            _cards.TryGetValue(instanceId, out var card) ? resolver(card.DefinitionId) : null;

        // ─── Event emission ───────────────────────────────────────────────────────

        /// <summary>Emit an event to all subscribers. Rule services call this after mutating state.</summary>
        public void EmitEvent(IGameEvent evt) => OnEvent?.Invoke(evt);

        // ─── Command dispatch ─────────────────────────────────────────────────────

        public CommandResult Apply(IGameCommand command)
        {
            // Pre-check: season/agekeeper gating
            if (_rules?.Validator != null)
            {
                var validation = _rules.Validator.Validate(this, command);
                if (!validation.IsOk) return validation;
            }

            return command switch
            {
                // ── Phase + lock (always available without rules) ─────────────────
                AdvancePhaseCommand  _   => HandleAdvancePhase(),
                SetCardLockCommand   cmd => HandleSetCardLock(cmd),
                RegisterCardCommand  cmd => HandleRegisterCard(cmd),

                // ── Setup ─────────────────────────────────────────────────────────
                SetupGameCommand     _   => _rules is not null
                    ? ApplyWithAudit(_rules.Setup.Setup(this), command)
                    : CommandResult.NotImplemented(nameof(SetupGameCommand)),

                // ── Spring ────────────────────────────────────────────────────────
                RollCosmicAgeCommand cmd => _rules is not null
                    ? ApplyWithAudit(RunVoid(() =>
                    {
                        _rules.Harvest.RollCosmicAge(this);
                        _rules.Winter.ResolveWagers(this, Board.CosmicAgeSign);
                    }), command)
                    : CommandResult.NotImplemented(nameof(RollCosmicAgeCommand)),

                RollZodiacCommand    cmd => _rules is not null
                    ? ApplyWithAudit(RunVoid(() => _rules.Harvest.RollZodiac(this, cmd.PlayerId)), command)
                    : CommandResult.NotImplemented(nameof(RollZodiacCommand)),

                HarvestCommand       cmd => _rules is not null
                    ? ApplyWithAudit(RunVoid(() => _rules.Harvest.ExecuteHarvest(this, cmd.PlayerId)), command)
                    : CommandResult.NotImplemented(nameof(HarvestCommand)),

                CommuneCommand       cmd => _rules is not null
                    ? ApplyWithAudit(_rules.Harvest.HandleCommune(this, cmd.PlayerId, cmd.SpreadCardIds, cmd.HandCardIds), command)
                    : CommandResult.NotImplemented(nameof(CommuneCommand)),

                BuyAdeptCommand      cmd => _rules is not null
                    ? ApplyWithAudit(_rules.Harvest.HandleBuyAdept(this, cmd.PlayerId, cmd.AdeptCardId,
                        cmd.PaymentCardIds, cmd.SwapOutAdeptId), command)
                    : CommandResult.NotImplemented(nameof(BuyAdeptCommand)),

                DeclineAdeptCommand  cmd => _rules is not null
                    ? ApplyWithAudit(_rules.Harvest.HandleDeclineAdept(this, cmd.PlayerId, cmd.AdeptCardId), command)
                    : CommandResult.NotImplemented(nameof(DeclineAdeptCommand)),

                // ── Spring extensions ─────────────────────────────────────────────
                BuildAstralHouseCommand  cmd => _rules?.AstralHouse is not null
                    ? ApplyWithAudit(_rules.AstralHouse.TryBuild(this, cmd.PlayerId, cmd.Sign, cmd.PaymentCardIds), command)
                    : CommandResult.NotImplemented(nameof(BuildAstralHouseCommand)),

                // ── Summer ────────────────────────────────────────────────────────
                ActivateCrucibleCommand cmd => _rules is not null
                    ? ApplyWithAudit(_rules.Crucible.TryActivate(this, cmd.PlayerId, cmd.SlotIndex, cmd.CardInstanceIds), command)
                    : CommandResult.NotImplemented(nameof(ActivateCrucibleCommand)),

                CraftReagentCommand  cmd => _rules is not null
                    ? ApplyWithAudit(_rules.Crafting.TryCraft(this, cmd.PlayerId, cmd.ReagentType, cmd.CardInstanceIds), command)
                    : CommandResult.NotImplemented(nameof(CraftReagentCommand)),

                CraftKingReagentCommand cmd => _rules is not null
                    ? ApplyWithAudit(_rules.Crafting.TryKingDiscardCraft(this, cmd.PlayerId, cmd.KingCardId), command)
                    : CommandResult.NotImplemented(nameof(CraftKingReagentCommand)),

                MarkEmpressReagentCommand cmd => _rules is not null
                    ? ApplyWithAudit(_rules.Crafting.TryMarkEmpressReagent(this, cmd.PlayerId, cmd.ReagentType), command)
                    : CommandResult.NotImplemented(nameof(MarkEmpressReagentCommand)),

                MarkTemperanceWildReagentCommand cmd => _rules is not null
                    ? ApplyWithAudit(_rules.Crafting.TryMarkTemperanceWildReagent(this, cmd.PlayerId, cmd.ReagentType), command)
                    : CommandResult.NotImplemented(nameof(MarkTemperanceWildReagentCommand)),

                // ── Autumn ────────────────────────────────────────────────────────
                FireStoneCommand     cmd => _rules is not null
                    ? ApplyWithAudit(_rules.Crucible.TryFire(this, cmd.PlayerId, cmd.SlotIndex, cmd.AlignmentCardIds), command)
                    : CommandResult.NotImplemented(nameof(FireStoneCommand)),

                TemperCommand        cmd => _rules is not null
                    ? ApplyWithAudit(_rules.Crucible.TryTemper(this, cmd.PlayerId), command)
                    : CommandResult.NotImplemented(nameof(TemperCommand)),

                InitiateOppositionCommand cmd => _rules is not null
                    ? ApplyWithAudit(_rules.Crucible.TryInitiateOppose(this, cmd.AttackerId, cmd.DefenderId), command)
                    : CommandResult.NotImplemented(nameof(InitiateOppositionCommand)),

                LeaveStasisCommand       cmd => _rules is not null
                    ? ApplyWithAudit(_rules.Crucible.TryLeaveStasis(this, cmd.PlayerId), command)
                    : CommandResult.NotImplemented(nameof(LeaveStasisCommand)),

                // ── Winter ────────────────────────────────────────────────────────
                WinterMoveCardCommand    cmd => _rules is not null
                    ? ApplyWithAudit(_rules.Winter.TryMoveCard(this, cmd.PlayerId, cmd.CardId, cmd.ToSpread), command)
                    : CommandResult.NotImplemented(nameof(WinterMoveCardCommand)),

                PlaceFatefulWagerCommand cmd => _rules is not null
                    ? ApplyWithAudit(_rules.Winter.TryPlaceWager(this, cmd.PlayerId, cmd.PredictedSign, cmd.CardIds), command)
                    : CommandResult.NotImplemented(nameof(PlaceFatefulWagerCommand)),

                DiscardToLimitCommand    cmd => _rules is not null
                    ? ApplyWithAudit(_rules.Winter.TryDiscardToLimit(this, cmd.PlayerId, cmd.DiscardSpreadIds, cmd.DiscardHandIds), command)
                    : CommandResult.NotImplemented(nameof(DiscardToLimitCommand)),

                // EnforceCardLimitsCommand was superseded by the per-player DiscardToLimit flow;
                // kept here to avoid hard failures if stale code submits it.
                EnforceCardLimitsCommand _ =>
                    CommandResult.Invalid("EnforceCardLimitsCommand is deprecated — use DiscardToLimitCommand instead."),

                TransitAgeCommand    _   => _rules is not null
                    ? ApplyWithAudit(RunVoid(() => _rules.Winter.Transit(this)), command)
                    : CommandResult.NotImplemented(nameof(TransitAgeCommand)),

                // ── Fate async decisions ──────────────────────────────────────────
                FateMoonDecisionCommand   cmd => _rules?.FateResolver is not null
                    ? ApplyWithAudit(_rules.FateResolver.HandleMoonDecision(this, cmd.PlayerId, cmd.KeepCardIds), command)
                    : CommandResult.NotImplemented(nameof(FateMoonDecisionCommand)),

                FateReagentChoiceCommand  cmd => _rules?.FateResolver is not null
                    ? ApplyWithAudit(_rules.FateResolver.HandleFoolReagentChoice(this, cmd.PlayerId, cmd.ReagentType), command)
                    : CommandResult.NotImplemented(nameof(FateReagentChoiceCommand)),

                FateLoversChoiceCommand   cmd => _rules?.FateResolver is not null
                    ? ApplyWithAudit(_rules.FateResolver.HandleLoversChoice(this, cmd.PlayerId, cmd.DrawCards,
                        cmd.ChosenReagent, cmd.ChooserId), command)
                    : CommandResult.NotImplemented(nameof(FateLoversChoiceCommand)),

                // ── Ward placement ────────────────────────────────────────────────
                PlaceCardWardCommand  cmd => _rules is not null
                    ? ApplyWithAudit(_rules.Crucible.TryPlaceCardWard(this, cmd.PlayerId, cmd.SlotIndex, cmd.ReagentType), command)
                    : CommandResult.NotImplemented(nameof(PlaceCardWardCommand)),

                PlaceStoneWardCommand cmd => _rules is not null
                    ? ApplyWithAudit(_rules.Crucible.TryPlaceStoneWard(this, cmd.PlayerId, cmd.ReagentType), command)
                    : CommandResult.NotImplemented(nameof(PlaceStoneWardCommand)),

                PlaceAdeptWardCommand cmd => _rules is not null
                    ? ApplyWithAudit(_rules.Crucible.TryPlaceAdeptWard(this, cmd.PlayerId, cmd.AdeptCardId, cmd.ReagentType), command)
                    : CommandResult.NotImplemented(nameof(PlaceAdeptWardCommand)),

                RefreshAdeptCommand   cmd => _rules is not null
                    ? ApplyWithAudit(_rules.Crucible.TryRefreshAdept(this, cmd.PlayerId, cmd.AdeptCardId), command)
                    : CommandResult.NotImplemented(nameof(RefreshAdeptCommand)),

                DirectTradeCommand    cmd => _rules?.Trade is not null
                    ? ApplyWithAudit(_rules.Trade.TryInitiateTrade(this, cmd.PlayerId, cmd.TargetId,
                        cmd.OfferCardIds, cmd.RequestCardIds), command)
                    : CommandResult.NotImplemented(nameof(DirectTradeCommand)),

                RespondTradeCommand   cmd => _rules?.Trade is not null
                    ? ApplyWithAudit(_rules.Trade.TryRespondTrade(this, cmd.PlayerId, cmd.Accept), command)
                    : CommandResult.NotImplemented(nameof(RespondTradeCommand)),

                // ── Combat ────────────────────────────────────────────────────────
                InitiateDuelCommand   cmd => _rules?.Combat is not null
                    ? ApplyWithAudit(_rules.Combat.TryInitiateDuel(this, cmd.AttackerId, cmd.DefenderId, cmd.TargetCardId, cmd.AnteCardId, cmd.SecondAnteCardId), command)
                    : CommandResult.NotImplemented(nameof(InitiateDuelCommand)),

                InitiateGambitCommand cmd => _rules?.Combat is not null
                    ? ApplyWithAudit(_rules.Combat.TryInitiateGambit(this, cmd.AttackerId, cmd.DefenderId, cmd.OfferedCardId), command)
                    : CommandResult.NotImplemented(nameof(InitiateGambitCommand)),

                RespondDuelCommand    cmd => _rules?.Combat is not null
                    ? ApplyWithAudit(_rules.Combat.TryRespondDuel(this, cmd.PlayerId, cmd.Accept, cmd.ChosenAnteCardId), command)
                    : CommandResult.NotImplemented(nameof(RespondDuelCommand)),

                RespondGambitCommand  cmd => _rules?.Combat is not null
                    ? ApplyWithAudit(_rules.Combat.TryRespondGambit(this, cmd.PlayerId, cmd.Accept, cmd.ReagentPayments), command)
                    : CommandResult.NotImplemented(nameof(RespondGambitCommand)),

                RespondOppositionCommand cmd => _rules is not null
                    ? ApplyWithAudit(_rules.Crucible.TryRespondOpposition(this, cmd.PlayerId, cmd.Accept), command)
                    : CommandResult.NotImplemented(nameof(RespondOppositionCommand)),

                FreeArrestedCommand   cmd => _rules?.Combat is not null
                    ? ApplyWithAudit(_rules.Combat.TryFreeArrested(this, cmd.PlayerId, cmd.SlotIndex), command)
                    : CommandResult.NotImplemented(nameof(FreeArrestedCommand)),

                // ── Adept passives ────────────────────────────────────────────────
                MagicianSwapCommand cmd => _rules?.Adept is not null
                    ? ApplyWithAudit(_rules.Adept.TryMagicianSwap(this, cmd.PlayerId, cmd.CardId, cmd.ToSpread), command)
                    : CommandResult.NotImplemented(nameof(MagicianSwapCommand)),

                ProtectSpreadCardsCommand cmd => _rules?.Adept is not null
                    ? ApplyWithAudit(_rules.Adept.TryProtectSpreadCards(this, cmd.PlayerId, cmd.CardIds), command)
                    : CommandResult.NotImplemented(nameof(ProtectSpreadCardsCommand)),

                ShiftZodiacCommand cmd => _rules?.Adept is not null
                    ? ApplyWithAudit(_rules.Adept.TryShiftZodiac(this, cmd.PlayerId, cmd.Delta), command)
                    : CommandResult.NotImplemented(nameof(ShiftZodiacCommand)),

                ShiftOppositionZodiacCommand cmd => _rules?.Adept is not null
                    ? ApplyWithAudit(_rules.Adept.TryShiftOppositionZodiac(this, cmd.PlayerId, cmd.Delta), command)
                    : CommandResult.NotImplemented(nameof(ShiftOppositionZodiacCommand)),

                DevilStealCommand cmd => _rules?.Adept is not null
                    ? ApplyWithAudit(_rules.Adept.TryDevilSteal(this, cmd.PlayerId, cmd.SacrificeCardId,
                        cmd.TargetPlayerId, cmd.StolenCardId), command)
                    : CommandResult.NotImplemented(nameof(DevilStealCommand)),

                DevilBanishAdeptCommand cmd => _rules?.Adept is not null
                    ? ApplyWithAudit(_rules.Adept.TryDevilBanishAdept(this, cmd.PlayerId,
                        cmd.TargetPlayerId, cmd.TargetAdeptCardId), command)
                    : CommandResult.NotImplemented(nameof(DevilBanishAdeptCommand)),

                StarNullifyCommand cmd => _rules?.Adept is not null
                    ? ApplyWithAudit(_rules.Adept.TryStarNullify(this, cmd.PlayerId,
                        cmd.TargetPlayerId, cmd.TargetCardId), command)
                    : CommandResult.NotImplemented(nameof(StarNullifyCommand)),

                RefreshStarNullifyCommand cmd => _rules?.Adept is not null
                    ? ApplyWithAudit(_rules.Adept.TryRefreshStarNullify(this, cmd.PlayerId, cmd.TargetCardId), command)
                    : CommandResult.NotImplemented(nameof(RefreshStarNullifyCommand)),

                ActivateWorldWildcardCommand cmd => _rules?.Adept is not null
                    ? ApplyWithAudit(_rules.Adept.TryActivateWorldWildcard(this, cmd.PlayerId), command)
                    : CommandResult.NotImplemented(nameof(ActivateWorldWildcardCommand)),

                RefreshWorldWildcardCommand cmd => _rules?.Adept is not null
                    ? ApplyWithAudit(_rules.Adept.TryRefreshWorldWildcard(this, cmd.PlayerId), command)
                    : CommandResult.NotImplemented(nameof(RefreshWorldWildcardCommand)),

                CompletePriestessHarvestCommand cmd => _rules?.Adept is not null
                    ? ApplyWithAudit(_rules.Adept.TryCompletePriestessHarvest(this, cmd.PlayerId, cmd.ReturnCardIds), command)
                    : CommandResult.NotImplemented(nameof(CompletePriestessHarvestCommand)),

                // ── Pass actions ──────────────────────────────────────────────────
                PassActionCommand        _ => ApplyWithAudit(CommandResult.Ok("Action passed."), command),
                PassCrucibleActionCommand _ => ApplyWithAudit(CommandResult.Ok("Crucible action passed."), command),

                _                          => CommandResult.NotImplemented(command.GetType().Name)
            };
        }

        private CommandResult ApplyWithAudit(CommandResult result, IGameCommand command)
        {
            if (result.IsOk)
                SessionInventoryAudit.RunIfEnabled(this, command.GetType().Name);
            return result;
        }

        // ─── Private handlers ─────────────────────────────────────────────────────

        private CommandResult HandleAdvancePhase()
        {
            Phase.Advance();
            EmitEvent(new PhaseChangedEvent(Phase.CurrentSeason, Phase.CurrentStepIndex));
            return CommandResult.Ok();
        }

        private CommandResult HandleSetCardLock(SetCardLockCommand cmd)
        {
            CardLockActive = cmd.Lock;
            EmitEvent(new CardLockChangedEvent(cmd.Lock));
            return CommandResult.Ok();
        }

        private CommandResult HandleRegisterCard(RegisterCardCommand cmd)
        {
            RegisterCard(cmd.Card);
            return CommandResult.Ok();
        }

        public void SetWinner(int playerId)
        {
            WinnerPlayerId = playerId;
            IsOver         = true;
            EmitEvent(new GameEndedEvent(playerId));
        }

        /// <summary>Adapts void rule-service calls into CommandResult.Ok().</summary>
        private static CommandResult RunVoid(Action action)
        {
            action();
            return CommandResult.Ok();
        }
    }
}
