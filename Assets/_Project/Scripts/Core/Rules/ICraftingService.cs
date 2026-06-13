using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Handles Reagent crafting available in Summer, Autumn, and Winter.
    /// Salt: any 3 cards, no Cauldron required.
    /// Elemental reagents: 3 matching-Suit cards + lit Cauldron of that element.
    /// </summary>
    public interface ICraftingService : IRuleService
    {
        CommandResult TryCraft(GameSession session, int playerId,
            ReagentType reagentType, IReadOnlyList<string> cardInstanceIds);
    }
}
