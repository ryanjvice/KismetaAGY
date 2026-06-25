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

        public static Color PlayerColor(int playerId) => UiTheme.PlayerColor(playerId);

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
