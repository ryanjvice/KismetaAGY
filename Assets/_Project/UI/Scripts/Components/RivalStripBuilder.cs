using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Views;
using Kismeta.UI.Controllers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Builds rival strip rows from public player views.</summary>
    public static class RivalStripBuilder
    {
        public static void Populate(
            VisualElement rivalsHost,
            GameSession session,
            GamePublicView view,
            int localPlayerId,
            int activePlayerId = -1,
            Season season = Season.Spring)
        {
            if (rivalsHost == null) return;
            rivalsHost.Clear();

            foreach (var player in view.Players)
            {
                if (player.PlayerId == localPlayerId)
                    continue;

                var row = new VisualElement();
                row.AddToClassList("rival");
                if (player.PlayerId == activePlayerId)
                    row.AddToClassList("rival--active");

                var dot = new VisualElement();
                dot.AddToClassList("rival__dot");
                dot.style.backgroundColor = PlayerUiNames.PlayerColor(player.PlayerId);
                row.Add(dot);

                var name = new Label(CeremonyBindings.PlayerName(session, player.PlayerId));
                name.AddToClassList("rival__name");
                row.Add(name);

                var stat = new Label(FormatRivalStat(session, player, season));
                stat.AddToClassList("rival__stat");
                row.Add(stat);

                rivalsHost.Add(row);
            }
        }

        static string FormatRivalStat(GameSession session, PublicPlayerView player, Season season)
        {
            return season switch
            {
                Season.Summer => $"Spr {player.Spread.Count}",
                Season.Autumn => FormatAutumnStat(session, player.PlayerId),
                Season.Spring => player.CurrentSign == ZodiacSign.None
                    ? "no sign"
                    : player.CurrentSign.ToString(),
                Season.Winter => $"Spr {player.Spread.Count}/5",
                _ => $"Spr {player.Spread.Count}"
            };
        }

        static string FormatAutumnStat(GameSession session, int playerId)
        {
            if (playerId < 0 || playerId >= session.Players.Count)
                return "—";
            return AutumnActionBindings.StoneStatusLabel(session.Players[playerId]);
        }
    }
}
