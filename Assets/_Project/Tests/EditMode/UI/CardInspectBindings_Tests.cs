using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Rules;
using Kismeta.Data.Loaders;
using Kismeta.UI.Components;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Tests
{
    public sealed class CardInspectBindings_Tests
    {
        const string CardModalsPath = "Assets/_Project/UI/UXML/batch7/CardModals.uxml";

        CardDatabase _db = null!;

        [OneTimeSetUp]
        public void LoadDatabase()
        {
            var path = Path.Combine(Application.dataPath,
                "_Project/Scripts/Data/Generated/cards.json");
            _db = CardDatabase.LoadFromJson(File.ReadAllText(path));
        }

        [Test]
        public void SevenOfSwords_BindsAspectsSubtitleAndGoodForTags()
        {
            var modal = InstantiateInspectModal();
            var session = BuildSession("minor.swords.seven.1", ZodiacSign.Scorpio);

            CardInspectBindings.BindInspectModal(modal, session, "inspect-card");

            Assert.AreEqual("Seven of Swords", modal.Q<Label>("inspect-name")?.text);
            Assert.AreEqual("minor arcana · rank 7", modal.Q<Label>("inspect-subtitle")?.text);
            Assert.AreEqual("Air", modal.Q<Label>("inspect-aspect-element")?.text);
            Assert.AreEqual("Mars", modal.Q<Label>("inspect-aspect-planet")?.text);
            Assert.AreEqual("Swords", modal.Q<Label>("inspect-aspect-suit")?.text);

            var hero = modal.Q<VisualElement>("inspect-hero-tarot");
            Assert.IsTrue(hero!.ClassListContains("card-chip--swords"));

            var tags = modal.Q<VisualElement>("inspect-good-for")!.Children()
                .OfType<Label>()
                .Select(l => l.text)
                .ToList();
            CollectionAssert.Contains(tags, "crafting Quicksilver");
            CollectionAssert.Contains(tags, "Mars sets");
            CollectionAssert.Contains(tags, "alignment +2");
            Assert.AreEqual("its Mars matches the age's planet",
                modal.Q<Label>("inspect-align-why")?.text);
        }

        [Test]
        public void AceOfPentacles_BindsEarthSunVitriolAndRankOne()
        {
            var modal = InstantiateInspectModal();
            var session = BuildSession("minor.pentacles.ace.1", ZodiacSign.Taurus);

            CardInspectBindings.BindInspectModal(modal, session, "inspect-card");

            Assert.AreEqual("Ace of Pentacles", modal.Q<Label>("inspect-name")?.text);
            Assert.AreEqual("minor arcana · rank 1", modal.Q<Label>("inspect-subtitle")?.text);
            Assert.AreEqual("Earth", modal.Q<Label>("inspect-aspect-element")?.text);
            Assert.AreEqual("Sun", modal.Q<Label>("inspect-aspect-planet")?.text);
            Assert.AreEqual("Pentacles", modal.Q<Label>("inspect-aspect-suit")?.text);

            var elementIcon = modal.Q<Label>("inspect-aspect-element-icon");
            var suitIcon = modal.Q<Label>("inspect-aspect-suit-icon");
            Assert.AreEqual(SymbolGlyphs.ElementGlyph(Element.Earth), elementIcon?.text);
            Assert.AreEqual(SymbolGlyphs.SuitGlyph(Suit.Pentacles), suitIcon?.text);
            Assert.IsTrue(elementIcon!.ClassListContains(SymbolGlyphs.TablerIconClass));
            Assert.IsTrue(suitIcon!.ClassListContains(SymbolGlyphs.TablerIconClass));

            var hero = modal.Q<VisualElement>("inspect-hero-tarot");
            Assert.IsTrue(hero!.ClassListContains("card-chip--pentacles"));

            var tags = modal.Q<VisualElement>("inspect-good-for")!.Children()
                .OfType<Label>()
                .Select(l => l.text)
                .ToList();
            CollectionAssert.Contains(tags, "crafting Vitriol");
            CollectionAssert.Contains(tags, "Sun sets");
            CollectionAssert.Contains(tags, "alignment +1");
            Assert.AreEqual("its Earth matches the age's element",
                modal.Q<Label>("inspect-align-why")?.text);
        }

        [Test]
        public void TwoOfPentacles_BindsHarvestEffectSection()
        {
            var modal = InstantiateInspectModal();
            var session = BuildSession("minor.pentacles.two.1", ZodiacSign.Scorpio);

            CardInspectBindings.BindInspectModal(modal, session, "inspect-card");

            var section = modal.Q<VisualElement>("inspect-effect-section");
            Assert.IsNotNull(section);
            Assert.AreEqual(DisplayStyle.Flex, section!.style.display);

            Assert.AreEqual("Harvest", modal.Q<Label>("inspect-effect-eyebrow")?.text);
            Assert.AreEqual(
                "Astral Houses on Earth signs earn double at Harvest.",
                modal.Q<Label>("inspect-effect")?.text);
        }

        [Test]
        public void KingOfCups_BindsPassiveEffectAndGoodForTags()
        {
            var modal = InstantiateInspectModal();
            var session = BuildSession("minor.cups.king.2", ZodiacSign.Aries);

            CardInspectBindings.BindInspectModal(modal, session, "inspect-card");

            Assert.AreEqual("Passive", modal.Q<Label>("inspect-effect-eyebrow")?.text);
            Assert.AreEqual(
                "Cups are protected from Duels. This card remains vulnerable.",
                modal.Q<Label>("inspect-effect")?.text);

            var tags = modal.Q<VisualElement>("inspect-good-for")!.Children()
                .OfType<Label>()
                .Select(l => l.text)
                .ToList();
            CollectionAssert.Contains(tags, "crafting Aqua Regia");
            CollectionAssert.Contains(tags, "Saturn sets");
        }

        [Test]
        public void NineOfWands_BindsElementAndSuitTablerIcons()
        {
            var modal = InstantiateInspectModal();
            var session = BuildSession("minor.wands.nine.2", ZodiacSign.Aries);

            CardInspectBindings.BindInspectModal(modal, session, "inspect-card");

            var elementIcon = modal.Q<Label>("inspect-aspect-element-icon");
            var suitIcon = modal.Q<Label>("inspect-aspect-suit-icon");
            var heroIcon = modal.Q<Label>("inspect-hero-icon");
            Assert.IsNotNull(elementIcon);
            Assert.IsNotNull(suitIcon);
            Assert.IsNotNull(heroIcon);

            Assert.AreEqual(SymbolGlyphs.ElementGlyph(Element.Fire), elementIcon!.text);
            Assert.AreEqual(SymbolGlyphs.SuitGlyph(Suit.Wands), suitIcon!.text);
            Assert.AreEqual(SymbolGlyphs.SuitGlyph(Suit.Wands), heroIcon!.text);
            Assert.IsTrue(elementIcon.ClassListContains(SymbolGlyphs.TablerIconClass));
            Assert.IsTrue(suitIcon.ClassListContains(SymbolGlyphs.TablerIconClass));
            Assert.IsTrue(heroIcon.ClassListContains(SymbolGlyphs.TablerIconClass));
            Assert.IsFalse(elementIcon.ClassListContains(SymbolGlyphs.ZodiacFontClass));
        }

        static VisualElement InstantiateInspectModal()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CardModalsPath);
            Assert.IsNotNull(asset, $"Missing UXML at {CardModalsPath}");
            var tree = asset.Instantiate();
            var modal = tree.Q<VisualElement>("inspect-modal");
            Assert.IsNotNull(modal);
            return modal!;
        }

        GameSession BuildSession(string definitionId, ZodiacSign cosmicAge)
        {
            var codexPath = Path.Combine(Application.dataPath,
                "_Project/Data/Resources/crucible-codex.json");
            var codexDb = CrucibleCodexDatabase.LoadFromJson(File.ReadAllText(codexPath));
            var rules = new GameRuleSet(
                cardDatabase: _db,
                codexDatabase: codexDb,
                setup: new GameSetupService(_db, 42),
                harvest: new SpringRules(_db, 42),
                crucible: new CrucibleRules(_db, codexDb, seed: 42),
                crafting: new CraftingRules(_db),
                winter: new WinterRules(_db),
                validator: new ActionValidator());

            var players = new List<PlayerState>
            {
                new(0, PlayerColor.Red),
                new(1, PlayerColor.Blue)
            };
            var session = new GameSession("inspect-bind-test", GameMode.Quickplay, players, rules);
            session.Board.CosmicAgeSign = cosmicAge;
            session.RegisterCard(new CardInstance("inspect-card", definitionId, CardZone.Spread, 0));
            return session;
        }
    }
}
