using System.Collections.Generic;
using System.IO;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Rules;
using Kismeta.Data.Loaders;
using Kismeta.UI;
using Kismeta.UI.Controllers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Tests
{
    public sealed class CrucibleCardDetailController_Tests
    {
        const string CrucibleDetailPath = "Assets/_Project/UI/UXML/batch4/CrucibleCardDetail.uxml";

        CardDatabase _db = null!;

        [OneTimeSetUp]
        public void LoadDatabase()
        {
            var path = Path.Combine(Application.dataPath,
                "_Project/Scripts/Data/Generated/cards.json");
            _db = CardDatabase.LoadFromJson(File.ReadAllText(path));
        }

        [Test]
        public void InstanceMode_BindsNameNumberFormulaAndDoneButton()
        {
            var root = InstantiateDetailRoot();
            var session = BuildSessionWithActiveCrucible("crucible-inst-0", "crucible.a.0", playerId: 0, slotIndex: 0);
            var go = new GameObject("crucible-detail-test");
            var controller = go.AddComponent<CrucibleCardDetailController>();
            controller.AttachTo(root);
            controller.CardInstanceId = "crucible-inst-0";
            controller.SlotIndex = null;

            controller.BindState(session, new CommandBridge());

            Assert.AreEqual("The Fool", root.Q<Label>("crucible-name")?.text);
            Assert.AreEqual("0", root.Q<Label>("crucible-number")?.text);
            Assert.AreEqual(
                "Pair of Swords with the same Rank",
                root.Q<Label>("crucible-formula")?.text);
            Assert.AreEqual("Red · slot A · 0 · active", root.Q<Label>("crucible-slot-label")?.text);
            Assert.AreEqual("Done", root.Q<Button>("back-btn")?.text);
            Assert.AreEqual(2, root.Q<VisualElement>("crucible-cost")!.childCount);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void InstanceMode_FindsCrucibleOnRivalPlayer()
        {
            var root = InstantiateDetailRoot();
            var session = BuildSessionWithActiveCrucible("crucible-inst-rival", "crucible.b.4", playerId: 1, slotIndex: 2);
            var go = new GameObject("crucible-detail-test");
            var controller = go.AddComponent<CrucibleCardDetailController>();
            controller.AttachTo(root);
            controller.CardInstanceId = "crucible-inst-rival";
            controller.SlotIndex = null;

            controller.BindState(session, new CommandBridge());

            Assert.AreEqual("The Emperor", root.Q<Label>("crucible-name")?.text);
            Assert.AreEqual("IV", root.Q<Label>("crucible-number")?.text);
            Assert.AreEqual("Blue · slot C · IV · active", root.Q<Label>("crucible-slot-label")?.text);

            Object.DestroyImmediate(go);
        }

        static VisualElement InstantiateDetailRoot()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CrucibleDetailPath);
            Assert.IsNotNull(asset, $"Missing UXML at {CrucibleDetailPath}");
            var tree = asset.Instantiate();
            var root = tree.Q<VisualElement>("crucible-card-detail");
            Assert.IsNotNull(root);
            return root!;
        }

        GameSession BuildSessionWithActiveCrucible(
            string instanceId,
            string definitionId,
            int playerId,
            int slotIndex)
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

            var session = new GameSession("crucible-detail-test", GameMode.Quickplay, players, rules);
            session.RegisterCard(new CardInstance(instanceId, definitionId, CardZone.Spread, playerId));

            var slot = new PlayerCrucibleSlot(instanceId);
            slot.Activate();
            while (players[playerId].CrucibleSlots.Count <= slotIndex)
                players[playerId].CrucibleSlots.Add(new PlayerCrucibleSlot($"placeholder-{players[playerId].CrucibleSlots.Count}"));
            players[playerId].CrucibleSlots[slotIndex] = slot;

            return session;
        }
    }
}
