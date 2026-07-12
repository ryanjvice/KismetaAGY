using System.Collections.Generic;
using System.IO;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.Data.Loaders;
using NUnit.Framework;
using UnityEngine;

namespace Kismeta.Core.Tests
{
    public sealed class ExchangeEventEmitter_Tests
    {
        static CardDatabase LoadDb()
        {
            var path = Path.Combine(Application.dataPath,
                "_Project/Scripts/Data/Generated/cards.json");
            return CardDatabase.LoadFromJson(File.ReadAllText(path));
        }

        static GameSession BuildSession(CardDatabase db, int playerCount = 2)
        {
            var players = new List<PlayerState>(playerCount);
            for (int i = 0; i < playerCount; i++)
                players.Add(new PlayerState(i, (PlayerColor)i));
            var codexPath = Path.Combine(Application.dataPath, "_Project/Data/Resources/crucible-codex.json");
            var codexDb = CrucibleCodexDatabase.LoadFromJson(File.ReadAllText(codexPath));
            var rules = new GameRuleSet(
                db, codexDb,
                new GameSetupService(db, 42),
                new SpringRules(db, 42),
                new CrucibleRules(db, codexDb, seed: 42),
                new CraftingRules(db),
                new WinterRules(db),
                new ActionValidator());
            return new GameSession("test", GameMode.Quickplay, players, rules, CrucibleBuildMode.Curated);
        }

        static void AddHandCard(GameSession session, int playerId, string instanceId, string definitionId)
        {
            var inst = new CardInstance(instanceId, definitionId, CardZone.Hand, playerId);
            session.RegisterCard(inst);
            session.Players[playerId].Hand.Add(instanceId);
        }

        static void AddAdept(GameSession session, int playerId, string instanceId)
        {
            var inst = new CardInstance(instanceId, "major.adept.8", CardZone.Arcanum, playerId);
            session.RegisterCard(inst);
            session.Players[playerId].Arcanum.Add(instanceId);
        }

        [Test]
        public void ResolveTower_EmitsFateTowerExchange()
        {
            var db = LoadDb();
            var session = BuildSession(db);
            AddAdept(session, 0, "adept-0");
            AddAdept(session, 1, "adept-1");

            PlayerExchangeEvent? exchange = null;
            session.OnEvent += e => { if (e is PlayerExchangeEvent pe) exchange = pe; };

            var resolver = new FateCardResolver(db);
            Assert.IsTrue(resolver.Resolve(session, 0, "tower-fate", 16));

            Assert.NotNull(exchange);
            Assert.AreEqual(ExchangeKind.FateTower, exchange!.Kind);
            Assert.GreaterOrEqual(exchange.Legs.Count, 1);
        }

        [Test]
        public void ResolveDeath_EmitsFateDeathExchange_AndClearsHands()
        {
            var db = LoadDb();
            var session = BuildSession(db);
            AddHandCard(session, 0, "hand-0", "minor.wands.ace.1");
            AddHandCard(session, 1, "hand-1", "minor.cups.ace.1");

            PlayerExchangeEvent? exchange = null;
            session.OnEvent += e => { if (e is PlayerExchangeEvent pe) exchange = pe; };

            var resolver = new FateCardResolver(db);
            Assert.IsTrue(resolver.Resolve(session, 0, "death-fate", 13));

            Assert.NotNull(exchange);
            Assert.AreEqual(ExchangeKind.FateDeath, exchange!.Kind);
            Assert.AreEqual(2, exchange.Legs.Count);
            Assert.AreEqual(0, session.Players[0].Hand.Count);
            Assert.AreEqual(0, session.Players[1].Hand.Count);
        }

        [Test]
        public void ResolveSun_EmitsFateSunExchange()
        {
            var db = LoadDb();
            var session = BuildSession(db);

            PlayerExchangeEvent? exchange = null;
            session.OnEvent += e => { if (e is PlayerExchangeEvent pe) exchange = pe; };

            var resolver = new FateCardResolver(db);
            Assert.IsTrue(resolver.Resolve(session, 0, "sun-fate", 19));

            Assert.NotNull(exchange);
            Assert.AreEqual(ExchangeKind.FateSun, exchange!.Kind);
            Assert.AreEqual(2, exchange.Legs.Count);
        }

        [Test]
        public void ResolveJudgement_NoCauldrons_EmitsEmptyLegContext()
        {
            var db = LoadDb();
            var session = BuildSession(db);

            PlayerExchangeEvent? exchange = null;
            session.OnEvent += e => { if (e is PlayerExchangeEvent pe) exchange = pe; };

            var resolver = new FateCardResolver(db);
            Assert.IsTrue(resolver.Resolve(session, 0, "judgement-fate", 20));

            Assert.NotNull(exchange);
            Assert.AreEqual(ExchangeKind.FateJudgement, exchange!.Kind);
            StringAssert.Contains("no lit cauldrons", exchange.ContextLine);
        }

        [Test]
        public void ResolveJudgement_LitCauldrons_EmitsDrawCount()
        {
            var db = LoadDb();
            var session = BuildSession(db);
            session.Players[0].LightCauldron(Suit.Wands);
            session.Players[0].LightCauldron(Suit.Cups);
            session.Board.CommonDeck.Clear();
            for (int i = 0; i < 2; i++)
            {
                var minor = new CardInstance($"jud-ex-{i}", "minor.cups.seven.1", CardZone.Deck, -1);
                session.RegisterCard(minor);
                session.Board.CommonDeck.Push(minor.InstanceId);
            }

            PlayerExchangeEvent? exchange = null;
            session.OnEvent += e => { if (e is PlayerExchangeEvent pe) exchange = pe; };

            var resolver = new FateCardResolver(db);
            Assert.IsTrue(resolver.Resolve(session, 0, "judgement-fate", 20));

            Assert.NotNull(exchange);
            Assert.AreEqual(ExchangeKind.FateJudgement, exchange!.Kind);
            StringAssert.Contains("drew 2", exchange.ContextLine);
        }

        [Test]
        public void ResolveWheel_EmitsFateWheelExchange()
        {
            var db = LoadDb();
            var session = BuildSession(db);
            AddHandCard(session, 0, "wheel-hand", "minor.swords.ace.1");

            PlayerExchangeEvent? exchange = null;
            session.OnEvent += e => { if (e is PlayerExchangeEvent pe) exchange = pe; };

            var resolver = new FateCardResolver(db);
            Assert.IsTrue(resolver.Resolve(session, 0, "wheel-fate", 10));

            Assert.NotNull(exchange);
            Assert.AreEqual(ExchangeKind.FateWheel, exchange!.Kind);
        }

        [Test]
        public void LoversEmptyDraw_EmitsContextOnlyExchange()
        {
            var db = LoadDb();
            var session = BuildSession(db);

            PlayerExchangeEvent? exchange = null;
            session.OnEvent += e => { if (e is PlayerExchangeEvent pe) exchange = pe; };

            var resolver = new FateCardResolver(db);
            var result = resolver.HandleLoversChoice(session, 0, drawCards: true,
                chosenReagent: ReagentType.Salt, chooserId: 1);

            Assert.IsTrue(result.IsOk, result.Message);
            Assert.NotNull(exchange);
            Assert.AreEqual(ExchangeKind.FateLovers, exchange!.Kind);
            StringAssert.Contains("no cards were available", exchange.ContextLine);
        }
    }
}
