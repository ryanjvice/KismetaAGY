using Kismeta.Core.Domain;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Unicode emoji / symbol glyphs rendered with Noto Color Emoji (<see cref="EmojiFontClass"/>).</summary>
    public static class SymbolGlyphs
    {
        public const string EmojiFontClass = "font-emoji";

        public static void TagEmoji(VisualElement element) => element.AddToClassList(EmojiFontClass);

        public static Label CreateEmojiLabel(string glyph, string? ussClass = null)
        {
            var label = new Label(glyph);
            TagEmoji(label);
            if (ussClass != null)
                label.AddToClassList(ussClass);
            return label;
        }

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
