using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>
    /// Full-viewport UI host: safe-area padding, responsive USS tokens, and overlay layer
    /// for modals and bottom sheets. Design baseline is 380×844; production fills the screen.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ViewportLayout : MonoBehaviour
    {
        public const float DesignWidth = 380f;
        public const float DesignHeight = 844f;
        public const float ScaleMin = 0.85f;
        public const float ScaleMax = 1.35f;
        public const float TabletBreakpoint = 600f;

        [SerializeField] private PanelSettings _panelSettings;
        [SerializeField] private VisualTreeAsset _initialScreen;

        private UIDocument _document;
        private VisualElement _overlayLayer;

        public VisualElement Root => _document != null ? _document.rootVisualElement : null;
        public bool IsOverlayVisible => _overlayLayer != null && _overlayLayer.style.display == DisplayStyle.Flex;

        protected virtual void Awake()
        {
            _document = GetComponent<UIDocument>();
            if (_panelSettings != null)
                _document.panelSettings = _panelSettings;
            if (_initialScreen != null)
                _document.visualTreeAsset = _initialScreen;
        }

        protected virtual void OnEnable()
        {
            var root = Root;
            if (root == null) return;

            root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            root.schedule.Execute(InitializeTree).StartingIn(0);
        }

        protected virtual void OnDisable()
        {
            Root?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        public void SetScreen(VisualTreeAsset screen)
        {
            if (_document == null) return;
            DismissOverlay();
            _document.visualTreeAsset = screen;
            _document.rootVisualElement.schedule.Execute(InitializeTree).StartingIn(0);
        }

        public void ShowModal(VisualTreeAsset asset)
        {
            if (asset == null) return;
            var overlay = EnsureOverlayLayer();
            overlay.Clear();
            overlay.RemoveFromClassList("overlay-layer--sheet");
            overlay.style.display = DisplayStyle.Flex;

            var content = asset.Instantiate();
            content.style.flexGrow = 0;
            content.style.flexShrink = 1;
            overlay.Add(content);
        }

        public void ShowBottomSheet(VisualTreeAsset asset)
        {
            if (asset == null) return;
            var overlay = EnsureOverlayLayer();
            overlay.Clear();
            overlay.AddToClassList("overlay-layer--sheet");
            overlay.style.display = DisplayStyle.Flex;

            var content = asset.Instantiate();
            content.style.flexGrow = 0;
            content.style.flexShrink = 1;
            overlay.Add(content);
        }

        public void DismissOverlay()
        {
            if (_overlayLayer == null) return;
            _overlayLayer.Clear();
            _overlayLayer.style.display = DisplayStyle.None;
            _overlayLayer.RemoveFromClassList("overlay-layer--sheet");
        }

        private void OnGeometryChanged(GeometryChangedEvent evt) => ApplyLayout(evt.target as VisualElement);

        private void InitializeTree()
        {
            var root = Root;
            if (root == null) return;

            EnsureAppShell(root);
            ApplyLayout(root);
        }

        private void EnsureAppShell(VisualElement root)
        {
            root.AddToClassList("kismeta-root");

            if (root.Q("app-shell") != null)
                return;

            var toReparent = new List<VisualElement>();
            foreach (var child in root.Children())
                toReparent.Add(child);

            var shell = new VisualElement { name = "app-shell" };
            shell.AddToClassList("app-shell");

            var content = new VisualElement { name = "content-layer" };
            content.AddToClassList("content-layer");

            foreach (var child in toReparent)
            {
                child.RemoveFromHierarchy();
                content.Add(child);
            }

            shell.Add(content);
            root.Add(shell);
            EnsureOverlayLayer();
        }

        private VisualElement EnsureOverlayLayer()
        {
            if (_overlayLayer != null)
                return _overlayLayer;

            var shell = Root?.Q("app-shell");
            if (shell == null)
                return null;

            _overlayLayer = shell.Q<VisualElement>("overlay-layer");
            if (_overlayLayer != null)
                return _overlayLayer;

            _overlayLayer = new VisualElement { name = "overlay-layer" };
            _overlayLayer.AddToClassList("overlay-layer");
            _overlayLayer.style.display = DisplayStyle.None;
            _overlayLayer.RegisterCallback<ClickEvent>(OnOverlayBackgroundClicked);
            shell.Add(_overlayLayer);
            return _overlayLayer;
        }

        private void OnOverlayBackgroundClicked(ClickEvent evt)
        {
            if (evt.target != _overlayLayer)
                return;
            DismissOverlay();
        }

        private void ApplyLayout(VisualElement root)
        {
            if (root == null) return;

            float w = root.resolvedStyle.width;
            float h = root.resolvedStyle.height;
            if (w <= 0f || h <= 0f)
                return;

            float scale = Mathf.Clamp(Mathf.Min(w / DesignWidth, h / DesignHeight), ScaleMin, ScaleMax);
            ApplySafeAreaPadding(root, w, h);
            ApplyScaleTokens(root, scale);
            ApplyViewportClass(root, w, h);
        }

        private static void ApplySafeAreaPadding(VisualElement root, float panelW, float panelH)
        {
            var safe = Screen.safeArea;
            float sw = Screen.width;
            float sh = Screen.height;
            if (sw <= 0f || sh <= 0f)
                return;

            root.style.paddingLeft = safe.x / sw * panelW;
            root.style.paddingRight = (sw - safe.xMax) / sw * panelW;
            root.style.paddingBottom = safe.y / sh * panelH;
            root.style.paddingTop = (sh - safe.yMax) / sh * panelH;
        }

        private static void ApplyScaleTokens(VisualElement root, float scale)
        {
            SetToken(root, "--ui-scale", scale);
            SetToken(root, "--space-xs", 4f * scale);
            SetToken(root, "--space-sm", 8f * scale);
            SetToken(root, "--space-md", 12f * scale);
            SetToken(root, "--space-lg", 16f * scale);
            SetToken(root, "--space-xl", 24f * scale);
            SetToken(root, "--font-micro", 8f * scale);
            SetToken(root, "--font-label", 10f * scale);
            SetToken(root, "--font-body", 12f * scale);
            SetToken(root, "--font-title", 14f * scale);
            SetToken(root, "--font-display", 34f * scale);
            SetToken(root, "--touch-min", Mathf.Max(44f, 44f * scale));
            SetToken(root, "--card-chip-w", 34f * scale);
            SetToken(root, "--card-chip-h", 48f * scale);
        }

        private static void ApplyViewportClass(VisualElement root, float w, float h)
        {
            root.RemoveFromClassList("viewport--compact");
            root.RemoveFromClassList("viewport--regular");
            root.RemoveFromClassList("viewport--tablet");

            float shortest = Mathf.Min(w, h);
            if (shortest >= TabletBreakpoint)
                root.AddToClassList("viewport--tablet");
            else if (w < 360f)
                root.AddToClassList("viewport--compact");
            else
                root.AddToClassList("viewport--regular");
        }

        private static void SetToken(VisualElement element, string name, float value) =>
            element.style.SetCustomProperty(name, new StyleFloat(value));
    }
}
