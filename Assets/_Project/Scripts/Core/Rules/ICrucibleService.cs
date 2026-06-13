using Kismeta.Core.Commands;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Handles Crucible Card lifecycle: Activate, Fire, Temper, Stasis, Arrest.
    /// </summary>
    public interface ICrucibleService : IRuleService
    {
        CommandResult TryActivate(GameSession session, int playerId, int slotIndex);
        CommandResult TryFire(GameSession session, int playerId, int slotIndex);
        CommandResult TryTemper(GameSession session, int playerId);
        CommandResult TryLeaveStasis(GameSession session, int playerId);
    }
}
