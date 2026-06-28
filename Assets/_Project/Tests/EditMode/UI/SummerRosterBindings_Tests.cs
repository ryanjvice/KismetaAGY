using System.Collections.Generic;
using System.IO;
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
    public sealed class SummerRosterBindings_Tests
    {
        CardDatabase _db = null!;

        [OneTimeSetUp]
        public void LoadDatabase()
        {
            var path = Path.Combine(Application.dataPath,
                "_Project/Scripts/Data/Generated/cards.json");
            _db = CardDatabase.LoadFromJson(File.ReadAllText(path));
        }

        [Test]
        public void CollapsedBlock_ShowsSpreadOnly()
        {
            using var ui = new UiDocumentScope();
            var root = ui.Root;
            var session = BuildSessionWithRivals();
            var expanded = new HashSet<int>();

            SummerRosterBindings.Populate(root, session, localPlayerId: 0, expanded, null, null);

            var block = root.Q("player-block-1");
            Assert.IsNotNull(block);
            Assert.IsFalse(block!.ClassListContains("player-block--expanded"));
            Assert.IsNotNull(block.Q(className: "player-block__zone"));
            Assert.IsNull(block.Q(className: "player-block__inline-zones"));
            Assert.IsNull(block.Q(className: "player-block__detail"));
        }

        [Test]
        public void ExpandedBlock_IncludesInlineZonesAndDetail()
        {
            using var ui = new UiDocumentScope();
            var root = ui.Root;
            var session = BuildSessionWithRivals();
            var expanded = new HashSet<int> { 1 };

            SummerRosterBindings.Populate(root, session, localPlayerId: 0, expanded, null, null);

            var block = root.Q("player-block-1");
            Assert.IsNotNull(block);
            Assert.IsTrue(block!.ClassListContains("player-block--expanded"));
            Assert.IsNotNull(block.Q(className: "player-block__inline-zones"));
            Assert.IsNotNull(block.Q(className: "player-block__detail"));
        }

        [Test]
        public void Header_WithToggleCallback_IsConfiguredForInteraction()
        {
            using var ui = new UiDocumentScope();
            var root = ui.Root;
            var session = BuildSessionWithRivals();

            SummerRosterBindings.Populate(
                root, session, localPlayerId: 0, new HashSet<int>(), null, _ => { });

            var header = root.Q("player-block-1")?.Q(className: "player-block__header");
            Assert.IsNotNull(header);
            Assert.IsTrue(header!.focusable);
            Assert.AreEqual(PickingMode.Position, header.pickingMode);
            Assert.IsNotNull(header.Q(className: "player-block__expand-hint"));
        }

        [Test]
        public void SpreadChip_WithInspectCallback_MarksInspectable()
        {
            using var ui = new UiDocumentScope();
            var root = ui.Root;
            var session = BuildSessionWithRivals();

            SummerRosterBindings.Populate(
                root, session, localPlayerId: 0, new HashSet<int>(), _ => { }, null);

            var chip = root.Q("player-block-1")?.Q(className: "card-chip--inspectable");
            Assert.IsNotNull(chip, "Expected inspectable spread chip.");
            Assert.AreEqual(PickingMode.Position, chip!.pickingMode);
        }

        sealed class UiDocumentScope : System.IDisposable
        {
            readonly GameObject _go;

            public VisualElement Root { get; }

            public UiDocumentScope()
            {
                _go = new GameObject("summer-roster-ui-test");
                var doc = _go.AddComponent<UIDocument>();
                doc.panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                Root = doc.rootVisualElement;
                Root.style.width = 800;
                Root.style.height = 600;
                Root.Add(new ScrollView { name = "players" });
                EditorApplication.QueuePlayerLoopUpdate();
            }

            public void Dispose() => Object.DestroyImmediate(_go);
        }

        GameSession BuildSessionWithRivals()
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
                new(1, PlayerColor.Blue),
            };
            players[1].Spread.Add("rival-spread-1");
            var session = new GameSession("summer-roster-test", GameMode.Quickplay, players, rules);
            session.RegisterCard(new CardInstance("rival-spread-1", "minor.wands.ace.1", CardZone.Spread, 1));
            return session;
        }
    }
}
