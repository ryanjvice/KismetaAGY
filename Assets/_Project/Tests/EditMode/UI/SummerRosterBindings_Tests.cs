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
        public void SpreadChip_Click_InvokesOnInspectWithCardId()
        {
            const string cardId = "rival-spread-card";
            var session = BuildSessionWithRivalSpread(cardId, "minor.swords.seven.1");

            string? inspectedId = null;
            using var host = AttachToEditorPanel(BuildSummerRoot(), root =>
            {
                SummerRosterBindings.Populate(root, session, localPlayerId: 0, id => inspectedId = id);

                var spreadRow = root.Q("player-block-1")?.Q(className: "player-block__zone");
                var chip = spreadRow?.Q(className: "card-chip");
                Assert.IsNotNull(chip, "Expected rival spread chip in roster.");

                using (var evt = ClickEvent.GetPooled())
                {
                    evt.target = chip;
                    chip!.SendEvent(evt);
                }
            });

            Assert.AreEqual(cardId, inspectedId);
        }

        [Test]
        public void SpreadChip_HasInspectableClass()
        {
            var root = BuildSummerRoot();
            var session = BuildSessionWithRivalSpread("rival-spread-card", "minor.swords.seven.1");

            SummerRosterBindings.Populate(root, session, localPlayerId: 0, _ => { });

            var chip = root.Q("player-block-1")?.Q(className: "card-chip");
            Assert.IsNotNull(chip);
            Assert.IsTrue(chip!.ClassListContains("card-chip--inspectable"));
        }

        static VisualElement BuildSummerRoot()
        {
            var root = new VisualElement();
            root.Add(new ScrollView { name = "players" });
            return root;
        }

        sealed class EditorPanelHost : System.IDisposable
        {
            readonly EditorWindow _window;
            readonly VisualElement _root;

            EditorPanelHost(EditorWindow window, VisualElement root)
            {
                _window = window;
                _root = root;
                _window.rootVisualElement.Add(root);
            }

            public static EditorPanelHost Attach(VisualElement root)
            {
                var window = ScriptableObject.CreateInstance<EditorWindow>();
                window.Show();
                return new EditorPanelHost(window, root);
            }

            public void Dispose()
            {
                _root.RemoveFromHierarchy();
                _window.Close();
            }
        }

        static EditorPanelHost AttachToEditorPanel(VisualElement root, System.Action<VisualElement> action)
        {
            var host = EditorPanelHost.Attach(root);
            action(root);
            return host;
        }

        GameSession BuildSessionWithRivalSpread(string cardId, string definitionId)
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
            var session = new GameSession("summer-roster-test", GameMode.Quickplay, players, rules);
            session.Players[1].Spread.Add(cardId);
            session.RegisterCard(new CardInstance(cardId, definitionId, CardZone.Spread, 1));
            return session;
        }
    }
}
