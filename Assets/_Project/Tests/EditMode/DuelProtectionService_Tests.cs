using System.IO;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.Data.Loaders;
using NUnit.Framework;
using UnityEngine;

namespace Kismeta.Core.Tests
{
    public sealed class DuelProtectionService_Tests
    {
        static CardDatabase LoadDb()
        {
            var path = Path.Combine(Application.dataPath,
                "_Project/Scripts/Data/Generated/cards.json");
            return CardDatabase.LoadFromJson(File.ReadAllText(path));
        }

        static CrucibleCodexDatabase LoadCodexDb()
        {
            var path = Path.Combine(Application.dataPath,
                "_Project/Data/Resources/crucible-codex.json");
            return CrucibleCodexDatabase.LoadFromJson(File.ReadAllText(path));
        }

        static GameSession BuildSession(CardDatabase db)
        {
            var codexDb = LoadCodexDb();
            var rules = new GameRuleSet(
                cardDatabase: db,
                codexDatabase: codexDb,
                setup: new GameSetupService(db, 42),
                harvest: new SpringRules(db, 42),
                crucible: new CrucibleRules(db, codexDb),
                crafting: new CraftingRules(db),
                winter: new WinterRules(db),
                validator: new ActionValidator());
            var players = new System.Collections.Generic.List<PlayerState>
            {
                new PlayerState(0, PlayerColor.Red),
                new PlayerState(1, PlayerColor.Blue)
            };
            return new GameSession("test", GameMode.Quickplay, players, rules);
        }

        static void AddSpread(GameSession session, int playerId, string instanceId, string definitionId)
        {
            session.RegisterCard(new CardInstance(instanceId, definitionId, CardZone.Spread, playerId));
            session.Players[playerId].Spread.Add(instanceId);
        }

        [Test]
        public void IsKingV2Passive_TrueForKingOfCupsV2()
        {
            var db = LoadDb();
            var def = db.GetById("minor.cups.king.2");
            Assert.IsNotNull(def);
            Assert.IsTrue(DuelProtectionService.IsKingV2Passive(def!));
        }

        [Test]
        public void IsSuitProtectedFromDuel_ProtectsSameSuitNotKing()
        {
            var db = LoadDb();
            var session = BuildSession(db);
            var defender = session.Players[1];
            AddSpread(session, 1, "king-cups", "minor.cups.king.2");
            AddSpread(session, 1, "target-cups", "minor.cups.two.1");

            Assert.IsTrue(DuelProtectionService.IsSuitProtectedFromDuel(session, defender, "target-cups"));
            Assert.IsFalse(DuelProtectionService.IsSuitProtectedFromDuel(session, defender, "king-cups"));
        }

        [Test]
        public void IsSuitProtectedFromDuel_WrongSuitNotProtected()
        {
            var db = LoadDb();
            var session = BuildSession(db);
            var defender = session.Players[1];
            AddSpread(session, 1, "king-cups", "minor.cups.king.2");
            AddSpread(session, 1, "target-wands", "minor.wands.two.1");

            Assert.IsFalse(DuelProtectionService.IsSuitProtectedFromDuel(session, defender, "target-wands"));
        }

        [Test]
        public void FilterDuelTargets_ExcludesProtectedCards()
        {
            var db = LoadDb();
            var session = BuildSession(db);
            var defender = session.Players[1];
            AddSpread(session, 1, "king-cups", "minor.cups.king.2");
            AddSpread(session, 1, "prot-cups", "minor.cups.three.1");
            AddSpread(session, 1, "ok-wands", "minor.wands.two.1");

            var filtered = DuelProtectionService.FilterDuelTargets(
                session, defender, defender.Spread);

            Assert.Contains("king-cups", filtered);
            Assert.Contains("ok-wands", filtered);
            Assert.IsFalse(filtered.Contains("prot-cups"));
        }
    }
}
