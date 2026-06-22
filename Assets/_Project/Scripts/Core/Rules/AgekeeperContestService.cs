using System;
using System.Collections.Generic;
using Kismeta.Core.Domain;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Resolves the first Agekeeper by zodiac die contest (Rules/setup.md §V).
    /// Each player rolls 1–12; highest wins; ties reroll among tied players.
    /// </summary>
    public static class AgekeeperContestService
    {
        public sealed class RollResult
        {
            public int PlayerId { get; }
            public int DieValue { get; }
            public RollResult(int playerId, int dieValue)
            {
                PlayerId = playerId;
                DieValue = dieValue;
            }
        }

        public sealed class ContestResult
        {
            public int WinnerPlayerId { get; }
            /// <summary>Every contest round in order; round 0 includes all players.</summary>
            public IReadOnlyList<IReadOnlyList<RollResult>> Rounds { get; }
            /// <summary>Rolls from the final tiebreaker round (convenience alias).</summary>
            public IReadOnlyList<RollResult> FinalRolls { get; }
            public ContestResult(int winnerPlayerId, IReadOnlyList<IReadOnlyList<RollResult>> rounds)
            {
                WinnerPlayerId = winnerPlayerId;
                Rounds = rounds;
                FinalRolls = rounds.Count > 0 ? rounds[rounds.Count - 1] : Array.Empty<RollResult>();
            }
        }

        public static ContestResult Resolve(int playerCount, Random rng)
        {
            if (playerCount < 2 || playerCount > 4)
                throw new ArgumentOutOfRangeException(nameof(playerCount));

            var contenders = new List<int>(playerCount);
            for (int i = 0; i < playerCount; i++)
                contenders.Add(i);

            var rounds = new List<IReadOnlyList<RollResult>>();
            while (contenders.Count > 1)
            {
                var rolls = new List<RollResult>(contenders.Count);
                int best = int.MinValue;
                foreach (int id in contenders)
                {
                    int value = rng.Next(1, 13);
                    rolls.Add(new RollResult(id, value));
                    if (value > best) best = value;
                }

                rounds.Add(rolls);
                var next = new List<int>();
                foreach (var roll in rolls)
                {
                    if (roll.DieValue == best)
                        next.Add(roll.PlayerId);
                }

                contenders = next;
            }

            return new ContestResult(contenders[0], rounds);
        }
    }
}
