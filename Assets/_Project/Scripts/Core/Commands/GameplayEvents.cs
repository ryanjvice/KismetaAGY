using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Commands
{
    public sealed class GameSetupCompleteEvent : IGameEvent
    {
        public int PlayerCount { get; }
        public GameSetupCompleteEvent(int playerCount) => PlayerCount = playerCount;
    }

    public sealed class CosmicAgeSetEvent : IGameEvent
    {
        public ZodiacSign Sign    { get; }
        public Planet     Planet  { get; }
        public Element    Element { get; }
        public CosmicAgeSetEvent(ZodiacSign sign, Planet planet, Element element)
        {
            Sign = sign; Planet = planet; Element = element;
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
        public int    PlayerId        { get; }
        public int    SlotIndex       { get; }
        public string CardInstanceId  { get; }
        public CrucibleActivatedEvent(int playerId, int slotIndex, string cardInstanceId)
        {
            PlayerId = playerId; SlotIndex = slotIndex; CardInstanceId = cardInstanceId;
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
        public int           PlayerId     { get; }
        public int           SlotIndex    { get; }
        public StonePosition NewPosition  { get; }
        public StoneFiredEvent(int playerId, int slotIndex, StonePosition newPos)
        {
            PlayerId = playerId; SlotIndex = slotIndex; NewPosition = newPos;
        }
    }

    public sealed class StoneTemperedEvent : IGameEvent
    {
        public int           PlayerId    { get; }
        public StonePosition NewPosition { get; }
        public StoneTemperedEvent(int playerId, StonePosition newPos) { PlayerId = playerId; NewPosition = newPos; }
    }

    /// <summary>
    /// Dice-only opposition result. Both rolls stored for log/replay.
    /// The loser's stone enters Stasis.
    /// </summary>
    public sealed class OppositionResolvedEvent : IGameEvent
    {
        public int AttackerId  { get; }
        public int DefenderId  { get; }
        public int AttackRoll  { get; }
        public int DefendRoll  { get; }
        public int LoserId     { get; }
        public OppositionResolvedEvent(int attackerId, int defenderId, int attackRoll, int defendRoll, int loserId)
        {
            AttackerId = attackerId; DefenderId = defenderId;
            AttackRoll = attackRoll; DefendRoll = defendRoll;
            LoserId    = loserId;
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

    public sealed class AdeptPurchasedEvent : IGameEvent
    {
        public int    PlayerId    { get; }
        public string AdeptCardId { get; }
        public AdeptPurchasedEvent(int playerId, string adeptCardId)
        {
            PlayerId = playerId; AdeptCardId = adeptCardId;
        }
    }

    public sealed class AdeptDeclinedEvent : IGameEvent
    {
        public int    PlayerId    { get; }
        public string AdeptCardId { get; }
        public AdeptDeclinedEvent(int playerId, string adeptCardId)
        {
            PlayerId = playerId; AdeptCardId = adeptCardId;
        }
    }

    public sealed class AstralHouseBuiltEvent : IGameEvent
    {
        public int        PlayerId { get; }
        public ZodiacSign Sign     { get; }
        public AstralHouseBuiltEvent(int playerId, ZodiacSign sign)
        {
            PlayerId = playerId; Sign = sign;
        }
    }

    public sealed class FateResolvedEvent : IGameEvent
    {
        public int    PlayerId   { get; }
        public string FateCardId { get; }
        public int    ArcanaNum  { get; }
        public FateResolvedEvent(int playerId, string fateCardId, int arcanaNum)
        {
            PlayerId = playerId; FateCardId = fateCardId; ArcanaNum = arcanaNum;
        }
    }

    public sealed class CosmicEffectAppliedEvent : IGameEvent
    {
        public ZodiacSign Sign { get; }
        public string     EffectSummary { get; }
        public CosmicEffectAppliedEvent(ZodiacSign sign, string effectSummary)
        {
            Sign = sign; EffectSummary = effectSummary;
        }
    }
}
