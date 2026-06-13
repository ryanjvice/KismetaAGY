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

        /// <summary>Autumn: player fires their stone — pays reagent cost, stone enters the Forge.</summary>
        CommandResult TryFire(GameSession session, int playerId, int slotIndex);

        /// <summary>Autumn: player tempers their stone — advances from Forge to next Mantle position.</summary>
        CommandResult TryTemper(GameSession session, int playerId);

        /// <summary>Autumn: player moves their stone out of Stasis (costs 2 Salt in standard rules).</summary>
        CommandResult TryLeaveStasis(GameSession session, int playerId);

        /// <summary>
        /// Autumn Step 2: attacker initiates an Opposition against a Forging stone.
        /// M2 simplification: each side rolls a zodiac die; lower roll → loser's stone to Stasis.
        /// </summary>
        CommandResult TryOppose(GameSession session, int attackerId, int defenderId);
    }
}
