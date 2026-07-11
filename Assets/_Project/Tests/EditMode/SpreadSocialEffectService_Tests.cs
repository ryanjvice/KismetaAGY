using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using NUnit.Framework;

namespace Kismeta.Core.Tests
{
    public sealed class SpreadSocialEffectService_Tests
    {
        static GameSession NewSession(string socialDefId)
        {
            var players = new List<PlayerState>
            {
                new PlayerState(0, PlayerColor.Red),
                new PlayerState(1, PlayerColor.Blue)
            };
            var session = new GameSession("test", GameMode.Quickplay, players, null);
            for (int i = 0; i < 5; i++)
            {
                var id = $"deck-{i}";
                session.RegisterCard(new CardInstance(id, "minor.cups.two.1", CardZone.Deck, -1));
                session.Board.CommonDeck.Push(id);
            }
            session.RegisterCard(new CardInstance("social", socialDefId, CardZone.Spread, 0));
            session.Players[0].Spread.Add("social");
            return session;
        }

        static ICardDatabase LoadDb()
        {
            var path = UnityEngine.Application.dataPath + "/_Project/Scripts/Data/Generated/cards.json";
            return Kismeta.Data.Loaders.CardDatabase.LoadFromJson(System.IO.File.ReadAllText(path));
        }

        [Test]
        public void TryApplyContestWinDraw_OwnerOnlyOnDuelWin()
        {
            var db = LoadDb();
            var session = NewSession("minor.swords.nine.1");
            int deckBefore = session.Board.CommonDeck.Count;

            SpreadSocialEffectService.TryApplyContestWinDraw(session, 0, ContestKind.Duel, db);
            Assert.AreEqual(deckBefore - 2, session.Board.CommonDeck.Count);
            Assert.AreEqual(2, session.Players[0].Hand.Count);
        }

        [Test]
        public void TryApplyContestWinDraw_NoDrawForNonOwner()
        {
            var db = LoadDb();
            var session = NewSession("minor.swords.nine.1");
            int deckBefore = session.Board.CommonDeck.Count;

            SpreadSocialEffectService.TryApplyContestWinDraw(session, 1, ContestKind.Duel, db);
            Assert.AreEqual(deckBefore, session.Board.CommonDeck.Count);
        }
    }
}
