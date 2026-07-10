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

        public static Result Resolve(Random rng, int attackerId, int defenderId, bool bestOfThree) =>
            Resolve(rng, attackerId, defenderId,
                ContestResolveOptions.Simple(ContestKind.Duel, bestOfThree));

        public static Result Resolve(Random rng, int attackerId, int defenderId, ContestResolveOptions options)
        {
            var rounds = new List<ContestDiceRound>();
            int aWins = 0, dWins = 0;

            do
            {
                int rawAttack = RollD12(rng, options.Attacker.MayRerollAttack);
                int rawDefend = RollD12(rng, options.Defender.MayRerollDefend);
                int effectiveAttack = ClampRoll(rawAttack + options.Attacker.AttackBonus);
                int effectiveDefend = ClampRoll(rawDefend + options.Defender.DefendBonus);
                bool attackerWinsRound = effectiveAttack > effectiveDefend;
                int roundWinner = attackerWinsRound ? attackerId : defenderId;

                rounds.Add(new ContestDiceRound(
                    rawAttack, rawDefend, effectiveAttack, effectiveDefend, roundWinner));

                if (attackerWinsRound) aWins++;
                else dWins++;
            }
            while (options.BestOfThree && aWins < 2 && dWins < 2);

            int winnerId = aWins > dWins ? attackerId : defenderId;
            return new Result(rounds, aWins, dWins, winnerId);
        }

        static int RollD12(Random rng, bool mayReroll)
        {
            int first = rng.Next(1, 13);
            if (!mayReroll)
                return first;
            int second = rng.Next(1, 13);
            return Math.Max(first, second);
        }

        static int ClampRoll(int value) => Math.Clamp(value, 1, 12);
    }
}
