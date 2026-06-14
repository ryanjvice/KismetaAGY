using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Manages the full Crucible Card lifecycle:
    /// Activate (Summer), Fire / Temper / Stasis (Autumn), and Opposition (Autumn Step 2).
    /// </summary>
    public interface ICrucibleService : IRuleService
    {
        /// <summary>Summer: player activates a Dormant Crucible Card by discarding the required card set.</summary>
        CommandResult TryActivate(GameSession session, int playerId, int slotIndex,
            IReadOnlyList<string> cardInstanceIds);

        /// <summary>Autumn: player fires their stone — satisfies alignment, pays reagent cost, stone moves to Forge.</summary>
        CommandResult TryFire(GameSession session, int playerId, int slotIndex,
            IReadOnlyList<string>? alignmentCardIds = null);

        /// <summary>Autumn: player tempers their stone — advances from Forge to next Mantle position.</summary>
        CommandResult TryTemper(GameSession session, int playerId);

        /// <summary>Autumn: player moves their stone out of Stasis (costs 2 Salt in standard rules).</summary>
        CommandResult TryLeaveStasis(GameSession session, int playerId);

        /// <summary>
        /// Autumn Step 2: attacker initiates an Opposition against a Forging stone.
        /// Score = alignment points + dice roll; higher total wins. Attacker wins ties.
        /// </summary>
        CommandResult TryOppose(GameSession session, int attackerId, int defenderId);

        /// <summary>Summer/Autumn: place one Ward Reagent on an Active Crucible Card slot.</summary>
        CommandResult TryPlaceCardWard(GameSession session, int playerId, int slotIndex,
            ReagentType reagentType);

        /// <summary>Autumn: place one Ward Reagent on the stone's Forge position (sets Opposition entry fee).</summary>
        CommandResult TryPlaceStoneWard(GameSession session, int playerId, ReagentType reagentType);
    }
}
