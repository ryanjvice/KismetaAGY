using System.Collections.Generic;
using System.IO;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.Data.Loaders;
using NUnit.Framework;
using UnityEngine;

namespace Kismeta.Core.Tests
{
    public sealed class ForgeReagentPaymentService_Tests
    {
        static CardDatabase LoadDb()
        {
            var path = Path.Combine(Application.dataPath,
                "_Project/Scripts/Data/Generated/cards.json");
            return CardDatabase.LoadFromJson(File.ReadAllText(path));
        }

        static GameSession NewSession(ICardDatabase db)
        {
            var players = new List<PlayerState>
            {
                new PlayerState(0, PlayerColor.Red),
                new PlayerState(1, PlayerColor.Blue)
            };
            var session = new GameSession("test", GameMode.Quickplay, players, null);
            session.RegisterCard(new CardInstance("queen-swords", "minor.swords.queen.1", CardZone.Spread, 0));
            session.Players[0].Spread.Add("queen-swords");
            return session;
        }

        [Test]
        public void CanPayFireCost_QueenWild_CoversTypedShortfall()
        {
            var db = LoadDb();
            var session = NewSession(db);
            var player = session.Players[0];
            player.AddReagent(ReagentType.Quicksilver, 3);

            var wild = ForgeReagentPaymentService.GetWildReagentTypes(player, session, db);
            var cost = new ReagentCost(2, 0, 0, 0, 0);

            Assert.IsNotEmpty(wild);
            Assert.IsTrue(ForgeReagentPaymentService.CanPayFireCost(player, cost, wild));
        }

        [Test]
        public void PayFireCost_QueenWild_SpendsQuicksilverForSulphurCost()
        {
            var db = LoadDb();
            var session = NewSession(db);
            var player = session.Players[0];
            player.AddReagent(ReagentType.Quicksilver, 2);

            var wild = ForgeReagentPaymentService.GetWildReagentTypes(player, session, db);
            var cost = new ReagentCost(2, 0, 0, 0, 0);

            ForgeReagentPaymentService.PayFireCost(player, cost, wild);
            Assert.AreEqual(0, player.GetReagent(ReagentType.Quicksilver));
            Assert.AreEqual(0, player.GetReagent(ReagentType.Sulphur));
        }

        [Test]
        public void CanPayFireCost_NoQueen_RequiresExactTypes()
        {
            var player = new PlayerState(0, PlayerColor.Red);
            player.AddReagent(ReagentType.Sulphur, 1);
            var cost = new ReagentCost(2, 0, 0, 0, 0);
            var wild = new HashSet<ReagentType>();

            Assert.IsFalse(ForgeReagentPaymentService.CanPayFireCost(player, cost, wild));
        }
    }
}
