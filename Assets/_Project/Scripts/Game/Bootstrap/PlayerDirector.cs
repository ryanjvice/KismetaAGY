using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kismeta.Core.Commands;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Views;

namespace Kismeta.Game.Bootstrap
{
    /// <summary>
    /// Mediates between the active IPlayerController and the GameSession.
    /// Given the current phase and active player, it requests a command, validates it
    /// (stub for now), applies it, and publishes resulting events. Lives in Kismeta.Game
    /// because it bridges the pure core with Unity-specific lifecycle concerns.
    /// </summary>
    public sealed class PlayerDirector
    {
        private readonly GameSession _session;
        private readonly IReadOnlyList<IPlayerController> _controllers;

        public PlayerDirector(GameSession session, IReadOnlyList<IPlayerController> controllers)
        {
            _session = session;
            _controllers = controllers;
        }

        /// <summary>
        /// Ask the controller for the given player slot to provide a command,
        /// validate it (stub: accept all), and apply to the session.
        /// </summary>
        public async Task<CommandResult> RequestAndApplyAsync(int playerId, CancellationToken ct = default)
        {
            var controller = FindController(playerId);
            if (controller == null)
                return CommandResult.Invalid($"No controller registered for player {playerId}.");

            var publicView  = GamePublicView.From(_session);
            var privateView = PlayerPrivateView.From(_session, playerId);
            var context     = new GameContext(publicView, privateView, playerId);

            var command = await controller.RequestActionAsync(context, ct);

            // Stub validation: accept all commands for now.
            return _session.Apply(command);
        }

        private IPlayerController? FindController(int playerId)
        {
            foreach (var c in _controllers)
                if (c.Slot.Index == playerId) return c;
            return null;
        }
    }
}
