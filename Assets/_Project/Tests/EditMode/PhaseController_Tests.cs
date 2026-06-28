using Kismeta.Core.Domain;
using Kismeta.Core.Phases;
using NUnit.Framework;

namespace Kismeta.Core.Tests
{
    public sealed class PhaseController_Tests
    {
        [Test]
        public void Starts_In_Spring_Step_0()
        {
            var pc = new PhaseController();
            Assert.AreEqual(Season.Spring, pc.CurrentSeason);
            Assert.AreEqual(0, pc.CurrentStepIndex);
        }

        [Test]
        public void Spring_Has_5_Steps()
        {
            Assert.AreEqual(5, PhaseDefinitions.SpringSteps.Count);
        }

        [Test]
        public void Summer_Has_4_Steps_All_Unordered()
        {
            var steps = PhaseDefinitions.SummerSteps;
            Assert.AreEqual(4, steps.Count);
            foreach (var step in steps)
                Assert.IsFalse(step.IsOrdered, $"Summer step '{step.Name}' should be unordered (free-action pool).");
        }

        [Test]
        public void Advancing_Through_All_Spring_Steps_Transitions_To_Summer()
        {
            var pc = new PhaseController();
            // Spring has 5 steps (indices 0–4). Advance 5 times.
            for (int i = 0; i < 5; i++)
                pc.Advance();

            Assert.AreEqual(Season.Summer, pc.CurrentSeason);
            Assert.AreEqual(0, pc.CurrentStepIndex);
        }

        [Test]
        public void Winter_Transitions_Back_To_Spring()
        {
            var pc = new PhaseController();
            pc.SetSeason(Season.Winter);

            // Winter has 4 steps (indices 0–3). Advance 4 times.
            for (int i = 0; i < 4; i++)
                pc.Advance();

            Assert.AreEqual(Season.Spring, pc.CurrentSeason);
            Assert.AreEqual(0, pc.CurrentStepIndex);
        }

        [Test]
        public void SetSeason_Resets_Step_Index()
        {
            var pc = new PhaseController();
            pc.Advance(); // step 1
            pc.Advance(); // step 2
            pc.SetSeason(Season.Autumn);

            Assert.AreEqual(Season.Autumn, pc.CurrentSeason);
            Assert.AreEqual(0, pc.CurrentStepIndex);
        }

        [Test]
        public void Step5_Of_Spring_Is_CardLock()
        {
            // Spring steps are 0-indexed: 0=SetCosmicAge … 4=CardLock
            var step = PhaseDefinitions.SpringSteps[4];
            Assert.AreEqual("CardLock", step.Name);
        }

        [Test]
        public void Full_Round_Cycle_Completes_Without_Error()
        {
            var pc = new PhaseController();
            int totalSteps = 5 + 4 + 5 + 4; // Spring + Summer + Autumn + Winter
            for (int i = 0; i < totalSteps; i++)
                pc.Advance();

            Assert.AreEqual(Season.Spring, pc.CurrentSeason,
                "After one full round, should be back at Spring.");
        }
    }
}
