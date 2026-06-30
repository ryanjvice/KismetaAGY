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
    public sealed class WardRules_Tests
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

        static GameRuleSet BuildRules(CardDatabase db, CrucibleCodexDatabase codexDb) =>
            new GameRuleSet(
                cardDatabase: db,
                codexDatabase: codexDb,
                setup: new GameSetupService(db, 42),
                harvest: new SpringRules(db, 42),
                crucible: new CrucibleRules(db, codexDb, seed: 42),
                crafting: new CraftingRules(db),
                winter: new WinterRules(db),
                validator: new ActionValidator());

        static GameSession SetupSession(CardDatabase db, CrucibleCodexDatabase codexDb)
        {
            var players = new List<PlayerState> { new(0, PlayerColor.Red), new(1, PlayerColor.Blue) };
            var session = new GameSession("test", GameMode.Quickplay, players, BuildRules(db, codexDb));
            session.FirstAgekeeperPlayerId = 0;
            var setup = session.Apply(new SetupGameCommand());
            Assert.IsTrue(setup.IsOk, setup.Message);
            return session;
        }

        static void SetSummerTurn(GameSession session, int playerId)
        {
            session.Phase.SetSeason(Season.Summer);
            session.CurrentTurnPlayerId = playerId;
        }

        static void SetAutumnTurn(GameSession session, int playerId)
        {
            session.Phase.SetSeason(Season.Autumn);
            session.CurrentTurnPlayerId = playerId;
        }

        static void AddAdeptToArcanum(GameSession session, int playerId, string instanceId, string definitionId)
        {
            session.RegisterCard(new CardInstance(instanceId, definitionId, CardZone.Arcanum, playerId));
            session.Players[playerId].Arcanum.Add(instanceId);
        }

        [Test]
        public void PlaceAdeptWard_Succeeds_During_Summer_Turn()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            const string adeptId = "adept-empress";
            AddAdeptToArcanum(session, 0, adeptId, "major.adept.3");
            session.Players[0].AddReagent(ReagentType.Salt, 2);
            SetSummerTurn(session, 0);

            var result = session.Apply(new PlaceAdeptWardCommand(0, adeptId, ReagentType.Salt));

            Assert.IsTrue(result.IsOk, result.Message);
            Assert.AreEqual(1, session.Players[0].GetAdeptWardCount(adeptId));
            Assert.AreEqual(1, session.Players[0].GetReagent(ReagentType.Salt));
        }

        [Test]
        public void PlaceAdeptWard_Rejected_When_Not_In_Arcanum()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            session.Players[0].AddReagent(ReagentType.Salt, 1);
            SetSummerTurn(session, 0);

            var result = session.Apply(new PlaceAdeptWardCommand(0, "missing-adept", ReagentType.Salt));

            Assert.IsFalse(result.IsOk);
            StringAssert.Contains("Arcanum", result.Message);
        }

        [Test]
        public void PlaceAdeptWard_Rejected_When_Not_Adept()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            const string fateId = "fate-wheel";
            AddAdeptToArcanum(session, 0, fateId, "major.fate.10");
            session.Players[0].AddReagent(ReagentType.Salt, 1);
            SetSummerTurn(session, 0);

            var result = session.Apply(new PlaceAdeptWardCommand(0, fateId, ReagentType.Salt));

            Assert.IsFalse(result.IsOk);
            StringAssert.Contains("Adept", result.Message);
        }

        [Test]
        public void PlaceAdeptWard_Rejected_Outside_Summer()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            const string adeptId = "adept-empress";
            AddAdeptToArcanum(session, 0, adeptId, "major.adept.3");
            session.Players[0].AddReagent(ReagentType.Salt, 1);
            session.Phase.SetSeason(Season.Autumn);
            session.CurrentTurnPlayerId = 0;

            var result = session.Apply(new PlaceAdeptWardCommand(0, adeptId, ReagentType.Salt));

            Assert.IsFalse(result.IsOk);
            StringAssert.Contains("Summer", result.Message);
        }

        [Test]
        public void PlaceCardWard_Rejected_When_Not_Your_Turn()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            session.Players[0].CrucibleSlots[0].Activate();
            session.Players[0].AddReagent(ReagentType.Salt, 1);
            SetSummerTurn(session, 1);

            var result = session.Apply(new PlaceCardWardCommand(0, 0, ReagentType.Salt));

            Assert.IsFalse(result.IsOk);
            StringAssert.Contains("turn", result.Message);
        }

        [Test]
        public void PlaceStoneWard_Rejected_Outside_Autumn()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            session.Players[0].StoneState = StoneState.Forging;
            session.Players[0].AddReagent(ReagentType.Salt, 1);
            SetSummerTurn(session, 0);

            var result = session.Apply(new PlaceStoneWardCommand(0, ReagentType.Salt));

            Assert.IsFalse(result.IsOk);
            StringAssert.Contains("Autumn", result.Message);
        }

        [Test]
        public void AdeptWard_Refunded_When_Adept_Removed_From_Arcanum()
        {
            var db = LoadDb();
            var codexDb = LoadCodexDb();
            var session = SetupSession(db, codexDb);
            const string adeptId = "adept-empress";
            AddAdeptToArcanum(session, 0, adeptId, "major.adept.3");
            session.Players[0].AddAdeptWard(adeptId);
            session.Players[0].AddAdeptWard(adeptId);
            Assert.AreEqual(0, session.Players[0].GetReagent(ReagentType.Salt));

            WardRefundHelper.RefundAdeptWards(session.Players[0], adeptId);

            Assert.AreEqual(0, session.Players[0].GetAdeptWardCount(adeptId));
            Assert.AreEqual(2, session.Players[0].GetReagent(ReagentType.Salt));
        }
    }
}
