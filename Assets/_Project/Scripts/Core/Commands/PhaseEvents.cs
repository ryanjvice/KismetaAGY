using Kismeta.Core.Domain;

namespace Kismeta.Core.Commands
{
    public sealed class PhaseChangedEvent : IGameEvent
    {
        public Season Season { get; }
        public int StepIndex { get; }
        public PhaseChangedEvent(Season season, int stepIndex) { Season = season; StepIndex = stepIndex; }
    }

    public sealed class CardsDrawnEvent : IGameEvent
    {
        public int PlayerId { get; }
        public int Count { get; }
        public CardsDrawnEvent(int playerId, int count) { PlayerId = playerId; Count = count; }
    }

    public sealed class StoneMovedEvent : IGameEvent
    {
        public int PlayerId { get; }
        public int NewPosition { get; }
        public StoneMovedEvent(int playerId, int newPosition) { PlayerId = playerId; NewPosition = newPosition; }
    }

    public sealed class GameEndedEvent : IGameEvent
    {
        public int WinnerPlayerId { get; }
        public GameEndedEvent(int winnerPlayerId) => WinnerPlayerId = winnerPlayerId;
    }
}
