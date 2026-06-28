using System;

namespace Kismeta.UI.Components
{
    /// <summary>Tarot-standard arcana labels for crucible card faces (0 = The Fool, I–XXI).</summary>
    public static class RomanNumerals
    {
        static readonly (int value, string numeral)[] Pairs =
        {
            (21, "XXI"), (20, "XX"), (19, "XIX"), (18, "XVIII"), (17, "XVII"), (16, "XVI"),
            (15, "XV"), (14, "XIV"), (13, "XIII"), (12, "XII"), (11, "XI"), (10, "X"),
            (9, "IX"), (8, "VIII"), (7, "VII"), (6, "VI"), (5, "V"), (4, "IV"),
            (3, "III"), (2, "II"), (1, "I"),
        };

        public static string ToArcanaLabel(int arcanaNumber)
        {
            if (arcanaNumber == 0) return "0";
            if (arcanaNumber < 1 || arcanaNumber > 21) return "?";
            return ToRoman(arcanaNumber);
        }

        static string ToRoman(int value)
        {
            var result = "";
            foreach (var (v, numeral) in Pairs)
            {
                while (value >= v)
                {
                    result += numeral;
                    value -= v;
                }
            }

            return result;
        }
    }
}
