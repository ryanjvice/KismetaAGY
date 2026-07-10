using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Rules;
using Kismeta.Core.Views;
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

        private static CrucibleCodexDatabase LoadCodexDb()
        {
            var path = Path.Combine(Application.dataPath,
                "_Project/Data/Resources/crucible-codex.json");
            Assert.IsTrue(File.Exists(path), $"crucible-codex.json not found at {path}.");
            return CrucibleCodexDatabase.LoadFromJson(File.ReadAllText(path));
        }

        private static GameRuleSet BuildRules(CardDatabase db, CrucibleCodexDatabase codexDb, int seed = 42) =>
            new GameRuleSet(
                cardDatabase:  db,
                codexDatabase: codexDb,
                setup:         new GameSetupService(db, seed),
                harvest:       new SpringRules(db, seed),
                crucible:      new CrucibleRules(db, codexDb, seed: seed),
                crafting:      new CraftingRules(db),
                winter:        new WinterRules(db),
                validator:     new ActionValidator(),
                combat:        new CombatRules(seed),
                trade:         new TradeService(db));

        private static GameSession BuildSession(CardDatabase db, CrucibleCodexDatabase codexDb,
            int playerCount = 2, int seed = 42, GameMode mode = GameMode.Quickplay,
            CrucibleBuildMode crucibleBuild = CrucibleBuildMode.Curated)
        {
            var players = new List<PlayerState>(playerCount);
            for (int i = 0; i < playerCount; i++)
                players.Add(new PlayerState(i, (PlayerColor)i));
            return new GameSession("test", mode, players, BuildRules(db, codexDb, seed), crucibleBuild);
        }

        /// <summary>Run Setup and return the configured session.</summary>
        private static GameSession SetupSession(CardDatabase db, CrucibleCodexDatabase codexDb,
            int playerCount = 2, int seed = 42, GameMode mode = GameMode.Quickplay,
            CrucibleBuildMode crucibleBuild = CrucibleBuildMode.Curated, int firstAgekeeperId = 0)
        {
            var session = BuildSession(db, codexDb, playerCount, seed, mode, crucibleBuild);
            session.FirstAgekeeperPlayerId = firstAgekeeperId;
            var result  = session.Apply(new SetupGameCommand());
            Assert.IsTrue(result.IsOk, $"Setup failed: {result.Message}");
            return session;
        }

        private static void SetSeason(GameSession session, Season season) =>
            session.Phase.SetSeason(season);

        private static void AssertInventoryConsistent(GameSession session, string? context = null)
        {
            var result = SessionInventoryAudit.Audit(session, context);
            Assert.IsTrue(result.IsConsistent,
                context == null
                    ? string.Join("; ", result.Violations)
                    : $"{context}: {string.Join("; ", result.Violations)}");
        }

        // ─── GameSetupService tests ────────────────────────────────────────────────

        [Test]
        public void Setup_Populates_CommonDeck()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            Assert.Greater(session.Board.CommonDeck.Count, 0,
                "Common deck must be populated after setup.");
        }

        [Test]
        public void Setup_Deals_Four_CrucibleSlots_Per_Player()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            foreach (var player in session.Players)
                Assert.AreEqual(4, player.CrucibleSlots.Count,
                    $"Player {player.PlayerId} should have 4 Crucible slots.");
        }

        [Test]
        public void Setup_CrucibleSlots_Are_Dormant_With_Coal()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
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
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            foreach (var player in session.Players)
                Assert.GreaterOrEqual(player.Spread.Count, 1,
                    $"Player {player.PlayerId} must have at least 1 Spread card.");
        }

        [Test]
        public void Setup_Starter_Spread_Card_Is_Not_Major_Arcana()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
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
        public void Setup_Sets_Designated_Agekeeper()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb, firstAgekeeperId: 1);
            Assert.IsFalse(session.Players[0].IsAgekeeper);
            Assert.IsTrue(session.Players[1].IsAgekeeper);
            for (int i = 2; i < session.Players.Count; i++)
                Assert.IsFalse(session.Players[i].IsAgekeeper);
        }

        [Test]
        public void AgekeeperContest_SeededWinner_IsDeterministic()
        {
            var first = AgekeeperContestService.Resolve(3, new System.Random(99));
            var again = AgekeeperContestService.Resolve(3, new System.Random(99));
            Assert.AreEqual(first.WinnerPlayerId, again.WinnerPlayerId);
            Assert.AreEqual(1, first.Rounds.Count);
            Assert.AreEqual(3, first.FinalRolls.Count);
        }

        [Test]
        public void AgekeeperContest_Tie_RerollsUntilSingleWinner()
        {
            var rng = new SeededContestRng(6, 6, 3, 8, 4);
            var result = AgekeeperContestService.Resolve(3, rng);
            Assert.AreEqual(0, result.WinnerPlayerId);
            Assert.AreEqual(2, result.Rounds.Count);
            Assert.AreEqual(3, result.Rounds[0].Count);
            Assert.AreEqual(2, result.Rounds[1].Count);
            Assert.AreEqual(2, result.FinalRolls.Count);
        }

        [Test]
        public void AgekeeperContest_AllPlayersRollInFirstRound()
        {
            for (int playerCount = 2; playerCount <= 4; playerCount++)
            {
                var result = AgekeeperContestService.Resolve(playerCount, new System.Random(42 + playerCount));
                Assert.GreaterOrEqual(result.Rounds.Count, 1);
                Assert.AreEqual(playerCount, result.Rounds[0].Count);
                var seen = new HashSet<int>();
                foreach (var roll in result.Rounds[0])
                    seen.Add(roll.PlayerId);
                for (int id = 0; id < playerCount; id++)
                    Assert.IsTrue(seen.Contains(id), $"Player {id} missing from round 1.");
            }
        }

        [Test]
        public void Setup_Curated_Standard_2p_Builds_Eight_Crucible_Cards()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb, playerCount: 2, mode: GameMode.Standard);
            int total = 0;
            foreach (var player in session.Players)
                total += player.CrucibleSlots.Count;
            Assert.AreEqual(8, total);
            Assert.AreEqual(0, session.Board.CrucibleDeck.Count);
        }

        [Test]
        public void Setup_Fates_2p_Deals_Four_Per_Player_From_Random_Pool()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb, playerCount: 2,
                crucibleBuild: CrucibleBuildMode.LetTheFatesDecide, seed: 7);
            foreach (var player in session.Players)
                Assert.AreEqual(4, player.CrucibleSlots.Count);
            Assert.AreEqual(0, session.Board.CrucibleDeck.Count);
        }

        [Test]
        public void Setup_Registers_All_Cards_In_Session()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);

            int kismetaCount = 0;
            foreach (var def in db.GetAll())
                if (def.Deck == Deck.Kismeta) kismetaCount++;

            int inPlayCrucible = 0;
            foreach (var player in session.Players)
                inPlayCrucible += player.CrucibleSlots.Count;

            Assert.AreEqual(kismetaCount + inPlayCrucible, session.Cards.Count,
                "Session should register all Kismeta cards plus in-play Crucible cards only.");
        }

        // ─── SpringRules tests ─────────────────────────────────────────────────────

        [Test]
        public void RollCosmicAge_Sets_NonNone_Sign()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            session.Apply(new RollCosmicAgeCommand(0));
            Assert.AreNotEqual(ZodiacSign.None, session.Board.CosmicAgeSign);
        }

        [Test]
        public void RollCosmicAge_Emits_CosmicAgeSetEvent()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            IGameEvent? evt = null;
            session.OnEvent += e => { if (e is CosmicAgeSetEvent) evt = e; };
            session.Apply(new RollCosmicAgeCommand(0));
            Assert.IsNotNull(evt);
        }

        [Test]
        public void RollZodiac_Sets_Player_Sign()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            session.Apply(new RollZodiacCommand(0));
            Assert.AreNotEqual(ZodiacSign.None, session.Players[0].CurrentSign);
        }

        [Test]
        public void Harvest_Adds_Cards_To_Player_Hand()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            int handBefore = session.Players[0].Hand.Count;
            session.Apply(new HarvestCommand(0, 0));
            Assert.Greater(session.Players[0].Hand.Count, handBefore);
        }

        [Test]
        public void Harvest_Count_At_Least_3()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var before  = CaptureHarvestSnapshot(session, 0);
            session.Apply(new HarvestCommand(0, 0));
            Assert.GreaterOrEqual(HarvestCardsReceived(session, 0, before), 3,
                "Harvest deals at least 3 cards (Hand, Arcanum, or pending Adept).");
        }

        [Test]
        public void Harvest_Count_Bonus_For_Matching_Sign()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            // Use player 1 (not Agekeeper) so the Agekeeper's Boon doesn't apply.
            // Ensure the Agekeeper (player 0) does NOT match the Cosmic Age sign.
            session.Board.CosmicAgeSign    = ZodiacSign.Aries;
            session.Players[1].CurrentSign = ZodiacSign.Aries;  // matches → +3
            session.Players[0].CurrentSign = ZodiacSign.Taurus; // Agekeeper, no match → no Boon
            var before = CaptureHarvestSnapshot(session, 1);
            session.Apply(new HarvestCommand(1, 0));
            Assert.AreEqual(6, HarvestCardsReceived(session, 1, before),
                "Same Sign (non-Agekeeper, no Boon) should yield base 3 + alignment 3 = 6 cards dealt.");
        }

        [Test]
        public void HarvestBreakdown_Total_Matches_CalculateHarvestCount()
        {
            var db      = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            session.Apply(new RollCosmicAgeCommand(0));
            session.Apply(new RollZodiacCommand(0));

            var breakdown = HarvestBreakdownService.Build(session, 0);
            int expected  = session.Rules!.Harvest.CalculateHarvestCount(session, 0);

            Assert.AreEqual(expected, breakdown.Total);
            Assert.AreEqual(HarvestBreakdownService.BaseDraw + breakdown.BonusSubtotal + breakdown.Boon,
                breakdown.Total);
        }

        [Test]
        public void HarvestBreakdown_Omits_NoMatch_Sources_Includes_Base()
        {
            var db      = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            session.Board.CosmicAgeSign = ZodiacSign.Scorpio;
            session.Players[0].CurrentSign = ZodiacSign.Taurus;

            var breakdown = HarvestBreakdownService.Build(session, 0);

            var hasBase = false;
            var hasZodiacDie = false;
            foreach (var row in breakdown.Sources)
            {
                if (row.Title == "Base harvest") hasBase = true;
                if (row.Title.StartsWith("Zodiac die:")) hasZodiacDie = true;
            }

            Assert.IsTrue(hasBase, "Base harvest row should always appear.");
            Assert.IsFalse(hasZodiacDie, "Non-matching zodiac die should not appear in sources.");
        }

        [Test]
        public void Commune_Moves_Cards_To_Correct_Zones()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
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
        public void Commune_Allows_Spread_Above_Winter_Limit()
        {
            var db      = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            GivePlayerCards(session, 0, 7);

            var spreadIds = new List<string>(session.Players[0].Spread);
            int expectedCount = spreadIds.Count;
            var result = session.Apply(new CommuneCommand(0, spreadIds, System.Array.Empty<string>()));

            Assert.IsTrue(result.IsOk, result.Message);
            Assert.AreEqual(expectedCount, session.Players[0].Spread.Count);
            Assert.Greater(expectedCount, WinterRules.SpreadLimit);
        }

        [Test]
        public void Commune_Fails_If_Cards_Not_Owned()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
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
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            // Pre-activate slot 0 manually (bypasses rules — direct state mutation).
            session.Players[0].CrucibleSlots[0].Activate();
            session.Players[0].AssignedCodex = CodexVariant.A;
            GiveMarsCards(session, 0, 3);
            SetSeason(session, Season.Autumn);

            var cards  = LastSpreadCards(session.Players[0], 3);
            var result = session.Apply(new ActivateCrucibleCommand(0, 0, cards));
            Assert.IsFalse(result.IsOk, "Activating a non-Dormant slot should fail.");
            StringAssert.Contains("Dormant", result.Message);
        }

        [Test]
        public void Activate_Fails_With_Fewer_Than_3_Cards()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            SetSeason(session, Season.Autumn);
            var result  = session.Apply(new ActivateCrucibleCommand(0, 0,
                new List<string> { "x", "y" }));
            Assert.IsFalse(result.IsOk);
            StringAssert.Contains("Spread", result.Message);
        }

        [Test]
        public void Activate_Success_Makes_Slot_Active()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            // Force Codex A so slot 0 = "Any Three Mars". Give 3 Mars cards (minor.cups.seven.1).
            session.Players[0].AssignedCodex = CodexVariant.A;
            GiveMarsCards(session, 0, 3);
            SetSeason(session, Season.Autumn);
            var cards  = LastSpreadCards(session.Players[0], 3);
            var result = session.Apply(new ActivateCrucibleCommand(0, 0, cards));
            Assert.IsTrue(result.IsOk, result.Message);
            Assert.AreEqual(CrucibleCardState.Active, session.Players[0].CrucibleSlots[0].State);
        }

        [Test]
        public void Fire_Fails_If_Slot_Not_Active()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            SetSeason(session, Season.Autumn);
            var result  = session.Apply(new FireStoneCommand(0, 0));
            Assert.IsFalse(result.IsOk, "Firing a Dormant slot should fail.");
            StringAssert.Contains("Active", result.Message);
        }

        [Test]
        public void Fire_Fails_When_Stone_In_Stasis()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            ActivateSlot(session, db, 0, 0);
            session.Players[0].StoneState = StoneState.Stasis;
            SetSeason(session, Season.Autumn);
            var result = session.Apply(new FireStoneCommand(0, 0));
            Assert.IsFalse(result.IsOk);
        }

        [Test]
        public void Temper_Fails_If_No_Fired_Slot_From_Previous_Round()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            // Stone is Tempering (default), no slot has been fired
            var result  = session.Apply(new TemperCommand(0));
            Assert.IsFalse(result.IsOk);
        }

        [Test]
        public void Temper_Advances_Stone_And_Discards_Slot()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            ActivateSlot(session, db, 0, 0);
            var player = session.Players[0];
            // Fire in a "previous round" — stone must reach a Forge position (Fire advances from Mantle).
            player.CrucibleSlots[0].Fire(session.Board.RoundNumber);
            player.StonePosition = StonePosition.Start.Advance();
            player.StoneState    = StoneState.Forging;
            session.Board.RoundNumber++; // current round > FiredAtRound

            var before = player.StonePosition;
            SetSeason(session, Season.Autumn);
            var result = session.Apply(new TemperCommand(0));
            Assert.IsTrue(result.IsOk, result.Message);
            Assert.AreEqual(before.Advance().Value, session.Players[0].StonePosition.Value);
            Assert.AreEqual(CrucibleCardState.Discarded, session.Players[0].CrucibleSlots[0].State);
        }

        [Test]
        public void Opposition_Sends_Loser_To_Stasis()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            session.Players[1].StoneState = StoneState.Forging;
            SetSeason(session, Season.Autumn);
            session.CurrentTurnPlayerId = 0;
            var initiate = session.Apply(new InitiateOppositionCommand(0, 1));
            Assert.IsTrue(initiate.IsOk, initiate.Message);
            var respond = session.Apply(new RespondOppositionCommand(1, accept: true));
            Assert.IsTrue(respond.IsOk, respond.Message);
            // One of the two players must be in Stasis
            bool someoneInStasis = session.Players[0].StoneState == StoneState.Stasis
                                || session.Players[1].StoneState == StoneState.Stasis;
            Assert.IsTrue(someoneInStasis);
        }

        // ─── Season-gating tests (ActionValidator) ─────────────────────────────────

        [Test]
        public void Activate_Rejected_Outside_Autumn()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            session.Players[0].AssignedCodex = CodexVariant.A;
            GiveMarsCards(session, 0, 3);
            SetSeason(session, Season.Summer);

            var cards  = LastSpreadCards(session.Players[0], 3);
            var result = session.Apply(new ActivateCrucibleCommand(0, 0, cards));
            Assert.IsFalse(result.IsOk, "Activate should be rejected outside Autumn.");
            StringAssert.Contains("Autumn", result.Message);
        }

        [Test]
        public void Opposition_Rejected_Outside_Autumn()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            session.Players[1].StoneState = StoneState.Forging;
            SetSeason(session, Season.Summer);

            var result = session.Apply(new InitiateOppositionCommand(0, 1));
            Assert.IsFalse(result.IsOk, "Opposition should be rejected outside Autumn.");
            StringAssert.Contains("Autumn", result.Message);
        }

        [Test]
        public void BuildAstralHouse_Rejected_Outside_Summer()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            SetSeason(session, Season.Spring);

            var result = session.Apply(new BuildAstralHouseCommand(
                0, ZodiacSign.Aries, new List<string>()));
            Assert.IsFalse(result.IsOk, "Build Astral House should be rejected outside Summer.");
            StringAssert.Contains("Summer", result.Message);
        }

        // ─── CraftingRules tests ───────────────────────────────────────────────────

        [Test]
        public void Craft_Salt_With_3_Cards_Succeeds()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
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
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var result  = session.Apply(new CraftReagentCommand(0, ReagentType.Salt,
                new List<string> { "a", "b" }));
            Assert.IsFalse(result.IsOk);
        }

        [Test]
        public void Craft_Elemental_Fails_Without_Lit_Cauldron()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var cards   = GivePlayerWandsCards(session, db, 0, 3);
            var result  = session.Apply(new CraftReagentCommand(0, ReagentType.Sulphur, cards));
            Assert.IsFalse(result.IsOk, "Should fail without a lit Fire Cauldron.");
        }

        [Test]
        public void Craft_Elemental_Succeeds_With_Lit_Cauldron()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var cards   = GivePlayerWandsCards(session, db, 0, 3);
            session.Players[0].LightCauldron(Suit.Wands);
            var result  = session.Apply(new CraftReagentCommand(0, ReagentType.Sulphur, cards));
            Assert.IsTrue(result.IsOk, result.Message);
            Assert.AreEqual(1, session.Players[0].GetReagent(ReagentType.Sulphur));
        }

        // ─── WinterRules tests ─────────────────────────────────────────────────────

        [Test]
        public void DiscardToLimit_Trims_Spread_To_5()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var player  = session.Players[0];
            player.Spread.Clear();
            GivePlayerCards(session, 0, 8);
            Assert.Greater(player.Spread.Count, 5, "Setup check: player should have >5 cards.");

            var toDiscard = player.Spread.GetRange(0, player.Spread.Count - 5);
            var result = session.Apply(new DiscardToLimitCommand(0, toDiscard, new List<string>()));

            Assert.IsTrue(result.IsOk, result.Message);
            Assert.AreEqual(5, player.Spread.Count);
        }

        [Test]
        public void Transit_Increments_Round_Number()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            int before  = session.Board.RoundNumber;
            session.Apply(new TransitAgeCommand());
            Assert.AreEqual(before + 1, session.Board.RoundNumber);
        }

        [Test]
        public void Transit_Rotates_Agekeeper()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            Assert.IsTrue(session.Players[0].IsAgekeeper);
            session.Apply(new TransitAgeCommand());
            Assert.IsFalse(session.Players[0].IsAgekeeper);
            Assert.IsTrue(session.Players[1].IsAgekeeper);
        }

        [Test]
        public void Transit_Reshuffles_Discard_Into_Deck()
        {
            var db      = LoadDb();            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
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

        /// <summary>
        /// Register N Wands cards for Sulphur crafting tests.
        /// Returns the list of added instance IDs so tests can reference them directly
        /// (avoids confusion with pre-existing Spread cards from setup).
        /// </summary>
        private static List<string> GivePlayerWandsCards(GameSession session, CardDatabase db,
            int playerId, int count)
        {
            var added = new List<string>(count);
            int idx = 0;
            foreach (var def in db.GetBySuit(Suit.Wands))
            {
                if (idx >= count) break;
                var id   = $"test-wands-{playerId}-{idx}";
                var inst = new CardInstance(id, def.Id, CardZone.Spread, playerId);
                session.RegisterCard(inst);
                session.Players[playerId].Spread.Add(id);
                added.Add(id);
                idx++;
            }
            return added;
        }

        /// <summary>
        /// Activates slot <paramref name="slotIdx"/> by forcing Codex A (slot 0 = "Any Three Mars"),
        /// giving the player 3 Mars cards, and applying ActivateCrucibleCommand.
        /// </summary>
        private static void ActivateSlot(GameSession session, CardDatabase db, int playerId, int slotIdx)
        {
            // Force Codex A so we know exactly what formula applies to each slot.
            // Slot 0 = Any Three Mars, Slot 1 = Any Three Venus, etc.
            session.Players[playerId].AssignedCodex = CodexVariant.A;
            GiveMarsCards(session, playerId, 3);
            SetSeason(session, Season.Autumn);
            var spread = session.Players[playerId].Spread;
            var cards  = LastSpreadCards(session.Players[playerId], 3);
            var result = session.Apply(new ActivateCrucibleCommand(playerId, slotIdx, cards));
            Assume.That(result.IsOk, $"ActivateSlot helper failed: {result.Message}");
        }

        /// <summary>Give the player N Mars-planet cards (minor.cups.seven.1) in their Spread.</summary>
        private static void GiveMarsCards(GameSession session, int playerId, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var id   = $"test-mars-{playerId}-{i}-{System.Guid.NewGuid()}";
                var inst = new CardInstance(id, "minor.cups.seven.1", CardZone.Spread, playerId);
                session.RegisterCard(inst);
                session.Players[playerId].Spread.Add(id);
            }
        }

        /// <summary>Last N Spread cards — avoids mixing setup starter cards with test cards.</summary>
        private static List<string> LastSpreadCards(PlayerState player, int count) =>
            player.Spread.GetRange(player.Spread.Count - count, count);

        private struct HarvestSnapshot
        {
            public int Hand;
            public int Arcanum;
            public int PendingAdepts;
        }

        /// <summary>
        /// Cards dealt during Harvest land in Hand, Arcanum (Fate), or pending Adept limbo — not always Hand.
        /// </summary>
        private static HarvestSnapshot CaptureHarvestSnapshot(GameSession session, int playerId) =>
            new HarvestSnapshot
            {
                Hand          = session.Players[playerId].Hand.Count,
                Arcanum       = session.Players[playerId].Arcanum.Count,
                PendingAdepts = CountPendingAdepts(session, playerId),
            };

        private static int HarvestCardsReceived(GameSession session, int playerId, HarvestSnapshot before)
        {
            var player = session.Players[playerId];
            return (player.Hand.Count - before.Hand)
                 + (player.Arcanum.Count - before.Arcanum)
                 + (CountPendingAdepts(session, playerId) - before.PendingAdepts);
        }

        private static int CountPendingAdepts(GameSession session, int playerId)
        {
            int count = 0;
            foreach (var (pid, _) in session.Board.PendingAdeptDecisions)
                if (pid == playerId) count++;
            return count;
        }

        // ─── Setup: Codex assignment tests ────────────────────────────────────────

        [Test]
        public void Setup_Assigns_Codex_To_Each_Player()
        {
            var db      = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            foreach (var player in session.Players)
                Assert.AreNotEqual(CodexVariant.None, player.AssignedCodex,
                    $"Player {player.PlayerId} must have a Codex assigned.");
        }

        [Test]
        public void Setup_Players_Get_Unique_Codex_Variants_TwoPlayer()
        {
            var db      = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb, playerCount: 2);
            var p0 = session.Players[0].AssignedCodex;
            var p1 = session.Players[1].AssignedCodex;
            Assert.AreNotEqual(p0, p1, "Two players should receive different Codex variants.");
        }

        // ─── CrucibleRules: Codex formula tests ───────────────────────────────────

        [Test]
        public void Activate_Fails_With_Wrong_Planet_Cards()
        {
            var db      = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            // Codex A slot 0 = Any Three Mars.
            // minor.cups.seven.1 has Planet=Mars, so give a non-Mars card instead.
            // minor.wands.two.1 → planet is Moon (2 = Moon in Kismeta correspondence).
            session.Players[0].AssignedCodex = CodexVariant.A;
            for (int i = 0; i < 3; i++)
            {
                var id   = $"test-moon-{i}";
                var inst = new CardInstance(id, "minor.wands.two.1", CardZone.Spread, 0);
                session.RegisterCard(inst);
                session.Players[0].Spread.Add(id);
            }
            SetSeason(session, Season.Autumn);
            var cards  = LastSpreadCards(session.Players[0], 3);
            var result = session.Apply(new ActivateCrucibleCommand(0, 0, cards));
            Assert.IsFalse(result.IsOk, "Wrong planet cards should not satisfy the formula.");
            StringAssert.Contains("Formula not satisfied", result.Message);
        }

        [Test]
        public void Activate_Removes_Coal_From_Slot()
        {
            var db      = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            session.Players[0].AssignedCodex = CodexVariant.A;
            Assert.IsTrue(session.Players[0].CrucibleSlots[0].HasCoal, "Slot should start with coal.");
            GiveMarsCards(session, 0, 3);
            SetSeason(session, Season.Autumn);
            var cards  = LastSpreadCards(session.Players[0], 3);
            var result = session.Apply(new ActivateCrucibleCommand(0, 0, cards));
            Assert.IsTrue(result.IsOk, result.Message);
            Assert.IsFalse(session.Players[0].CrucibleSlots[0].HasCoal, "Coal should be removed after activation.");
        }

        [Test]
        public void Activate_Lights_Correct_Cauldron()
        {
            var db      = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            // Codex A slot 1 = Any Three Venus → lights Blue (Cups) cauldron.
            session.Players[0].AssignedCodex = CodexVariant.A;
            // Venus cards: minor.cups.four.1 (4 = Venus in Kismeta correspondence).
            for (int i = 0; i < 3; i++)
            {
                var id   = $"test-venus-{i}";
                var inst = new CardInstance(id, "minor.cups.four.1", CardZone.Spread, 0);
                session.RegisterCard(inst);
                session.Players[0].Spread.Add(id);
            }
            var spread = session.Players[0].Spread;
            var cards  = spread.GetRange(spread.Count - 3, 3);
            SetSeason(session, Season.Autumn);
            var result = session.Apply(new ActivateCrucibleCommand(0, 1, cards));
            Assert.IsTrue(result.IsOk, result.Message);
            Assert.IsTrue(session.Players[0].IsCauldronLit(Suit.Cups),
                "Blue (Cups) cauldron should be lit after Codex A slot 1 activation.");
        }

        [Test]
        public void Activate_RankSum_Succeeds_With_Enough_Wands()
        {
            var db      = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            // Codex B slot 0 = 25 Total Ranks · Wands. Supply 3 Wands cards summing >= 25.
            // King (14) + King (14) + Ten (10) = 38 >= 25.
            session.Players[0].AssignedCodex = CodexVariant.B;
            var wandsIds = new List<string>();
            foreach (var defId in new[] { "minor.wands.king.1", "minor.wands.king.2", "minor.wands.ten.1" })
            {
                var id   = $"test-wands-rs-{defId}";
                var inst = new CardInstance(id, defId, CardZone.Spread, 0);
                session.RegisterCard(inst);
                session.Players[0].Spread.Add(id);
                wandsIds.Add(id);
            }
            SetSeason(session, Season.Autumn);
            var result = session.Apply(new ActivateCrucibleCommand(0, 0, wandsIds));
            Assert.IsTrue(result.IsOk, result.Message);
        }

        [Test]
        public void Activate_RankSum_Fails_With_Insufficient_Sum()
        {
            var db      = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            // Codex B slot 0 = 25 Total Ranks · Wands. Supply 3 Wands cards summing < 25.
            // Two (2) + Two (2) + Three (3) = 7 < 25.
            session.Players[0].AssignedCodex = CodexVariant.B;
            var wandsIds = new List<string>();
            foreach (var defId in new[] { "minor.wands.two.1", "minor.wands.two.2", "minor.wands.three.1" })
            {
                var id   = $"test-wands-low-{defId}";
                var inst = new CardInstance(id, defId, CardZone.Spread, 0);
                session.RegisterCard(inst);
                session.Players[0].Spread.Add(id);
                wandsIds.Add(id);
            }
            SetSeason(session, Season.Autumn);
            var result = session.Apply(new ActivateCrucibleCommand(0, 0, wandsIds));
            Assert.IsFalse(result.IsOk, "Insufficient rank sum should fail.");
        }

        [Test]
        public void Activate_Fails_With_No_Coal()
        {
            var db      = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            session.Players[0].AssignedCodex = CodexVariant.A;
            session.Players[0].CrucibleSlots[0].RemoveCoal();
            GiveMarsCards(session, 0, 3);
            SetSeason(session, Season.Autumn);
            var cards  = LastSpreadCards(session.Players[0], 3);
            var result = session.Apply(new ActivateCrucibleCommand(0, 0, cards));
            Assert.IsFalse(result.IsOk, "Cannot activate a slot with no coal.");
        }

        [Test]
        public void Activate_Fails_When_Cards_Not_In_Spread()
        {
            var db      = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            session.Players[0].AssignedCodex = CodexVariant.A;
            // Register Mars cards but put them in Hand, not Spread.
            var handIds = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                var id   = $"test-hand-{i}";
                var inst = new CardInstance(id, "minor.cups.seven.1", CardZone.Hand, 0);
                session.RegisterCard(inst);
                session.Players[0].Hand.Add(id);
                handIds.Add(id);
            }
            SetSeason(session, Season.Autumn);
            var result = session.Apply(new ActivateCrucibleCommand(0, 0, handIds));
            Assert.IsFalse(result.IsOk, "Hand cards should not be usable for activation.");
        }

        // ─── Major Arcana Zone Enforcement tests ──────────────────────────────────

        [Test]
        public void RouteDrawnCard_Fate_GoesToArcanum_NotHand()
        {
            var db      = LoadDb();  var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var player  = session.Players[0];

            // Place a Fate card on top of the deck
            var fateInst = new CardInstance("fate-test-01", "major.fate.16", CardZone.Deck, -1);
            session.RegisterCard(fateInst);
            session.Board.CommonDeck.Push(fateInst.InstanceId);

            int handBefore    = player.Hand.Count;
            int arcanumBefore = player.Arcanum.Count;

            session.Rules!.Harvest.RouteDrawnCard(session, 0, fateInst.InstanceId);

            Assert.AreEqual(handBefore,        player.Hand.Count,    "Fate must NOT go to Hand.");
            Assert.AreEqual(arcanumBefore + 1, player.Arcanum.Count, "Fate must go to Arcanum.");
            Assert.Contains(fateInst.InstanceId, player.Arcanum);
        }

        [Test]
        public void RouteDrawnCard_Adept_Queued_NotHand()
        {
            var db      = LoadDb();  var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var player  = session.Players[0];

            var adeptInst = new CardInstance("adept-test-01", "major.adept.3", CardZone.Deck, -1);
            session.RegisterCard(adeptInst);
            session.Board.CommonDeck.Push(adeptInst.InstanceId);

            int handBefore    = player.Hand.Count;
            int pendingBefore = session.Board.PendingAdeptDecisions.Count;

            session.Rules!.Harvest.RouteDrawnCard(session, 0, adeptInst.InstanceId);

            Assert.AreEqual(handBefore,          player.Hand.Count,                       "Adept must NOT go to Hand.");
            Assert.AreEqual(pendingBefore + 1,   session.Board.PendingAdeptDecisions.Count, "Adept must be queued in PendingAdeptDecisions.");
        }

        [Test]
        public void Commune_Rejects_MajorArcana_InSpread()
        {
            var db      = LoadDb();  var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var player  = session.Players[0];

            // Place a Fate card directly into Hand (simulating a bug state)
            var fateInst = new CardInstance("fate-commune-01", "major.fate.19", CardZone.Hand, 0);
            session.RegisterCard(fateInst);
            player.Hand.Add(fateInst.InstanceId);

            // Try to commune, assigning the Fate card to Spread (all other Hand+Spread cards to Hand)
            var spreadIds = new List<string> { fateInst.InstanceId };
            var handIds   = new List<string>(player.Spread);
            foreach (var id in player.Hand)
                if (id != fateInst.InstanceId) handIds.Add(id);

            var result = session.Apply(new CommuneCommand(0, spreadIds, handIds));
            Assert.IsFalse(result.IsOk, "Commune must reject Major Arcana in Spread.");
        }

        [Test]
        public void BuyAdept_Rejects_MajorArcana_Payment()
        {
            var db      = LoadDb();  var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var player  = session.Players[0];

            // The Adept to buy (in limbo / Deck zone as usual)
            var adeptBuy = new CardInstance("adept-buy-01", "major.adept.2", CardZone.Deck, -1);
            session.RegisterCard(adeptBuy);
            session.Board.PendingAdeptDecisions.Add((0, adeptBuy.InstanceId));

            // Try to pay with a Fate card placed in Spread
            var fateInst = new CardInstance("fate-pay-01", "major.fate.13", CardZone.Spread, 0);
            session.RegisterCard(fateInst);
            player.Spread.Add(fateInst.InstanceId);

            // Top up with 2 minor cards
            for (int i = 0; i < 2; i++)
            {
                var minor = new CardInstance($"minor-pay-0{i}", "minor.cups.seven.1", CardZone.Spread, 0);
                session.RegisterCard(minor);
                player.Spread.Add(minor.InstanceId);
            }

            var payment = new List<string> { fateInst.InstanceId,
                player.Spread[player.Spread.Count - 2], player.Spread[player.Spread.Count - 1] };
            var result = session.Apply(new BuyAdeptCommand(0, adeptBuy.InstanceId, payment));
            Assert.IsFalse(result.IsOk, "BuyAdept must reject a Fate card as payment.");
        }

        [Test]
        public void MoonDraw_MajorArcana_NotInMoonPool()
        {
            var db      = LoadDb();  var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var player  = session.Players[0];

            // Clear deck and place 1 Fate then 3 minors so Moon's 4-draw has mixed types
            session.Board.CommonDeck.Clear();

            var fate = new CardInstance("fate-moon-01", "major.fate.11", CardZone.Deck, -1);
            session.RegisterCard(fate);
            session.Board.CommonDeck.Push(fate.InstanceId); // drawn last (stack: last-in first-out)

            for (int i = 0; i < 3; i++)
            {
                var minor = new CardInstance($"minor-moon-0{i}", "minor.cups.seven.1", CardZone.Deck, -1);
                session.RegisterCard(minor);
                session.Board.CommonDeck.Push(minor.InstanceId);
            }
            // Deck top → minor-moon-02, minor-moon-01, minor-moon-00, fate-moon-01

            session.Board.FateMoonDrawnCardIds.Clear();
            var harvest = session.Rules!.Harvest;
            // Simulate what GameLoop does for Moon: draw 4, collect minors into pool
            for (int i = 0; i < 4 && session.Board.CommonDeck.Count > 0; i++)
            {
                var id = session.Board.CommonDeck.Pop();
                harvest.RouteDrawnCard(session, 0, id, session.Board.FateMoonDrawnCardIds);
            }

            // The Fate must not be in the moon pool and must be in Arcanum
            Assert.IsFalse(session.Board.FateMoonDrawnCardIds.Contains(fate.InstanceId),
                "Fate card must NOT appear in the Moon draw pool.");
            Assert.IsTrue(player.Arcanum.Contains(fate.InstanceId),
                "Fate card must be routed to Arcanum during Moon draw.");

            // All 3 minors should be in the pool
            Assert.AreEqual(3, session.Board.FateMoonDrawnCardIds.Count,
                "Only the 3 minor arcana should be in the Moon draw pool.");
        }

        // ─── Winter Phase tests ───────────────────────────────────────────────────

        [Test]
        public void WinterMoveCard_HandToSpread_Works()
        {
            var db      = LoadDb();  var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var player  = session.Players[0];

            var card = new CardInstance("wmc-hand-01", "minor.cups.seven.1", CardZone.Hand, 0);
            session.RegisterCard(card);
            player.Hand.Add(card.InstanceId);

            var result = session.Apply(new WinterMoveCardCommand(0, card.InstanceId, toSpread: true));
            Assert.IsTrue(result.IsOk, result.Message);
            Assert.IsTrue(player.Spread.Contains(card.InstanceId), "Card should be in Spread.");
            Assert.IsFalse(player.Hand.Contains(card.InstanceId), "Card should no longer be in Hand.");
        }

        [Test]
        public void WinterMoveCard_Rejects_NonOwned()
        {
            var db      = LoadDb();  var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);

            // card belongs to no one's hand
            var card = new CardInstance("wmc-none-01", "minor.cups.seven.1", CardZone.Deck, -1);
            session.RegisterCard(card);

            var result = session.Apply(new WinterMoveCardCommand(0, card.InstanceId, toSpread: true));
            Assert.IsFalse(result.IsOk, "Should reject moving a card not in player's Hand.");
        }

        [Test]
        public void FatefulWager_Placed_CardsRemovedFromSpread()
        {
            var db      = LoadDb();  var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var player  = session.Players[0];

            var c1 = new CardInstance("fw-sp-01", "minor.cups.seven.1", CardZone.Spread, 0);
            var c2 = new CardInstance("fw-sp-02", "minor.cups.eight.1", CardZone.Spread, 0);
            session.RegisterCard(c1); session.RegisterCard(c2);
            player.Spread.Add(c1.InstanceId); player.Spread.Add(c2.InstanceId);

            var result = session.Apply(new PlaceFatefulWagerCommand(0, ZodiacSign.Aries,
                new List<string> { c1.InstanceId, c2.InstanceId }));

            Assert.IsTrue(result.IsOk, result.Message);
            Assert.IsFalse(player.Spread.Contains(c1.InstanceId), "Wagered card removed from Spread.");
            Assert.AreEqual(ZodiacSign.Aries, player.FatefulWagerSign);
            Assert.AreEqual(2, player.FatefulWagerCards.Count);
        }

        [Test]
        public void FatefulWager_CorrectSign_DoublesCards()
        {
            var db      = LoadDb();  var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var player  = session.Players[0];

            // Place 1 card wager on Aries
            var waged = new CardInstance("fw-win-01", "minor.cups.seven.1", CardZone.Spread, 0);
            session.RegisterCard(waged);
            player.Spread.Add(waged.InstanceId);
            session.Apply(new PlaceFatefulWagerCommand(0, ZodiacSign.Aries,
                new List<string> { waged.InstanceId }));

            // Put 1 card in the common deck as the bonus prize
            var prize = new CardInstance("fw-prize-01", "minor.cups.nine.1", CardZone.Deck, -1);
            session.RegisterCard(prize);
            session.Board.CommonDeck.Push(prize.InstanceId);

            // Resolve with matching sign — should return wagered + draw 1 bonus
            session.Rules!.Winter.ResolveWagers(session, ZodiacSign.Aries);

            Assert.IsTrue(player.Hand.Contains(waged.InstanceId), "Wagered card returned to Hand.");
            Assert.IsTrue(player.Hand.Contains(prize.InstanceId), "Bonus card drawn to Hand.");
            Assert.AreEqual(ZodiacSign.None, player.FatefulWagerSign, "Wager sign cleared after resolution.");
        }

        [Test]
        public void FatefulWager_WrongSign_DiscardedCards()
        {
            var db      = LoadDb();  var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var player  = session.Players[0];

            var waged = new CardInstance("fw-lose-01", "minor.cups.seven.1", CardZone.Spread, 0);
            session.RegisterCard(waged);
            player.Spread.Add(waged.InstanceId);
            session.Apply(new PlaceFatefulWagerCommand(0, ZodiacSign.Aries,
                new List<string> { waged.InstanceId }));

            // Resolve with different sign — wagered card goes to discard
            session.Rules!.Winter.ResolveWagers(session, ZodiacSign.Taurus);

            Assert.IsFalse(player.Hand.Contains(waged.InstanceId), "Lost wager card not in Hand.");
            Assert.IsTrue(session.Board.CommonDiscard.Contains(waged.InstanceId), "Lost wager card in discard.");
            Assert.AreEqual(ZodiacSign.None, player.FatefulWagerSign, "Wager sign cleared.");
        }

        [Test]
        public void FatefulWager_SurvivesTransit_AndResolvesOnCosmicAgeRoll_Win()
        {
            var db      = LoadDb();  var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var player  = session.Players[0];

            // Place a wager on Aries
            var waged = new CardInstance("fw-lc-win-01", "minor.cups.seven.1", CardZone.Spread, 0);
            session.RegisterCard(waged);
            player.Spread.Add(waged.InstanceId);
            session.Apply(new PlaceFatefulWagerCommand(0, ZodiacSign.Aries,
                new List<string> { waged.InstanceId }));

            Assert.AreEqual(ZodiacSign.Aries, player.FatefulWagerSign, "Wager sign set.");

            // Transit the age — wager state must survive
            session.Rules!.Winter.Transit(session);

            Assert.AreEqual(ZodiacSign.Aries, player.FatefulWagerSign,
                "Wager sign must survive Transit.");
            Assert.AreEqual(1, player.FatefulWagerCards.Count,
                "Wager cards must survive Transit.");

            // Seed the deck with a prize card for the win-path bonus draw
            var prize = new CardInstance("fw-lc-prize-01", "minor.cups.nine.1", CardZone.Deck, -1);
            session.RegisterCard(prize);
            session.Board.CommonDeck.Push(prize.InstanceId);

            // Force the Cosmic Age sign so we can assert deterministically
            session.Board.CosmicAgeSign = ZodiacSign.Aries;

            // Capture the resolved event
            FatefulWagerResolvedEvent? resolved = null;
            session.OnEvent += e => { if (e is FatefulWagerResolvedEvent r) resolved = r; };

            session.Rules.Winter.ResolveWagers(session, session.Board.CosmicAgeSign);

            Assert.IsNotNull(resolved, "FatefulWagerResolvedEvent must be emitted.");
            Assert.IsTrue(resolved!.Won, "Should be a win when predicted sign matches Cosmic Age.");
            Assert.AreEqual(ZodiacSign.Aries, resolved.PredictedSign);
            Assert.IsTrue(player.Hand.Contains(waged.InstanceId), "Wagered card returned to Hand.");
            Assert.IsTrue(player.Hand.Contains(prize.InstanceId), "Bonus card drawn to Hand.");
            Assert.AreEqual(ZodiacSign.None, player.FatefulWagerSign, "Wager sign cleared after resolution.");
        }

        [Test]
        public void FatefulWager_SurvivesTransit_AndResolvesOnCosmicAgeRoll_Loss()
        {
            var db      = LoadDb();  var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var player  = session.Players[0];

            // Place a wager on Aries
            var waged = new CardInstance("fw-lc-lose-01", "minor.cups.seven.1", CardZone.Spread, 0);
            session.RegisterCard(waged);
            player.Spread.Add(waged.InstanceId);
            session.Apply(new PlaceFatefulWagerCommand(0, ZodiacSign.Aries,
                new List<string> { waged.InstanceId }));

            // Transit — wager must survive
            session.Rules!.Winter.Transit(session);

            Assert.AreEqual(ZodiacSign.Aries, player.FatefulWagerSign,
                "Wager sign must survive Transit.");

            // Cosmic Age rolls a different sign — wager is lost
            session.Board.CosmicAgeSign = ZodiacSign.Taurus;

            FatefulWagerResolvedEvent? resolved = null;
            session.OnEvent += e => { if (e is FatefulWagerResolvedEvent r) resolved = r; };

            session.Rules.Winter.ResolveWagers(session, session.Board.CosmicAgeSign);

            Assert.IsNotNull(resolved, "FatefulWagerResolvedEvent must be emitted.");
            Assert.IsFalse(resolved!.Won, "Should be a loss when predicted sign does not match.");
            Assert.AreEqual(ZodiacSign.Aries, resolved.PredictedSign);
            Assert.IsFalse(player.Hand.Contains(waged.InstanceId), "Lost wager card not returned to Hand.");
            Assert.IsTrue(session.Board.CommonDiscard.Contains(waged.InstanceId), "Lost wager card in discard.");
            Assert.AreEqual(ZodiacSign.None, player.FatefulWagerSign, "Wager sign cleared after resolution.");
        }

        [Test]
        public void DiscardToLimit_Over5_PlayerChooses()
        {
            var db      = LoadDb();  var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var player  = session.Players[0];
            player.Spread.Clear();

            // Give player 7 cards in Spread
            for (int i = 0; i < 7; i++)
            {
                var c = new CardInstance($"dtl-sp-0{i}", "minor.cups.seven.1", CardZone.Spread, 0);
                session.RegisterCard(c);
                player.Spread.Add(c.InstanceId);
            }

            // Player discards 2 specific cards
            var toDiscard = new List<string> { player.Spread[0], player.Spread[1] };
            var result = session.Apply(new DiscardToLimitCommand(0, toDiscard,
                new List<string>()));

            Assert.IsTrue(result.IsOk, result.Message);
            Assert.AreEqual(5, player.Spread.Count, "Spread should be trimmed to 5.");
            foreach (var id in toDiscard)
                Assert.IsTrue(session.Board.CommonDiscard.Contains(id), "Discarded card should be in discard pile.");
        }

        [Test]
        public void DiscardToLimit_Under5_AutoPasses()
        {
            var db      = LoadDb();  var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var player  = session.Players[0];
            player.Spread.Clear();

            // Give player exactly 3 cards — within limit
            for (int i = 0; i < 3; i++)
            {
                var c = new CardInstance($"dtl-ok-0{i}", "minor.cups.seven.1", CardZone.Spread, 0);
                session.RegisterCard(c);
                player.Spread.Add(c.InstanceId);
            }

            // Submit empty discard — should succeed because result is within limits
            var result = session.Apply(new DiscardToLimitCommand(0,
                new List<string>(), new List<string>()));

            Assert.IsTrue(result.IsOk, result.Message);
            Assert.AreEqual(3, player.Spread.Count, "Spread count unchanged when already within limit.");
        }

        [Test]
        public void DiscardToLimit_MoveThenNoDiscardNeeded()
        {
            var db      = LoadDb();  var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var player  = session.Players[0];
            player.Spread.Clear();
            player.Hand.Clear();

            for (int i = 0; i < 4; i++)
            {
                var c = new CardInstance($"mv-sp-0{i}", "minor.cups.seven.1", CardZone.Spread, 0);
                session.RegisterCard(c);
                player.Spread.Add(c.InstanceId);
            }
            for (int i = 0; i < 6; i++)
            {
                var c = new CardInstance($"mv-hd-0{i}", "minor.wands.two.1", CardZone.Hand, 0);
                session.RegisterCard(c);
                player.Hand.Add(c.InstanceId);
            }

            var moveCard = player.Hand[0];
            var moveResult = session.Apply(new WinterMoveCardCommand(0, moveCard, toSpread: true));
            Assert.IsTrue(moveResult.IsOk, moveResult.Message);
            Assert.AreEqual(5, player.Spread.Count);
            Assert.AreEqual(5, player.Hand.Count);

            var discardResult = session.Apply(new DiscardToLimitCommand(0,
                new List<string>(), new List<string>()));
            Assert.IsTrue(discardResult.IsOk, discardResult.Message);
            Assert.AreEqual(5, player.Spread.Count);
            Assert.AreEqual(5, player.Hand.Count);
        }

        [Test]
        public void DiscardToLimit_CraftSaltThenDiscardRest()
        {
            var db      = LoadDb();  var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var player  = session.Players[0];
            player.Spread.Clear();
            player.Hand.Clear();

            for (int i = 0; i < 7; i++)
            {
                var c = new CardInstance($"cr-sp-0{i}", "minor.cups.seven.1", CardZone.Spread, 0);
                session.RegisterCard(c);
                player.Spread.Add(c.InstanceId);
            }

            int saltBefore = player.GetReagent(ReagentType.Salt);
            var craftCards = player.Spread.GetRange(0, 3);
            var craftResult = session.Apply(new CraftReagentCommand(0, ReagentType.Salt, craftCards));
            Assert.IsTrue(craftResult.IsOk, craftResult.Message);
            Assert.AreEqual(saltBefore + 1, player.GetReagent(ReagentType.Salt));
            Assert.AreEqual(4, player.Spread.Count);

            var discardResult = session.Apply(new DiscardToLimitCommand(0,
                new List<string>(), new List<string>()));
            Assert.IsTrue(discardResult.IsOk, discardResult.Message);
            Assert.AreEqual(4, player.Spread.Count);
            foreach (var id in craftCards)
                Assert.IsTrue(session.Board.CommonDiscard.Contains(id));
        }

        // ─── TradeService tests ────────────────────────────────────────────────────

        static List<string> PopulateSpread(GameSession session, int playerId, int count, string prefix)
        {
            var ids = new List<string>();
            var player = session.Players[playerId];
            foreach (var id in player.Spread)
            {
                if (!session.Board.CommonDiscard.Contains(id))
                    session.Board.CommonDiscard.Add(id);
                session.GetCard(id)?.MoveTo(CardZone.Discard, -1);
            }
            player.Spread.Clear();
            for (int i = 0; i < count; i++)
            {
                string id = $"{prefix}-p{playerId}-{i}";
                var c = new CardInstance(id, "minor.cups.seven.1", CardZone.Spread, playerId);
                session.RegisterCard(c);
                player.Spread.Add(id);
                ids.Add(id);
            }
            return ids;
        }

        [Test]
        public void Trade_MagnusMisaligned_1For1_Rejected()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb, mode: GameMode.MagnusAlchemist);
            session.Players[0].CurrentSign = ZodiacSign.Aries;
            session.Players[1].CurrentSign = ZodiacSign.Gemini;

            var offer = PopulateSpread(session, 0, 1, "trade");
            var request = PopulateSpread(session, 1, 1, "trade");

            var trade = new TradeService(db);
            var result = trade.TryTrade(session, 0, 1, offer, request);

            Assert.IsFalse(result.IsOk, "Misaligned Magnus 1:1 trade should be rejected.");
        }

        [Test]
        public void Trade_MagnusMisaligned_2For1_Accepted()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb, mode: GameMode.MagnusAlchemist);
            session.Players[0].CurrentSign = ZodiacSign.Aries;
            session.Players[1].CurrentSign = ZodiacSign.Gemini;

            var offer = PopulateSpread(session, 0, 2, "trade");
            var request = PopulateSpread(session, 1, 1, "trade");

            var trade = new TradeService(db);
            var result = trade.TryTrade(session, 0, 1, offer, request);

            Assert.IsTrue(result.IsOk, result.Message);
            Assert.AreEqual(1, session.Players[0].Spread.Count);
            Assert.AreEqual(2, session.Players[1].Spread.Count);
        }

        [Test]
        public void Trade_MagnusMisaligned_GiftOnly_Accepted()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb, mode: GameMode.MagnusAlchemist);
            session.Players[0].CurrentSign = ZodiacSign.Aries;
            session.Players[1].CurrentSign = ZodiacSign.Gemini;

            var offer = PopulateSpread(session, 0, 2, "trade");
            PopulateSpread(session, 1, 1, "trade");

            var trade = new TradeService(db);
            var result = trade.TryTrade(session, 0, 1, offer, new List<string>());

            Assert.IsTrue(result.IsOk, result.Message);
            Assert.AreEqual(0, session.Players[0].Spread.Count);
            Assert.AreEqual(3, session.Players[1].Spread.Count);
        }

        [Test]
        public void Trade_MagnusAligned_1For1_Accepted()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb, mode: GameMode.MagnusAlchemist);
            session.Players[0].CurrentSign = ZodiacSign.Aries;
            session.Players[1].CurrentSign = ZodiacSign.Leo;

            var offer = PopulateSpread(session, 0, 1, "trade");
            var request = PopulateSpread(session, 1, 1, "trade");

            var trade = new TradeService(db);
            var result = trade.TryTrade(session, 0, 1, offer, request);

            Assert.IsTrue(result.IsOk, result.Message);
            Assert.AreEqual(1, session.Players[0].Spread.Count);
            Assert.AreEqual(1, session.Players[1].Spread.Count);
        }

        [Test]
        public void Trade_QuickplayMisaligned_1For1_Accepted()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb, mode: GameMode.Quickplay);
            session.Players[0].CurrentSign = ZodiacSign.Aries;
            session.Players[1].CurrentSign = ZodiacSign.Gemini;

            var offer = PopulateSpread(session, 0, 1, "trade");
            var request = PopulateSpread(session, 1, 1, "trade");

            var trade = new TradeService(db);
            var result = trade.TryTrade(session, 0, 1, offer, request);

            Assert.IsTrue(result.IsOk, result.Message);
        }

        // ─── CombatRules Duel tests ────────────────────────────────────────────────

        static int FindDuelSeed(bool attackerWins)
        {
            for (int seed = 0; seed < 10000; seed++)
            {
                var r = new System.Random(seed);
                if ((r.Next(1, 13) > r.Next(1, 13)) == attackerWins)
                    return seed;
            }
            Assert.Fail("Could not find duel seed.");
            return 0;
        }

        [Test]
        public void Duel_InvalidTarget_Rejected()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var attackerCards = PopulateSpread(session, 0, 1, "duel");
            PopulateSpread(session, 1, 1, "duel");

            var combat = new CombatRules(42);
            var result = combat.TryDuel(session, 0, 1, "not-in-spread", attackerCards[0]);

            Assert.IsFalse(result.IsOk);
        }

        [Test]
        public void Duel_AttackerWins_StealsTarget_KeepsAnte()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var ante = PopulateSpread(session, 0, 1, "duel-ante");
            var targets = PopulateSpread(session, 1, 2, "duel-target");
            string targetId = targets[0];
            string anteId = ante[0];

            var combat = new CombatRules(FindDuelSeed(attackerWins: true));
            var result = combat.TryDuel(session, 0, 1, targetId, anteId);

            Assert.IsTrue(result.IsOk, result.Message);
            Assert.IsTrue(session.Players[0].Spread.Contains(anteId));
            Assert.IsTrue(session.Players[0].Spread.Contains(targetId));
            Assert.IsFalse(session.Players[1].Spread.Contains(targetId));
            Assert.AreEqual(1, session.Players[1].Spread.Count);
        }

        [Test]
        public void Duel_AttackerLoses_AnteDiscarded_TargetStays()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var ante = PopulateSpread(session, 0, 1, "duel-ante");
            var targets = PopulateSpread(session, 1, 1, "duel-target");
            string targetId = targets[0];
            string anteId = ante[0];

            var combat = new CombatRules(FindDuelSeed(attackerWins: false));
            var result = combat.TryDuel(session, 0, 1, targetId, anteId);

            Assert.IsTrue(result.IsOk, result.Message);
            Assert.IsFalse(session.Players[0].Spread.Contains(anteId));
            Assert.IsTrue(session.Board.CommonDiscard.Contains(anteId));
            Assert.IsTrue(session.Players[1].Spread.Contains(targetId));
        }

        static int FindDuelTieSeed()
        {
            for (int seed = 0; seed < 10000; seed++)
            {
                var r = new System.Random(seed);
                if (r.Next(1, 13) == r.Next(1, 13))
                    return seed;
            }
            Assert.Fail("Could not find duel tie seed.");
            return 0;
        }

        [Test]
        public void Duel_Tie_DefenderWins()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var ante = PopulateSpread(session, 0, 1, "duel-ante");
            var targets = PopulateSpread(session, 1, 1, "duel-target");
            string targetId = targets[0];
            string anteId = ante[0];

            var combat = new CombatRules(FindDuelTieSeed());
            var result = combat.TryDuel(session, 0, 1, targetId, anteId);

            Assert.IsTrue(result.IsOk, result.Message);
            Assert.IsFalse(session.Players[0].Spread.Contains(anteId));
            Assert.IsTrue(session.Board.CommonDiscard.Contains(anteId));
            Assert.IsTrue(session.Players[1].Spread.Contains(targetId));
        }

        [Test]
        public void CombatRules_AcceptPendingDuel_Resolves()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var ante = PopulateSpread(session, 0, 1, "duel-ante");
            var targets = PopulateSpread(session, 1, 1, "duel-target");

            var combat = new CombatRules(42);
            var offered = combat.TryInitiateDuel(session, 0, 1, targets[0], ante[0]);
            Assert.IsTrue(offered.IsOk, offered.Message);
            Assert.NotNull(session.Board.PendingContest);

            var accepted = combat.TryRespondDuel(session, 1, accept: true);
            Assert.IsTrue(accepted.IsOk, accepted.Message);
            Assert.IsNull(session.Board.PendingContest);
        }

        [Test]
        public void CombatRules_SecondDuelSameRound_Rejected()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var ante = PopulateSpread(session, 0, 2, "duel-ante");
            var targets = PopulateSpread(session, 1, 2, "duel-target");

            var combat = new CombatRules(42);
            var first = combat.TryInitiateDuel(session, 0, 1, targets[0], ante[0]);
            Assert.IsTrue(first.IsOk, first.Message);
            Assert.AreEqual(1, session.Players[0].DuelChallengedRivalId);

            var declined = combat.TryRespondDuel(session, 1, accept: false);
            Assert.IsTrue(declined.IsOk, declined.Message);

            var second = combat.TryInitiateDuel(session, 0, 1, targets[1], ante[1]);
            Assert.IsFalse(second.IsOk);
            StringAssert.Contains("already initiated", second.Message);
        }

        [Test]
        public void CombatRules_AcceptPendingDuel_KeepsPendingOnValidationFailure()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var ante = PopulateSpread(session, 0, 1, "duel-ante");
            var targets = PopulateSpread(session, 1, 1, "duel-target");

            var combat = new CombatRules(42);
            combat.TryInitiateDuel(session, 0, 1, targets[0], ante[0]);

            session.Players[0].Spread.Remove(ante[0]);

            var accepted = combat.TryRespondDuel(session, 1, accept: true);
            Assert.IsFalse(accepted.IsOk);
            Assert.NotNull(session.Board.PendingContest);
        }

        [Test]
        public void WinterTransit_ClearsDuelChallengedRivalId()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            session.Players[0].DuelChallengedRivalId = 1;

            session.Rules!.Winter.Transit(session);

            Assert.AreEqual(-1, session.Players[0].DuelChallengedRivalId);
        }

        static GameContext BuildSummerContext(GameSession session, int playerId) =>
            new GameContext(
                GamePublicView.From(session),
                PlayerPrivateView.From(session, playerId),
                playerId,
                ActionHint.SummerAction);

        [Test]
        public void AiDuelPolicy_30PercentGate_BelowThreshold_Initiates()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            PopulateSpread(session, 0, 2, "ai-duel");
            PopulateSpread(session, 1, 1, "ai-target");
            var ctx = BuildSummerContext(session, 0);

            Assert.IsTrue(AiDuelPolicy.ShouldInitiate(ctx, 0, 0.29, out var cmd));
            Assert.IsNotNull(cmd);
            Assert.AreEqual(1, cmd!.DefenderId);
        }

        [Test]
        public void AiDuelPolicy_30PercentGate_AtThreshold_Skips()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            PopulateSpread(session, 0, 2, "ai-duel");
            PopulateSpread(session, 1, 1, "ai-target");
            var ctx = BuildSummerContext(session, 0);

            Assert.IsFalse(AiDuelPolicy.ShouldInitiate(ctx, 0, 0.30, out var cmd));
            Assert.IsNull(cmd);
        }

        [Test]
        public void AiDuelPolicy_SkipsWhenAlreadyChallenged()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            PopulateSpread(session, 0, 2, "ai-duel");
            PopulateSpread(session, 1, 1, "ai-target");
            session.Players[0].DuelChallengedRivalId = 1;
            var ctx = BuildSummerContext(session, 0);

            Assert.IsFalse(AiDuelPolicy.ShouldInitiate(ctx, 0, 0.0, out var cmd));
            Assert.IsNull(cmd);
        }

        [Test]
        public void Trade_EmitsPlayerExchangeEvent_WithCardLegs()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var offer = PopulateSpread(session, 0, 1, "trade-ex");
            var request = PopulateSpread(session, 1, 1, "trade-ex");

            PlayerExchangeEvent? exchange = null;
            void Handler(IGameEvent e)
            {
                if (e is PlayerExchangeEvent pe) exchange = pe;
            }
            session.OnEvent += Handler;

            var trade = new TradeService(db);
            var result = trade.TryTrade(session, 0, 1, offer, request);
            session.OnEvent -= Handler;

            Assert.IsTrue(result.IsOk, result.Message);
            Assert.NotNull(exchange);
            Assert.AreEqual(ExchangeKind.Trade, exchange!.Kind);
            Assert.AreEqual(2, exchange.Legs.Count);
            Assert.AreEqual(0, exchange.Legs[0].FromPlayerId);
            Assert.AreEqual(1, exchange.Legs[0].ToPlayerId);
            Assert.AreEqual(offer[0], exchange.Legs[0].Items[0].CardInstanceId);
            Assert.AreEqual(request[0], exchange.Legs[1].Items[0].CardInstanceId);
        }

        [Test]
        public void Duel_AttackerWins_EmitsPlayerExchangeEvent()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var ante = PopulateSpread(session, 0, 1, "duel-ex-ante");
            var targets = PopulateSpread(session, 1, 1, "duel-ex-target");
            string targetId = targets[0];
            string anteId = ante[0];

            PlayerExchangeEvent? exchange = null;
            void Handler(IGameEvent e)
            {
                if (e is PlayerExchangeEvent pe) exchange = pe;
            }
            session.OnEvent += Handler;

            var combat = new CombatRules(FindDuelSeed(attackerWins: true));
            var result = combat.TryDuel(session, 0, 1, targetId, anteId);
            session.OnEvent -= Handler;

            Assert.IsTrue(result.IsOk, result.Message);
            Assert.NotNull(exchange);
            Assert.AreEqual(ExchangeKind.Duel, exchange!.Kind);
            Assert.AreEqual(1, exchange.Legs.Count);
            Assert.AreEqual(1, exchange.Legs[0].FromPlayerId);
            Assert.AreEqual(0, exchange.Legs[0].ToPlayerId);
            Assert.AreEqual(targetId, exchange.Legs[0].Items[0].CardInstanceId);
        }

        [Test]
        public void FoolReagentChoice_EmitsPlayerExchangeEvent()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb, playerCount: 3);
            session.Board.PendingFateDecisions.Add((0, "fool-fate", 0));

            var resolver = new FateCardResolver(db);
            PlayerExchangeEvent? exchange = null;
            void Handler(IGameEvent e)
            {
                if (e is PlayerExchangeEvent pe) exchange = pe;
            }
            session.OnEvent += Handler;

            var result = resolver.HandleFoolReagentChoice(session, 1, ReagentType.Salt);
            session.OnEvent -= Handler;

            Assert.IsTrue(result.IsOk, result.Message);
            Assert.NotNull(exchange);
            Assert.AreEqual(ExchangeKind.FateFool, exchange!.Kind);
            Assert.AreEqual(0, exchange.Legs[0].FromPlayerId);
            Assert.AreEqual(1, exchange.Legs[0].ToPlayerId);
            Assert.AreEqual(ReagentType.Salt, exchange.Legs[0].Items[0].ReagentType);
        }

        // ─── SessionInventoryAudit tests ───────────────────────────────────────────

        [Test]
        public void Setup_InventoryIsConsistent()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            AssertInventoryConsistent(session, "after setup");
        }

        [Test]
        public void Setup_MajorArcanaRedeals_HaveDiscardZone()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            foreach (var id in session.Board.CommonDiscard)
            {
                var inst = session.GetCard(id);
                Assert.IsNotNull(inst, $"Discard card {id} missing from registry.");
                Assert.AreEqual(CardZone.Discard, inst!.Zone,
                    $"Card {id} in CommonDiscard should have Zone=Discard.");
            }
        }

        [Test]
        public void FreeArrested_SpendsSalt_RestoresActiveSlot()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var player = session.Players[0];
            var slot = player.CrucibleSlots[0];
            slot.Arrest();
            player.AddReagent(ReagentType.Salt, 1);
            SetSeason(session, Season.Summer);

            var result = session.Apply(new FreeArrestedCommand(0, 0));
            Assert.IsTrue(result.IsOk, result.Message);
            Assert.AreEqual(CrucibleCardState.Active, slot.State);
            Assert.AreEqual(0, player.GetReagent(ReagentType.Salt));
            AssertInventoryConsistent(session, "after FreeArrested");
        }

        [Test]
        public void MoonDecision_InvalidKeepCount_LeavesPoolIntact()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var player = session.Players[0];
            session.Board.PendingFateDecisions.Add((0, "fate-moon", 18));
            var moonIds = new List<string>();
            for (int i = 0; i < 4; i++)
            {
                string id = $"moon-{i}";
                var c = new CardInstance(id, "minor.cups.seven.1", CardZone.Hand, 0);
                session.RegisterCard(c);
                player.Hand.Add(id);
                session.Board.FateMoonDrawnCardIds.Add(id);
                moonIds.Add(id);
            }

            var resolver = new FateCardResolver(db);
            var result = resolver.HandleMoonDecision(session, 0, new List<string> { moonIds[0] });

            Assert.IsFalse(result.IsOk, "Moon must require exactly 2 kept cards.");
            Assert.AreEqual(4, session.Board.FateMoonDrawnCardIds.Count);
            Assert.AreEqual(4, player.Hand.Count);
            AssertInventoryConsistent(session, "after invalid Moon decision");
        }

        [Test]
        public void Duel_AttackerWins_InventoryConsistent()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var ante = PopulateSpread(session, 0, 1, "duel-audit-ante");
            var targets = PopulateSpread(session, 1, 2, "duel-audit-target");

            var combat = new CombatRules(FindDuelSeed(attackerWins: true));
            var result = combat.TryDuel(session, 0, 1, targets[0], ante[0]);

            Assert.IsTrue(result.IsOk, result.Message);
            AssertInventoryConsistent(session, "after duel win");
        }

        // ─── Justice / contest effect tests ─────────────────────────────────────

        static void ApplyJusticeContestEffects(GameSession session)
        {
            var effects = session.Board.ContestEffects;
            effects.DuelBestOfThree = true;
            effects.GambitBestOfThree = true;
            session.Board.ContestEffects = effects;
        }

        [Test]
        public void Justice_Fate_SetsDuelAndGambitBestOfThree()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var resolver = new FateCardResolver(db);

            var fateInst = new CardInstance("fate-justice", "major.fate.11", CardZone.Arcanum, 0);
            session.RegisterCard(fateInst);
            session.Players[0].Arcanum.Add(fateInst.InstanceId);

            resolver.Resolve(session, 0, fateInst.InstanceId, 11);

            Assert.IsTrue(session.Board.ContestEffects.DuelBestOfThree);
            Assert.IsTrue(session.Board.ContestEffects.GambitBestOfThree);
        }

        [Test]
        public void WinterTransit_ClearsContestEffects()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            ApplyJusticeContestEffects(session);

            session.Rules!.Winter.Transit(session);

            Assert.IsFalse(session.Board.ContestEffects.DuelBestOfThree);
            Assert.IsFalse(session.Board.ContestEffects.GambitBestOfThree);
        }

        static int FindJusticeMultiRoundSeed()
        {
            for (int seed = 0; seed < 10000; seed++)
            {
                var series = ContestDiceSeriesResolver.Resolve(
                    new System.Random(seed), 0, 1, bestOfThree: true);
                if (series.Rounds.Count >= 2)
                    return seed;
            }
            Assert.Fail("Could not find justice multi-round seed.");
            return 0;
        }

        [Test]
        public void Duel_WithJustice_EmitsMultiRoundEvent()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            ApplyJusticeContestEffects(session);

            var ante = PopulateSpread(session, 0, 1, "justice-duel-ante");
            var targets = PopulateSpread(session, 1, 1, "justice-duel-target");

            DuelResolvedEvent? resolved = null;
            session.OnEvent += e => { if (e is DuelResolvedEvent d) resolved = d; };

            var combat = new CombatRules(FindJusticeMultiRoundSeed());
            var result = combat.TryDuel(session, 0, 1, targets[0], ante[0]);

            Assert.IsTrue(result.IsOk, result.Message);
            Assert.NotNull(resolved);
            Assert.GreaterOrEqual(resolved!.Rounds.Count, 2);
            Assert.AreEqual(resolved.Rounds.Count,
                resolved.AttackerRoundWins + resolved.DefenderRoundWins);
            Assert.GreaterOrEqual(
                System.Math.Max(resolved.AttackerRoundWins, resolved.DefenderRoundWins), 2);
        }

        [Test]
        public void Gambit_WithJustice_EmitsMultiRoundEvent()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            ApplyJusticeContestEffects(session);

            const string offeredId = "justice-gambit-offer";
            var offeredInst = new CardInstance(offeredId, "minor.cups.seven.1", CardZone.Arcanum, 0);
            session.RegisterCard(offeredInst);
            session.Players[0].Arcanum.Add(offeredId);
            PopulateSpread(session, 1, 1, "justice-gambit-def");

            GambitResolvedEvent? resolved = null;
            session.OnEvent += e => { if (e is GambitResolvedEvent g) resolved = g; };

            var combat = new CombatRules(FindJusticeMultiRoundSeed());
            var result = combat.TryGambit(session, 0, 1, offeredId);

            Assert.IsTrue(result.IsOk, result.Message);
            Assert.NotNull(resolved);
            Assert.GreaterOrEqual(resolved!.Rounds.Count, 2);
        }

        [Test]
        public void Opposition_WithJustice_StillSingleRoll()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            ApplyJusticeContestEffects(session);
            session.Players[1].StoneState = StoneState.Forging;
            SetSeason(session, Season.Autumn);
            session.CurrentTurnPlayerId = 0;

            OppositionResolvedEvent? resolved = null;
            session.OnEvent += e => { if (e is OppositionResolvedEvent o) resolved = o; };

            var initiate = session.Apply(new InitiateOppositionCommand(0, 1));
            Assert.IsTrue(initiate.IsOk, initiate.Message);
            var respond = session.Apply(new RespondOppositionCommand(1, accept: true));
            Assert.IsTrue(respond.IsOk, respond.Message);

            Assert.NotNull(resolved);
            Assert.GreaterOrEqual(resolved!.AttackRoll, 1);
            Assert.LessOrEqual(resolved.AttackRoll, 12);
            Assert.GreaterOrEqual(resolved.DefendRoll, 1);
            Assert.LessOrEqual(resolved.DefendRoll, 12);
        }

        // ─── Phase 1 contest modifier tests ─────────────────────────────────────

        static void AddSpreadCard(GameSession session, int playerId, string instanceId, string definitionId)
        {
            var inst = new CardInstance(instanceId, definitionId, CardZone.Spread, playerId);
            session.RegisterCard(inst);
            session.Players[playerId].Spread.Add(instanceId);
        }

        static int FindDuelEqualRollSeed(int rollValue)
        {
            for (int seed = 0; seed < 20000; seed++)
            {
                var rng = new System.Random(seed);
                if (rng.Next(1, 13) == rollValue && rng.Next(1, 13) == rollValue)
                    return seed;
            }
            Assert.Fail($"No duel equal-roll seed for value {rollValue}.");
            return 0;
        }

        static int FindSixSwordsMultiRoundSeed()
        {
            for (int seed = 0; seed < 20000; seed++)
            {
                var options = ContestResolveOptions.Simple(ContestKind.Duel, bestOfThree: true);
                var series = ContestDiceSeriesResolver.Resolve(new System.Random(seed), 0, 1, options);
                if (series.Rounds.Count >= 2)
                    return seed;
            }
            Assert.Fail("No six-swords multi-round seed.");
            return 0;
        }

        [Test]
        public void Duel_KnightOfSwords_FlipsTieToAttackerWin()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            int seed = FindDuelEqualRollSeed(5);

            var baselineSession = SetupSession(db, codexDb);
            var baselineAnte = PopulateSpread(baselineSession, 0, 1, "knight-duel-ante");
            var baselineTargets = PopulateSpread(baselineSession, 1, 1, "knight-duel-target");

            DuelResolvedEvent? baseline = null;
            baselineSession.OnEvent += e => { if (e is DuelResolvedEvent d) baseline = d; };
            var combatBaseline = new CombatRules(seed);
            Assert.IsTrue(combatBaseline.TryDuel(baselineSession, 0, 1, baselineTargets[0], baselineAnte[0]).IsOk);
            Assert.AreEqual(1, baseline!.WinnerId, "Tie should favor defender without modifiers.");

            var boostedSession = SetupSession(db, codexDb);
            var ante = PopulateSpread(boostedSession, 0, 1, "knight-duel-ante-b");
            var targets = PopulateSpread(boostedSession, 1, 1, "knight-duel-target-b");
            AddSpreadCard(boostedSession, 0, "knight-swords", "minor.swords.knight.1");

            DuelResolvedEvent? boosted = null;
            boostedSession.OnEvent += e => { if (e is DuelResolvedEvent d) boosted = d; };
            var combatBoosted = new CombatRules(seed);
            Assert.IsTrue(combatBoosted.TryDuel(boostedSession, 0, 1, targets[0], ante[0]).IsOk);
            Assert.AreEqual(0, boosted!.WinnerId, "Knight of Swords should flip tied duel to attacker.");
        }

        [Test]
        public void SixOfSwords_AttackerOnlyBestOfThree()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            Assert.IsFalse(session.Board.ContestEffects.DuelBestOfThree);

            var ante = PopulateSpread(session, 0, 1, "six-swords-ante");
            var targets = PopulateSpread(session, 1, 1, "six-swords-target");
            AddSpreadCard(session, 0, "six-swords", "minor.swords.six.1");

            DuelResolvedEvent? resolved = null;
            session.OnEvent += e => { if (e is DuelResolvedEvent d) resolved = d; };

            var combat = new CombatRules(FindSixSwordsMultiRoundSeed());
            Assert.IsTrue(combat.TryDuel(session, 0, 1, targets[0], ante[0]).IsOk);
            Assert.GreaterOrEqual(resolved!.Rounds.Count, 2);
        }

        [Test]
        public void Justice_And_SixOfSwords_StillBestOfThree()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            ApplyJusticeContestEffects(session);

            var ante = PopulateSpread(session, 0, 1, "justice-six-ante");
            var targets = PopulateSpread(session, 1, 1, "justice-six-target");
            AddSpreadCard(session, 0, "six-swords", "minor.swords.six.1");

            DuelResolvedEvent? resolved = null;
            session.OnEvent += e => { if (e is DuelResolvedEvent d) resolved = d; };

            var combat = new CombatRules(FindJusticeMultiRoundSeed());
            Assert.IsTrue(combat.TryDuel(session, 0, 1, targets[0], ante[0]).IsOk);
            Assert.GreaterOrEqual(resolved!.Rounds.Count, 2);
            Assert.AreEqual(resolved.Rounds.Count,
                resolved.AttackerRoundWins + resolved.DefenderRoundWins);
        }

        [Test]
        public void FiveOfCups_Defender_GrantsAttackerReroll()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);

            var ante = PopulateSpread(session, 0, 1, "cups5-ante");
            var targets = PopulateSpread(session, 1, 1, "cups5-target");
            AddSpreadCard(session, 1, "cups-five", "minor.cups.five.1");

            int seed = FindCups5RerollWinSeed();

            DuelResolvedEvent? resolved = null;
            session.OnEvent += e => { if (e is DuelResolvedEvent d) resolved = d; };

            var combat = new CombatRules(seed);
            Assert.IsTrue(combat.TryDuel(session, 0, 1, targets[0], ante[0]).IsOk);
            Assert.NotNull(resolved);
            Assert.AreEqual(8, resolved!.Rounds[0].RawAttackRoll);
            Assert.AreEqual(5, resolved.Rounds[0].RawDefendRoll);
            Assert.AreEqual(0, resolved.WinnerId);
        }

        static int FindCups5RerollWinSeed()
        {
            var options = new ContestResolveOptions(
                ContestKind.Duel,
                false,
                new ContestRollModifiers { MayRerollAttack = true },
                ContestRollModifiers.Default);
            for (int seed = 0; seed < 50000; seed++)
            {
                var series = ContestDiceSeriesResolver.Resolve(new System.Random(seed), 0, 1, options);
                if (series.Rounds[0].RawAttackRoll == 8
                    && series.Rounds[0].RawDefendRoll == 5
                    && series.WinnerId == 0)
                    return seed;
            }
            Assert.Fail("Could not find seed demonstrating Cups 5 reroll win.");
            return 0;
        }

        [Test]
        public void Trade_Quickplay_InventoryConsistent()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb, mode: GameMode.Quickplay);
            var offer = PopulateSpread(session, 0, 1, "trade-audit");
            var request = PopulateSpread(session, 1, 1, "trade-audit");

            var trade = new TradeService(db);
            var result = trade.TryTrade(session, 0, 1, offer, request);

            Assert.IsTrue(result.IsOk, result.Message);
            AssertInventoryConsistent(session, "after trade");
        }

        [Test]
        public void Commune_ReassignsCards_InventoryConsistent()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var player = session.Players[0];
            foreach (var id in player.Spread.ToList())
            {
                if (!session.Board.CommonDiscard.Contains(id))
                    session.Board.CommonDiscard.Add(id);
                session.GetCard(id)?.MoveTo(CardZone.Discard, -1);
            }
            foreach (var id in player.Hand.ToList())
            {
                if (!session.Board.CommonDiscard.Contains(id))
                    session.Board.CommonDiscard.Add(id);
                session.GetCard(id)?.MoveTo(CardZone.Discard, -1);
            }
            player.Hand.Clear();
            player.Spread.Clear();
            var spreadIds = new List<string>();
            var handIds = new List<string>();
            for (int i = 0; i < 2; i++)
            {
                string sid = $"commune-s-{i}";
                var sc = new CardInstance(sid, "minor.cups.seven.1", CardZone.Spread, 0);
                session.RegisterCard(sc);
                player.Spread.Add(sid);
                spreadIds.Add(sid);
            }
            for (int i = 0; i < 2; i++)
            {
                string hid = $"commune-h-{i}";
                var hc = new CardInstance(hid, "minor.wands.three.1", CardZone.Hand, 0);
                session.RegisterCard(hc);
                player.Hand.Add(hid);
                handIds.Add(hid);
            }

            var result = session.Apply(new CommuneCommand(0, spreadIds, handIds));
            Assert.IsTrue(result.IsOk, result.Message);
            AssertInventoryConsistent(session, "after commune");
        }

        [Test]
        public void FatefulWager_PlaceAndResolve_InventoryConsistent()
        {
            var db = LoadDb(); var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            var player = session.Players[0];
            var cardIds = new List<string>();
            foreach (var id in player.Spread)
                cardIds.Add(id);
            if (cardIds.Count == 0)
            {
                cardIds.AddRange(PopulateSpread(session, 0, 1, "wager-audit"));
            }
            else
            {
                cardIds = cardIds.GetRange(0, Math.Min(1, cardIds.Count));
            }

            var place = session.Apply(new PlaceFatefulWagerCommand(0, ZodiacSign.Aries, cardIds));
            Assert.IsTrue(place.IsOk, place.Message);
            AssertInventoryConsistent(session, "after wager place");

            session.Board.CosmicAgeSign = ZodiacSign.Aries;
            session.Rules!.Winter.ResolveWagers(session, ZodiacSign.Aries);
            AssertInventoryConsistent(session, "after wager resolve");
        }

        private sealed class SeededContestRng : System.Random
        {
            readonly Queue<int> _values = new();

            public SeededContestRng(params int[] rounds)
            {
                foreach (int v in rounds)
                    _values.Enqueue(v);
            }

            public override int Next(int minValue, int maxValue)
            {
                Assert.IsTrue(_values.Count > 0, "SeededContestRng exhausted.");
                return _values.Dequeue();
            }
        }

        // ─── Phase 2 craft & harvest modifier tests ───────────────────────────────

        static void AddArcanumCard(GameSession session, int playerId, string instanceId, string definitionId)
        {
            var inst = new CardInstance(instanceId, definitionId, CardZone.Arcanum, playerId);
            session.RegisterCard(inst);
            session.Players[playerId].Arcanum.Add(instanceId);
        }

        static List<string> GivePlayerSuitCards(GameSession session, CardDatabase db,
            int playerId, Suit suit, int count)
        {
            var added = new List<string>(count);
            int idx = 0;
            foreach (var def in db.GetBySuit(suit))
            {
                if (idx >= count) break;
                var id = $"test-{suit}-{playerId}-{idx}";
                var inst = new CardInstance(id, def.Id, CardZone.Spread, playerId);
                session.RegisterCard(inst);
                session.Players[playerId].Spread.Add(id);
                added.Add(id);
                idx++;
            }
            return added;
        }

        [Test]
        public void AceV2_WaterCosmic_AddsTwoHarvest()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            session.Board.CosmicAgeSign = ZodiacSign.Cancer;
            session.Players[0].CurrentSign = ZodiacSign.Aries;

            int before = session.Rules!.Harvest.CalculateHarvestCount(session, 0);
            AddSpreadCard(session, 0, "ace-cups", "minor.cups.ace.2");
            int after = session.Rules.Harvest.CalculateHarvestCount(session, 0);

            // Ace V2 +2 plus existing spread element match (+1) for Water cosmic + Cups suit.
            Assert.AreEqual(before + 3, after);
            Assert.AreEqual(2, HarvestModifierService.SpreadPassiveBonus(session, 0));
        }

        [Test]
        public void Rank2_WaterHouse_DoublesHouseBonus()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            session.Board.CosmicAgeSign = ZodiacSign.Cancer;
            session.Players[0].CurrentSign = ZodiacSign.Aries;
            session.Players[0].AstralHouses.Add(ZodiacSign.Pisces);

            int before = session.Rules!.Harvest.CalculateHarvestCount(session, 0);
            AddSpreadCard(session, 0, "two-cups", "minor.cups.two.1");
            int after = session.Rules.Harvest.CalculateHarvestCount(session, 0);

            int housePts = HarvestBreakdownService.AlignmentBonus(ZodiacSign.Pisces, ZodiacSign.Cancer);
            // House doubling +housePts plus existing spread element match (+1) for Water cosmic + Cups suit.
            Assert.AreEqual(before + housePts + 1, after);
            Assert.AreEqual(housePts, HarvestModifierService.HouseDoublingBonus(session, 0));
        }

        [Test]
        public void Rank8_ReducesElementalCost()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            AddSpreadCard(session, 0, "eight-wands", "minor.wands.eight.1");
            var cards = GivePlayerSuitCards(session, db, 0, Suit.Wands, 2);
            session.Players[0].LightCauldron(Suit.Wands);

            var result = session.Apply(new CraftReagentCommand(0, ReagentType.Sulphur, cards));
            Assert.IsTrue(result.IsOk, result.Message);
            Assert.AreEqual(1, session.Players[0].GetReagent(ReagentType.Sulphur));
        }

        [Test]
        public void BuildV1_SaltWithTwoSuitCards()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            AddSpreadCard(session, 0, "three-swords", "minor.swords.three.1");
            var cards = GivePlayerSuitCards(session, db, 0, Suit.Swords, 2);

            var result = session.Apply(new CraftReagentCommand(0, ReagentType.Salt, cards));
            Assert.IsTrue(result.IsOk, result.Message);
            Assert.AreEqual(1, session.Players[0].GetReagent(ReagentType.Salt));
        }

        [Test]
        public void KingOfCups_DiscardSelfCraftsAquaRegia()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            AddSpreadCard(session, 0, "king-cups", "minor.cups.king.1");
            session.Players[0].LightCauldron(Suit.Cups);

            var result = session.Apply(new CraftKingReagentCommand(0, "king-cups"));
            Assert.IsTrue(result.IsOk, result.Message);
            Assert.AreEqual(1, session.Players[0].GetReagent(ReagentType.AquaRegia));
            Assert.IsFalse(session.Players[0].Spread.Contains("king-cups"));
        }

        [Test]
        public void EmpressMark_SaltThenTwoCardVitriol()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            AddArcanumCard(session, 0, "empress", "major.adept.3");
            session.Players[0].AddReagent(ReagentType.Salt);

            var mark = session.Apply(new MarkEmpressReagentCommand(0, ReagentType.Vitriol));
            Assert.IsTrue(mark.IsOk, mark.Message);

            var cards = GivePlayerSuitCards(session, db, 0, Suit.Pentacles, 2);
            session.Players[0].LightCauldron(Suit.Pentacles);
            var craft = session.Apply(new CraftReagentCommand(0, ReagentType.Vitriol, cards));
            Assert.IsTrue(craft.IsOk, craft.Message);
            Assert.AreEqual(1, session.Players[0].GetReagent(ReagentType.Vitriol));
        }

        [Test]
        public void TemperanceBase_SaltTwoAnyCards()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            AddArcanumCard(session, 0, "temperance", "major.adept.14");
            GivePlayerCards(session, 0, 2);

            var cards = session.Players[0].Spread.GetRange(0, 2);
            var result = session.Apply(new CraftReagentCommand(0, ReagentType.Salt, cards));
            Assert.IsTrue(result.IsOk, result.Message);
            Assert.AreEqual(1, session.Players[0].GetReagent(ReagentType.Salt));
        }

        [Test]
        public void EmpressResonant_AllowsTwoMarks_WithOneSalt()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            AddArcanumCard(session, 0, "empress", "major.adept.3");
            session.Players[0].CurrentSign = ZodiacSign.Taurus;
            session.Players[0].AddReagent(ReagentType.Salt);

            Assert.IsTrue(session.Apply(new MarkEmpressReagentCommand(0, ReagentType.Vitriol)).IsOk);
            Assert.IsTrue(session.Apply(new MarkEmpressReagentCommand(0, ReagentType.Sulphur)).IsOk);
            Assert.AreEqual(2, session.Players[0].EmpressMarkedReagents.Count);
            Assert.IsFalse(session.Apply(new MarkEmpressReagentCommand(0, ReagentType.AquaRegia)).IsOk);
        }

        [Test]
        public void TemperanceResonant_WildPayment_ForMarkedReagent()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            AddArcanumCard(session, 0, "temperance", "major.adept.14");
            session.Players[0].CurrentSign = ZodiacSign.Libra;
            session.Players[0].AddReagent(ReagentType.Salt);

            Assert.IsTrue(session.Apply(new MarkTemperanceWildReagentCommand(0, ReagentType.Vitriol)).IsOk);

            var pentacles = GivePlayerSuitCards(session, db, 0, Suit.Pentacles, 2);
            var wands = GivePlayerSuitCards(session, db, 0, Suit.Wands, 1);
            var payment = new List<string> { pentacles[0], pentacles[1], wands[0] };
            session.Players[0].LightCauldron(Suit.Pentacles);

            var result = session.Apply(new CraftReagentCommand(0, ReagentType.Vitriol, payment));
            Assert.IsTrue(result.IsOk, result.Message);
            Assert.AreEqual(1, session.Players[0].GetReagent(ReagentType.Vitriol));
        }

        [Test]
        public void Transit_Clears_EmpressAndTemperanceCraftMarks()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            session.Players[0].EmpressMarkedReagents.Add(ReagentType.Sulphur);
            session.Players[0].TemperanceSaltWildReagent = ReagentType.Vitriol;

            session.Rules!.Winter.Transit(session);

            Assert.AreEqual(0, session.Players[0].EmpressMarkedReagents.Count);
            Assert.IsNull(session.Players[0].TemperanceSaltWildReagent);
        }
    }
}
