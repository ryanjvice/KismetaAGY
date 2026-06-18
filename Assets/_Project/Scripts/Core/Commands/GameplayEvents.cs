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

    public sealed class FirstAgekeeperDeterminedEvent : IGameEvent
    {
        public int PlayerId { get; }
        public FirstAgekeeperDeterminedEvent(int playerId) => PlayerId = playerId;
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

    public sealed class CodexAssignedEvent : IGameEvent
    {
        public int          PlayerId { get; }
        public CodexVariant Codex    { get; }
        public CodexAssignedEvent(int playerId, CodexVariant codex) { PlayerId = playerId; Codex = codex; }
    }

    public sealed class CrucibleActivatedEvent : IGameEvent
    {
        public int    PlayerId        { get; }
        public int    SlotIndex       { get; }
        public string CardInstanceId  { get; }
        public Suit   CauldronSuit    { get; }
        public CrucibleActivatedEvent(int playerId, int slotIndex, string cardInstanceId, Suit cauldronSuit)
        {
            PlayerId = playerId; SlotIndex = slotIndex; CardInstanceId = cardInstanceId; CauldronSuit = cauldronSuit;
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
        public int AttackerId     { get; }
        public int DefenderId     { get; }
        public int AttackRoll     { get; }
        public int DefendRoll     { get; }
        public int AttackAlign    { get; }
        public int DefendAlign    { get; }
        public int LoserId        { get; }

        public OppositionResolvedEvent(int attackerId, int defenderId,
            int attackRoll, int defendRoll, int loserId,
            int attackAlign = 0, int defendAlign = 0)
        {
            AttackerId  = attackerId;
            DefenderId  = defenderId;
            AttackRoll  = attackRoll;
            DefendRoll  = defendRoll;
            LoserId     = loserId;
            AttackAlign = attackAlign;
            DefendAlign = defendAlign;
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

    public sealed class DuelResolvedEvent : IGameEvent
    {
        public int AttackerId  { get; }
        public int DefenderId  { get; }
        public int AttackRoll  { get; }
        public int DefendRoll  { get; }
        public int WinnerId    { get; }
        public string AnteCardId { get; }

        public DuelResolvedEvent(int attackerId, int defenderId,
            int attackRoll, int defendRoll, int winnerId, string anteCardId)
        {
            AttackerId = attackerId; DefenderId = defenderId;
            AttackRoll = attackRoll; DefendRoll = defendRoll;
            WinnerId   = winnerId;   AnteCardId = anteCardId;
        }
    }

    public sealed class GambitResolvedEvent : IGameEvent
    {
        public int AttackerId     { get; }
        public int DefenderId     { get; }
        public int AttackRoll     { get; }
        public int DefendRoll     { get; }
        public int WinnerId       { get; }
        public string OfferedCardId { get; }

        public GambitResolvedEvent(int attackerId, int defenderId,
            int attackRoll, int defendRoll, int winnerId, string offeredCardId)
        {
            AttackerId    = attackerId; DefenderId  = defenderId;
            AttackRoll    = attackRoll; DefendRoll  = defendRoll;
            WinnerId      = winnerId;   OfferedCardId = offeredCardId;
        }
    }

    public sealed class CardMovedToZoneEvent : IGameEvent
    {
        public int    PlayerId { get; }
        public string CardId  { get; }
        public bool   ToSpread { get; }
        public CardMovedToZoneEvent(int playerId, string cardId, bool toSpread)
        { PlayerId = playerId; CardId = cardId; ToSpread = toSpread; }
    }

    public sealed class FatefulWagerPlacedEvent : IGameEvent
    {
        public int       PlayerId      { get; }
        public ZodiacSign PredictedSign { get; }
        public int       CardCount     { get; }
        public FatefulWagerPlacedEvent(int playerId, ZodiacSign sign, int cardCount)
        { PlayerId = playerId; PredictedSign = sign; CardCount = cardCount; }
    }

    public sealed class FatefulWagerResolvedEvent : IGameEvent
    {
        public int        PlayerId  { get; }
        public ZodiacSign Sign      { get; }
        public bool       Won       { get; }
        public int        CardCount { get; }
        public FatefulWagerResolvedEvent(int playerId, ZodiacSign sign, bool won, int cardCount)
        { PlayerId = playerId; Sign = sign; Won = won; CardCount = cardCount; }
    }

    public sealed class CardsDiscardedToLimitEvent : IGameEvent
    {
        public int PlayerId      { get; }
        public int CardsDiscard  { get; }
        public CardsDiscardedToLimitEvent(int playerId, int cardsDiscarded)
        { PlayerId = playerId; CardsDiscard = cardsDiscarded; }
    }

    public sealed class TowerFateResolvedEvent : IGameEvent
    {
        public int DrawerId        { get; }
        public int AdeptsArrested  { get; }
        public TowerFateResolvedEvent(int drawerId, int adeptsArrested)
        { DrawerId = drawerId; AdeptsArrested = adeptsArrested; }
    }

    public sealed class AdeptRefreshedEvent : IGameEvent
    {
        public int    PlayerId    { get; }
        public string AdeptCardId { get; }
        public AdeptRefreshedEvent(int playerId, string adeptCardId)
        { PlayerId = playerId; AdeptCardId = adeptCardId; }
    }

    public sealed class TradeCompletedEvent : IGameEvent
    {
        public int InitiatorId   { get; }
        public int TargetId      { get; }
        public int OfferedCards  { get; }
        public int ReceivedCards { get; }
        public TradeCompletedEvent(int initiatorId, int targetId, int offeredCards, int receivedCards)
        { InitiatorId = initiatorId; TargetId = targetId; OfferedCards = offeredCards; ReceivedCards = receivedCards; }
    }

    /// <summary>
    /// Emitted when a stone leaving Stasis clashes with an occupying stone at the same Forge position.
    /// The loser is sent back to Stasis; the winner remains Forging.
    /// </summary>
    public sealed class StasisOppositionEvent : IGameEvent
    {
        public int ReturnerId { get; }
        public int OccupierId { get; }
        public int ReturnerRoll { get; }
        public int OccupierRoll { get; }
        public int LoserId { get; }

        public StasisOppositionEvent(int returnerId, int occupierId,
            int returnerRoll, int occupierRoll, int loserId)
        {
            ReturnerId   = returnerId;
            OccupierId   = occupierId;
            ReturnerRoll = returnerRoll;
            OccupierRoll = occupierRoll;
            LoserId      = loserId;
        }
    }

    /// <summary>Emitted when the Common Deck is exhausted mid-Harvest and Fates intervene.</summary>
    public sealed class HarvestCatastropheEvent : IGameEvent
    {
        public int CardsReturnedToDecks { get; }
        public HarvestCatastropheEvent(int count) => CardsReturnedToDecks = count;
    }
}
