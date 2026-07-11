using System;
using System.Collections.Generic;
using System.IO;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.Data.Loaders;
using NUnit.Framework;
using UnityEngine;

namespace Kismeta.Core.Tests
{
    public sealed class ContestModifierService_Tests
    {
        static CardDatabase LoadDb()
        {
            var path = Path.Combine(Application.dataPath,
                "_Project/Scripts/Data/Generated/cards.json");
            return CardDatabase.LoadFromJson(File.ReadAllText(path));
        }

        static GameSession BuildSession(CardDatabase db)
        {
            var players = new List<PlayerState>
            {
                new(0, PlayerColor.Red),
                new(1, PlayerColor.Blue)
            };
            var codexPath = Path.Combine(Application.dataPath, "_Project/Data/Resources/crucible-codex.json");
            var codexDb = CrucibleCodexDatabase.LoadFromJson(File.ReadAllText(codexPath));
            var rules = new GameRuleSet(
                db, codexDb,
                new GameSetupService(db, 42),
                new SpringRules(db, 42),
                new CrucibleRules(db, codexDb, seed: 42),
                new CraftingRules(db),
                new WinterRules(db),
                new ActionValidator());
            return new GameSession("test", GameMode.Quickplay, players, rules, CrucibleBuildMode.Curated);
        }

        static void AddSpreadCard(GameSession session, int playerId, string instanceId, string definitionId)
        {
            var inst = new CardInstance(instanceId, definitionId, CardZone.Spread, playerId);
            session.RegisterCard(inst);
            session.Players[playerId].Spread.Add(instanceId);
        }

        static void AddAdept(GameSession session, int playerId, string instanceId, string definitionId,
            ZodiacSign sign = ZodiacSign.None)
        {
            var inst = new CardInstance(instanceId, definitionId, CardZone.Arcanum, playerId);
            session.RegisterCard(inst);
            session.Players[playerId].Arcanum.Add(instanceId);
            if (sign != ZodiacSign.None)
                session.Players[playerId].CurrentSign = sign;
        }

        [Test]
        public void KnightOfSwords_GrantsAttackerAttackBonus()
        {
            var db = LoadDb();
            var session = BuildSession(db);
            AddSpreadCard(session, 0, "knight", "minor.swords.knight.1");

            var mods = ContestModifierService.Build(session, ContestKind.Duel, 0, 1);

            Assert.AreEqual(1, mods.Attacker.AttackBonus);
            Assert.AreEqual(0, mods.Defender.DefendBonus);
        }

        [Test]
        public void KnightOfCups_GrantsDefenderDefendBonus()
        {
            var db = LoadDb();
            var session = BuildSession(db);
            AddSpreadCard(session, 1, "knight", "minor.cups.knight.1");

            var mods = ContestModifierService.Build(session, ContestKind.Duel, 0, 1);

            Assert.AreEqual(1, mods.Defender.DefendBonus);
        }

        [Test]
        public void PrincessOfCups_GrantsDefenderGambitBonus()
        {
            var db = LoadDb();
            var session = BuildSession(db);
            AddSpreadCard(session, 1, "princess", "minor.cups.princess.1");

            var mods = ContestModifierService.Build(session, ContestKind.Gambit, 0, 1);

            Assert.AreEqual(1, mods.Defender.DefendBonus);
        }

        [Test]
        public void SixOfSwords_ForcesAttackerBestOfThree()
        {
            var db = LoadDb();
            var session = BuildSession(db);
            session.Board.CosmicAgeSign = ZodiacSign.Aries;
            AddSpreadCard(session, 0, "six", "minor.swords.six.1");

            var mods = ContestModifierService.Build(session, ContestKind.Duel, 0, 1);

            Assert.IsTrue(mods.AttackerForcesBestOfThree);
            Assert.IsTrue(ContestModifierService.ResolveBestOfThree(session, ContestKind.Duel, mods));
        }

        [Test]
        public void FiveOfCups_GrantsAttackerReroll()
        {
            var db = LoadDb();
            var session = BuildSession(db);
            session.Board.CosmicAgeSign = ZodiacSign.Aries;
            AddSpreadCard(session, 1, "cups5", "minor.cups.five.1");

            var mods = ContestModifierService.Build(session, ContestKind.Duel, 0, 1);

            Assert.IsTrue(mods.Attacker.MayRerollAttack);
        }

        [Test]
        public void StrengthResonant_RequiresAttunement()
        {
            var db = LoadDb();
            var session = BuildSession(db);
            AddAdept(session, 0, "strength", "major.adept.8", ZodiacSign.Sagittarius);

            var attuned = ContestModifierService.Build(session, ContestKind.Duel, 0, 1);
            Assert.AreEqual(2, attuned.Attacker.AttackBonus);

            session.Players[0].CurrentSign = ZodiacSign.Aries;
            var notAttuned = ContestModifierService.Build(session, ContestKind.Duel, 0, 1);
            Assert.AreEqual(1, notAttuned.Attacker.AttackBonus);
        }

        [Test]
        public void ChariotResonant_RerollOnlyWhenAttuned()
        {
            var db = LoadDb();
            var session = BuildSession(db);
            AddAdept(session, 0, "chariot", "major.adept.7", ZodiacSign.Leo);

            var attuned = ContestModifierService.Build(session, ContestKind.Duel, 0, 1);
            Assert.IsTrue(attuned.Attacker.MayRerollAttack);

            session.Players[0].CurrentSign = ZodiacSign.Aries;
            var notAttuned = ContestModifierService.Build(session, ContestKind.Duel, 0, 1);
            Assert.IsFalse(notAttuned.Attacker.MayRerollAttack);
        }
    }
}
