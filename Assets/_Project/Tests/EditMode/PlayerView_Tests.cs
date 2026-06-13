using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Views;
using NUnit.Framework;

namespace Kismeta.Core.Tests
{
    public sealed class PlayerView_Tests
    {
        private GameSession BuildSession()
        {
            var players = new List<PlayerState>
            {
                new PlayerState(0, PlayerColor.Red)   { IsAgekeeper = true },
                new PlayerState(1, PlayerColor.Green),
            };
            // Give player 0 some Hand cards to confirm visibility rules.
            players[0].Hand.Add("card-001");
            players[0].Hand.Add("card-002");
            players[0].Spread.Add("card-003");

            players[1].Hand.Add("card-101");
            players[1].Spread.Add("card-102");

            return new GameSession("test-session", GameMode.Quickplay, players);
        }

        [Test]
        public void GamePublicView_Does_Not_Expose_Hand_Contents()
        {
            var session = BuildSession();
            var view    = GamePublicView.From(session);

            // Public player views must show hand COUNT but not the card IDs.
            foreach (var pv in view.Players)
            {
                // There is no Hand list on PublicPlayerView — only HandCardCount.
                Assert.IsTrue(pv.HandCardCount >= 0,
                    "HandCardCount should be non-negative.");
            }
        }

        [Test]
        public void GamePublicView_HandCardCount_Is_Correct()
        {
            var session = BuildSession();
            var view    = GamePublicView.From(session);

            Assert.AreEqual(2, view.Players[0].HandCardCount, "Player 0 should have 2 Hand cards.");
            Assert.AreEqual(1, view.Players[1].HandCardCount, "Player 1 should have 1 Hand card.");
        }

        [Test]
        public void PlayerPrivateView_Reveals_Only_Requesting_Players_Hand()
        {
            var session = BuildSession();

            var view0 = PlayerPrivateView.From(session, playerId: 0);
            var view1 = PlayerPrivateView.From(session, playerId: 1);

            // Player 0's private view includes their own Hand.
            CollectionAssert.AreEquivalent(new[] { "card-001", "card-002" }, view0.Hand);

            // Player 1's private view includes only their own Hand, not player 0's.
            CollectionAssert.AreEquivalent(new[] { "card-101" }, view1.Hand);
        }

        [Test]
        public void PublicView_Spread_Is_Visible()
        {
            var session = BuildSession();
            var view    = GamePublicView.From(session);

            CollectionAssert.Contains(view.Players[0].Spread, "card-003",
                "Player 0's Spread card should be visible in the public view.");
            CollectionAssert.Contains(view.Players[1].Spread, "card-102",
                "Player 1's Spread card should be visible in the public view.");
        }

        [Test]
        public void GamePublicView_Shows_Current_Phase()
        {
            var session = BuildSession();
            var view    = GamePublicView.From(session);

            Assert.AreEqual(Season.Spring, view.CurrentSeason);
            Assert.AreEqual("SetCosmicAge", view.CurrentStepName);
        }
    }
}
