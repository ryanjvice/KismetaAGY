using System;
using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Views;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Binds the Summer main-scene bottom crucible card row from player state.</summary>
    internal static class SummerCrucibleRowBindings
    {
        static readonly string[] ColorKeys = { "red", "blue", "green", "yellow" };

        static readonly Dictionary<VisualElement, List<(VisualElement el, EventCallback<ClickEvent> cb)>> Wired =
            new();

        public static void Bind(
            VisualElement? root,
            PublicPlayerView? player,
            GameSession? session,
            Action<int>? onCardTap)
        {
            if (root == null) return;

            Unwire(root);

            var codexDb = session?.Rules?.CodexDatabase;
            var entries = new List<(VisualElement el, EventCallback<ClickEvent> cb)>();

            for (int i = 0; i < 4; i++)
            {
                var card = root.Q<VisualElement>($"crucible-card-{i}");
                var token = root.Q<VisualElement>($"crucible-token-{i}");
                if (card == null || token == null) continue;

                foreach (var key in ColorKeys)
                    token.EnableInClassList($"crucible-card__token--{key}", false);

                if (player == null || i >= player.CrucibleSlots.Count)
                {
                    card.EnableInClassList("crucible-card--active", false);
                    token.style.display = DisplayStyle.Flex;
                    continue;
                }

                var slot = player.CrucibleSlots[i];
                bool active = slot.State >= CrucibleCardState.Active;
                card.EnableInClassList("crucible-card--active", active);

                if (active)
                {
                    token.style.display = DisplayStyle.None;
                }
                else
                {
                    token.style.display = DisplayStyle.Flex;
                    var formula = codexDb?.GetFormula(player.AssignedCodex, i);
                    if (formula != null)
                    {
                        var colorKey = ColorKey(formula.Cauldron);
                        token.AddToClassList($"crucible-card__token--{colorKey}");
                    }
                }

                if (onCardTap != null)
                {
                    int captured = i;
                    EventCallback<ClickEvent> cb = _ => onCardTap(captured);
                    card.RegisterCallback(cb);
                    entries.Add((card, cb));
                }
            }

            if (entries.Count > 0)
                Wired[root] = entries;
        }

        public static void Unwire(VisualElement? root)
        {
            if (root == null) return;
            if (!Wired.TryGetValue(root, out var entries)) return;

            foreach (var (el, cb) in entries)
                el.UnregisterCallback(cb);

            Wired.Remove(root);
        }

        static string ColorKey(string cauldron) =>
            string.IsNullOrEmpty(cauldron) ? "red" : cauldron.ToLowerInvariant();
    }
}
