using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.Data.Loaders;
using NUnit.Framework;
using UnityEngine;

namespace Kismeta.Core.Tests
{
    public sealed class HarvestTableau_Tests
    {
        static CardDatabase LoadDb()
        {
            var path = Path.Combine(Application.dataPath,
                "_Project/Scripts/Data/Generated/cards.json");
            return CardDatabase.LoadFromJson(File.ReadAllText(path));
        }

        static GameSession BuildSession(CardDatabase db, FateCardResolver? fateResolver = null)
        {
            var players = new List<PlayerState>
            {
                new(0, PlayerColor.Red),
                new(1, PlayerColor.Blue)
            };
            var codexPath = Path.Combine(Application.dataPath, "_Project/Data/Resources/crucible-codex.json");
            var codexDb = CrucibleCodexDatabase.LoadFromJson(File.ReadAllText(codexPath));
            var spring = new SpringRules(db, 42, fateResolver: fateResolver);
            var rules = new GameRuleSet(
                db, codexDb,
                new GameSetupService(db, 42),
                spring,
                new CrucibleRules(db, codexDb, seed: 42),
                new CraftingRules(db),
                new WinterRules(db),
                new ActionValidator());
            return new GameSession("test", GameMode.Quickplay, players, rules, CrucibleBuildMode.Curated);
        }

        static void SeedDeck(GameSession session, params string[] instanceIds)
        {
            for (int i = instanceIds.Length - 1; i >= 0; i--)
            {
                var id = instanceIds[i];
                if (session.GetCard(id) == null)
                {
                    var inst = new CardInstance(id, "minor.cups.two.1", CardZone.Deck, -1);
                    session.RegisterCard(inst);
                }
                session.Board.CommonDeck.Push(id);
            }
        }

        [Test]
        public void BeginHarvest_CreatesActiveDeal()
        {
            var db = LoadDb();
            var session = BuildSession(db);
            session.Players[0].CurrentSign = ZodiacSign.Aries;
            session.Board.CosmicAgeSign = ZodiacSign.Libra;

            var result = session.Rules!.Harvest.BeginHarvest(session, 0);
            Assert.IsTrue(result.IsOk);
            Assert.NotNull(session.Board.ActiveHarvestDeal);
            Assert.AreEqual(0, session.Board.ActiveHarvestDeal!.PlayerId);
            Assert.Greater(session.Board.ActiveHarvestDeal.TotalDraws, 0);
        }

        [Test]
        public void DealNextHarvestCard_EmitsHarvestCardRoutedEvent()
        {
            var db = LoadDb();
            var session = BuildSession(db);
            SeedDeck(session, "draw-1", "draw-2", "draw-3");

            var events = new List<IGameEvent>();
            session.OnEvent += e => events.Add(e);

            Assert.IsTrue(session.Rules!.Harvest.BeginHarvest(session, 0).IsOk);
            int totalDraws = session.Board.ActiveHarvestDeal!.TotalDraws;
            Assert.IsTrue(session.Rules.Harvest.DealNextHarvestCard(session, 0));

            var routed = events.OfType<HarvestCardRoutedEvent>().SingleOrDefault();
            Assert.NotNull(routed);
            Assert.AreEqual(0, routed!.PlayerId);
            Assert.AreEqual("draw-1", routed.CardId);
            Assert.AreEqual(HarvestRouteTarget.Hand, routed.Target);
            Assert.AreEqual(totalDraws - 1, routed.Remaining);
            Assert.NotNull(session.Board.ActiveHarvestDeal);

            while (session.Rules.Harvest.DealNextHarvestCard(session, 0)) { }

            var last = events.OfType<HarvestCardRoutedEvent>().Last();
            Assert.AreEqual(0, last.Remaining);
            Assert.IsNull(session.Board.ActiveHarvestDeal);
        }

        [Test]
        public void ResolveDeath_ClearsAllHands_AndEmitsHandsClearedEvent()
        {
            var db = LoadDb();
            var session = BuildSession(db, new FateCardResolver(db));
            AddHand(session, 0, "h0");
            AddHand(session, 1, "h1");

            HarvestHandsClearedEvent? cleared = null;
            session.OnEvent += e => { if (e is HarvestHandsClearedEvent h) cleared = h; };

            var resolver = new FateCardResolver(db);
            Assert.IsTrue(resolver.Resolve(session, 0, "death-fate", 13));

            Assert.AreEqual(0, session.Players[0].Hand.Count);
            Assert.AreEqual(0, session.Players[1].Hand.Count);
            Assert.NotNull(cleared);
            Assert.AreEqual(0, cleared!.DrawerId);
        }

        [Test]
        public void DealNextHarvestCard_RemainingDecrementsPerCard()
        {
            var db = LoadDb();
            var session = BuildSession(db);
            SeedDeck(session, "draw-1", "draw-2", "draw-3");

            var events = new List<HarvestCardRoutedEvent>();
            session.OnEvent += e =>
            {
                if (e is HarvestCardRoutedEvent r) events.Add(r);
            };

            Assert.IsTrue(session.Rules!.Harvest.BeginHarvest(session, 0).IsOk);
            var deal = session.Board.ActiveHarvestDeal!;
            int initialRemaining = deal.Remaining;
            Assert.GreaterOrEqual(initialRemaining, 3);

            Assert.IsTrue(session.Rules.Harvest.DealNextHarvestCard(session, 0));
            Assert.AreEqual(initialRemaining - 1, session.Board.ActiveHarvestDeal!.Remaining);
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(initialRemaining - 1, events[0].Remaining);

            Assert.IsTrue(session.Rules.Harvest.DealNextHarvestCard(session, 0));
            Assert.AreEqual(initialRemaining - 2, session.Board.ActiveHarvestDeal!.Remaining);
            Assert.AreEqual(2, events.Count);
        }

        [Test]
        public void FateCard_RoutesToArcanum()
        {
            var db = LoadDb();
            var session = BuildSession(db, new FateCardResolver(db));
            var sunInst = new CardInstance("sun-draw", "major.fate.19", CardZone.Deck, -1);
            session.RegisterCard(sunInst);
            session.Board.CommonDeck.Push("sun-draw");

            HarvestCardRoutedEvent? routed = null;
            session.OnEvent += e => { if (e is HarvestCardRoutedEvent r) routed = r; };

            Assert.IsTrue(session.Rules!.Harvest.BeginHarvest(session, 0).IsOk);
            Assert.IsTrue(session.Rules.Harvest.DealNextHarvestCard(session, 0));

            Assert.NotNull(routed);
            Assert.AreEqual(HarvestRouteTarget.Arcanum, routed!.Target);
            Assert.Contains("sun-draw", session.Players[0].Arcanum);
        }

        [Test]
        public void AdeptCard_RoutesToAdeptLimbo()
        {
            var db = LoadDb();
            var session = BuildSession(db);
            var adeptInst = new CardInstance("adept-draw", "major.adept.1", CardZone.Deck, -1);
            session.RegisterCard(adeptInst);
            session.Board.CommonDeck.Push("adept-draw");

            HarvestCardRoutedEvent? routed = null;
            session.OnEvent += e => { if (e is HarvestCardRoutedEvent r) routed = r; };

            Assert.IsTrue(session.Rules!.Harvest.BeginHarvest(session, 0).IsOk);
            Assert.IsTrue(session.Rules.Harvest.DealNextHarvestCard(session, 0));

            Assert.NotNull(routed);
            Assert.AreEqual(HarvestRouteTarget.AdeptLimbo, routed!.Target);
            Assert.AreEqual(1, session.Board.PendingAdeptDecisions.Count);
            Assert.IsFalse(session.Players[0].Arcanum.Contains("adept-draw"));
        }

        [Test]
        public void DeathMidDeal_ClearsHand_ThenLaterDrawAddsCard()
        {
            var db = LoadDb();
            var fateResolver = new FateCardResolver(db);
            var session = BuildSession(db, fateResolver);
            AddHand(session, 0, "existing-hand");

            var deathInst = new CardInstance("death-draw", "major.fate.13", CardZone.Deck, -1);
            var minorInst = new CardInstance("after-death", "minor.cups.nine.1", CardZone.Deck, -1);
            session.RegisterCard(deathInst);
            session.RegisterCard(minorInst);
            session.Board.CommonDeck.Push("after-death");
            session.Board.CommonDeck.Push("death-draw");

            Assert.IsTrue(session.Rules!.Harvest.BeginHarvest(session, 0).IsOk);
            Assert.IsTrue(session.Rules.Harvest.DealNextHarvestCard(session, 0));
            Assert.AreEqual(0, session.Players[0].Hand.Count, "Death should clear hands.");

            Assert.IsTrue(session.Rules.Harvest.DealNextHarvestCard(session, 0));
            Assert.AreEqual(1, session.Players[0].Hand.Count, "Remaining harvest draw should add to hand.");
            // Death returns cleared hand cards to the deck; the next harvest draw may pull one back.
            Assert.IsTrue(
                session.Players[0].Hand.Contains("after-death")
                || session.Players[0].Hand.Contains("existing-hand"),
                "Next harvest draw should add a card from the deck.");
        }

        static void AddHand(GameSession session, int playerId, string instanceId)
        {
            var inst = new CardInstance(instanceId, "minor.wands.ace.1", CardZone.Hand, playerId);
            session.RegisterCard(inst);
            session.Players[playerId].Hand.Add(instanceId);
        }
    }
}
