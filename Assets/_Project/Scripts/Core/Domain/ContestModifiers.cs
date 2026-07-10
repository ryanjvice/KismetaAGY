namespace Kismeta.Core.Domain
{
    /// <summary>Aggregated contest modifiers for both participants before dice resolve.</summary>
    public sealed class ContestModifiers
    {
        public ContestRollModifiers Attacker { get; }
        public ContestRollModifiers Defender { get; }
        /// <summary>When true, this duel resolves best-of-three even without board-wide Justice.</summary>
        public bool AttackerForcesBestOfThree { get; }

        public ContestModifiers(
            ContestRollModifiers attacker,
            ContestRollModifiers defender,
            bool attackerForcesBestOfThree)
        {
            Attacker                  = attacker;
            Defender                  = defender;
            AttackerForcesBestOfThree = attackerForcesBestOfThree;
        }

        public static ContestModifiers None =>
            new(ContestRollModifiers.Default, ContestRollModifiers.Default, false);
    }
}
