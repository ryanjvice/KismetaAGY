using UnityEngine;

namespace Kismeta.UI.Components
{
    /// <summary>C# color constants mirroring <c>Kismeta.uss</c> design tokens.</summary>
    public static class UiTheme
    {
        public static readonly Color Ink = new(0.953f, 0.914f, 0.824f);
        public static readonly Color Gold = new(0.910f, 0.725f, 0.290f);
        public static readonly Color GoldDeep = new(0.788f, 0.588f, 0.184f);
        public static readonly Color GoldBright = new(0.957f, 0.827f, 0.369f);
        public static readonly Color Muted = new(0.722f, 0.604f, 0.431f);
        public static readonly Color MutedDim = new(0.541f, 0.416f, 0.478f);
        public static readonly Color TextBody = new(0.910f, 0.894f, 0.820f);
        public static readonly Color TextSub = new(0.722f, 0.600f, 0.431f);
        public static readonly Color TextDim = new(0.541f, 0.416f, 0.478f);

        public static readonly Color ZodiacRed = new(0.890f, 0.118f, 0.141f);
        public static readonly Color ZodiacGreen = new(0f, 0.659f, 0.310f);
        public static readonly Color ZodiacYellow = new(1f, 0.843f, 0f);
        public static readonly Color ZodiacBlue = new(0f, 0.588f, 0.839f);

        public static Color PlayerColor(int playerId) => playerId switch
        {
            0 => ZodiacRed,
            1 => ZodiacGreen,
            2 => ZodiacBlue,
            3 => ZodiacYellow,
            _ => new Color(0.7f, 0.7f, 0.7f)
        };
    }
}
