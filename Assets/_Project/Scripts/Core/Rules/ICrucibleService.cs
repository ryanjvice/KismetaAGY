using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
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
        /// Summer: attacker initiates an Opposition against a Forging stone.
        /// Records a pending contest awaiting defender response.
        /// </summary>
        CommandResult TryInitiateOppose(GameSession session, int attackerId, int defenderId);

        /// <summary>Summer: defender accepts or declines a pending Opposition.</summary>
        CommandResult TryRespondOpposition(GameSession session, int defenderId, bool accept);

        /// <summary>
        /// Legacy entry point: initiates then auto-accepts (tests / debug).
        /// </summary>
        CommandResult TryOppose(GameSession session, int attackerId, int defenderId);

        /// <summary>Summer/Autumn: place one Ward Reagent on an Active Crucible Card slot.</summary>
        CommandResult TryPlaceCardWard(GameSession session, int playerId, int slotIndex,
            ReagentType reagentType);

        /// <summary>Autumn: place one Ward Reagent on the stone's Forge position (sets Opposition entry fee).</summary>
        CommandResult TryPlaceStoneWard(GameSession session, int playerId, ReagentType reagentType);

        /// <summary>
        /// Summer/Autumn: spend 1 Salt to refresh an Adept card arrested by The Tower,
        /// restoring its contribution to alignment scoring.
        /// </summary>
        CommandResult TryRefreshAdept(GameSession session, int playerId, string adeptCardId);
    }
}
