using System;
using System.Collections.Generic;
using Kismeta.Core.Domain;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Resolves one or more d12 roll-offs for Duel/Gambit contests.
    /// Ties favor the defender, matching existing combat rules.
    /// </summary>
    public static class ContestDiceSeriesResolver
    {
        public sealed class Result
        {
            public IReadOnlyList<ContestDiceRound> Rounds { get; }
            public int AttackerRoundWins { get; }
            public int DefenderRoundWins { get; }
            public int WinnerId { get; }

            public Result(IReadOnlyList<ContestDiceRound> rounds, int attackerRoundWins,
                int defenderRoundWins, int winnerId)
            {
                Rounds              = rounds;
                AttackerRoundWins   = attackerRoundWins;
                DefenderRoundWins   = defenderRoundWins;
                WinnerId            = winnerId;
            }

            public ContestDiceRound FinalRound => Rounds[Rounds.Count - 1];
            public int FinalAttackRoll => FinalRound.AttackRoll;
            public int FinalDefendRoll => FinalRound.DefendRoll;
        }

        public static Result Resolve(Random rng, int attackerId, int defenderId, bool bestOfThree)
        {
            var rounds = new List<ContestDiceRound>();
            int aWins = 0, dWins = 0;

            do
            {
                int attackRoll = rng.Next(1, 13);
                int defendRoll = rng.Next(1, 13);
                bool attackerWinsRound = attackRoll > defendRoll;
                int roundWinner = attackerWinsRound ? attackerId : defenderId;

                rounds.Add(new ContestDiceRound(attackRoll, defendRoll, roundWinner));

                if (attackerWinsRound) aWins++;
                else dWins++;
            }
            while (bestOfThree && aWins < 2 && dWins < 2);

            int winnerId = aWins > dWins ? attackerId : defenderId;
            return new Result(rounds, aWins, dWins, winnerId);
        }
    }
}
