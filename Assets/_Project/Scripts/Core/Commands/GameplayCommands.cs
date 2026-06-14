using System.Collections.Generic;
using Kismeta.Core.Domain;

namespace Kismeta.Core.Commands
{
    /// <summary>
    /// Player activates a Dormant Crucible Card by discarding the required card set.
    /// M2 simplification: card type matching is not enforced; only card count is checked.
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
}
