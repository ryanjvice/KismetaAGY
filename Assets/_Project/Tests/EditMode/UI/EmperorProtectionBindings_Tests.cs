using System.Collections.Generic;
using System.IO;
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
    public sealed class EmperorProtectionBindings_Tests
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
        public void EmperorModal_HasRequiredElements()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CardModalsPath);
            Assert.IsNotNull(asset, $"Missing UXML at {CardModalsPath}");
            var tree = asset.Instantiate();
            var modal = tree.Q<VisualElement>("emperor-modal");
            Assert.IsNotNull(modal);
            Assert.IsNotNull(modal!.Q<VisualElement>("emperor-cards"));
            Assert.IsNotNull(modal.Q<Label>("emperor-count"));
            Assert.IsNotNull(modal.Q<Button>("emperor-confirm"));
            Assert.IsNotNull(modal.Q<Button>("emperor-cancel"));
        }

        [Test]
        public void PopulateCards_OneSelected_ShowsOneChipSelected()
        {
            var host = new VisualElement();
            var session = BuildSessionWithSpreadCards(3);
            var selected = new HashSet<string> { session.Players[0].Spread[0] };

            EmperorProtectionBindings.PopulateCards(host, session, 0, selected, () => { });

            Assert.AreEqual(3, host.childCount);
            Assert.IsTrue(host[0].ClassListContains("card-chip--selected"));
            Assert.IsFalse(host[1].ClassListContains("card-chip--selected"));
        }

        [Test]
        public void PopulateCards_BaseMode_OnlySpreadMinors()
        {
            var host = new VisualElement();
            var session = BuildSessionWithSpreadCards(2);
            GiveHandCard(session, "hand-card", "minor.wands.two.1");
            var selected = new HashSet<string>();

            EmperorProtectionBindings.PopulateCards(host, session, 0, selected, () => { });

            Assert.AreEqual(2, host.childCount);
        }

        [Test]
        public void PopulateCards_Resonant_IncludesHandMinors()
        {
            var host = new VisualElement();
            var session = BuildSessionWithSpreadCards(1);
            GiveHandCard(session, "hand-card", "minor.wands.two.1");
            AttunePlayer(session, ZodiacSign.Aries);
            var selected = new HashSet<string>();

            EmperorProtectionBindings.PopulateCards(host, session, 0, selected, () => { });

            Assert.AreEqual(2, host.childCount);
        }

        static void GiveHandCard(GameSession session, string id, string definitionId)
        {
            session.RegisterCard(new CardInstance(id, definitionId, CardZone.Hand, 0));
            session.Players[0].Hand.Add(id);
        }

        static void AttunePlayer(GameSession session, ZodiacSign sign)
        {
            session.Players[0].CurrentSign = sign;
            session.Players[0].PersonalCosmicEffects = CosmicEffectService.ComputePersonalEffects(
                sign, session.Players[0].AstralHouses, session.Board.CosmicAgeSign);
        }

        GameSession BuildSessionWithSpreadCards(int spreadCount)
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
                validator: new ActionValidator(),
                adept: new AdeptRules(_db));

            var players = new List<PlayerState>
            {
                new(0, PlayerColor.Red),
                new(1, PlayerColor.Blue)
            };
            var session = new GameSession("emperor-ui-test", GameMode.Quickplay, players, rules);
            session.Phase.SetSeason(Season.Spring);
            session.RegisterCard(new CardInstance("emperor", "major.adept.4", CardZone.Arcanum, 0));
            session.Players[0].Arcanum.Add("emperor");

            string[] defs =
            {
                "minor.cups.two.1",
                "minor.cups.three.1",
                "minor.cups.four.1"
            };
            for (int i = 0; i < spreadCount && i < defs.Length; i++)
            {
                string id = $"spread-{i}";
                session.RegisterCard(new CardInstance(id, defs[i], CardZone.Spread, 0));
                session.Players[0].Spread.Add(id);
            }

            return session;
        }
    }
}
