using System.Collections.Generic;
using Kismeta.Core.Domain;

namespace Kismeta.Core.Commands
{
    /// <summary>
    /// The Moon (18): player keeps exactly 2 of the 4 drawn cards;
    /// the rest return to the bottom of the Common Deck.
    /// </summary>
    public sealed class FateMoonDecisionCommand : IGameCommand
    {
        public int PlayerId { get; }
        /// <summary>Exactly 2 card instance IDs to keep in Hand.</summary>
        public IReadOnlyList<string> KeepCardIds { get; }

        public FateMoonDecisionCommand(int playerId, IReadOnlyList<string> keepCardIds)
        {
            PlayerId    = playerId;
            KeepCardIds = keepCardIds;
        }
    }

    /// <summary>
    /// The Fool (0) / Lovers (6): an opponent picks a Reagent to receive.
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
            ReagentType chosenReagent = ReagentType.Salt)
        {
            PlayerId      = playerId;
            DrawCards     = drawCards;
            ChosenReagent = chosenReagent;
        }
    }
}
