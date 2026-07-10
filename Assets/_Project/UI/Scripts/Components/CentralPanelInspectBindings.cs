using System;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>
    /// Wires the reusable central-panel inspect FAB. Opening the panel is delegated
    /// to the season overlay host via the supplied callback.
    /// </summary>
    public static class CentralPanelInspectBindings
    {
        const string FabName = "central-panel-inspect-fab";
        const float InsetPx = 8f;

        static Action? s_onOpen;
        static EventCallback<ClickEvent>? s_fabClick;
        static VisualElement? s_wiredRoot;

        public static void Wire(VisualElement? screenRoot, Action onOpen)
        {
            Unwire();

            if (screenRoot == null) return;

            s_wiredRoot = screenRoot;
            s_onOpen = onOpen;

            var fab = screenRoot.Q<Button>(FabName);
            s_fabClick = _ => s_onOpen?.Invoke();
            fab?.RegisterCallback(s_fabClick);
            ApplyFabOverlay(screenRoot);
        }

        public static void Unwire()
        {
            s_wiredRoot?.Q<Button>(FabName)?.UnregisterCallback(s_fabClick);

            s_wiredRoot = null;
            s_onOpen = null;
            s_fabClick = null;
        }

        public static void SetFabVisible(VisualElement? screenRoot, bool visible, Action? onHide = null)
        {
            var fab = screenRoot?.Q<Button>(FabName);
            if (fab == null) return;

            fab.EnableInClassList("central-panel-inspect-fab--hidden", !visible);
            if (visible)
                ApplyFabOverlay(screenRoot);
            if (!visible)
                onHide?.Invoke();
        }

        /// <summary>
        /// UXML template instances wrap the host in a flex sibling; reparent onto
        /// <c>.central-panel</c> and pin top-right so the FAB overlays the board.
        /// </summary>
        static void ApplyFabOverlay(VisualElement? screenRoot)
        {
            var panel = screenRoot?.Q(className: "central-panel");
            var host = screenRoot?.Q(className: "central-panel-inspect");
            if (panel == null || host == null) return;

            if (host.parent != panel)
                panel.Add(host);

            var frame = screenRoot.Q(className: "summer-consult-frame");
            var rightInset = frame != null ? InsetPx + 12f : InsetPx;

            host.style.position = Position.Absolute;
            host.style.top = InsetPx;
            host.style.right = rightInset;
            host.style.left = StyleKeyword.Auto;
            host.style.bottom = StyleKeyword.Auto;
            host.pickingMode = PickingMode.Ignore;
            host.BringToFront();
        }
    }
}
