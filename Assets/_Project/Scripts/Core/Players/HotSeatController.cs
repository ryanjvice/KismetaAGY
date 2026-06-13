using System;
using System.Threading;
using System.Threading.Tasks;
using Kismeta.Core.Commands;

namespace Kismeta.Core.Players
{
    /// <summary>
    /// Human player in hot-seat / pass-and-play mode. Blocks until the UI layer
    /// submits a command via <see cref="SubmitCommand"/>. The UI calls SubmitCommand
    /// on the main thread; RequestActionAsync awaits from the game loop.
    /// </summary>
    public sealed class HotSeatController : IPlayerController
    {
        public PlayerSlot Slot { get; }
        public bool IsLocalHuman => true;

        private TaskCompletionSource<IGameCommand>? _pending;

        public HotSeatController(PlayerSlot slot) => Slot = slot;

        public Task<IGameCommand> RequestActionAsync(GameContext context, CancellationToken ct = default)
        {
            _pending = new TaskCompletionSource<IGameCommand>(TaskCreationOptions.RunContinuationsAsynchronously);

            ct.Register(() => _pending.TrySetCanceled());
            return _pending.Task;
        }

        /// <summary>UI calls this to deliver the player's chosen action.</summary>
        public void SubmitCommand(IGameCommand command)
        {
            if (_pending == null)
                throw new InvalidOperationException("No pending action request.");
            _pending.TrySetResult(command);
        }
    }
}
