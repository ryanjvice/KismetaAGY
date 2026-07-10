using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>Spread card definitions that modify crafting (Build V1, rank 8, King V1).</summary>
    public static class SpreadCraftEffectCatalog
    {
        public static bool IsBuildV1Salt(CardDefinition def)
            => def.Rank == Rank.Three && def.Variant == CardVariant.One
               && def.EffectType.Equals("Build", System.StringComparison.OrdinalIgnoreCase);

        public static bool IsRank8CraftDiscount(CardDefinition def, ReagentType reagent)
        {
            if (def.Rank != Rank.Eight || def.Variant != CardVariant.One)
                return false;
            if (!def.EffectType.Equals("Craft", System.StringComparison.OrdinalIgnoreCase))
                return false;

            return Correspondence.SuitFor(reagent) == def.Suit;
        }

        public static bool IsKingV1DiscardCraft(CardDefinition def)
            => def.Rank == Rank.King && def.Variant == CardVariant.One
               && def.EffectType.Equals("Craft", System.StringComparison.OrdinalIgnoreCase);

        public static ReagentType ReagentForKing(CardDefinition def)
            => Correspondence.ReagentFor(def.Suit);
    }
}
