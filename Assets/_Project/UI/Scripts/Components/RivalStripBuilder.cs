using System;
using Kismeta.Core.Views;
using Kismeta.UI.Controllers;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Builds rival strip rows from public player views.</summary>
    public static class RivalStripBuilder
    {
        static VisualElement? s_lastHost;
        static int s_lastHash;

        public static void ResetCache()
        {
            s_lastHost = null;
            s_lastHash = 0;
        }

        public static void Populate(
            VisualElement rivalsHost,
            GamePublicView view,
            int localPlayerId,
            int activePlayerId = -1,
            Action<int>? onRivalSelected = null)
        {
            if (rivalsHost == null) return;

            int hash = ComputeHash(view, localPlayerId, activePlayerId);
            if (ReferenceEquals(rivalsHost, s_lastHost) && hash == s_lastHash && rivalsHost.childCount > 0)
                return;

            s_lastHost = rivalsHost;
            s_lastHash = hash;
            rivalsHost.Clear();

            foreach (var player in view.Players)
            {
                if (player.PlayerId == localPlayerId)
                    continue;

                var row = new VisualElement();
                row.AddToClassList("rival");
                if (player.PlayerId == activePlayerId)
                    row.AddToClassList("rival--active");

                row.AddToClassList("rival--clickable");
                row.pickingMode = PickingMode.Position;
                int playerId = player.PlayerId;
                row.userData = playerId;
                row.AddManipulator(new Clickable(() => HeaderOverlayBindings.InvokeRivalSelected(playerId)));

                var dot = new VisualElement();
                dot.AddToClassList("rival__dot");
                dot.style.backgroundColor = PlayerUiNames.PlayerColor(player.PlayerId);
                dot.pickingMode = PickingMode.Ignore;
                row.Add(dot);

                var summary = PlayerSummaryRowBuilder.Build(player);
                SetPickingModeIgnore(summary);
                row.Add(summary);

                rivalsHost.Add(row);
            }
        }

        static void SetPickingModeIgnore(VisualElement root)
        {
            root.pickingMode = PickingMode.Ignore;
            foreach (var child in root.Children())
                SetPickingModeIgnore(child);
        }

        static int ComputeHash(GamePublicView view, int localPlayerId, int activePlayerId)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + localPlayerId;
                hash = hash * 31 + activePlayerId;
                foreach (var player in view.Players)
                {
                    if (player.PlayerId == localPlayerId)
                        continue;

                    hash = hash * 31 + player.PlayerId;
                    hash = hash * 31 + (int)player.CurrentSign;
                    hash = hash * 31 + player.Spread.Count;
                    hash = hash * 31 + player.HandCardCount;
                    hash = hash * 31 + player.Arcanum.Count;
                    hash = hash * 31 + ReagentTotal(player);
                }
                return hash;
            }
        }

        static int ReagentTotal(PublicPlayerView player)
        {
            int total = 0;
            foreach (var kv in player.Reagents)
                total += kv.Value;
            return total;
        }
    }
}
