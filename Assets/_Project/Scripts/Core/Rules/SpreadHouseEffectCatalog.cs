using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>Spread Ace V1 Entry Fee cards for alternate Astral House build payment.</summary>
    public static class SpreadHouseEffectCatalog
    {
        public static bool IsEntryFeeAce(CardDefinition def)
            => def.Rank == Rank.Ace && def.Variant == CardVariant.One
               && def.EffectType.Equals("Entry Fee", System.StringComparison.OrdinalIgnoreCase);
    }
}
