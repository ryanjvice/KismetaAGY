using System.Collections.Generic;
using Kismeta.Core.Domain;

namespace Kismeta.Core.Commands
{
    /// <summary>
    /// The Moon (18): give a meaningful gift (minor cards and/or reagents) to another player.
    /// </summary>
    public sealed class FateMoonGiftCommand : IGameCommand
    {
        public int GiverId { get; }
        public int RecipientId { get; }
        public IReadOnlyList<string> CardIds { get; }
        public IReadOnlyDictionary<ReagentType, int> Reagents { get; }

        public FateMoonGiftCommand(int giverId, int recipientId,
            IReadOnlyList<string> cardIds,
            IReadOnlyDictionary<ReagentType, int>? reagents = null)
        {
            GiverId     = giverId;
            RecipientId = recipientId;
            CardIds     = cardIds;
            Reagents    = reagents ?? new Dictionary<ReagentType, int>();
        }
    }

    /// <summary>
    /// The Fool (0) / Lovers (6): an opponent picks a Reagent to receive (Lovers only).
    /// </summary>
    public sealed class FateReagentChoiceCommand : IGameCommand
    {
        public int        PlayerId    { get; }
        public ReagentType ReagentType { get; }

        public FateReagentChoiceCommand(int playerId, ReagentType reagentType)
        {
            PlayerId    = playerId;
            ReagentType = reagentType;
        }
    }

    /// <summary>
    /// Claim the Fool altar Crucible card by completing its Alchemical Formula from Spread.
    /// </summary>
    public sealed class ClaimFoolAltarCrucibleCommand : IGameCommand
    {
        public int PlayerId { get; }
        public int DormantSlotIndex { get; }
        public IReadOnlyList<string> AlignmentCardIds { get; }

        public ClaimFoolAltarCrucibleCommand(int playerId, int dormantSlotIndex,
            IReadOnlyList<string> alignmentCardIds)
        {
            PlayerId          = playerId;
            DormantSlotIndex  = dormantSlotIndex;
            AlignmentCardIds  = alignmentCardIds;
        }
    }

    /// <summary>
    /// The Lovers (6): the drawer nominates which opponent will choose their reward.
    /// </summary>
    public sealed class FateLoversTargetCommand : IGameCommand
    {
        public int DrawerId       { get; }
        public int ChosenTargetId { get; }

        public FateLoversTargetCommand(int drawerId, int chosenTargetId)
        {
            DrawerId       = drawerId;
            ChosenTargetId = chosenTargetId;
        }
    }

    /// <summary>
    /// The Lovers (6): the chosen target decides the drawer's reward.
    /// DrawCards = true → drawer draws 2 cards; false → drawer gains 1 Reagent of target's choice.
    /// </summary>
    public sealed class FateLoversChoiceCommand : IGameCommand
    {
        public int  PlayerId  { get; }
        public bool DrawCards { get; }
        /// <summary>When DrawCards is false, the Reagent the target picks for the drawer.</summary>
        public ReagentType ChosenReagent { get; }

        public FateLoversChoiceCommand(int playerId, bool drawCards,
            ReagentType chosenReagent = ReagentType.Salt, int chooserId = -1)
        {
            PlayerId      = playerId;
            DrawCards     = drawCards;
            ChosenReagent = chosenReagent;
            ChooserId     = chooserId;
        }

        public int ChooserId { get; }
    }
}
