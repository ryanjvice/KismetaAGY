using System;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>
    /// Wires the reusable central-panel inspect FAB. Opening the panel is delegated
    /// to <see cref="SpringOverlayHost"/> via the supplied callback.
    /// </summary>
    public static class CentralPanelInspectBindings
    {
        const string FabName = "central-panel-inspect-fab";

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
            if (!visible)
                onHide?.Invoke();
        }
    }
}
