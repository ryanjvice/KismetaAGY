using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Phases;
using NUnit.Framework;

namespace Kismeta.Core.Tests
{
    public sealed class GameSession_Tests
    {
        private static GameSession BuildTwoPlayerSession()
        {
            return new GameSession("test", GameMode.Quickplay, new List<PlayerState>
            {
                new PlayerState(0, PlayerColor.Red),
                new PlayerState(1, PlayerColor.Green),
            });
        }

        [Test]
        public void Session_Initialises_With_Spring_Phase()
        {
            var session = BuildTwoPlayerSession();
            Assert.AreEqual(Season.Spring, session.Phase.CurrentSeason);
            Assert.AreEqual(0, session.Phase.CurrentStepIndex);
        }

        [Test]
        public void Apply_AdvancePhaseCommand_Moves_To_Next_Step()
        {
            var session = BuildTwoPlayerSession();
            var result  = session.Apply(new AdvancePhaseCommand());

            Assert.IsTrue(result.IsOk);
            Assert.AreEqual(1, session.Phase.CurrentStepIndex);
        }

        [Test]
        public void Apply_AdvancePhaseCommand_Emits_PhaseChangedEvent()
        {
            var session = BuildTwoPlayerSession();
            IGameEvent? captured = null;
            session.OnEvent += e => captured = e;

            session.Apply(new AdvancePhaseCommand());

            Assert.IsNotNull(captured);
            Assert.IsInstanceOf<PhaseChangedEvent>(captured);
            var pce = (PhaseChangedEvent)captured!;
            Assert.AreEqual(Season.Spring, pce.Season);
            Assert.AreEqual(1, pce.StepIndex);
        }

        [Test]
        public void Apply_UnhandledCommand_Returns_NotImplemented()
        {
            var session = BuildTwoPlayerSession();
            // HarvestCommand is in the vocabulary but not yet handled beyond the stub.
            var result = session.Apply(new HarvestCommand(0, 3));

            Assert.AreEqual(CommandStatus.NotImplemented, result.Status);
        }

        [Test]
        public void Apply_SetCardLock_Engages_CardLock()
        {
            var session = BuildTwoPlayerSession();
            Assert.IsFalse(session.CardLockActive);

            session.Apply(new SetCardLockCommand(true));
            Assert.IsTrue(session.CardLockActive);

            session.Apply(new SetCardLockCommand(false));
            Assert.IsFalse(session.CardLockActive);
        }

        [Test]
        public void Register_And_Retrieve_CardInstance()
        {
            var session = BuildTwoPlayerSession();
            var card    = new CardInstance("inst-001", "minor.cups.seven.1", CardZone.Deck, -1);
            session.Apply(new RegisterCardCommand(card));

            var retrieved = session.GetCard("inst-001");
            Assert.IsNotNull(retrieved);
            Assert.AreEqual("minor.cups.seven.1", retrieved!.DefinitionId);
        }

        [Test]
        public void Session_Requires_Two_To_Four_Players()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new GameSession("bad", GameMode.Quickplay, new List<PlayerState>
                {
                    new PlayerState(0, PlayerColor.Red)
                }));
        }

        [Test]
        public void SetWinner_Marks_Session_Over()
        {
            var session  = BuildTwoPlayerSession();
            IGameEvent? captured = null;
            session.OnEvent += e => captured = e;

            session.SetWinner(0);

            Assert.IsTrue(session.IsOver);
            Assert.AreEqual(0, session.WinnerPlayerId);
            Assert.IsInstanceOf<GameEndedEvent>(captured);
        }

        [Test]
        public void Full_Spring_Sequence_Advances_To_Summer()
        {
            var session = BuildTwoPlayerSession();
            // Spring has 5 steps; advance 5 times.
            for (int i = 0; i < 5; i++)
                session.Apply(new AdvancePhaseCommand());

            Assert.AreEqual(Season.Summer, session.Phase.CurrentSeason);
        }
    }
}
