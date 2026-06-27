using System;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
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
            public Action? OnOpenActiveEffects;
        }

        public static void Wire(VisualElement? root, Callbacks callbacks)
        {
            if (root == null) return;

            root.Q<Button>("hand-btn")?.RegisterCallback<ClickEvent>(_ => callbacks.OnHandToggle?.Invoke());
            root.Q<Button>("arcanum-btn")?.RegisterCallback<ClickEvent>(_ => callbacks.OnArcanumToggle?.Invoke());
            root.Q<Button>("inventory-toggle-btn")?.RegisterCallback<ClickEvent>(_ => ToggleExpanded(root));
            root.Q<Button>("table-fab")?.RegisterCallback<ClickEvent>(_ => callbacks.OnOpenCardTable?.Invoke());
            root.Q<Button>("effects-fab")?.RegisterCallback<ClickEvent>(_ => callbacks.OnOpenActiveEffects?.Invoke());

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

            PlayerSummaryRowBuilder.BindExisting(
                root,
                PublicPlayerView.From(session.Players[localPlayerId]));
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
            var effectsFab = root?.Q<Button>("effects-fab");
            if (tableFab != null)
                tableFab.EnableInClassList("table-fab--hidden", !visible);
            if (effectsFab != null)
                effectsFab.EnableInClassList("effects-fab--hidden", !visible);
            root?.Q(className: "hub-fab-stack")
                ?.EnableInClassList("hub-fab-stack--hidden", !visible);
            SetNarrativeToolbarVisible(root, visible);
            ApplyInventoryPad(root);
            HeaderOverlayBindings.ApplyHeaderPad(root);
        }

        static void SetNarrativeToolbarVisible(VisualElement? root, bool visible)
        {
            var body = root?.Q(className: "screen__body");
            if (body == null) return;

            body.Q(className: "spring-hub__toolbar")
                ?.EnableInClassList("spring-hub__toolbar--hidden", !visible);
            body.Q(className: "summer-main__toolbar")
                ?.EnableInClassList("summer-main__toolbar--hidden", !visible);
            body.Q(className: "summer-hub__toolbar")
                ?.EnableInClassList("summer-hub__toolbar--hidden", !visible);
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
