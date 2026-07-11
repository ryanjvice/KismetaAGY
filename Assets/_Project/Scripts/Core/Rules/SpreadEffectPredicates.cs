using System;
using System.Linq;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>Shared spread-effect active checks used by UI and rule enforcement.</summary>
    public static class SpreadEffectPredicates
    {
        public static bool IsPassiveCosmicElementActive(CardDefinition def, ZodiacSign cosmic)
        {
            if (!def.EffectType.Equals("Passive", StringComparison.OrdinalIgnoreCase))
                return false;
            if (!TryParseCosmicElementCondition(def.EffectText, out var requiredElement))
                return false;
            return requiredElement == Correspondence.ElementFor(cosmic);
        }

        public static bool IsHarvestHouseElementActive(PlayerState player, CardDefinition def)
        {
            if (!def.EffectType.Equals("Harvest", StringComparison.OrdinalIgnoreCase))
                return false;
            if (!TryParseHarvestElementCondition(def.EffectText, out var requiredElement))
                return HarvestElementForSuit(def.Suit) == Element.None
                    ? false
                    : player.AstralHouses.Any(h => Correspondence.ElementFor(h) == HarvestElementForSuit(def.Suit));

            foreach (var houseSign in player.AstralHouses)
            {
                if (Correspondence.ElementFor(houseSign) == requiredElement)
                    return true;
            }

            return false;
        }

        public static bool IsSocialSpreadActive(CardDefinition def)
            => def.EffectType.Equals("Social", StringComparison.OrdinalIgnoreCase);

        public static bool IsEntryFeeActive(PlayerState player, CardDefinition def)
        {
            if (!SpreadHouseEffectCatalog.IsEntryFeeAce(def))
                return false;
            if (player.CurrentSign == ZodiacSign.None)
                return false;
            return Correspondence.ElementFor(def.Suit) == Correspondence.ElementFor(player.CurrentSign);
        }

        public static Element HarvestElementForSuit(Suit suit) => Correspondence.ElementFor(suit);

        public static Element HarvestDoubleElementFor(CardDefinition def)
        {
            if (!SpreadHarvestEffectCatalog.IsRank2HarvestDouble(def))
                return Element.None;
            if (TryParseHarvestElementCondition(def.EffectText, out var element))
                return element;
            return HarvestElementForSuit(def.Suit);
        }

        public static bool TryParseCosmicElementCondition(string text, out Element element)
        {
            element = Element.None;
            const string marker = "Cosmic Age's Element is ";
            int idx = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return false;

            string tail = text[(idx + marker.Length)..].Trim().TrimEnd('.');
            return Enum.TryParse(tail, ignoreCase: true, out element);
        }

        public static bool TryParseHarvestElementCondition(string text, out Element element)
        {
            element = Element.None;
            const string marker = "Astral Houses on ";
            int idx = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return false;

            string tail = text[(idx + marker.Length)..];
            int end = tail.IndexOf(' ');
            if (end < 0)
                return false;

            return Enum.TryParse(tail[..end], ignoreCase: true, out element);
        }
    }
}
