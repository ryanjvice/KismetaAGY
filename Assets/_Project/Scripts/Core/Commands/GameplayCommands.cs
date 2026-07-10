using System.Collections.Generic;
using Kismeta.Core.Domain;

namespace Kismeta.Core.Commands
{
    /// <summary>
    /// Player activates a Dormant Crucible Card by discarding the required card set.
    /// Full Codex formula validation (planet match or rank-sum threshold) is enforced by CrucibleRules.
    /// </summary>
    public sealed class ActivateCrucibleCommand : IGameCommand
    {
        public int PlayerId { get; }
        public int SlotIndex { get; }
        public IReadOnlyList<string> CardInstanceIds { get; }

        public ActivateCrucibleCommand(int playerId, int slotIndex, IReadOnlyList<string> cardIds)
        {
            PlayerId = playerId;
            SlotIndex = slotIndex;
            CardInstanceIds = cardIds;
        }
    }

    /// <summary>Player crafts one Reagent by discarding the required cards.</summary>
    public sealed class CraftReagentCommand : IGameCommand
    {
        public int PlayerId { get; }
        public ReagentType ReagentType { get; }
        public IReadOnlyList<string> CardInstanceIds { get; }

        public CraftReagentCommand(int playerId, ReagentType type, IReadOnlyList<string> cardIds)
        {
            PlayerId = playerId;
            ReagentType = type;
            CardInstanceIds = cardIds;
        }
    }

    /// <summary>Discard a King V1 from Spread to craft one matching elemental reagent.</summary>
    public sealed class CraftKingReagentCommand : IGameCommand
    {
        public int PlayerId { get; }
        public string KingCardId { get; }

        public CraftKingReagentCommand(int playerId, string kingCardId)
        {
            PlayerId = playerId;
            KingCardId = kingCardId;
        }
    }

    /// <summary>Spend Salt to mark a reagent type for Empress 2-for-1 crafting this age.</summary>
    public sealed class MarkEmpressReagentCommand : IGameCommand
    {
        public int PlayerId { get; }
        public ReagentType ReagentType { get; }

        public MarkEmpressReagentCommand(int playerId, ReagentType reagentType)
        {
            PlayerId = playerId;
            ReagentType = reagentType;
        }
    }

    /// <summary>Resonant Temperance: mark one elemental reagent Salt may substitute for when crafting.</summary>
    public sealed class MarkTemperanceWildReagentCommand : IGameCommand
    {
        public int PlayerId { get; }
        public ReagentType ReagentType { get; }

        public MarkTemperanceWildReagentCommand(int playerId, ReagentType reagentType)
        {
            PlayerId = playerId;
            ReagentType = reagentType;
        }
    }

    /// <summary>
    /// Player fires the stone: satisfies alchemical alignment (discards cards from Spread),
    /// pays reagent cost, and moves stone from Mantle to the next Forge position.
    /// The slot at SlotIndex must be Active.
    /// </summary>
    public sealed class FireStoneCommand : IGameCommand
    {
        public int PlayerId { get; }
        public int SlotIndex { get; }
        /// <summary>
        /// Instance IDs of Spread cards that satisfy the Crucible card's Alchemical Alignment.
        /// May be empty if no AlchemicalAlignmentValidator is wired (tests / early dev).
        /// </summary>
        public IReadOnlyList<string> AlignmentCardIds { get; }

        public FireStoneCommand(int playerId, int slotIndex,
            IReadOnlyList<string>? alignmentCardIds = null)
        {
            PlayerId         = playerId;
            SlotIndex        = slotIndex;
            AlignmentCardIds = alignmentCardIds ?? System.Array.Empty<string>();
        }
    }

    /// <summary>
    /// Player tempers the stone: stone was Forging for a full round, moves to next Mantle position.
    /// Discards the Fired Crucible Card slot.
    /// </summary>
    public sealed class TemperCommand : IGameCommand
    {
        public int PlayerId { get; }
        public TemperCommand(int playerId) => PlayerId = playerId;
    }

    /// <summary>
    /// Attacker initiates Opposition against a player whose stone is Forging.
    /// Simplified M2 resolution: dice roll, higher wins; loser's Forging stone → Stasis.
    /// </summary>
    public sealed class InitiateOppositionCommand : IGameCommand
    {
        public int AttackerId { get; }
        public int DefenderId { get; }
        public InitiateOppositionCommand(int attackerId, int defenderId)
        {
            AttackerId = attackerId;
            DefenderId = defenderId;
        }
    }

    /// <summary>Player spends 2 Salt to leave Stasis and return to Tempering.</summary>
    public sealed class LeaveStasisCommand : IGameCommand
    {
        public int PlayerId { get; }
        public LeaveStasisCommand(int playerId) => PlayerId = playerId;
    }

    /// <summary>Player explicitly passes their current action window (Summer or Autumn crucible step).</summary>
    public sealed class PassCrucibleActionCommand : IGameCommand
    {
        public int PlayerId { get; }
        public PassCrucibleActionCommand(int playerId) => PlayerId = playerId;
    }

    /// <summary>
    /// Player places a Ward Reagent on an Active Crucible Card slot.
    /// The reagent is spent immediately; the ward count on the slot increases by 1.
    /// </summary>
    public sealed class PlaceCardWardCommand : IGameCommand
    {
        public int PlayerId   { get; }
        public int SlotIndex  { get; }
        public ReagentType ReagentType { get; }

        public PlaceCardWardCommand(int playerId, int slotIndex, ReagentType reagentType)
        {
            PlayerId   = playerId;
            SlotIndex  = slotIndex;
            ReagentType = reagentType;
        }
    }

    /// <summary>
    /// Player places a Ward Reagent on their stone's Forge position.
    /// Used before or immediately after Firing; the reagent is spent and sets the entry fee for Opposition.
    /// </summary>
    public sealed class PlaceStoneWardCommand : IGameCommand
    {
        public int PlayerId    { get; }
        public ReagentType ReagentType { get; }

        public PlaceStoneWardCommand(int playerId, ReagentType reagentType)
        {
            PlayerId    = playerId;
            ReagentType = reagentType;
        }
    }

    /// <summary>
    /// Player places a Ward Reagent on an Adept card in their Arcanum.
    /// Wards return to supply if the Adept leaves play.
    /// </summary>
    public sealed class PlaceAdeptWardCommand : IGameCommand
    {
        public int PlayerId { get; }
        public string AdeptCardId { get; }
        public ReagentType ReagentType { get; }

        public PlaceAdeptWardCommand(int playerId, string adeptCardId, ReagentType reagentType)
        {
            PlayerId     = playerId;
            AdeptCardId  = adeptCardId;
            ReagentType  = reagentType;
        }
    }

    // ── Arcanum refresh ───────────────────────────────────────────────────────

    /// <summary>
    /// Player spends 1 Salt to "refresh" an Adept card arrested by The Tower,
    /// restoring its contribution to alignment scoring.
    /// </summary>
    public sealed class RefreshAdeptCommand : IGameCommand
    {
        public int    PlayerId    { get; }
        public string AdeptCardId { get; }
        public RefreshAdeptCommand(int playerId, string adeptCardId)
        { PlayerId = playerId; AdeptCardId = adeptCardId; }
    }

    // ── Trade ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Direct hot-seat trade: both parties exchange Spread cards immediately.
    /// Minor arcana only; major arcana cannot be traded.
    /// </summary>
    public sealed class DirectTradeCommand : IGameCommand
    {
        public int PlayerId { get; }
        public int TargetId { get; }
        /// <summary>Cards the initiator offers from their own Spread.</summary>
        public IReadOnlyList<string> OfferCardIds { get; }
        /// <summary>Cards the initiator requests from the target's Spread.</summary>
        public IReadOnlyList<string> RequestCardIds { get; }

        public DirectTradeCommand(int playerId, int targetId,
            IReadOnlyList<string> offerCardIds,
            IReadOnlyList<string> requestCardIds)
        {
            PlayerId       = playerId;
            TargetId       = targetId;
            OfferCardIds   = offerCardIds;
            RequestCardIds = requestCardIds;
        }
    }

    // ── Winter commands ───────────────────────────────────────────────────────

    /// <summary>
    /// During the Winter Activities free-action pool, a player moves one card
    /// between Hand and Spread (cards are unlocked at this point).
    /// </summary>
    public sealed class WinterMoveCardCommand : IGameCommand
    {
        public int PlayerId { get; }
        public string CardId { get; }
        /// <summary>True = move to Spread; false = move to Hand.</summary>
        public bool ToSpread { get; }

        public WinterMoveCardCommand(int playerId, string cardId, bool toSpread)
        {
            PlayerId = playerId;
            CardId   = cardId;
            ToSpread = toSpread;
        }
    }

    /// <summary>
    /// Player wagers cards on a Zodiac Sign prediction. Resolved at next Spring Cosmic Age roll.
    /// Wagered cards are removed from Hand/Spread and held until resolution.
    /// </summary>
    public sealed class PlaceFatefulWagerCommand : IGameCommand
    {
        public int PlayerId { get; }
        public ZodiacSign PredictedSign { get; }
        /// <summary>Card instance IDs from Spread or Hand (minor arcana only).</summary>
        public IReadOnlyList<string> CardIds { get; }

        public PlaceFatefulWagerCommand(int playerId, ZodiacSign predictedSign,
            IReadOnlyList<string> cardIds)
        {
            PlayerId      = playerId;
            PredictedSign = predictedSign;
            CardIds       = cardIds;
        }
    }

    /// <summary>
    /// Player selects which cards to discard when over the Winter card limits
    /// (Spread max 5, Hand max 5).
    /// </summary>
    public sealed class DiscardToLimitCommand : IGameCommand
    {
        public int PlayerId { get; }
        public IReadOnlyList<string> DiscardSpreadIds { get; }
        public IReadOnlyList<string> DiscardHandIds   { get; }

        public DiscardToLimitCommand(int playerId,
            IReadOnlyList<string> discardSpreadIds,
            IReadOnlyList<string> discardHandIds)
        {
            PlayerId         = playerId;
            DiscardSpreadIds = discardSpreadIds;
            DiscardHandIds   = discardHandIds;
        }
    }
}
