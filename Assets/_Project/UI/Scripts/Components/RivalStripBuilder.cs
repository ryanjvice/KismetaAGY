using Kismeta.Core.Views;
using Kismeta.UI.Controllers;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Builds rival strip rows from public player views.</summary>
    public static class RivalStripBuilder
    {
        public static void Populate(
            VisualElement rivalsHost,
            GamePublicView view,
            int localPlayerId,
            int activePlayerId = -1)
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

                row.Add(PlayerSummaryRowBuilder.Build(player));

                rivalsHost.Add(row);
            }
        }
    }
}
