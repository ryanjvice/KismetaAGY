using System.IO;
using System.Linq;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.Data.Loaders;
using NUnit.Framework;
using UnityEngine;

namespace Kismeta.Core.Tests
{
    public sealed class ActiveEffectsService_Tests
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

        static GameSession BuildSession(CardDatabase db, CrucibleCodexDatabase codexDb)
        {
            var players = new[]
            {
                new PlayerState(0, PlayerColor.Red) { IsAgekeeper = true },
                new PlayerState(1, PlayerColor.Green)
            };
            var rules = new GameRuleSet(
                cardDatabase: db,
                codexDatabase: codexDb,
                setup: new GameSetupService(db, 42),
                harvest: new SpringRules(db, 42),
                crucible: new CrucibleRules(db, codexDb, seed: 42),
                crafting: new CraftingRules(db),
                winter: new WinterRules(db),
                validator: new ActionValidator());
            return new GameSession("test", GameMode.Quickplay, players, rules, CrucibleBuildMode.Curated);
        }

        static void AddSpreadCard(GameSession session, int playerId, string instanceId, string definitionId)
        {
            var inst = new CardInstance(instanceId, definitionId, CardZone.Spread, playerId);
            session.RegisterCard(inst);
            session.Players[playerId].Spread.Add(instanceId);
        }

        static ActiveEffectSection SpreadSection(ActiveEffectsSnapshot snapshot) => snapshot.Sections[3];

        [Test]
        public void Build_CosmicAgeFeatured_IncludesSubtitleAndFooter()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = BuildSession(db, codexDb);
            session.Board.CosmicAgeSign = ZodiacSign.Scorpio;
            new CosmicEffectService().Apply(session, ZodiacSign.Scorpio);

            var snapshot = ActiveEffectsService.Build(session, 0);

            StringAssert.Contains("Age of Scorpio", snapshot.Subtitle);
            StringAssert.Contains("Court Cups", snapshot.CosmicAge.Title);
            StringAssert.AreEqualIgnoringCase("expires at age end", snapshot.CosmicAge.Badge.Text);
            Assert.IsNotNull(snapshot.CosmicAge.Footer);
            StringAssert.Contains("Agekeeper", snapshot.CosmicAge.Footer);
        }

        [Test]
        public void Build_AstralHouse_HasPermanentBadge()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = BuildSession(db, codexDb);
            session.Board.CosmicAgeSign = ZodiacSign.Scorpio;
            session.Players[0].AstralHouses.Add(ZodiacSign.Aries);

            var snapshot = ActiveEffectsService.Build(session, 0);
            var houses = snapshot.Sections[0];

            Assert.AreEqual("astral-houses", houses.SectionId);
            Assert.AreEqual(1, houses.Items.Count);
            Assert.AreEqual("permanent", houses.Items[0].Badge.Text);
            Assert.AreEqual(ActiveEffectBadgeTone.Permanent, houses.Items[0].Badge.Tone);
        }

        [Test]
        public void Build_Adept_OncePerAge_ShowsAvailableThenUsed()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = BuildSession(db, codexDb);
            const string adeptId = "adept-empress";
            var inst = new CardInstance(adeptId, "major.adept.3", CardZone.Arcanum, 0);
            session.RegisterCard(inst);
            session.Players[0].Arcanum.Add(adeptId);

            var available = ActiveEffectsService.Build(session, 0);
            var adepts = available.Sections[1];
            Assert.AreEqual("once per age · available", adepts.Items[0].Badge.Text);

            ActiveEffectsService.MarkAdeptUsed(session, 0, adeptId);
            var used = ActiveEffectsService.Build(session, 0);
            Assert.AreEqual("once per age · used", used.Sections[1].Items[0].Badge.Text);
        }

        [Test]
        public void Build_Fate_UsesResolutionNote()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = BuildSession(db, codexDb);
            const string fateId = "fate-wheel";
            var inst = new CardInstance(fateId, "major.fate.10", CardZone.Arcanum, 0);
            inst.SetFateResolutionNote("Already resolved: drew 3 cards, lost 1 reagent.");
            session.RegisterCard(inst);
            session.Players[0].Arcanum.Add(fateId);

            var snapshot = ActiveEffectsService.Build(session, 0);
            var fates = snapshot.Sections[2];

            Assert.AreEqual("Already resolved: drew 3 cards, lost 1 reagent.", fates.Items[0].Description);
            Assert.AreEqual("resolved · face-up", fates.Items[0].Badge.Text);
        }

        [Test]
        public void Build_SpreadCards_SplitsActiveAndInactive()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = BuildSession(db, codexDb);
            session.Board.CosmicAgeSign = ZodiacSign.Scorpio;

            AddSpreadCard(session, 0, "spread-mars", "minor.cups.seven.1");
            AddSpreadCard(session, 0, "spread-idle", "minor.pentacles.two.1");

            var spread = SpreadSection(ActiveEffectsService.Build(session, 0));

            Assert.AreEqual("1 active", spread.BadgeText);
            Assert.AreEqual(1, spread.Items.Count);
            StringAssert.Contains("forge · active", spread.Items[0].Badge.Text);
            Assert.AreEqual(ActiveEffectPolarity.Buff, spread.Items[0].Polarity);
            Assert.IsNotNull(spread.FooterNote);
            StringAssert.Contains("1 other spread card", spread.FooterNote);
        }

        [Test]
        public void Build_SpreadCards_Reversed_IsDebuff()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = BuildSession(db, codexDb);
            session.Board.CosmicAgeSign = ZodiacSign.Cancer;

            AddSpreadCard(session, 0, "spread-reversed", "minor.pentacles.four.1");

            var item = SpreadSection(ActiveEffectsService.Build(session, 0)).Items[0];

            StringAssert.Contains("reversed", item.Badge.Text);
            Assert.AreEqual(ActiveEffectPolarity.Debuff, item.Polarity);
            StringAssert.Contains("offer +2 Resources", item.Description);
        }

        [Test]
        public void Build_SpreadCards_PassiveCosmicMatch_IsBuff()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = BuildSession(db, codexDb);
            session.Board.CosmicAgeSign = ZodiacSign.Cancer;

            AddSpreadCard(session, 0, "spread-passive", "minor.cups.ace.2");

            var item = SpreadSection(ActiveEffectsService.Build(session, 0)).Items[0];

            Assert.AreEqual(ActiveEffectPolarity.Buff, item.Polarity);
            StringAssert.Contains("Bonus Harvest Cards", item.Description);
            StringAssert.Contains("passive · active", item.Badge.Text);
        }

        [Test]
        public void Build_SpreadCards_PassiveCosmicMismatch_IsInactive()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = BuildSession(db, codexDb);
            session.Board.CosmicAgeSign = ZodiacSign.Aries;

            AddSpreadCard(session, 0, "spread-passive", "minor.cups.ace.2");

            var spread = SpreadSection(ActiveEffectsService.Build(session, 0));

            Assert.AreEqual("0 active", spread.BadgeText);
            Assert.AreEqual(0, spread.Items.Count);
        }

        [Test]
        public void Build_SpreadCards_Gambit_IsBuff()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = BuildSession(db, codexDb);
            session.Board.CosmicAgeSign = ZodiacSign.Aries;

            AddSpreadCard(session, 0, "spread-gambit", "minor.wands.princess.1");

            var item = SpreadSection(ActiveEffectsService.Build(session, 0)).Items[0];

            Assert.AreEqual(ActiveEffectPolarity.Buff, item.Polarity);
            StringAssert.Contains("gambit · active", item.Badge.Text);
        }

        [Test]
        public void Build_SpreadCards_WildcardLink_IsNeutral()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = BuildSession(db, codexDb);
            session.Board.CosmicAgeSign = ZodiacSign.Aries;

            AddSpreadCard(session, 0, "spread-wildcard", "minor.pentacles.princess.2");

            var item = SpreadSection(ActiveEffectsService.Build(session, 0)).Items[0];

            Assert.AreEqual(ActiveEffectPolarity.Neutral, item.Polarity);
            StringAssert.Contains("wildcard link", item.Badge.Text);
        }

        [Test]
        public void Build_SpreadCards_ActionCard_IsNeutral()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = BuildSession(db, codexDb);
            session.Board.CosmicAgeSign = ZodiacSign.Aries;

            AddSpreadCard(session, 0, "spread-action", "minor.cups.three.1");

            var item = SpreadSection(ActiveEffectsService.Build(session, 0)).Items[0];

            Assert.AreEqual(ActiveEffectPolarity.Neutral, item.Polarity);
            Assert.AreEqual("action · available", item.Badge.Text);
        }

        [Test]
        public void Build_SpreadCards_SortsBuffsBeforeDebuffs()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = BuildSession(db, codexDb);
            session.Board.CosmicAgeSign = ZodiacSign.Cancer;

            AddSpreadCard(session, 0, "spread-reversed", "minor.pentacles.four.1");
            AddSpreadCard(session, 0, "spread-gambit", "minor.wands.princess.1");
            AddSpreadCard(session, 0, "spread-wildcard", "minor.pentacles.princess.2");

            var items = SpreadSection(ActiveEffectsService.Build(session, 0)).Items;

            Assert.AreEqual(3, items.Count);
            Assert.AreEqual(ActiveEffectPolarity.Buff, items[0].Polarity);
            Assert.AreEqual(ActiveEffectPolarity.Neutral, items[1].Polarity);
            Assert.AreEqual(ActiveEffectPolarity.Debuff, items[2].Polarity);
            Assert.IsTrue(items.Select(i => i.Polarity).SequenceEqual(new[]
            {
                ActiveEffectPolarity.Buff,
                ActiveEffectPolarity.Neutral,
                ActiveEffectPolarity.Debuff
            }));
        }

        [Test]
        public void BuildDuelRelevant_BestOfThree_IncludesCosmicAgeFeatured()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = BuildSession(db, codexDb);
            session.Board.CosmicAgeSign = ZodiacSign.Libra;
            session.Board.ContestEffects = new ContestEffectFlags
            {
                DuelBestOfThree = true,
                GambitBestOfThree = true
            };

            var snapshot = ActiveEffectsService.BuildDuelRelevant(session, 0, 1, null, null);

            StringAssert.Contains("best-of-three", snapshot.CosmicAge.Description);
            StringAssert.Contains("Gambits", snapshot.CosmicAge.Description);
        }

        [Test]
        public void BuildDuelRelevant_FiltersCombatSpreadCardsAndStakedCards()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = BuildSession(db, codexDb);
            session.Board.CosmicAgeSign = ZodiacSign.Scorpio;

            AddSpreadCard(session, 0, "def-duel", "minor.cups.knight.1");
            AddSpreadCard(session, 0, "def-target", "minor.pentacles.two.1");
            AddSpreadCard(session, 0, "def-idle", "minor.cups.ace.2");
            AddSpreadCard(session, 1, "att-ante", "minor.wands.seven.1");

            var snapshot = ActiveEffectsService.BuildDuelRelevant(
                session, 0, 1, "def-target", "att-ante");

            var spreadSections = snapshot.Sections.Where(s => s.SectionId.StartsWith("spread-cards")).ToList();
            Assert.AreEqual(2, spreadSections.Count);

            var yours = spreadSections.First(s => s.Title == "Your spread");
            Assert.AreEqual(2, yours.Items.Count);
            Assert.IsTrue(yours.Items.Any(i => i.Id == "def-duel"));
            Assert.IsTrue(yours.Items.Any(i => i.Id == "def-target"));

            var theirs = spreadSections.First(s => s.Title.Contains("Green"));
            Assert.AreEqual(1, theirs.Items.Count);
            Assert.AreEqual("att-ante", theirs.Items[0].Id);
        }
    }
}
