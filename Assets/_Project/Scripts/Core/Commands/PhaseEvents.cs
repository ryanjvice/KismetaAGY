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

    /// <summary>Where a single harvest draw was routed (Hand, Arcanum, etc.).</summary>
    public enum HarvestRouteTarget
    {
        Hand,
        Arcanum,
        AdeptLimbo,
        DiscardedDuplicate
    }

    /// <summary>Emitted after each card is routed during a stepped harvest deal.</summary>
    public sealed class HarvestCardRoutedEvent : IGameEvent
    {
        public int PlayerId { get; }
        public string CardId { get; }
        public HarvestRouteTarget Target { get; }
        /// <summary>1-based index of this draw within the harvest.</summary>
        public int Index { get; }
        /// <summary>Draws remaining after this card (0 when harvest deal is complete).</summary>
        public int Remaining { get; }

        public HarvestCardRoutedEvent(int playerId, string cardId, HarvestRouteTarget target, int index, int remaining)
        {
            PlayerId = playerId;
            CardId = cardId;
            Target = target;
            Index = index;
            Remaining = remaining;
        }
    }

    /// <summary>Emitted when an inline fate (e.g. Death) clears all player hands mid-harvest.</summary>
    public sealed class HarvestHandsClearedEvent : IGameEvent
    {
        public int DrawerId { get; }
        public HarvestHandsClearedEvent(int drawerId) => DrawerId = drawerId;
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
