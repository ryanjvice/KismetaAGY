using Kismeta.Core.Domain;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Unicode symbol glyphs — zodiac/planets via Amarante, suit icons via Noto Color Emoji.</summary>
    public static class SymbolGlyphs
    {
        public const string EmojiFontClass = "font-emoji";
        public const string ZodiacFontClass = "font-zodiac";

        public static readonly ZodiacSign[] AllSigns =
        {
            ZodiacSign.Aries, ZodiacSign.Taurus, ZodiacSign.Gemini, ZodiacSign.Cancer,
            ZodiacSign.Leo, ZodiacSign.Virgo, ZodiacSign.Libra, ZodiacSign.Scorpio,
            ZodiacSign.Sagittarius, ZodiacSign.Capricorn, ZodiacSign.Aquarius, ZodiacSign.Pisces
        };

        public static void TagEmoji(VisualElement element) => element.AddToClassList(EmojiFontClass);

        public static void TagZodiac(VisualElement element)
        {
            element.RemoveFromClassList(EmojiFontClass);
            element.AddToClassList(ZodiacFontClass);
        }

        public static Label CreateEmojiLabel(string glyph, string? ussClass = null)
        {
            var label = new Label(glyph);
            TagEmoji(label);
            if (ussClass != null)
                label.AddToClassList(ussClass);
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

        public static string SuitGlyph(Suit suit) => suit switch
        {
            Suit.Wands => "\U0001FA84",
            Suit.Cups => "\U0001F377",
            Suit.Pentacles => "\U0001FA99",
            Suit.Swords => "\U0001F5E1\uFE0F",
            _ => "\u00B7"
        };
    }
}
