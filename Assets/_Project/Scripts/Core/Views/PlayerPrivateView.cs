using System.Collections.Generic;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Views
{
    /// <summary>
    /// Extends GamePublicView with the requesting player's Hand card IDs.
    /// This is the only view that includes Hand contents; it must never be sent
    /// to other players in a networked game.
    /// </summary>
    public sealed class PlayerPrivateView
    {
        public GamePublicView Public { get; }
        public int PlayerId { get; }
        public IReadOnlyList<string> Hand { get; }

        private PlayerPrivateView(GameSession session, int playerId)
        {
            Public = GamePublicView.From(session);
            PlayerId = playerId;

            // Find the requesting player's Hand — only this player's Hand is revealed.
            var hand = new List<string>();
            foreach (var p in session.Players)
            {
                if (p.PlayerId == playerId)
                {
                    hand.AddRange(p.Hand);
                    break;
                }
            }
            Hand = hand;
        }

        /// <param name="playerId">The player requesting their private view.</param>
        public static PlayerPrivateView From(GameSession session, int playerId) =>
            new(session, playerId);
    }
}
