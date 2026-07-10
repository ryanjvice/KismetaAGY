using System;
using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Rules;
using NUnit.Framework;

namespace Kismeta.Core.Tests
{
    public sealed class ContestDiceSeriesResolver_Tests
    {
        [Test]
        public void SingleRoll_WhenNotBestOfThree()
        {
            var rng = new SeededRollRng(6, 3);
            var result = ContestDiceSeriesResolver.Resolve(rng, 0, 1, bestOfThree: false);

            Assert.AreEqual(1, result.Rounds.Count);
            Assert.AreEqual(6, result.FinalAttackRoll);
            Assert.AreEqual(3, result.FinalDefendRoll);
            Assert.AreEqual(0, result.WinnerId);
            Assert.AreEqual(1, result.AttackerRoundWins);
            Assert.AreEqual(0, result.DefenderRoundWins);
        }

        [Test]
        public void Tie_FavorsDefender()
        {
            var rng = new SeededRollRng(5, 5);
            var result = ContestDiceSeriesResolver.Resolve(rng, 0, 1, bestOfThree: false);

            Assert.AreEqual(1, result.Rounds.Count);
            Assert.AreEqual(1, result.WinnerId);
            Assert.AreEqual(0, result.AttackerRoundWins);
            Assert.AreEqual(1, result.DefenderRoundWins);
        }

        [Test]
        public void BestOfThree_StopsWhenAttackerReachesTwoWins()
        {
            var rng = new SeededRollRng(6, 3, 3, 8, 7, 4);
            var result = ContestDiceSeriesResolver.Resolve(rng, 0, 1, bestOfThree: true);

            Assert.AreEqual(3, result.Rounds.Count);
            Assert.AreEqual(2, result.AttackerRoundWins);
            Assert.AreEqual(1, result.DefenderRoundWins);
            Assert.AreEqual(0, result.WinnerId);
        }

        [Test]
        public void BestOfThree_StopsWhenDefenderReachesTwoWins()
        {
            var rng = new SeededRollRng(3, 8, 7, 4, 2, 9);
            var result = ContestDiceSeriesResolver.Resolve(rng, 0, 1, bestOfThree: true);

            Assert.AreEqual(3, result.Rounds.Count);
            Assert.AreEqual(1, result.AttackerRoundWins);
            Assert.AreEqual(2, result.DefenderRoundWins);
            Assert.AreEqual(1, result.WinnerId);
        }

        sealed class SeededRollRng : Random
        {
            readonly Queue<int> _values = new();

            public SeededRollRng(params int[] rolls)
            {
                foreach (int v in rolls)
                    _values.Enqueue(v);
            }

            public override int Next(int minValue, int maxValue)
            {
                Assert.IsTrue(_values.Count > 0, "SeededRollRng exhausted.");
                return _values.Dequeue();
            }
        }
    }
}
