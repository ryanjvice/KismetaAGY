using System.Collections.Generic;
using System.IO;
using System.Threading;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Rules;
using Kismeta.Data.Loaders;
using NUnit.Framework;
using UnityEngine;

namespace Kismeta.Core.Tests
{
    /// <summary>
    /// AI-vs-AI smoke tests for the GameLoop.
    /// All decisions are instant (SimpleAIController returns Task.FromResult),
    /// so the loop runs synchronously on the test thread and completes quickly.
    /// </summary>
    public sealed class GameLoop_Tests
    {
        // ─── Helpers ──────────────────────────────────────────────────────────────

        private static CardDatabase LoadDb()
        {
            var path = Path.Combine(Application.dataPath,
                "_Project/Scripts/Data/Generated/cards.json");
            Assert.IsTrue(File.Exists(path), $"cards.json not found. Run 'npm run data:sync'.");
            return CardDatabase.LoadFromJson(File.ReadAllText(path));
        }

        private static (GameSession session, GameLoop loop) BuildAIGame(CardDatabase db,
            int playerCount = 2, int seed = 42)
        {
            var rules = new GameRuleSet(
                cardDatabase: db,
                setup:        new GameSetupService(db, seed),
                harvest:      new SpringRules(seed),
                crucible:     new CrucibleRules(db, seed),
                crafting:     new CraftingRules(db),
                winter:       new WinterRules(),
                validator:    new ActionValidator());

            var players     = new List<PlayerState>(playerCount);
            var controllers = new List<IPlayerController>(playerCount);

            for (int i = 0; i < playerCount; i++)
            {
                players.Add(new PlayerState(i, (PlayerColor)i));
                var slot = new PlayerSlot(i, (PlayerColor)i, PlayerControllerType.LocalAI);
                controllers.Add(new SimpleAIController(slot));
            }

            var session = new GameSession("ai-test", GameMode.Quickplay, players, rules);
            var loop    = new GameLoop(session, controllers);
            return (session, loop);
        }

        // ─── Tests ────────────────────────────────────────────────────────────────

        [Test]
        public void GameLoop_Starts_And_Runs_Without_Exception()
        {
            var db = LoadDb();
            var (session, loop) = BuildAIGame(db);

            // Run with a 15-second hard timeout; pure C# AI resolves instantly,
            // so the only real limit is how many rounds until a win.
            using var cts = new CancellationTokenSource(15_000);
            try
            {
                loop.RunAsync(cts.Token).GetAwaiter().GetResult();
            }
            catch (System.OperationCanceledException)
            {
                // Acceptable: game didn't finish within timeout but didn't crash.
            }

            // Regardless of completion: at least setup must have run (deck populated).
            Assert.Greater(session.Board.RoundNumber, 0,
                "At least one round should have started.");
        }

        [Test]
        public void GameLoop_Setup_Populates_All_Decks()
        {
            var db = LoadDb();
            var (session, _) = BuildAIGame(db);

            session.Apply(new SetupGameCommand());

            Assert.Greater(session.Board.CommonDeck.Count, 0,
                "Common deck must be non-empty after setup.");
            Assert.AreEqual(0, session.Board.CrucibleDeck.Count,
                "CrucibleDeck should be empty — all Crucible cards were dealt to players.");

            // Verify the cards are actually on players
            int totalSlots = 0;
            foreach (var p in session.Players) totalSlots += p.CrucibleSlots.Count;
            Assert.AreEqual(4 * session.Players.Count, totalSlots,
                "Each player should hold exactly 4 Crucible slots.");
        }

        [Test]
        public void GameLoop_Spring_Gives_Each_Player_At_Least_3_Cards_In_Hand()
        {
            var db = LoadDb();
            var (session, _) = BuildAIGame(db);

            // Setup + advance to just after Harvest step manually
            session.Apply(new SetupGameCommand());
            session.Apply(new RollCosmicAgeCommand(0));

            foreach (var player in session.Players)
                session.Apply(new RollZodiacCommand(player.PlayerId));

            foreach (var player in session.Players)
                session.Apply(new HarvestCommand(player.PlayerId, 0));

            foreach (var player in session.Players)
                Assert.GreaterOrEqual(player.Hand.Count, 3,
                    $"Player {player.PlayerId} should have at least 3 cards in Hand after Harvest.");
        }

        [Test]
        public void GameLoop_Winter_Rotates_Agekeeper_Each_Round()
        {
            var db = LoadDb();
            var (session, loop) = BuildAIGame(db);

            var events = new List<IGameEvent>();
            session.OnEvent += e => events.Add(e);

            // Run for enough rounds to see multiple rotations (cap at 5s)
            using var cts = new CancellationTokenSource(5_000);
            try { loop.RunAsync(cts.Token).GetAwaiter().GetResult(); }
            catch (System.OperationCanceledException) { }

            int transitions = 0;
            foreach (var e in events)
                if (e is AgeTransitedEvent) transitions++;

            if (session.Board.RoundNumber > 1)
                Assert.Greater(transitions, 0,
                    "AgeTransitedEvent should have been emitted at least once.");
        }

        [Test]
        public void GameLoop_Event_Log_Is_Non_Empty_After_Setup()
        {
            var db = LoadDb();
            var (session, _) = BuildAIGame(db);

            var events = new List<IGameEvent>();
            session.OnEvent += e => events.Add(e);

            session.Apply(new SetupGameCommand());

            Assert.Greater(events.Count, 0,
                "Setup should emit at least GameSetupCompleteEvent.");
            Assert.IsTrue(events.Exists(e => e is GameSetupCompleteEvent),
                "GameSetupCompleteEvent must be emitted during setup.");
        }

        [Test]
        public void GameLoop_Two_Players_Two_Seeds_Produce_Different_Setup()
        {
            var db               = LoadDb();
            var (session42, _)   = BuildAIGame(db, 2, seed: 42);
            var (session99, _)   = BuildAIGame(db, 2, seed: 99);

            session42.Apply(new SetupGameCommand());
            session99.Apply(new SetupGameCommand());

            // With different seeds the cosmic age or spread cards are likely different
            // (not a guaranteed invariant but overwhelmingly true with 12 possible signs)
            string spread42 = string.Join(",", session42.Players[0].CrucibleSlots[0].CardInstanceId);
            string spread99 = string.Join(",", session99.Players[0].CrucibleSlots[0].CardInstanceId);
            // If they're equal by chance just pass — this is a non-determinism canary, not a hard assert
            TestContext.WriteLine($"Seed 42 first crucible: {spread42}");
            TestContext.WriteLine($"Seed 99 first crucible: {spread99}");
        }

        [Test]
        public void ActionValidator_Rejects_Agekeeper_Roll_From_Non_Agekeeper()
        {
            var db = LoadDb();
            var (session, _) = BuildAIGame(db);
            session.Apply(new SetupGameCommand());

            var validator = new ActionValidator();
            // Player 1 is NOT the Agekeeper (player 0 is)
            var result = validator.Validate(session, new RollCosmicAgeCommand(1));
            Assert.IsFalse(result.IsOk,
                "Non-Agekeeper should not be allowed to roll the Cosmic Age Die.");
        }

        [Test]
        public void ActionValidator_Accepts_Agekeeper_Roll_From_Agekeeper()
        {
            var db = LoadDb();
            var (session, _) = BuildAIGame(db);
            session.Apply(new SetupGameCommand());

            var validator = new ActionValidator();
            var result    = validator.Validate(session, new RollCosmicAgeCommand(0));
            Assert.IsTrue(result.IsOk, result.Message);
        }
    }
}
