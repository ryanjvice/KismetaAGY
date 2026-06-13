using Kismeta.Core.Entities;
using Kismeta.Core.Views;

namespace Kismeta.Core.Players
{
    /// <summary>
    /// Read-only context passed to IPlayerController when requesting an action.
    /// Contains the requesting player's private view plus the full public view.
    /// </summary>
    public sealed class GameContext
    {
        public GamePublicView PublicView { get; }
        public PlayerPrivateView PrivateView { get; }
        public int ActivePlayerId { get; }

        public GameContext(GamePublicView publicView, PlayerPrivateView privateView, int activePlayerId)
        {
            PublicView = publicView;
            PrivateView = privateView;
            ActivePlayerId = activePlayerId;
        }
    }
}
