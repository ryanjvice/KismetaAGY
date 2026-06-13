using Kismeta.Core.Entities;
using Kismeta.Core.Views;

namespace Kismeta.Core.Players
{
    /// <summary>
    /// Read-only context passed to IPlayerController when requesting an action.
    /// Contains the requesting player's private view plus the full public view,
    /// and an ActionHint so the controller knows what kind of command is expected.
    /// </summary>
    public sealed class GameContext
    {
        public GamePublicView  PublicView    { get; }
        public PlayerPrivateView PrivateView { get; }
        public int ActivePlayerId            { get; }
        public ActionHint Hint               { get; }

        public GameContext(GamePublicView publicView, PlayerPrivateView privateView,
            int activePlayerId, ActionHint hint = ActionHint.None)
        {
            PublicView    = publicView;
            PrivateView   = privateView;
            ActivePlayerId = activePlayerId;
            Hint           = hint;
        }
    }
}
