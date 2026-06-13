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
    /// <summary>
    /// Edit Mode tests for all M2 rule services: Setup, Spring, Crucible, Crafting, Winter.
    /// Uses a seeded RNG so results are deterministic.
    /// </summary>
    public sealed class RuleServices_Tests
    {
        // ─── Shared fixtures ──────────────────────────────────────────────────────

        private static CardDatabase LoadDb()
        {
            var path = Path.Combine(Application.dataPath,
                "_Project/Scripts/Data/Generated/cards.json");
            Assert.IsTrue(File.Exists(path), $"cards.json not found at {path}. Run 'npm run data:sync'.");
            return CardDatabase.LoadFromJson(File.ReadAllText(path));
        }

        private static GameRuleSet BuildRules(CardDatabase db, int seed = 42) => new GameRuleSet(
            cardDatabase: db,
            setup:        new GameSetupService(db, seed),
            harvest:      new SpringRules(seed),
            crucible:     new CrucibleRules(db, seed),
            crafting:     new CraftingRules(db),
            winter:       new WinterRules(),
            validator:    new ActionValidator());

        private static GameSession BuildSession(CardDatabase db, int playerCount = 2, int seed = 42)
        {
            var players = new List<PlayerState>(playerCount);
            for (int i = 0; i < playerCount; i++)
                players.Add(new PlayerState(i, (PlayerColor)i));
            return new GameSession("test", GameMode.Quickplay, players, BuildRules(db, seed));
        }

        /// <summary>Run Setup and return the configured session.</summary>
        private static GameSession SetupSession(CardDatabase db, int playerCount = 2, int seed = 42)
        {
            var session = BuildSession(db, playerCount, seed);
            var result  = session.Apply(new SetupGameCommand());
            Assert.IsTrue(result.IsOk, $"Setup failed: {result.Message}");
            return session;
        }

        // ─── GameSetupService tests ────────────────────────────────────────────────

        [Test]
        public void Setup_Populates_CommonDeck()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            Assert.Greater(session.Board.CommonDeck.Count, 0,
                "Common deck must be populated after setup.");
        }

        [Test]
        public void Setup_Deals_Four_CrucibleSlots_Per_Player()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            foreach (var player in session.Players)
                Assert.AreEqual(4, player.CrucibleSlots.Count,
                    $"Player {player.PlayerId} should have 4 Crucible slots.");
        }

        [Test]
        public void Setup_CrucibleSlots_Are_Dormant_With_Coal()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            foreach (var player in session.Players)
                foreach (var slot in player.CrucibleSlots)
                {
                    Assert.AreEqual(CrucibleCardState.Dormant, slot.State);
                    Assert.IsTrue(slot.HasCoal, "Each slot should start with a Coal.");
                }
        }

        [Test]
        public void Setup_Deals_Starter_Spread_Card_To_Each_Player()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            foreach (var player in session.Players)
                Assert.GreaterOrEqual(player.Spread.Count, 1,
                    $"Player {player.PlayerId} must have at least 1 Spread card.");
        }

        [Test]
        public void Setup_Starter_Spread_Card_Is_Not_Major_Arcana()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            foreach (var player in session.Players)
            {
                var id   = player.Spread[0];
                var inst = session.GetCard(id);
                Assert.IsNotNull(inst);
                var def = db.GetById(inst!.DefinitionId);
                Assert.IsNotNull(def);
                Assert.IsFalse(def!.IsMajorArcana,
                    "Starter Spread card must not be a Major Arcana.");
            }
        }

        [Test]
        public void Setup_Sets_Player0_As_Agekeeper()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            Assert.IsTrue(session.Players[0].IsAgekeeper);
            for (int i = 1; i < session.Players.Count; i++)
                Assert.IsFalse(session.Players[i].IsAgekeeper);
        }

        [Test]
        public void Setup_Registers_All_Cards_In_Session()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            Assert.AreEqual(db.Count, session.Cards.Count,
                "Session should contain one instance per card definition.");
        }

        // ─── SpringRules tests ─────────────────────────────────────────────────────

        [Test]
        public void RollCosmicAge_Sets_NonNone_Sign()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            session.Apply(new RollCosmicAgeCommand(0));
            Assert.AreNotEqual(ZodiacSign.None, session.Board.CosmicAgeSign);
        }

        [Test]
        public void RollCosmicAge_Emits_CosmicAgeSetEvent()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            IGameEvent? evt = null;
            session.OnEvent += e => { if (e is CosmicAgeSetEvent) evt = e; };
            session.Apply(new RollCosmicAgeCommand(0));
            Assert.IsNotNull(evt);
        }

        [Test]
        public void RollZodiac_Sets_Player_Sign()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            session.Apply(new RollZodiacCommand(0));
            Assert.AreNotEqual(ZodiacSign.None, session.Players[0].CurrentSign);
        }

        [Test]
        public void Harvest_Adds_Cards_To_Player_Hand()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            int handBefore = session.Players[0].Hand.Count;
            session.Apply(new HarvestCommand(0, 0));
            Assert.Greater(session.Players[0].Hand.Count, handBefore);
        }

        [Test]
        public void Harvest_Count_At_Least_3()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            int handBefore = session.Players[0].Hand.Count;
            session.Apply(new HarvestCommand(0, 0));
            Assert.GreaterOrEqual(session.Players[0].Hand.Count - handBefore, 3);
        }

        [Test]
        public void Harvest_Count_Bonus_For_Matching_Sign()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            // Force Cosmic Age and player sign to match → +3 bonus = 6 total
            session.Board.CosmicAgeSign       = ZodiacSign.Aries;
            session.Players[0].CurrentSign    = ZodiacSign.Aries;
            int handBefore = session.Players[0].Hand.Count;
            session.Apply(new HarvestCommand(0, 0));
            Assert.AreEqual(6, session.Players[0].Hand.Count - handBefore,
                "Same Sign should yield base 3 + alignment 3 = 6 cards.");
        }

        [Test]
        public void Commune_Moves_Cards_To_Correct_Zones()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            // Give player a known Hand card
            var inst = new CardInstance("test-commune-card", "minor.cups.seven.1", CardZone.Hand, 0);
            session.RegisterCard(inst);
            session.Players[0].Hand.Add(inst.InstanceId);

            var allCards = new List<string>(session.Players[0].Spread);
            allCards.Add(inst.InstanceId); // put Hand card into Spread
            var cmd = new CommuneCommand(0, allCards, System.Array.Empty<string>());
            var result = session.Apply(cmd);

            Assert.IsTrue(result.IsOk, result.Message);
            Assert.IsTrue(session.Players[0].Spread.Contains(inst.InstanceId));
            Assert.IsFalse(session.Players[0].Hand.Contains(inst.InstanceId));
        }

        [Test]
        public void Commune_Fails_If_Cards_Not_Owned()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            // Try to commune a card belonging to player 1
            var inst = new CardInstance("alien-card", "minor.cups.ace.1", CardZone.Spread, 1);
            session.RegisterCard(inst);
            session.Players[1].Spread.Add(inst.InstanceId);

            var cmd = new CommuneCommand(0,
                new List<string> { inst.InstanceId },
                System.Array.Empty<string>());
            var result = session.Apply(cmd);
            Assert.IsFalse(result.IsOk);
        }

        // ─── CrucibleRules tests ───────────────────────────────────────────────────

        [Test]
        public void Activate_Fails_If_Slot_Not_Dormant()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            // Pre-activate slot 0 manually
            session.Players[0].CrucibleSlots[0].Activate();
            // Give cards
            GivePlayerCards(session, 0, 3);

            var cards  = new List<string>(session.Players[0].Spread);
            var result = session.Apply(new ActivateCrucibleCommand(0, 0, cards));
            Assert.IsFalse(result.IsOk, "Activating a non-Dormant slot should fail.");
        }

        [Test]
        public void Activate_Fails_With_Fewer_Than_3_Cards()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            var result  = session.Apply(new ActivateCrucibleCommand(0, 0,
                new List<string> { "x", "y" }));
            Assert.IsFalse(result.IsOk);
        }

        [Test]
        public void Activate_Success_Makes_Slot_Active()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            GivePlayerCards(session, 0, 3);
            var cards  = session.Players[0].Spread.GetRange(0, 3);
            var result = session.Apply(new ActivateCrucibleCommand(0, 0, cards));
            Assert.IsTrue(result.IsOk, result.Message);
            Assert.AreEqual(CrucibleCardState.Active, session.Players[0].CrucibleSlots[0].State);
        }

        [Test]
        public void Fire_Fails_If_Slot_Not_Active()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            var result  = session.Apply(new FireStoneCommand(0, 0));
            Assert.IsFalse(result.IsOk, "Firing a Dormant slot should fail.");
        }

        [Test]
        public void Fire_Fails_When_Stone_In_Stasis()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            ActivateSlot(session, db, 0, 0);
            session.Players[0].StoneState = StoneState.Stasis;
            var result = session.Apply(new FireStoneCommand(0, 0));
            Assert.IsFalse(result.IsOk);
        }

        [Test]
        public void Temper_Fails_If_No_Fired_Slot_From_Previous_Round()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            // Stone is Tempering (default), no slot has been fired
            var result  = session.Apply(new TemperCommand(0));
            Assert.IsFalse(result.IsOk);
        }

        [Test]
        public void Temper_Advances_Stone_And_Discards_Slot()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            ActivateSlot(session, db, 0, 0);
            // Fire in a "previous round"
            session.Players[0].CrucibleSlots[0].Fire(0);
            session.Players[0].StoneState = StoneState.Forging;
            session.Board.RoundNumber = 2; // current round > FiredAtRound(0)

            var before = session.Players[0].StonePosition;
            var result = session.Apply(new TemperCommand(0));
            Assert.IsTrue(result.IsOk, result.Message);
            Assert.AreEqual(before.Advance().Value, session.Players[0].StonePosition.Value);
            Assert.AreEqual(CrucibleCardState.Discarded, session.Players[0].CrucibleSlots[0].State);
        }

        [Test]
        public void Opposition_Sends_Loser_To_Stasis()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            session.Players[1].StoneState = StoneState.Forging;
            var result = session.Apply(new InitiateOppositionCommand(0, 1));
            Assert.IsTrue(result.IsOk, result.Message);
            // One of the two players must be in Stasis
            bool someoneInStasis = session.Players[0].StoneState == StoneState.Stasis
                                || session.Players[1].StoneState == StoneState.Stasis;
            Assert.IsTrue(someoneInStasis);
        }

        // ─── CraftingRules tests ───────────────────────────────────────────────────

        [Test]
        public void Craft_Salt_With_3_Cards_Succeeds()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            GivePlayerCards(session, 0, 3);
            int saltBefore = session.Players[0].GetReagent(ReagentType.Salt);
            var cards  = session.Players[0].Spread.GetRange(0, 3);
            var result = session.Apply(new CraftReagentCommand(0, ReagentType.Salt, cards));
            Assert.IsTrue(result.IsOk, result.Message);
            Assert.AreEqual(saltBefore + 1, session.Players[0].GetReagent(ReagentType.Salt));
        }

        [Test]
        public void Craft_Salt_Fails_With_Fewer_Than_3_Cards()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            var result  = session.Apply(new CraftReagentCommand(0, ReagentType.Salt,
                new List<string> { "a", "b" }));
            Assert.IsFalse(result.IsOk);
        }

        [Test]
        public void Craft_Elemental_Fails_Without_Lit_Cauldron()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            // Give 3 Wands cards (for Sulphur)
            GivePlayerWandsCards(session, db, 0, 3);
            var cards  = session.Players[0].Spread.GetRange(0, 3);
            var result = session.Apply(new CraftReagentCommand(0, ReagentType.Sulphur, cards));
            Assert.IsFalse(result.IsOk, "Should fail without a lit Fire Cauldron.");
        }

        [Test]
        public void Craft_Elemental_Succeeds_With_Lit_Cauldron()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            GivePlayerWandsCards(session, db, 0, 3);
            session.Players[0].LightCauldron(Suit.Wands);
            var cards  = session.Players[0].Spread.GetRange(0, 3);
            var result = session.Apply(new CraftReagentCommand(0, ReagentType.Sulphur, cards));
            Assert.IsTrue(result.IsOk, result.Message);
            Assert.AreEqual(1, session.Players[0].GetReagent(ReagentType.Sulphur));
        }

        // ─── WinterRules tests ─────────────────────────────────────────────────────

        [Test]
        public void EnforceLimits_Trims_Spread_To_5()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            GivePlayerCards(session, 0, 8); // gives 8+ spread cards
            Assert.Greater(session.Players[0].Spread.Count, 5, "Setup check: player should have >5 cards.");
            session.Apply(new EnforceCardLimitsCommand());
            Assert.LessOrEqual(session.Players[0].Spread.Count, 5);
        }

        [Test]
        public void Transit_Increments_Round_Number()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            int before  = session.Board.RoundNumber;
            session.Apply(new TransitAgeCommand());
            Assert.AreEqual(before + 1, session.Board.RoundNumber);
        }

        [Test]
        public void Transit_Rotates_Agekeeper()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            Assert.IsTrue(session.Players[0].IsAgekeeper);
            session.Apply(new TransitAgeCommand());
            Assert.IsFalse(session.Players[0].IsAgekeeper);
            Assert.IsTrue(session.Players[1].IsAgekeeper);
        }

        [Test]
        public void Transit_Reshuffles_Discard_Into_Deck()
        {
            var db      = LoadDb();
            var session = SetupSession(db);
            // Move all deck cards to discard
            while (session.Board.CommonDeck.Count > 0)
                session.Board.CommonDiscard.Add(session.Board.CommonDeck.Pop());
            int discardCount = session.Board.CommonDiscard.Count;
            session.Apply(new TransitAgeCommand());
            Assert.AreEqual(0, session.Board.CommonDiscard.Count);
            Assert.AreEqual(discardCount, session.Board.CommonDeck.Count);
        }

        // ─── Private helpers ──────────────────────────────────────────────────────

        /// <summary>Register N arbitrary minor-arcana-like cards and put them in player's Spread.</summary>
        private static void GivePlayerCards(GameSession session, int playerId, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var id   = $"test-give-{playerId}-{i}";
                var inst = new CardInstance(id, "minor.cups.seven.1", CardZone.Spread, playerId);
                session.RegisterCard(inst);
                session.Players[playerId].Spread.Add(id);
            }
        }

        /// <summary>Register N Wands cards (for Sulphur crafting tests).</summary>
        private static void GivePlayerWandsCards(GameSession session, CardDatabase db,
            int playerId, int count)
        {
            int idx = 0;
            foreach (var def in db.GetBySuit(Suit.Wands))
            {
                if (idx >= count) break;
                var id   = $"test-wands-{playerId}-{idx}";
                var inst = new CardInstance(id, def.Id, CardZone.Spread, playerId);
                session.RegisterCard(inst);
                session.Players[playerId].Spread.Add(id);
                idx++;
            }
        }

        /// <summary>
        /// Activates slot <paramref name="slotIdx"/> by giving the player 3 dummy cards
        /// and applying ActivateCrucibleCommand.
        /// </summary>
        private static void ActivateSlot(GameSession session, CardDatabase db, int playerId, int slotIdx)
        {
            GivePlayerCards(session, playerId, 3);
            var cards  = session.Players[playerId].Spread.GetRange(0, 3);
            var result = session.Apply(new ActivateCrucibleCommand(playerId, slotIdx, cards));
            Assume.That(result.IsOk, $"ActivateSlot helper failed: {result.Message}");
        }
    }
}
