using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>Spread rank-9 Social effects — post-contest card draws.</summary>
    public static class SpreadSocialEffectCatalog
    {
        public static bool IsRank9Social(CardDefinition def)
            => def.Rank == Rank.Nine && def.Variant == CardVariant.One
               && def.EffectType.Equals("Social", System.StringComparison.OrdinalIgnoreCase);

        public static bool TryGetDrawOnSuccess(CardDefinition def, ContestKind kind, out int drawCount)
        {
            drawCount = 0;
            if (!IsRank9Social(def))
                return false;

            return def.Suit switch
            {
                Suit.Cups when kind == ContestKind.Trade => SetDraw(1, out drawCount),
                Suit.Pentacles when kind == ContestKind.Gambit => SetDraw(2, out drawCount),
                Suit.Swords when kind == ContestKind.Duel => SetDraw(2, out drawCount),
                Suit.Wands when kind == ContestKind.Opposition => SetDraw(1, out drawCount),
                _ => false
            };
        }

        static bool SetDraw(int count, out int drawCount)
        {
            drawCount = count;
            return true;
        }
    }
}
