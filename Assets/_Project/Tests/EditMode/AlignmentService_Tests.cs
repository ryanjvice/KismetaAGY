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
    public sealed class AlignmentService_Tests
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
            var align = new AlignmentService(db);
            var rules = new GameRuleSet(
                db, codexDb,
                new GameSetupService(db, 42),
                new SpringRules(db, 42),
                new CrucibleRules(db, codexDb, alignmentService: align, seed: 42),
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

        [Test]
        public void Rank10WildSuit_ScoresHigherThanNativeSuitWhenAnotherSuitMatches()
        {
            var db = LoadDb();
            var def = db.GetById("minor.cups.ten.1");
            Assert.NotNull(def);

            var reference = ZodiacSign.Libra;
            int native = AlignmentService.ScoreCard(def!.Suit, def.Planet, reference);
            int wild = AlignmentService.ScoreCardForOpposition(def, reference);

            Assert.Less(native, wild, "Wild suit should beat native water suit vs Libra (air).");
            Assert.AreEqual(1, wild);
        }

        [Test]
        public void Rank10WildSuit_InSpread_IncreasesOppositionAlignment()
        {
            var db = LoadDb();
            var session = BuildSession(db);
            session.Board.CosmicAgeSign = ZodiacSign.Libra;
            session.Players[0].CurrentSign = ZodiacSign.None;
            AddSpreadCard(session, 0, "ten-cups", "minor.cups.ten.1");

            var service = new AlignmentService(db);
            int total = service.CalculateAlignmentPoints(session, 0, session.Board.CosmicAgeSign);

            Assert.AreEqual(1, total);
        }

        [Test]
        public void Rank10WildSuit_PlanetTier_WhenSaturnMatchesAgePlanet()
        {
            var db = LoadDb();
            var def = db.GetById("minor.wands.ten.1");
            Assert.NotNull(def);

            var reference = ZodiacSign.Capricorn;
            int wild = AlignmentService.ScoreCardForOpposition(def!, reference);

            Assert.AreEqual(2, wild, "Saturn on rank 10 should score planet tier vs Capricorn.");
        }
    }
}
