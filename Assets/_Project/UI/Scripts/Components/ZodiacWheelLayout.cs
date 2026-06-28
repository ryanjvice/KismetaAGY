using Kismeta.Core.Domain;
using UnityEngine;

namespace Kismeta.UI.Components
{
    /// <summary>
    /// Polar layout for the Spring hub zodiac wheel. Tokens target the center of each 30° outer
    /// segment (clockwise from 12 o'clock): Gemini 11–12, Cancer 12–1, Leo 1–2, …, Taurus 10–11.
    /// </summary>
    public static class ZodiacWheelLayout
    {
        static readonly ZodiacSign[] SignOrderFromTop =
        {
            ZodiacSign.Gemini, ZodiacSign.Cancer, ZodiacSign.Leo, ZodiacSign.Virgo,
            ZodiacSign.Libra, ZodiacSign.Scorpio, ZodiacSign.Sagittarius, ZodiacSign.Capricorn,
            ZodiacSign.Aquarius, ZodiacSign.Pisces, ZodiacSign.Aries, ZodiacSign.Taurus
        };

        /// <summary>Segment center angle in radians: slot * 30° − 15° clockwise from 12 o'clock.</summary>
        public static float SegmentCenterAngleRad(int slot) =>
            slot * (Mathf.PI / 6f) - (Mathf.PI / 12f);

        /// <summary>Wheel slot index (0 = Gemini at top boundary, advancing clockwise).</summary>
        public static int SlotForSign(ZodiacSign sign)
        {
            for (int i = 0; i < SignOrderFromTop.Length; i++)
            {
                if (SignOrderFromTop[i] == sign)
                    return i;
            }

            return -1;
        }

        public static float SegmentCenterAngleRad(ZodiacSign sign, float offsetDeg = 0f)
        {
            int slot = SlotForSign(sign);
            if (slot < 0)
                return 0f;
            return SegmentCenterAngleRad(slot) + offsetDeg * Mathf.Deg2Rad;
        }
    }
}
