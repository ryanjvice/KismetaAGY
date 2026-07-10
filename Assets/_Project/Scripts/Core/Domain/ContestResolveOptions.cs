namespace Kismeta.Core.Domain
{
    /// <summary>Inputs for resolving a Duel or Gambit dice series.</summary>
    public sealed class ContestResolveOptions
    {
        public ContestKind Kind { get; }
        public bool BestOfThree { get; }
        public ContestRollModifiers Attacker { get; }
        public ContestRollModifiers Defender { get; }

        public ContestResolveOptions(
            ContestKind kind,
            bool bestOfThree,
            ContestRollModifiers attacker,
            ContestRollModifiers defender)
        {
            Kind         = kind;
            BestOfThree  = bestOfThree;
            Attacker     = attacker;
            Defender     = defender;
        }

        public static ContestResolveOptions Simple(ContestKind kind, bool bestOfThree) =>
            new(kind, bestOfThree, ContestRollModifiers.Default, ContestRollModifiers.Default);
    }
}
