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
    public sealed class CraftModifierService_Tests
    {
        static CardDatabase LoadDb()
        {
            var path = Path.Combine(Application.dataPath,
                "_Project/Scripts/Data/Generated/cards.json");
            return CardDatabase.LoadFromJson(File.ReadAllText(path));
        }

        static GameSession BuildSession(CardDatabase db)
        {
            var players = new List<PlayerState>
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

        static List<string> GiveSuitCards(GameSession session, CardDatabase db, Suit suit, int count)
        {
            var added = new List<string>(count);
            int idx = 0;
            foreach (var def in db.GetBySuit(suit))
            {
                if (idx >= count) break;
                var id = $"suit-{suit}-{idx}";
                var inst = new CardInstance(id, def.Id, CardZone.Spread, 0);
                session.RegisterCard(inst);
                session.Players[0].Spread.Add(id);
                added.Add(id);
                idx++;
            }
            return added;
        }

        [Test]
        public void Rank8_InSpread_Reduces_Sulphur_Cost_To_Two()
        {
            var db = LoadDb();
            var session = BuildSession(db);
            AddSpread(session, "eight", "minor.wands.eight.1");

            int min = CraftModifierService.GetMinimumCost(session, 0, ReagentType.Sulphur);
            Assert.AreEqual(2, min);
        }

        [Test]
        public void BuildV1_Adds_TwoPentacles_Salt_Path()
        {
            var db = LoadDb();
            var session = BuildSession(db);
            AddSpread(session, "build", "minor.pentacles.three.1");

            var cards = GiveSuitCards(session, db, Suit.Pentacles, 2);
            var option = CraftModifierService.FindMatchingOption(
                session, 0, ReagentType.Salt, cards, db);

            Assert.IsTrue(option.HasValue);
            Assert.AreEqual(2, option.Value.Cost);
            Assert.AreEqual(CraftPaymentMode.BuildSaltSuit, option.Value.Mode);
        }

        [Test]
        public void EmpressMark_Reduces_Marked_Reagent_Cost()
        {
            var db = LoadDb();
            var session = BuildSession(db);
            session.Players[0].EmpressMarkedReagents.Add(ReagentType.Vitriol);

            int min = CraftModifierService.GetMinimumCost(session, 0, ReagentType.Vitriol);
            Assert.AreEqual(2, min);
        }
    }
}
