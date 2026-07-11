using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>Spread cards that modify hand or spread limits (Queen V2 passives).</summary>
    public static class SpreadLimitEffectCatalog
    {
        public static bool IsQueenV2Passive(CardDefinition def)
            => def.Rank == Rank.Queen && def.Variant == CardVariant.Two
               && def.EffectType.Equals("Passive", System.StringComparison.OrdinalIgnoreCase);

        public static bool GrantsHandBonus(CardDefinition def)
            => IsQueenV2Passive(def)
               && def.Suit is Suit.Cups or Suit.Pentacles;

        public static bool GrantsSpreadBonus(CardDefinition def)
            => IsQueenV2Passive(def)
               && def.Suit is Suit.Swords or Suit.Wands;
    }
}
