namespace Kismeta.Core.Domain
{
    /// <summary>One d12 roll-off in a Duel or Gambit series.</summary>
    public readonly struct ContestDiceRound
    {
        /// <summary>Effective attack value after bonuses (used for round winner).</summary>
        public int AttackRoll  { get; }
        /// <summary>Effective defend value after bonuses (used for round winner).</summary>
        public int DefendRoll  { get; }
        public int RawAttackRoll { get; }
        public int RawDefendRoll { get; }
        /// <summary>Player id of the round winner (attacker when attackRoll &gt; defendRoll).</summary>
        public int RoundWinnerId { get; }

        public ContestDiceRound(int rawAttackRoll, int rawDefendRoll, int attackRoll, int defendRoll, int roundWinnerId)
        {
            RawAttackRoll  = rawAttackRoll;
            RawDefendRoll  = rawDefendRoll;
            AttackRoll     = attackRoll;
            DefendRoll     = defendRoll;
            RoundWinnerId  = roundWinnerId;
        }
    }
}
