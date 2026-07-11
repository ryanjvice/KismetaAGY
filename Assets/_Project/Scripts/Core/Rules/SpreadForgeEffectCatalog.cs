using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>Spread cards that modify Fire (rank 7 reagent grant, Queen V1 wild reagent cost).</summary>
    public static class SpreadForgeEffectCatalog
    {
        public static bool IsRank7ForgeReagent(CardDefinition def)
            => def.Rank == Rank.Seven && def.Variant == CardVariant.One
               && def.EffectType.Equals("Forge", System.StringComparison.OrdinalIgnoreCase);

        public static bool IsQueenV1WildReagent(CardDefinition def)
            => def.Rank == Rank.Queen && def.Variant == CardVariant.One
               && def.EffectType.Equals("Forge", System.StringComparison.OrdinalIgnoreCase);

        public static ReagentType ReagentForRank7(CardDefinition def)
            => Correspondence.ReagentFor(def.Suit);

        public static ReagentType WildReagentForQueen(CardDefinition def)
            => Correspondence.ReagentFor(def.Suit);
    }
}
