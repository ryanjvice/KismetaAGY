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
            IReadOnlyList<PlayerState> players, GameRuleSet? rules = null)
        {
            if (players.Count < 2 || players.Count > 4)
                throw new ArgumentOutOfRangeException(nameof(players), "Kismeta requires 2–4 players.");

            SessionId = sessionId;
            Mode      = mode;
            _rules    = rules;

            _players = new List<PlayerState>(players);
            Players  = _players.AsReadOnly();
            Board    = new BoardState();
            Phase    = new PhaseController();
        }

        // ─── Card registry ────────────────────────────────────────────────────────

        public void RegisterCard(CardInstance card) => _cards[card.InstanceId] = card;

        public CardInstance? GetCard(string instanceId) =>
            _cards.TryGetValue(instanceId, out var card) ? card : null;

        public CardDefinition? GetDefinition(string instanceId, Func<string, CardDefinition?> resolver) =>
            _cards.TryGetValue(instanceId, out var card) ? resolver(card.DefinitionId) : null;

        // ─── Event emission ───────────────────────────────────────────────────────

        /// <summary>Emit an event to all subscribers. Rule services call this after mutating state.</summary>
        public void EmitEvent(IGameEvent evt) => OnEvent?.Invoke(evt);

        // ─── Command dispatch ─────────────────────────────────────────────────────

        public CommandResult Apply(IGameCommand command)
        {
            return command switch
            {
                // ── Phase + lock (always available without rules) ─────────────────
                AdvancePhaseCommand  _   => HandleAdvancePhase(),
                SetCardLockCommand   cmd => HandleSetCardLock(cmd),
                RegisterCardCommand  cmd => HandleRegisterCard(cmd),

                // ── Setup ─────────────────────────────────────────────────────────
                SetupGameCommand     _   => _rules is not null
                    ? _rules.Setup.Setup(this)
                    : CommandResult.NotImplemented(nameof(SetupGameCommand)),

                // ── Spring ────────────────────────────────────────────────────────
                RollCosmicAgeCommand cmd => _rules is not null
                    ? RunVoid(() => _rules.Harvest.RollCosmicAge(this))
                    : CommandResult.NotImplemented(nameof(RollCosmicAgeCommand)),

                RollZodiacCommand    cmd => _rules is not null
                    ? RunVoid(() => _rules.Harvest.RollZodiac(this, cmd.PlayerId))
                    : CommandResult.NotImplemented(nameof(RollZodiacCommand)),

                HarvestCommand       cmd => _rules is not null
                    ? RunVoid(() => _rules.Harvest.ExecuteHarvest(this, cmd.PlayerId))
                    : CommandResult.NotImplemented(nameof(HarvestCommand)),

                CommuneCommand       cmd => _rules is not null
                    ? _rules.Harvest.HandleCommune(this, cmd.PlayerId, cmd.SpreadCardIds, cmd.HandCardIds)
                    : CommandResult.NotImplemented(nameof(CommuneCommand)),

                BuyAdeptCommand      cmd => _rules is not null
                    ? _rules.Harvest.HandleBuyAdept(this, cmd.PlayerId, cmd.AdeptCardId,
                        cmd.PaymentCardIds, cmd.SwapOutAdeptId)
                    : CommandResult.NotImplemented(nameof(BuyAdeptCommand)),

                DeclineAdeptCommand  cmd => _rules is not null
                    ? _rules.Harvest.HandleDeclineAdept(this, cmd.PlayerId, cmd.AdeptCardId)
                    : CommandResult.NotImplemented(nameof(DeclineAdeptCommand)),

                // ── Spring extensions ─────────────────────────────────────────────
                BuildAstralHouseCommand  cmd => _rules?.AstralHouse is not null
                    ? _rules.AstralHouse.TryBuild(this, cmd.PlayerId, cmd.Sign, cmd.PaymentCardIds)
                    : CommandResult.NotImplemented(nameof(BuildAstralHouseCommand)),

                // ── Summer ────────────────────────────────────────────────────────
                ActivateCrucibleCommand cmd => _rules is not null
                    ? _rules.Crucible.TryActivate(this, cmd.PlayerId, cmd.SlotIndex, cmd.CardInstanceIds)
                    : CommandResult.NotImplemented(nameof(ActivateCrucibleCommand)),

                CraftReagentCommand  cmd => _rules is not null
                    ? _rules.Crafting.TryCraft(this, cmd.PlayerId, cmd.ReagentType, cmd.CardInstanceIds)
                    : CommandResult.NotImplemented(nameof(CraftReagentCommand)),

                // ── Autumn ────────────────────────────────────────────────────────
                FireStoneCommand     cmd => _rules is not null
                    ? _rules.Crucible.TryFire(this, cmd.PlayerId, cmd.SlotIndex, cmd.AlignmentCardIds)
                    : CommandResult.NotImplemented(nameof(FireStoneCommand)),

                TemperCommand        cmd => _rules is not null
                    ? _rules.Crucible.TryTemper(this, cmd.PlayerId)
                    : CommandResult.NotImplemented(nameof(TemperCommand)),

                InitiateOppositionCommand cmd => _rules is not null
                    ? _rules.Crucible.TryOppose(this, cmd.AttackerId, cmd.DefenderId)
                    : CommandResult.NotImplemented(nameof(InitiateOppositionCommand)),

                LeaveStasisCommand       cmd => _rules is not null
                    ? _rules.Crucible.TryLeaveStasis(this, cmd.PlayerId)
                    : CommandResult.NotImplemented(nameof(LeaveStasisCommand)),

                // ── Winter ────────────────────────────────────────────────────────
                EnforceCardLimitsCommand _ => _rules is not null
                    ? RunVoid(() => _rules.Winter.EnforceLimits(this))
                    : CommandResult.NotImplemented(nameof(EnforceCardLimitsCommand)),

                TransitAgeCommand    _   => _rules is not null
                    ? RunVoid(() => _rules.Winter.Transit(this))
                    : CommandResult.NotImplemented(nameof(TransitAgeCommand)),

                // ── Fate async decisions ──────────────────────────────────────────
                FateMoonDecisionCommand   cmd => _rules?.FateResolver is not null
                    ? _rules.FateResolver.HandleMoonDecision(this, cmd.PlayerId, cmd.KeepCardIds)
                    : CommandResult.NotImplemented(nameof(FateMoonDecisionCommand)),

                FateReagentChoiceCommand  cmd => _rules?.FateResolver is not null
                    ? _rules.FateResolver.HandleFoolReagentChoice(this, cmd.PlayerId, cmd.ReagentType)
                    : CommandResult.NotImplemented(nameof(FateReagentChoiceCommand)),

                FateLoversChoiceCommand   cmd => _rules?.FateResolver is not null
                    ? _rules.FateResolver.HandleLoversChoice(this, cmd.PlayerId, cmd.DrawCards, cmd.ChosenReagent)
                    : CommandResult.NotImplemented(nameof(FateLoversChoiceCommand)),

                // ── Ward placement ────────────────────────────────────────────────
                PlaceCardWardCommand  cmd => _rules is not null
                    ? _rules.Crucible.TryPlaceCardWard(this, cmd.PlayerId, cmd.SlotIndex, cmd.ReagentType)
                    : CommandResult.NotImplemented(nameof(PlaceCardWardCommand)),

                PlaceStoneWardCommand cmd => _rules is not null
                    ? _rules.Crucible.TryPlaceStoneWard(this, cmd.PlayerId, cmd.ReagentType)
                    : CommandResult.NotImplemented(nameof(PlaceStoneWardCommand)),

                // ── Combat ────────────────────────────────────────────────────────
                InitiateDuelCommand   cmd => _rules?.Combat is not null
                    ? _rules.Combat.TryDuel(this, cmd.AttackerId, cmd.DefenderId, cmd.AnteCardId)
                    : CommandResult.NotImplemented(nameof(InitiateDuelCommand)),

                InitiateGambitCommand cmd => _rules?.Combat is not null
                    ? _rules.Combat.TryGambit(this, cmd.AttackerId, cmd.DefenderId, cmd.OfferedCardId)
                    : CommandResult.NotImplemented(nameof(InitiateGambitCommand)),

                FreeArrestedCommand   cmd => _rules?.Combat is not null
                    ? _rules.Combat.TryFreeArrested(this, cmd.PlayerId, cmd.SlotIndex)
                    : CommandResult.NotImplemented(nameof(FreeArrestedCommand)),

                // ── Pass actions ──────────────────────────────────────────────────
                PassActionCommand        _ => CommandResult.Ok("Action passed."),
                PassCrucibleActionCommand _ => CommandResult.Ok("Crucible action passed."),

                _                          => CommandResult.NotImplemented(command.GetType().Name)
            };
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
