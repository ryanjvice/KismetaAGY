using Kismeta.Core.Domain;

namespace Kismeta.Core.Commands
{
    public sealed class GameSetupCompleteEvent : IGameEvent
    {
        public int PlayerCount { get; }
        public GameSetupCompleteEvent(int playerCount) => PlayerCount = playerCount;
    }

    public sealed class CosmicAgeSetEvent : IGameEvent
    {
        public ZodiacSign Sign      { get; }
        public Planet     Planet    { get; }
        public Element    Element   { get; }
        public string     EffectText { get; }
        public CosmicAgeSetEvent(ZodiacSign sign, Planet planet, Element element, string effectText)
        {
            Sign = sign; Planet = planet; Element = element; EffectText = effectText;
        }
    }

    public sealed class ZodiacRolledEvent : IGameEvent
    {
        public int        PlayerId { get; }
        public ZodiacSign Sign     { get; }
        public ZodiacRolledEvent(int playerId, ZodiacSign sign) { PlayerId = playerId; Sign = sign; }
    }

    public sealed class CardLockChangedEvent : IGameEvent
    {
        public bool IsLocked { get; }
        public CardLockChangedEvent(bool locked) => IsLocked = locked;
    }

    public sealed class CrucibleActivatedEvent : IGameEvent
    {
        public int PlayerId  { get; }
        public int SlotIndex { get; }
        public CrucibleActivatedEvent(int playerId, int slotIndex)
        {
            PlayerId = playerId; SlotIndex = slotIndex;
        }
    }

    public sealed class ReagentCraftedEvent : IGameEvent
    {
        public int        PlayerId    { get; }
        public ReagentType ReagentType { get; }
        public ReagentCraftedEvent(int playerId, ReagentType type) { PlayerId = playerId; ReagentType = type; }
    }

    public sealed class StoneFiredEvent : IGameEvent
    {
        public int PlayerId    { get; }
        public int NewPosition { get; }
        public StoneFiredEvent(int playerId, int newPosition) { PlayerId = playerId; NewPosition = newPosition; }
    }

    public sealed class StoneTemperedEvent : IGameEvent
    {
        public int PlayerId    { get; }
        public int NewPosition { get; }
        public StoneTemperedEvent(int playerId, int newPosition) { PlayerId = playerId; NewPosition = newPosition; }
    }

    public sealed class OppositionResolvedEvent : IGameEvent
    {
        public int  AttackerId            { get; }
        public int  DefenderId            { get; }
        public int  WinnerId              { get; }
        public bool DefenderSentToStasis  { get; }
        public OppositionResolvedEvent(int attackerId, int defenderId, int winnerId, bool defenderSentToStasis)
        {
            AttackerId = attackerId; DefenderId = defenderId;
            WinnerId = winnerId; DefenderSentToStasis = defenderSentToStasis;
        }
    }

    public sealed class CardLimitsEnforcedEvent : IGameEvent
    {
        public int PlayerId       { get; }
        public int CardsDiscarded { get; }
        public CardLimitsEnforcedEvent(int playerId, int cardsDiscarded)
        {
            PlayerId = playerId; CardsDiscarded = cardsDiscarded;
        }
    }

    public sealed class AgeTransitedEvent : IGameEvent
    {
        public int NewRoundNumber { get; }
        public int NewAgekeeperId { get; }
        public AgeTransitedEvent(int newRound, int newAgekeeper)
        {
            NewRoundNumber = newRound; NewAgekeeperId = newAgekeeper;
        }
    }
}
