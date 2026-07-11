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
    public sealed class ReversedCurseService_Tests
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
                new(1, PlayerColor.Blue)
            };
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

        static void AddSpreadCard(GameSession session, int playerId, string instanceId, string definitionId)
        {
            var inst = new CardInstance(instanceId, definitionId, CardZone.Spread, playerId);
            session.RegisterCard(inst);
            session.Players[playerId].Spread.Add(instanceId);
        }

        static void AddAdept(GameSession session, int playerId, string instanceId, ZodiacSign playerSign)
        {
            var inst = new CardInstance(instanceId, "major.adept.1", CardZone.Arcanum, playerId);
            session.RegisterCard(inst);
            session.Players[playerId].Arcanum.Add(instanceId);
            session.Players[playerId].CurrentSign = playerSign;
        }

        [Test]
        public void ReversedCurse_AlignmentNegates()
        {
            var db = LoadDb();
            var session = BuildSession(db);
            var player = session.Players[0];
            var def = db.GetById("minor.cups.four.1")!;

            session.Board.CosmicAgeSign = ZodiacSign.Aries;
            AddSpreadCard(session, 0, "cups4", "minor.cups.four.1");
            Assert.IsTrue(ReversedCurseService.IsCurseActive(session, player, "cups4", def));

            session.Board.CosmicAgeSign = ZodiacSign.Cancer;
            Assert.IsFalse(ReversedCurseService.IsCurseActive(session, player, "cups4", def));
        }

        [Test]
        public void HandCurse_IsIgnored()
        {
            var db = LoadDb();
            var session = BuildSession(db);
            var player = session.Players[0];
            var def = db.GetById("minor.cups.four.1")!;

            session.Board.CosmicAgeSign = ZodiacSign.Aries;
            var inst = new CardInstance("hand-curse", "minor.cups.four.1", CardZone.Hand, 0);
            session.RegisterCard(inst);
            player.Hand.Add("hand-curse");

            Assert.IsFalse(ReversedCurseService.IsCurseActive(session, player, "hand-curse", def));
        }

        [Test]
        public void MagicianResonant_NegatesAllReversed()
        {
            var db = LoadDb();
            var session = BuildSession(db);
            var player = session.Players[0];
            var def = db.GetById("minor.wands.five.1")!;

            session.Board.CosmicAgeSign = ZodiacSign.Aries;
            AddSpreadCard(session, 0, "wands5", "minor.wands.five.1");
            AddAdept(session, 0, "magician", ZodiacSign.Gemini);

            Assert.IsFalse(ReversedCurseService.IsCurseActive(session, player, "wands5", def));
            Assert.IsTrue(ReversedCurseService.MagicianResonantNegates(session, player));
        }

        [Test]
        public void CardDatabase_LoadsCurseFields()
        {
            var db = LoadDb();
            var def = db.GetById("minor.pentacles.four.1");
            Assert.IsNotNull(def);
            Assert.IsTrue(def!.IsCurse);
            Assert.AreEqual("5 of Swords", def.NullifiesCard);
        }
    }
}
