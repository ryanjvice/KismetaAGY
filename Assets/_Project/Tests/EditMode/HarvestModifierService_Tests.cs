using System.IO;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.Data.Loaders;
using NUnit.Framework;
using UnityEngine;

namespace Kismeta.Core.Tests
{
    public sealed class HarvestModifierService_Tests
    {
        static CardDatabase LoadDb()
        {
            var path = Path.Combine(Application.dataPath,
                "_Project/Scripts/Data/Generated/cards.json");
            return CardDatabase.LoadFromJson(File.ReadAllText(path));
        }

        static GameSession BuildSession(CardDatabase db)
        {
            var players = new System.Collections.Generic.List<PlayerState>
            {
                new(0, PlayerColor.Red),
                new(1, PlayerColor.Blue),
            };
            var rules = new GameRuleSet(
                db,
                null!,
                setup: null!,
                harvest: new SpringRules(db),
                crucible: null!,
                crafting: new CraftingRules(db),
                winter: null!,
                validator: new ActionValidator(),
                combat: null!,
                trade: null!);
            return new GameSession("test", GameMode.Quickplay, players, rules);
        }

        static void AddSpread(GameSession session, string instanceId, string definitionId)
        {
            var inst = new CardInstance(instanceId, definitionId, CardZone.Spread, 0);
            session.RegisterCard(inst);
            session.Players[0].Spread.Add(instanceId);
        }

        [Test]
        public void SpreadHarvestCatalog_Identifies_AceV2_And_Rank2()
        {
            var db = LoadDb();
            var ace = db.GetById("minor.cups.ace.2");
            var two = db.GetById("minor.cups.two.1");

            Assert.IsTrue(SpreadHarvestEffectCatalog.IsAceV2Passive(ace));
            Assert.IsTrue(SpreadHarvestEffectCatalog.IsRank2HarvestDouble(two));
        }

        [Test]
        public void SpreadPassiveBonus_AddsTwo_WhenCosmicElementMatches()
        {
            var db = LoadDb();
            var session = BuildSession(db);
            session.Board.CosmicAgeSign = ZodiacSign.Cancer;

            int before = HarvestModifierService.SpreadPassiveBonus(session, 0);
            AddSpread(session, "ace", "minor.cups.ace.2");
            int after = HarvestModifierService.SpreadPassiveBonus(session, 0);

            Assert.AreEqual(0, before);
            Assert.AreEqual(2, after);
        }

        [Test]
        public void HouseDoublingBonus_Doubles_WaterHouse_WhenCupsTwoActive()
        {
            var db = LoadDb();
            var session = BuildSession(db);
            session.Board.CosmicAgeSign = ZodiacSign.Cancer;
            session.Players[0].AstralHouses.Add(ZodiacSign.Pisces);
            AddSpread(session, "two", "minor.cups.two.1");

            int extra = HarvestModifierService.HouseDoublingBonus(session, 0);
            int expected = HarvestBreakdownService.AlignmentBonus(ZodiacSign.Pisces, ZodiacSign.Cancer);

            Assert.AreEqual(expected, extra);
        }
    }
}
