using Kismeta.Core.Domain;
using UnityEngine;

namespace Kismeta.UI.Components
{
    public static class PlayerUiNames
    {
        static readonly string[] ColorNames = { "Red", "Green", "Blue", "White" };

        public static string ForPlayer(int playerId) =>
            playerId >= 0 && playerId < ColorNames.Length
                ? $"{ColorNames[playerId]} alchemist"
                : $"Player {playerId}";

        public static string ShortName(int playerId) =>
            playerId >= 0 && playerId < ColorNames.Length
                ? ColorNames[playerId]
                : $"P{playerId}";

        public static Color PlayerColor(int playerId) => playerId switch
        {
            0 => new Color(0.75f, 0.22f, 0.17f),
            1 => new Color(0.12f, 0.43f, 0.29f),
            2 => new Color(0.18f, 0.43f, 0.64f),
            3 => new Color(0.96f, 0.83f, 0.37f),
            _ => new Color(0.7f, 0.7f, 0.7f)
        };

        /// <summary>Roster tag for pre-game screens (e.g. agekeeper contest).</summary>
        public static string RoleSuffix(int playerId, int humanPlayerCount)
        {
            int humans = System.Math.Clamp(humanPlayerCount, 0, ColorNames.Length);
            if (playerId < humans)
                return playerId == 0 ? " (you)" : "";
            return " (AI)";
        }
    }
}
