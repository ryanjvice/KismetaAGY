using UnityEngine.UIElements;
using Kismeta.Core.Views;

namespace Kismeta.UI.Components
{
    /// <summary>Builds rival strip rows from public player views.</summary>
    public static class RivalStripBuilder
    {
        public static void Populate(VisualElement rivalsHost, GamePublicView view, int localPlayerId)
        {
            if (rivalsHost == null) return;
            rivalsHost.Clear();

            foreach (var player in view.Players)
            {
                if (player.PlayerId == localPlayerId)
                    continue;

                var row = new VisualElement();
                row.AddToClassList("rival");

                var dot = new VisualElement();
                dot.AddToClassList("rival__dot");
                row.Add(dot);

                var name = new Label($"P{player.PlayerId}");
                name.AddToClassList("rival__name");
                row.Add(name);

                var stat = new Label($"Spr {player.Spread.Count}");
                stat.AddToClassList("rival__stat");
                row.Add(stat);

                rivalsHost.Add(row);
            }
        }
    }
}
