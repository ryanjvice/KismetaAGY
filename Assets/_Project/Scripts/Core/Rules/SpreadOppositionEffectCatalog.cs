using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>Spread cards that modify Opposition alignment (rank 10 wild suit).</summary>
    public static class SpreadOppositionEffectCatalog
    {
        public static bool IsRank10WildSuit(CardDefinition def)
            => def.Rank == Rank.Ten && def.Variant == CardVariant.One
               && def.EffectType.Equals("Opposition", System.StringComparison.OrdinalIgnoreCase);
    }
}
