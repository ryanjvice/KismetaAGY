using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>Spread card definitions that modify harvest (Ace V2 passive, rank 2 house doubling).</summary>
    public static class SpreadHarvestEffectCatalog
    {
        public static bool IsAceV2Passive(CardDefinition def)
            => def.Rank == Rank.Ace && def.Variant == CardVariant.Two
               && def.EffectType.Equals("Passive", System.StringComparison.OrdinalIgnoreCase);

        public static bool IsRank2HarvestDouble(CardDefinition def)
            => def.Rank == Rank.Two && def.Variant == CardVariant.One
               && def.EffectType.Equals("Harvest", System.StringComparison.OrdinalIgnoreCase);
    }
}
