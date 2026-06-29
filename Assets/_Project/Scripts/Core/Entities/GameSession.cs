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

                // ── Autumn ────────────────────────────────────────────────────────
                FireStoneCommand     cmd => _rules is not null
                    ? ApplyWithAudit(_rules.Crucible.TryFire(this, cmd.PlayerId, cmd.SlotIndex, cmd.AlignmentCardIds), command)
                    : CommandResult.NotImplemented(nameof(FireStoneCommand)),

                TemperCommand        cmd => _rules is not null
                    ? ApplyWithAudit(_rules.Crucible.TryTemper(this, cmd.PlayerId), command)
                    : CommandResult.NotImplemented(nameof(TemperCommand)),

                InitiateOppositionCommand cmd => _rules is not null
                    ? ApplyWithAudit(_rules.Crucible.TryOppose(this, cmd.AttackerId, cmd.DefenderId), command)
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

                RefreshAdeptCommand   cmd => _rules is not null
                    ? ApplyWithAudit(_rules.Crucible.TryRefreshAdept(this, cmd.PlayerId, cmd.AdeptCardId), command)
                    : CommandResult.NotImplemented(nameof(RefreshAdeptCommand)),

                DirectTradeCommand    cmd => _rules?.Trade is not null
                    ? ApplyWithAudit(_rules.Trade.TryTrade(this, cmd.PlayerId, cmd.TargetId,
                        cmd.OfferCardIds, cmd.RequestCardIds), command)
                    : CommandResult.NotImplemented(nameof(DirectTradeCommand)),

                // ── Combat ────────────────────────────────────────────────────────
                InitiateDuelCommand   cmd => _rules?.Combat is not null
                    ? ApplyWithAudit(_rules.Combat.TryDuel(this, cmd.AttackerId, cmd.DefenderId, cmd.TargetCardId, cmd.AnteCardId), command)
                    : CommandResult.NotImplemented(nameof(InitiateDuelCommand)),

                InitiateGambitCommand cmd => _rules?.Combat is not null
                    ? ApplyWithAudit(_rules.Combat.TryGambit(this, cmd.AttackerId, cmd.DefenderId, cmd.OfferedCardId), command)
                    : CommandResult.NotImplemented(nameof(InitiateGambitCommand)),

                FreeArrestedCommand   cmd => _rules?.Combat is not null
                    ? ApplyWithAudit(_rules.Combat.TryFreeArrested(this, cmd.PlayerId, cmd.SlotIndex), command)
                    : CommandResult.NotImplemented(nameof(FreeArrestedCommand)),

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
