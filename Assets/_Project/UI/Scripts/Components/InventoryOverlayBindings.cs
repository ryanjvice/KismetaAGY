using System;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Views;
using Kismeta.UI.Controllers;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    public static class InventoryOverlayBindings
    {
        static bool s_expanded;

        public struct Callbacks
        {
            public Action? OnHandToggle;
            public Action? OnArcanumToggle;
            public Action? OnOpenCardTable;
        }

        public static void Wire(VisualElement? root, Callbacks callbacks)
        {
            if (root == null) return;

            root.Q<Button>("hand-btn")?.RegisterCallback<ClickEvent>(_ => callbacks.OnHandToggle?.Invoke());
            root.Q<Button>("arcanum-btn")?.RegisterCallback<ClickEvent>(_ => callbacks.OnArcanumToggle?.Invoke());
            root.Q<Button>("inventory-toggle-btn")?.RegisterCallback<ClickEvent>(_ => ToggleExpanded(root));
            root.Q<Button>("table-fab")?.RegisterCallback<ClickEvent>(_ => callbacks.OnOpenCardTable?.Invoke());

            SetExpanded(root, s_expanded, animate: false);
        }

        public static VisualElement? OverlayRoot(VisualElement? root) =>
            root?.Q<VisualElement>("player-inventory-overlay")
            ?? root?.Q(className: "player-inventory-overlay");

        public static void ToggleExpanded(VisualElement? root) => SetExpanded(root, !s_expanded);

        public static void SetExpanded(VisualElement? root, bool expanded, bool animate = true)
        {
            s_expanded = expanded;
            var overlay = OverlayRoot(root);
            if (overlay == null) return;

            overlay.EnableInClassList("player-inventory-overlay--collapsed", !expanded);
            overlay.EnableInClassList("player-inventory-overlay--expanded", expanded);

            var toggle = root?.Q<Button>("inventory-toggle-btn");
            if (toggle != null)
                toggle.text = expanded ? "⌄" : "⌃";

            ApplyInventoryPad(root);

            if (animate && expanded)
                UiMotion.AnimateInventoryToggle(overlay, expanding: true);
        }

        public static void BindSummary(VisualElement? root, GameSession session, int localPlayerId)
        {
            if (root == null) return;
            if (localPlayerId < 0 || localPlayerId >= session.Players.Count)
                return;

            var player = session.Players[localPlayerId];
            var sign = player.CurrentSign;
            var view = GamePublicView.From(session);
            var local = MainSceneBindings.LocalPlayer(view, localPlayerId);
            var privateView = PlayerPrivateView.From(session, localPlayerId);

            int spreadCount = local?.Spread.Count ?? 0;
            int handCount = privateView.Hand.Count;
            int arcanumCount = player.Arcanum.Count;
            int reagentTotal = 0;
            foreach (ReagentType rt in Enum.GetValues(typeof(ReagentType)))
                reagentTotal += player.GetReagent(rt);

            var glyph = root.Q<Label>("summary-sign-glyph");
            if (glyph != null)
            {
                SymbolGlyphs.TagZodiac(glyph);
                glyph.text = sign == ZodiacSign.None ? "?" : SymbolGlyphs.Zodiac(sign);
            }

            SetSummaryLabel(root, "summary-spread-count", "S", spreadCount);
            SetSummaryLabel(root, "summary-hand-count", "H", handCount);
            SetSummaryLabel(root, "summary-arcanum-count", "A", arcanumCount);
            SetSummaryLabel(root, "summary-reagent-total", "R", reagentTotal);
        }

        static void SetSummaryLabel(VisualElement root, string name, string prefix, int count)
        {
            var lbl = root.Q<Label>(name);
            if (lbl != null)
                lbl.text = $"{prefix} {count}";
        }

        public static void RefreshInventory(
            VisualElement? root,
            GameSession session,
            int localPlayerId,
            DockZone zone,
            Action<string>? onInspect)
        {
            MainSceneBindings.BindPlayerStrip(root, session, localPlayerId);
            BindSummary(root, session, localPlayerId);
            MainSceneBindings.SetDockZoneFabActive(root, zone);
            MainSceneBindings.BindDockStrip(root, session, localPlayerId, zone, onInspect);
        }

        public static void SetVisible(VisualElement? root, bool visible)
        {
            OverlayRoot(root)?.EnableInClassList("player-inventory-overlay--hidden", !visible);
            var tableFab = root?.Q<Button>("table-fab");
            if (tableFab != null)
                tableFab.EnableInClassList("table-fab--hidden", !visible);
            ApplyInventoryPad(root);
        }

        static void ApplyInventoryPad(VisualElement? root)
        {
            var body = root?.Q(className: "screen__body");
            if (body == null) return;

            var overlay = OverlayRoot(root);
            bool hidden = overlay != null && overlay.ClassListContains("player-inventory-overlay--hidden");
            bool pad = !hidden && !s_expanded;
            body.EnableInClassList("screen__body--inventory-pad", pad);
        }
    }
}
