using System;
using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Views;
using Kismeta.UI.Controllers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    public static class HeaderOverlayBindings
    {
        const float CollapsedHeaderFallbackPx = 52f;
        const float HeaderContentGapPx = 8f;

        static bool s_expanded;
        static Action<int>? s_onRivalSelected;
        static Action<int>? s_globalRivalHandler;
        static readonly HashSet<VisualElement> s_geometryWired = new();
        static readonly HashSet<VisualElement> s_rivalClickWired = new();

        /// <summary>Game-level rival click handler (e.g. open Card Table). Takes precedence over per-screen Wire callbacks.</summary>
        public static void ConfigureRivalSelection(Action<int>? handler) => s_globalRivalHandler = handler;

        /// <summary>Resolves the rival handler at click time so stale Populate-time delegates cannot no-op.</summary>
        public static void InvokeRivalSelected(int playerId)
        {
            var handler = s_globalRivalHandler ?? s_onRivalSelected;
            if (handler != null)
                handler.Invoke(playerId);
            else
                Debug.LogWarning($"[HeaderOverlay] Rival {playerId} clicked but no rival handler is registered.");
        }

        public static void Wire(VisualElement? root, Action<int>? onRivalSelected = null)
        {
            if (root == null) return;
            s_onRivalSelected = onRivalSelected;
            RivalStripBuilder.ResetCache();
            WireRivalStripClicks(root);
            root.Q<Button>("header-toggle-btn")?.RegisterCallback<ClickEvent>(_ => ToggleExpanded(root));

            var overlay = OverlayRoot(root);
            if (overlay != null)
            {
                if (s_geometryWired.Add(overlay))
                    overlay.RegisterCallback<GeometryChangedEvent>(_ => ApplyHeaderPad(root));
                overlay.BringToFront();
            }

            SetExpanded(root, s_expanded, animate: false);
        }

        public static VisualElement? OverlayRoot(VisualElement? root) =>
            root?.Q<VisualElement>("game-header-overlay")
            ?? root?.Q(className: "game-header-overlay");

        public static void ToggleExpanded(VisualElement? root) => SetExpanded(root, !s_expanded);

        public static void SetExpanded(VisualElement? root, bool expanded, bool animate = true)
        {
            s_expanded = expanded;
            var overlay = OverlayRoot(root);
            if (overlay == null) return;

            overlay.EnableInClassList("game-header-overlay--collapsed", !expanded);
            overlay.EnableInClassList("game-header-overlay--expanded", expanded);

            var toggle = root?.Q<Button>("header-toggle-btn");
            if (toggle != null)
                toggle.text = expanded ? SymbolGlyphs.ChevronUpGlyph : SymbolGlyphs.ChevronDownGlyph;

            ApplyHeaderPad(root);

            if (animate && expanded)
                UiMotion.AnimateHeaderToggle(overlay, expanding: true);
        }

        public static void BindSummary(
            VisualElement? root,
            GameSession session,
            int localPlayerId)
        {
            if (root == null) return;

            var season = session.Phase.CurrentSeason;
            var sign = session.Board.CosmicAgeSign;
            int rivalCount = 0;
            foreach (var player in GamePublicView.From(session).Players)
            {
                if (player.PlayerId != localPlayerId)
                    rivalCount++;
            }

            var seasonLbl = root.Q<Label>("summary-season");
            if (seasonLbl != null)
                seasonLbl.text = season.ToString();

            var roundLbl = root.Q<Label>("summary-round");
            if (roundLbl != null)
                roundLbl.text = $"Round {session.Board.RoundNumber}";

            var glyph = root.Q<Label>("summary-age-glyph");
            if (glyph != null)
            {
                SymbolGlyphs.TagZodiac(glyph);
                glyph.text = sign == ZodiacSign.None ? "?" : SymbolGlyphs.Zodiac(sign);
            }

            var rivalsLbl = root.Q<Label>("summary-rival-count");
            if (rivalsLbl != null)
                rivalsLbl.text = rivalCount == 1 ? "1 rival" : $"{rivalCount} rivals";
        }

        public static void RefreshHeader(
            VisualElement? root,
            GameSession session,
            GameLoop loop,
            int localPlayerId,
            int activePlayerId,
            Season season,
            Action<int>? onRivalSelected = null)
        {
            StepRailBuilder.EnsureBuilt(root?.Q<VisualElement>("step-rail"), season);
            BindSummary(root, session, localPlayerId);
            MainSceneBindings.BindStatusBar(root, session, loop);
            MainSceneBindings.BindCosmicAgeBanner(root, session);
            RivalStripBuilder.Populate(
                root?.Q<VisualElement>("rivals"),
                GamePublicView.From(session),
                localPlayerId,
                activePlayerId,
                s_globalRivalHandler ?? onRivalSelected ?? s_onRivalSelected);
            ApplyHeaderPad(root);
        }

        public static void SetVisible(VisualElement? root, bool visible)
        {
            OverlayRoot(root)?.EnableInClassList("game-header-overlay--hidden", !visible);
            ApplyHeaderPad(root);
        }

        public static void ApplyHeaderPad(VisualElement? root)
        {
            var body = root?.Q(className: "screen__body");
            if (body == null) return;

            var overlay = OverlayRoot(root);
            bool hidden = overlay == null || overlay.ClassListContains("game-header-overlay--hidden");

            body.EnableInClassList("screen__body--header-pad", !hidden && !s_expanded);
            body.EnableInClassList("screen__body--header-expanded", !hidden && s_expanded);

            var contentHost = body.Q(className: "central-panel") ?? body.Q(className: "stage");
            var toolbar = FindNarrativeToolbar(body);
            var tableFab = root?.Q<Button>("table-fab");
            if (contentHost == null && toolbar == null) return;

            if (hidden)
            {
                if (contentHost != null)
                {
                    contentHost.style.paddingTop = StyleKeyword.Null;
                    contentHost.style.marginTop = StyleKeyword.Null;
                }
                if (toolbar != null)
                    toolbar.style.marginTop = StyleKeyword.Null;
                if (tableFab != null)
                    tableFab.style.top = StyleKeyword.Null;
                return;
            }

            float headerHeight = overlay!.resolvedStyle.height;
            float headerReserve = headerHeight > 0f ? headerHeight : CollapsedHeaderFallbackPx;
            float topOffset = headerReserve + HeaderContentGapPx;

            if (toolbar != null && IsNarrativeToolbarVisible(toolbar))
            {
                toolbar.style.marginTop = topOffset;
                if (contentHost != null)
                {
                    contentHost.style.paddingTop = StyleKeyword.Null;
                    contentHost.style.marginTop = StyleKeyword.Null;
                }
            }
            else
            {
                if (toolbar != null)
                    toolbar.style.marginTop = StyleKeyword.Null;
                if (contentHost != null)
                {
                    contentHost.style.paddingTop = StyleKeyword.Null;
                    contentHost.style.marginTop = topOffset;
                }
            }

            if (tableFab != null)
                tableFab.style.top = StyleKeyword.Null;

            if (headerHeight <= 0f)
            {
                overlay.schedule.Execute(() => ApplyHeaderPad(root)).ExecuteLater(0);
            }
            else
            {
                overlay.BringToFront();
            }
        }

        static VisualElement? FindNarrativeToolbar(VisualElement body) =>
            body.Q(className: "spring-hub__toolbar")
            ?? body.Q(className: "summer-main__toolbar")
            ?? body.Q(className: "summer-hub__toolbar")
            ?? body.Q(className: "autumn-main__toolbar")
            ?? body.Q(className: "autumn-hub__toolbar");

        static bool IsNarrativeToolbarVisible(VisualElement toolbar) =>
            !toolbar.ClassListContains("spring-hub__toolbar--hidden")
            && !toolbar.ClassListContains("summer-main__toolbar--hidden")
            && !toolbar.ClassListContains("summer-hub__toolbar--hidden")
            && !toolbar.ClassListContains("autumn-main__toolbar--hidden")
            && !toolbar.ClassListContains("autumn-hub__toolbar--hidden");

        static void WireRivalStripClicks(VisualElement root)
        {
            var rivals = root.Q<VisualElement>("rivals");
            if (rivals == null || !s_rivalClickWired.Add(rivals))
                return;

            rivals.RegisterCallback<ClickEvent>(evt =>
            {
                var el = evt.target as VisualElement;
                while (el != null && el != rivals)
                {
                    if (el.ClassListContains("rival") && el.userData is int playerId)
                    {
                        evt.StopPropagation();
                        InvokeRivalSelected(playerId);
                        return;
                    }
                    el = el.parent;
                }
            });
        }
    }
}
