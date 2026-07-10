namespace Kismeta.Core.Domain
{
    /// <summary>Per-participant dice adjustments applied during a contest series.</summary>
    public struct ContestRollModifiers
    {
        public int AttackBonus { get; set; }
        public int DefendBonus { get; set; }
        public bool MayRerollAttack { get; set; }
        public bool MayRerollDefend { get; set; }

        public static ContestRollModifiers Default => default;

        public bool HasAnyEffect =>
            AttackBonus != 0
            || DefendBonus != 0
            || MayRerollAttack
            || MayRerollDefend;
    }
}
