using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Handles Reagent crafting available in Summer, Autumn, and Winter.
    /// </summary>
    public interface ICraftingService : IRuleService
    {
        CommandResult TryCraft(GameSession session, int playerId,
            ReagentType reagentType, IReadOnlyList<string> cardInstanceIds);

        CommandResult TryKingDiscardCraft(GameSession session, int playerId, string kingCardId);

        CommandResult TryMarkEmpressReagent(GameSession session, int playerId, ReagentType reagentType);

        CommandResult TryMarkTemperanceWildReagent(GameSession session, int playerId, ReagentType reagentType);
    }
}
