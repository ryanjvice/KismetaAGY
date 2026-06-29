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
    public sealed class AutumnForgeInspectBindings_Tests
    {
        const string AutumnForgeInspectPath = "Assets/_Project/UI/UXML/shell/AutumnForgeInspect.uxml";

        [Test]
        public void PlayerAtForge_ShowsPositionAndState()
        {
            var root = InstantiateForgeInspect();
            var session = BuildSession();
            session.Players[0].StonePosition = new StonePosition(7);
            session.Players[0].StoneState = StoneState.Forging;

            AutumnForgeInspectBindings.Bind(root, session);

            var stone = root.Q<Label>(className: "central-panel-inspect-player-row__stone");
            StringAssert.Contains("Gold Forge (7)", stone!.text);
            StringAssert.Contains("Forging", stone.text);
        }

        [Test]
        public void PlayerInStasis_ShowsHaltedPosition()
        {
            var root = InstantiateForgeInspect();
            var session = BuildSession();
            session.Players[0].StonePosition = new StonePosition(3);
            session.Players[0].StoneState = StoneState.Stasis;

            AutumnForgeInspectBindings.Bind(root, session);

            var stone = root.Q<Label>(className: "central-panel-inspect-player-row__stone");
            Assert.AreEqual("In stasis — halted at Bronze Forge", stone?.text);

            var note = root.Q<Label>(className: "central-panel-inspect-player-row__stasis-note");
            Assert.AreEqual("cannot be opposed this age", note?.text);
        }

        [Test]
        public void LitCauldrons_ShowLitAndDormantChips()
        {
            var root = InstantiateForgeInspect();
            var session = BuildSession();
            session.Players[0].LightCauldron(Suit.Wands);
            session.Players[0].LightCauldron(Suit.Cups);

            AutumnForgeInspectBindings.Bind(root, session);

            var litChips = root.Query<Label>(className: "central-panel-inspect-cauldron-chip--lit").ToList();
            Assert.AreEqual(2, litChips.Count);
            CollectionAssert.AreEquivalent(new[] { "Wands", "Cups" }, new[] { litChips[0].text, litChips[1].text });
        }

        [Test]
        public void NoLitCauldrons_ShowsNoneLit()
        {
            var root = InstantiateForgeInspect();
            var session = BuildSession();

            AutumnForgeInspectBindings.Bind(root, session);

            var empty = root.Q<Label>(className: "central-panel-inspect-empty");
            Assert.AreEqual("None lit", empty?.text);
        }

        [Test]
        public void MultiplePlayers_AreListed()
        {
            var root = InstantiateForgeInspect();
            var session = BuildSession();
            session.Players[0].StonePosition = new StonePosition(1);
            session.Players[1].StonePosition = new StonePosition(7);

            AutumnForgeInspectBindings.Bind(root, session);

            var names = root.Query<Label>(className: "central-panel-inspect-player-row__name").ToList();
            Assert.AreEqual(2, names.Count);
            CollectionAssert.AreEquivalent(new[] { "Red alchemist", "Green alchemist" }, new[] { names[0].text, names[1].text });
        }

        static VisualElement InstantiateForgeInspect()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AutumnForgeInspectPath);
            Assert.IsNotNull(asset, $"Missing UXML at {AutumnForgeInspectPath}");
            var tree = asset.Instantiate();
            var root = tree.Q<VisualElement>("autumn-forge-inspect");
            Assert.IsNotNull(root);
            return root!;
        }

        static GameSession BuildSession()
        {
            var cardsPath = Path.Combine(Application.dataPath,
                "_Project/Scripts/Data/Generated/cards.json");
            var db = CardDatabase.LoadFromJson(File.ReadAllText(cardsPath));
            var codexPath = Path.Combine(Application.dataPath,
                "_Project/Data/Resources/crucible-codex.json");
            var codexDb = CrucibleCodexDatabase.LoadFromJson(File.ReadAllText(codexPath));
            var rules = new GameRuleSet(
                cardDatabase: db,
                codexDatabase: codexDb,
                setup: new GameSetupService(db, 42),
                harvest: new SpringRules(db, 42),
                crucible: new CrucibleRules(db, codexDb, seed: 42),
                crafting: new CraftingRules(db),
                winter: new WinterRules(db),
                validator: new ActionValidator());

            var players = new List<PlayerState>
            {
                new(0, PlayerColor.Red),
                new(1, PlayerColor.Blue)
            };
            return new GameSession("autumn-inspect-test", GameMode.Quickplay, players, rules);
        }
    }
}
