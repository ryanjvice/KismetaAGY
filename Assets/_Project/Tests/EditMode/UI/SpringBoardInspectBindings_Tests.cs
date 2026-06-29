using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.Data.Loaders;
using Kismeta.UI.Components;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Tests
{
    public sealed class SpringBoardInspectBindings_Tests
    {
        const string BoardInspectPath = "Assets/_Project/UI/UXML/shell/CentralPanelInspectModal.uxml";

        CardDatabase _db = null!;

        [OneTimeSetUp]
        public void LoadDatabase()
        {
            var path = Path.Combine(Application.dataPath,
                "_Project/Scripts/Data/Generated/cards.json");
            _db = CardDatabase.LoadFromJson(File.ReadAllText(path));
        }

        [Test]
        public void CosmicAgeSection_ShowsEffectWhenAgeIsSet()
        {
            var root = InstantiateBoardInspect();
            var session = BuildSession(ZodiacSign.Scorpio);

            SpringBoardInspectBindings.Bind(root, session);

            var content = root.Q<VisualElement>("central-panel-inspect-content");
            Assert.IsNotNull(content);
            var body = content!.Q<Label>(className: "active-effects-featured__body");
            Assert.IsNotNull(body);
            StringAssert.Contains("Court Cups are a Wild Suit", body!.text);
            StringAssert.Contains("Scorpio reigns", body.text);
        }

        [Test]
        public void CosmicAgeSection_ShowsUncastMessageWhenAgeIsNone()
        {
            var root = InstantiateBoardInspect();
            var session = BuildSession(ZodiacSign.None);

            SpringBoardInspectBindings.Bind(root, session);

            var body = root.Q<Label>(className: "active-effects-featured__body");
            Assert.AreEqual("The age has not been cast yet.", body?.text);
        }

        [Test]
        public void PlayersSection_ListsAllPlayerSigns()
        {
            var root = InstantiateBoardInspect();
            var session = BuildSession(ZodiacSign.Virgo);
            session.Players[0].CurrentSign = ZodiacSign.Aries;
            session.Players[1].CurrentSign = ZodiacSign.Libra;

            SpringBoardInspectBindings.Bind(root, session);

            var names = root.Query<Label>(className: "central-panel-inspect-player-row__name")
                .ToList()
                .Select(l => l.text)
                .ToList();
            CollectionAssert.AreEquivalent(new[] { "Red alchemist", "Green alchemist" }, names);

            var signNames = root.Query<Label>(className: "detail-zodiac__name")
                .ToList()
                .Select(l => l.text)
                .ToList();
            CollectionAssert.AreEquivalent(new[] { "Aries", "Libra" }, signNames);
        }

        [Test]
        public void AstralHousesSection_ShowsEmptyStateWhenNoneBuilt()
        {
            var root = InstantiateBoardInspect();
            var session = BuildSession(ZodiacSign.Gemini);

            SpringBoardInspectBindings.Bind(root, session);

            var empty = root.Q<Label>(className: "central-panel-inspect-empty");
            Assert.AreEqual("No astral houses built yet.", empty?.text);
        }

        [Test]
        public void AstralHousesSection_ListsHousesFromMultiplePlayers()
        {
            var root = InstantiateBoardInspect();
            var session = BuildSession(ZodiacSign.Gemini);
            session.Players[0].AstralHouses.Add(ZodiacSign.Aries);
            session.Players[1].AstralHouses.Add(ZodiacSign.Libra);

            SpringBoardInspectBindings.Bind(root, session);

            var owners = root.Query<Label>(className: "central-panel-inspect-house-row__owner-name")
                .ToList()
                .Select(l => l.text)
                .ToList();
            CollectionAssert.AreEquivalent(
                new[] { "Red alchemist · Aries", "Green alchemist · Libra" },
                owners);
        }

        static VisualElement InstantiateBoardInspect()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BoardInspectPath);
            Assert.IsNotNull(asset, $"Missing UXML at {BoardInspectPath}");
            var tree = asset.Instantiate();
            var root = tree.Q<VisualElement>("spring-board-inspect");
            Assert.IsNotNull(root);
            return root!;
        }

        GameSession BuildSession(ZodiacSign cosmicAge)
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
                new(0, PlayerColor.Red) { IsAgekeeper = true },
                new(1, PlayerColor.Blue)
            };
            var session = new GameSession("spring-inspect-test", GameMode.Quickplay, players, rules);
            session.Board.CosmicAgeSign = cosmicAge;
            return session;
        }
    }
}
