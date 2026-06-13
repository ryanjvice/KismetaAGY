using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Phases;

namespace Kismeta.Core.Entities
{
    /// <summary>
    /// Root aggregate for a single game of Kismeta. Holds all player states, the board,
    /// the phase controller, and the card-instance registry. External code mutates the
    /// session only via <see cref="Apply"/>.
    /// </summary>
    public sealed class GameSession
    {
        public string SessionId { get; }
        public GameMode Mode { get; }
        public bool IsOver { get; private set; }
        public int? WinnerPlayerId { get; private set; }

        // Card lock applies from end of Spring step 5 through end of Autumn.
        public bool CardLockActive { get; private set; }

        private readonly List<PlayerState> _players;
        public ReadOnlyCollection<PlayerState> Players { get; }

        public BoardState Board { get; }
        public PhaseController Phase { get; }

        // Canonical registry of all CardInstances in this game.
        private readonly Dictionary<string, CardInstance> _cards = new();
        public IReadOnlyDictionary<string, CardInstance> Cards => _cards;

        // Event subscribers
        public event Action<IGameEvent>? OnEvent;

        public GameSession(string sessionId, GameMode mode, IReadOnlyList<PlayerState> players)
        {
            if (players.Count < 2 || players.Count > 4)
                throw new ArgumentOutOfRangeException(nameof(players), "Kismeta requires 2–4 players.");

            SessionId = sessionId;
            Mode = mode;
            _players = new List<PlayerState>(players);
            Players = _players.AsReadOnly();
            Board = new BoardState();
            Phase = new PhaseController();
        }

        public void RegisterCard(CardInstance card) => _cards[card.InstanceId] = card;

        public CardInstance? GetCard(string instanceId) =>
            _cards.TryGetValue(instanceId, out var card) ? card : null;

        public CardDefinition? GetDefinition(string instanceId, Func<string, CardDefinition?> resolver) =>
            _cards.TryGetValue(instanceId, out var card) ? resolver(card.DefinitionId) : null;

        /// <summary>
        /// Applies a command, returning the result. The session is the only place state
        /// is mutated — no external code writes directly to PlayerState or BoardState.
        /// </summary>
        public CommandResult Apply(IGameCommand command)
        {
            return command switch
            {
                AdvancePhaseCommand cmd    => HandleAdvancePhase(cmd),
                SetCardLockCommand cmd     => HandleSetCardLock(cmd),
                RegisterCardCommand cmd    => HandleRegisterCard(cmd),
                _                          => CommandResult.NotImplemented(command.GetType().Name)
            };
        }

        private CommandResult HandleAdvancePhase(AdvancePhaseCommand _)
        {
            Phase.Advance();
            Emit(new PhaseChangedEvent(Phase.CurrentSeason, Phase.CurrentStepIndex));
            return CommandResult.Ok();
        }

        private CommandResult HandleSetCardLock(SetCardLockCommand cmd)
        {
            CardLockActive = cmd.Lock;
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
            IsOver = true;
            Emit(new GameEndedEvent(playerId));
        }

        private void Emit(IGameEvent evt) => OnEvent?.Invoke(evt);
    }
}
