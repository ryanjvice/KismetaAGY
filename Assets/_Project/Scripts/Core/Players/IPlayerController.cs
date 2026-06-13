using System.Threading;
using System.Threading.Tasks;
using Kismeta.Core.Commands;

namespace Kismeta.Core.Players
{
    /// <summary>
    /// Abstraction over how a player provides commands. The same interface covers:
    /// hot-seat (human submits via UI), local AI (heuristic), and future network remote.
    /// PlayerDirector calls RequestActionAsync and never cares which implementation responds.
    /// </summary>
    public interface IPlayerController
    {
        PlayerSlot Slot { get; }
        bool IsLocalHuman { get; }

        /// <summary>
        /// Called by PlayerDirector when it's this player's turn to act.
        /// Returns a command representing the chosen action.
        /// </summary>
        Task<IGameCommand> RequestActionAsync(GameContext context, CancellationToken ct = default);
    }
}
