using System.IO;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.Data.Loaders;
using NUnit.Framework;
using UnityEngine;

namespace Kismeta.Core.Tests
{
    public sealed class AdeptEffectService_Tests
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

        static GameSession BuildSession(CardDatabase db)
        {
            var players = new System.Collections.Generic.List<PlayerState>
            {
                new(0, PlayerColor.Red),
                new(1, PlayerColor.Blue)
            };
            var rules = new GameRuleSet(
                cardDatabase: db,
                codexDatabase: LoadCodexDb(),
                setup: new GameSetupService(db),
                harvest: new SpringRules(db),
                crucible: new CrucibleRules(db, LoadCodexDb()),
                crafting: new CraftingRules(db),
                winter: new WinterRules(db),
                validator: new ActionValidator(),
                adept: new AdeptRules(db));
            return new GameSession("test", GameMode.Quickplay, players, rules);
        }

        [Test]
        public void Star_IsClassifiedAsPassive()
        {
            Assert.AreEqual(AdeptUsageKind.Passive, AdeptEffectCatalog.UsageFor(AdeptEffectService.StarArcana));
        }

        [Test]
        public void HasAdept_RespectsArrest()
        {
            var db = LoadDb();
            var session = BuildSession(db);
            var player = session.Players[0];
            var inst = new CardInstance("magician", "major.adept.1", CardZone.Arcanum, 0);
            session.RegisterCard(inst);
            player.Arcanum.Add("magician");

            Assert.IsTrue(AdeptEffectService.HasAdept(session, player, AdeptEffectService.MagicianArcana));

            player.ArrestedAdepts.Add("magician");
            Assert.IsFalse(AdeptEffectService.HasAdept(session, player, AdeptEffectService.MagicianArcana));
        }
    }
}
