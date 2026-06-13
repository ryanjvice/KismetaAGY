using System.Threading;
using System.Threading.Tasks;
using Kismeta.Core.Commands;

namespace Kismeta.Core.Players
{
    /// <summary>
    /// Stub AI controller. For now always returns PassActionCommand. Real heuristics
    /// (Harvest decisions, crafting priority, Duel/Gambit evaluation) are deferred
    /// to a later milestone once the rules engine is implemented.
    /// </summary>
    public sealed class SimpleAIController : IPlayerController
    {
        public PlayerSlot Slot { get; }
        public bool IsLocalHuman => false;

        public SimpleAIController(PlayerSlot slot) => Slot = slot;

        public Task<IGameCommand> RequestActionAsync(GameContext context, CancellationToken ct = default)
        {
            IGameCommand action = new PassActionCommand(Slot.Index);
            return Task.FromResult(action);
        }
    }
}
