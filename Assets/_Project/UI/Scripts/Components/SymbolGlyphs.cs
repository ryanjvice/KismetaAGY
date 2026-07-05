using Kismeta.Core.Domain;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Unicode symbol glyphs — zodiac/planets via Amarante, suit/element icons via Tabler.</summary>
    public static class SymbolGlyphs
    {
        public const string ZodiacFontClass = "font-zodiac";
        public const string TablerIconClass = "ti-icon";
        public const string InfoGlyph = "\u2139";
        /// <summary>Season intro recap toolbar button — distinct from general info icons.</summary>
        public const string SeasonRecapGlyph = "\U0001F5D3\uFE0F";
        /// <summary>Card table FAB — other alchemists at the table (single codepoint; UI Toolkit cannot compose ZWJ emoji).</summary>
        public const string AlchemistsGlyph = "\U0001F9D9\uFE0F";
        public const string MenuGlyph = "\uec42";
        public const string ExternalLinkGlyph = "\uea99";
        public const string ChevronDownGlyph = "\uea5f";
        public const string ChevronUpGlyph = "\uea62";

        public static string SeasonEmoji(Season season) => season switch
        {
            Season.Spring => "\U0001F338",
            Season.Summer => "\U0001F324\uFE0F",
            Season.Autumn => "\U0001F342",
            Season.Winter => "\U0001F3D4\uFE0F",
            _ => string.Empty
        };

        public static readonly ZodiacSign[] AllSigns =
        {
            ZodiacSign.Aries, ZodiacSign.Taurus, ZodiacSign.Gemini, ZodiacSign.Cancer,
            ZodiacSign.Leo, ZodiacSign.Virgo, ZodiacSign.Libra, ZodiacSign.Scorpio,
            ZodiacSign.Sagittarius, ZodiacSign.Capricorn, ZodiacSign.Aquarius, ZodiacSign.Pisces
        };

        public static void TagZodiac(VisualElement element)
        {
            element.RemoveFromClassList(TablerIconClass);
            element.AddToClassList(ZodiacFontClass);
        }

        public static void ApplyTablerIcon(Label label, string glyph, Color? tint = null)
        {
            label.text = glyph;
            label.RemoveFromClassList(ZodiacFontClass);
            label.AddToClassList(TablerIconClass);
            if (tint.HasValue)
                label.style.color = tint.Value;
            label.style.display = DisplayStyle.Flex;
        }

        public static Label CreateTablerLabel(string glyph, string? ussClass = null, Color? tint = null)
        {
            var label = new Label(glyph);
            ApplyTablerIcon(label, glyph, tint);
            if (ussClass != null)
                label.AddToClassList(ussClass);
            return label;
        }

        /// <summary>Compact Tabler suit icon for card chips — avoids .ti-icon global sizing.</summary>
        public static Label CreateChipSuitLabel(string glyph, Color? tint = null)
        {
            var label = new Label(glyph);
            label.text = glyph;
            label.RemoveFromClassList(ZodiacFontClass);
            label.RemoveFromClassList(TablerIconClass);
            label.AddToClassList("card-chip__suit");
            if (tint.HasValue)
                label.style.color = tint.Value;
            return label;
        }

        public static Label CreateZodiacLabel(string glyph, string? ussClass = null)
        {
            var label = new Label(glyph);
            TagZodiac(label);
            if (ussClass != null)
                label.AddToClassList(ussClass);
            return label;
        }

        public static Label CreatePlanetLabel(string glyph, string? ussClass = null)
            => CreateZodiacLabel(glyph, ussClass);

        public static Label CreateInfoIconLabel(string? ussClass = null)
        {
            var label = new Label(InfoGlyph);
            if (ussClass != null)
                label.AddToClassList(ussClass);
            return label;
        }

        public static string CompactRank(Rank rank) => rank switch
        {
            Rank.Ace => "A",
            Rank.Two => "2",
            Rank.Three => "3",
            Rank.Four => "4",
            Rank.Five => "5",
            Rank.Six => "6",
            Rank.Seven => "7",
            Rank.Eight => "8",
            Rank.Nine => "9",
            Rank.Ten => "10",
            Rank.Princess => "Ps",
            Rank.Knight => "Kn",
            Rank.Queen => "Qu",
            Rank.King => "Kg",
            _ => "?"
        };

        public static string PlanetGlyph(Planet planet) => planet switch
        {
            Planet.Sun => "\u2609",
            Planet.Moon => "\u263D",
            Planet.Mercury => "\u263F",
            Planet.Venus => "\u2640",
            Planet.Mars => "\u2642",
            Planet.Jupiter => "\u2643",
            Planet.Saturn => "\u2644",
            _ => "?"
        };

        public static string Zodiac(ZodiacSign sign) => sign switch
        {
            ZodiacSign.Aries => "\u2648",
            ZodiacSign.Taurus => "\u2649",
            ZodiacSign.Gemini => "\u264A",
            ZodiacSign.Cancer => "\u264B",
            ZodiacSign.Leo => "\u264C",
            ZodiacSign.Virgo => "\u264D",
            ZodiacSign.Libra => "\u264E",
            ZodiacSign.Scorpio => "\u264F",
            ZodiacSign.Sagittarius => "\u2650",
            ZodiacSign.Capricorn => "\u2651",
            ZodiacSign.Aquarius => "\u2652",
            ZodiacSign.Pisces => "\u2653",
            _ => "?"
        };

        /// <summary>Tabler icon glyph for a minor-arcana suit (tabler-icons.ttf codepoints).</summary>
        public static string SuitGlyph(Suit suit) => suit switch
        {
            Suit.Wands => "\uebcb",
            Suit.Cups => "\ueab7",
            Suit.Pentacles => "\ueb82",
            Suit.Swords => "\uf030",
            _ => "\u00b7"
        };

        /// <summary>Tabler icon glyph for an elemental aspect (tabler-icons.ttf codepoints).</summary>
        public static string ElementGlyph(Element element) => element switch
        {
            Element.Fire => "\uec2c",
            Element.Water => "\uea97",
            Element.Earth => "\ued4f",
            Element.Air => "\uec34",
            _ => "\u00b7"
        };

        public static Color SuitTablerTint(Suit suit, bool hero = false) => suit switch
        {
            Suit.Wands => hero
                ? new Color(0.957f, 0.847f, 0.753f)
                : new Color(0.957f, 0.784f, 0.604f),
            Suit.Cups => new Color(0.435f, 0.659f, 0.831f),
            Suit.Pentacles => new Color(0.957f, 0.827f, 0.369f),
            Suit.Swords => new Color(0.878f, 0.753f, 0.376f),
            _ => Color.white
        };

        public static Color ElementTablerTint(Element element) => element switch
        {
            Element.Fire => new Color(0.941f, 0.600f, 0.482f),
            Element.Water => new Color(0.435f, 0.659f, 0.831f),
            Element.Earth => new Color(0.365f, 0.792f, 0.647f),
            Element.Air => new Color(0.878f, 0.753f, 0.376f),
            _ => Color.white
        };
    }
}
