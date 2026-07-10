namespace Kismeta.Core.Domain
{
    /// <summary>One d12 roll-off in a Duel or Gambit series.</summary>
    public readonly struct ContestDiceRound
    {
        public int AttackRoll  { get; }
        public int DefendRoll  { get; }
        /// <summary>Player id of the round winner (attacker when attackRoll &gt; defendRoll).</summary>
        public int RoundWinnerId { get; }

        public ContestDiceRound(int attackRoll, int defendRoll, int roundWinnerId)
        {
            AttackRoll     = attackRoll;
            DefendRoll     = defendRoll;
            RoundWinnerId  = roundWinnerId;
        }
    }
}
