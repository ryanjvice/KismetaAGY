namespace Kismeta.Core.Domain
{
    /// <summary>
    /// Per-age contest modifiers set by Fate cards and other effects.
    /// Cleared each Winter Transit.
    /// </summary>
    public struct ContestEffectFlags
    {
        /// <summary>Duels resolve as best-of-three (first to 2 round wins).</summary>
        public bool DuelBestOfThree;

        /// <summary>Gambits resolve as best-of-three (first to 2 round wins).</summary>
        public bool GambitBestOfThree;

        public bool IsBestOfThree(ContestKind kind) => kind switch
        {
            ContestKind.Duel   => DuelBestOfThree,
            ContestKind.Gambit => GambitBestOfThree,
            _ => false
        };

        public static ContestEffectFlags Default => default;
    }
}
