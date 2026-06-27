using System.IO;
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

            var aligned = new CardInstance("spread-mars", "minor.cups.seven.1", CardZone.Spread, 0);
            var inactive = new CardInstance("spread-idle", "minor.pentacles.two.1", CardZone.Spread, 0);
            session.RegisterCard(aligned);
            session.RegisterCard(inactive);
            session.Players[0].Spread.Add(aligned.InstanceId);
            session.Players[0].Spread.Add(inactive.InstanceId);

            var snapshot = ActiveEffectsService.Build(session, 0);
            var spread = snapshot.Sections[3];

            Assert.AreEqual("1 active", spread.BadgeText);
            Assert.AreEqual(1, spread.Items.Count);
            Assert.AreEqual("aligned this age", spread.Items[0].Badge.Text);
            Assert.IsNotNull(spread.FooterNote);
            StringAssert.Contains("1 other spread card", spread.FooterNote);
        }
    }
}
