using System;
using System.Collections.Generic;
using Kismeta.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>
    /// Full-viewport UI host: splash backdrop on the app shell; safe-area insets on
    /// screen chrome (top) and footers (bottom) only — content fills edge-to-edge.
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
        [SerializeField] private StyleSheet _tokenStylesheet;

        private UIDocument _document;
        private VisualElement _overlayLayer;
        private VisualElement _overlayContentRoot;

        public VisualElement Root => _document != null ? _document.rootVisualElement : null;

        /// <summary>Active screen root inside the content layer (after app shell is built).</summary>
        public VisualElement ContentScreenRoot
        {
            get
            {
                var layer = Root?.Q("content-layer");
                if (layer == null || layer.childCount == 0)
                    return null;
                return layer[0];
            }
        }
        public bool IsOverlayVisible => _overlayLayer != null && _overlayLayer.style.display == DisplayStyle.Flex;

        /// <summary>Root of the most recently shown overlay UXML clone.</summary>
        public VisualElement? OverlayContentRoot => _overlayContentRoot;

        /// <summary>Last computed scale factor (informational; PanelSettings also scales the panel).</summary>
        public float UiScale { get; private set; } = 1f;

        protected virtual void Awake()
        {
            _document = GetComponent<UIDocument>();
            if (_panelSettings != null)
                _document.panelSettings = _panelSettings;
            EnsureTokenStylesheetAssigned();
        }

        protected virtual void OnEnable()
        {
            var layoutRoot = GetLayoutRoot();
            if (layoutRoot == null) return;

            layoutRoot.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            layoutRoot.schedule.Execute(InitializeTree).StartingIn(0);
        }

        protected virtual void OnDisable()
        {
            GetLayoutRoot()?.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        public void SetScreen(VisualTreeAsset screen)
        {
            if (_document == null || screen == null)
                return;

            DismissOverlay();
            var root = Root;
            if (root == null)
                return;

            EnsurePanelRootFillsViewport(root);
            EnsureAppShell(root);
            var content = root.Q("content-layer");
            if (content == null)
            {
                Debug.LogError("[ViewportLayout] content-layer missing. Assign AppShell.uxml to UIDocument Source Asset.");
                return;
            }

            StretchFlexColumnChild(content);
            content.Clear();
            var host = new VisualElement();
            host.AddToClassList("screen-host");
            PrepareCloneHost(host, stretchFull: true);
            ApplyAssetStylesheets(host, screen);
            content.Add(host);
            screen.CloneTree(host);
            var screenRoot = host.Q(className: "screen") ?? (host.childCount > 0 ? host[0] : null);
            if (screenRoot != null)
            {
                ApplyAssetStylesheets(screenRoot, screen);
                StretchFlexColumnChild(screenRoot);
            }

            var layoutRoot = GetLayoutRoot();
            if (layoutRoot != null)
                layoutRoot.schedule.Execute(() => ApplyLayout(layoutRoot)).StartingIn(0);
        }

        public void ShowModal(VisualTreeAsset asset)
        {
            if (asset == null) return;
            var overlay = EnsureOverlayLayer();
            if (overlay == null) return;

            overlay.Clear();
            overlay.RemoveFromClassList("overlay-layer--sheet");
            overlay.style.display = DisplayStyle.Flex;
            InstantiateOverlay(overlay, asset, _tokenStylesheet);
            SetOverlayBackdropBlur(true);
        }

        public void ShowBottomSheet(VisualTreeAsset asset)
        {
            if (asset == null) return;
            var overlay = EnsureOverlayLayer();
            if (overlay == null) return;

            overlay.Clear();
            overlay.AddToClassList("overlay-layer--sheet");
            overlay.style.display = DisplayStyle.Flex;
            InstantiateOverlay(overlay, asset, _tokenStylesheet);
            if (_overlayContentRoot != null)
                UiMotion.AnimateSheetRise(_overlayContentRoot);
            SetOverlayBackdropBlur(true);
        }

        /// <summary>Show a programmatic overlay (no UXML asset).</summary>
        public void ShowOverlayElement(VisualElement content, bool asSheet = false)
        {
            if (content == null) return;
            var overlay = EnsureOverlayLayer();
            if (overlay == null) return;

            overlay.Clear();
            if (asSheet)
                overlay.AddToClassList("overlay-layer--sheet");
            else
                overlay.RemoveFromClassList("overlay-layer--sheet");
            overlay.style.display = DisplayStyle.Flex;

            var host = CreateOverlayCloneHost();
            host.Add(content);
            overlay.Add(host);
            _overlayContentRoot = content;
            if (asSheet)
                UiMotion.AnimateSheetRise(content);
            SetOverlayBackdropBlur(true);
        }

        /// <summary>
        /// Clone overlay UXML into a token-scope host that is attached before cloning.
        /// Avoids NRE from inline <c>var(--token)</c> in UXML during detached Instantiate().
        /// </summary>
        private VisualElement InstantiateOverlay(VisualElement overlay, VisualTreeAsset asset, StyleSheet? tokenStylesheet)
        {
            var host = CreateOverlayCloneHost(tokenStylesheet);
            ApplyAssetStylesheets(host, asset);
            overlay.Add(host);
            asset.CloneTree(host);
            var screenRoot = host.Q(className: "screen") ?? (host.childCount > 0 ? host[0] : null);
            if (screenRoot != null)
                ApplyAssetStylesheets(screenRoot, asset);
            _overlayContentRoot = screenRoot ?? host;
            return host;
        }

        VisualElement CreateOverlayCloneHost(StyleSheet? tokenStylesheet = null)
        {
            var host = new VisualElement();
            host.style.flexGrow = 0;
            host.style.flexShrink = 1;
            host.style.flexDirection = FlexDirection.Column;
            PrepareCloneHost(host, stretchFull: false, tokenStylesheet, overlayScope: true);
            host.AddToClassList("overlay-clone-host");
            return host;
        }

        private static void ApplyAssetStylesheets(VisualElement target, VisualTreeAsset asset)
        {
            if (target == null || asset == null)
                return;

            foreach (var sheet in asset.stylesheets)
            {
                if (sheet != null && !target.styleSheets.Contains(sheet))
                    target.styleSheets.Add(sheet);
            }
        }

        /// <summary>
        /// Attach token scope before <see cref="VisualTreeAsset.CloneTree"/>.
        /// Inline <c>var(--token)</c> in UXML <c>style=""</c> attributes NRE during clone — use USS classes or literal values in UXML instead.
        /// </summary>
        private void PrepareCloneHost(VisualElement host, bool stretchFull, StyleSheet? tokenStylesheet = null, bool overlayScope = false)
        {
            host.AddToClassList(overlayScope ? "kismeta-scope" : "kismeta-root");
            EnsureTokenStylesheets(host, tokenStylesheet);
            if (stretchFull)
                StretchFlexColumnChild(host);
        }

        private void EnsureTokenStylesheets(VisualElement host, StyleSheet? tokenStylesheet = null)
        {
            EnsureTokenStylesheetAssigned();
            var sheet = tokenStylesheet ?? _tokenStylesheet;
            if (sheet != null && !host.styleSheets.Contains(sheet))
                host.styleSheets.Add(sheet);
        }

        private void EnsureTokenStylesheetAssigned()
        {
            if (_tokenStylesheet != null)
                return;

#if UNITY_EDITOR
            _tokenStylesheet = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/_Project/UI/USS/Kismeta.uss");
#endif
            if (_tokenStylesheet == null)
                Debug.LogWarning("[ViewportLayout] Kismeta.uss not assigned; inline var() in UXML may fail at runtime.");
        }

        public void DismissOverlay()
        {
            if (_overlayLayer == null) return;
            _overlayLayer.Clear();
            _overlayLayer.style.display = DisplayStyle.None;
            _overlayLayer.RemoveFromClassList("overlay-layer--sheet");
            _overlayContentRoot = null;
            SetOverlayBackdropBlur(false);
        }

        void SetOverlayBackdropBlur(bool active)
        {
            var content = Root?.Q<VisualElement>("content-layer");
            if (active)
                content?.AddToClassList("content-layer--overlay-backdrop");
            else
                content?.RemoveFromClassList("content-layer--overlay-backdrop");
        }

        /// <summary>Runs after the panel has non-zero layout (avoids startup races).</summary>
        public void RunWhenReady(Action action)
        {
            if (action == null) return;

            var layoutRoot = GetLayoutRoot();
            if (layoutRoot == null)
            {
                action();
                return;
            }

            var attempts = 0;
            void TryRun()
            {
                attempts++;
                float w = layoutRoot.resolvedStyle.width;
                float h = layoutRoot.resolvedStyle.height;
                if (w > 0f && h > 0f)
                {
                    ApplyLayout(layoutRoot);
                    action();
                    return;
                }

                if (attempts >= 120)
                {
                    Debug.LogWarning("[ViewportLayout] RunWhenReady timed out; running action anyway.");
                    action();
                    return;
                }

                layoutRoot.schedule.Execute(TryRun).StartingIn(1);
            }

            layoutRoot.schedule.Execute(TryRun).StartingIn(1);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt) =>
            ApplyLayout(evt.target as VisualElement);

        private void InitializeTree()
        {
            var root = Root;
            if (root == null) return;

            EnsurePanelRootFillsViewport(root);
            EnsureAppShell(root);
            UiArtBindings.ApplyAppShellBackdrop(root.Q("app-shell"));
            BindOverlayLayer(root);

            var layoutRoot = GetLayoutRoot();
            if (layoutRoot == null) return;

            StretchToContentLayer(layoutRoot);

            bool routerManaged = GetComponent<ScreenRouter>() != null;
            if (!routerManaged && _initialScreen != null && ContentScreenRoot == null)
                SetScreen(_initialScreen);
            else
                ApplyLayout(layoutRoot);
        }

        private static void EnsurePanelRootFillsViewport(VisualElement panelRoot)
        {
            panelRoot.style.flexGrow = 1;
            panelRoot.style.flexShrink = 0;
            panelRoot.style.flexDirection = FlexDirection.Column;
            panelRoot.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            panelRoot.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
        }

        /// <summary>Fills the UIDocument panel (kismeta-root).</summary>
        private static void StretchToContentLayer(VisualElement el)
        {
            el.style.flexGrow = 1;
            el.style.flexShrink = 0;
            el.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            el.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            el.style.minHeight = new StyleLength(new Length(100, LengthUnit.Percent));
            el.style.flexDirection = FlexDirection.Column;
            el.style.alignSelf = Align.Stretch;
        }

        /// <summary>Flex child inside app-shell; shrinks when vertical space is tight.</summary>
        private static void StretchFlexColumnChild(VisualElement el)
        {
            el.style.flexGrow = 1;
            el.style.flexShrink = 1;
            el.style.minHeight = 0;
            el.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            el.style.height = StyleKeyword.Auto;
            el.style.flexDirection = FlexDirection.Column;
            el.style.alignSelf = Align.Stretch;
        }

        private VisualElement GetLayoutRoot()
        {
            var root = Root;
            if (root == null) return null;

            var shell = root.Q(className: "kismeta-root");
            return shell ?? root;
        }

        private void BindOverlayLayer(VisualElement root)
        {
            _overlayLayer = root.Q<VisualElement>("overlay-layer");
            if (_overlayLayer == null) return;

            _overlayLayer.UnregisterCallback<ClickEvent>(OnOverlayBackgroundClicked);
            _overlayLayer.RegisterCallback<ClickEvent>(OnOverlayBackgroundClicked);
        }

        private void EnsureAppShell(VisualElement root)
        {
            var layoutRoot = root.Q(className: "kismeta-root");
            if (layoutRoot != null)
                layoutRoot.AddToClassList("kismeta-root");
            else
                root.AddToClassList("kismeta-root");

            if (root.Q("app-shell") != null)
                return;

            var toReparent = new List<VisualElement>();
            foreach (var child in root.Children())
                toReparent.Add(child);

            var shell = new VisualElement { name = "app-shell" };
            shell.AddToClassList("app-shell");

            var backdrop = new VisualElement { name = "app-backdrop" };
            backdrop.AddToClassList("app-shell__backdrop");
            shell.Add(backdrop);

            var content = new VisualElement { name = "content-layer" };
            content.AddToClassList("content-layer");

            foreach (var child in toReparent)
            {
                child.RemoveFromHierarchy();
                content.Add(child);
            }

            shell.Add(content);
            root.Add(shell);
            UiArtBindings.ApplyAppShellBackdrop(shell);
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
            {
                _overlayLayer.RegisterCallback<ClickEvent>(OnOverlayBackgroundClicked);
                return _overlayLayer;
            }

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
            UiScale = scale;

            ClearSafeAreaPadding(root);
            ComputeSafeAreaInsets(w, h, out float topInset, out float bottomInset);

            if (UsesFullBleedTopLayout(root))
                ClearTopSafeInset(root);
            else
                ApplyTopSafeInsetToContent(root, topInset);

            ApplyBottomSafeInsetToFooters(root, bottomInset);

            ApplyViewportClass(root, w, h);
        }

        static void ComputeSafeAreaInsets(float panelW, float panelH, out float topInset, out float bottomInset)
        {
            topInset = 0f;
            bottomInset = 0f;

            var safe = Screen.safeArea;
            float sw = Screen.width;
            float sh = Screen.height;
            if (sw <= 0f || sh <= 0f)
                return;

            topInset = (sh - safe.yMax) / sh * panelH;
            bottomInset = safe.y / sh * panelH;
        }

        static bool UsesFullBleedTopLayout(VisualElement root)
        {
            var screen = GetActiveScreen(root);
            return screen != null && screen.ClassListContains("screen--title");
        }

        static void ApplyBottomSafeInsetToFooters(VisualElement root, float bottomInset)
        {
            var screen = GetActiveScreen(root);
            if (screen == null)
                return;

            float insetPad = bottomInset + 8f;
            ApplyFooterBottomPad(screen.Q(className: "title-menu"), insetPad, 16f);
            ApplyFooterBottomPad(screen.Q(className: "menu-screen__footer"), insetPad, 24f);
        }

        static void ApplyFooterBottomPad(VisualElement? footer, float minFromSafeArea, float ussDefault)
        {
            if (footer == null)
                return;

            footer.style.paddingBottom = Mathf.Max(ussDefault, minFromSafeArea);
        }

        static void ClearSafeAreaPadding(VisualElement root)
        {
            root.style.paddingLeft = 0;
            root.style.paddingRight = 0;
            root.style.paddingBottom = 0;
            root.style.paddingTop = 0;
        }

        static void ClearTopSafeInset(VisualElement root)
        {
            var overlay = root.Q<VisualElement>("overlay-layer");
            if (overlay != null)
                overlay.style.paddingTop = 0;

            var screen = GetActiveScreen(root);
            if (screen == null)
                return;

            screen.style.paddingTop = 0;
            var chrome = screen.Q<VisualElement>("game-header-overlay")
                ?? screen.Q(className: "game-header-overlay");
            if (chrome != null)
                chrome.style.paddingTop = 0;
        }

        static VisualElement? GetActiveScreen(VisualElement root)
        {
            var contentLayer = root.Q<VisualElement>("content-layer");
            if (contentLayer == null || contentLayer.childCount == 0)
                return null;

            var screenHost = contentLayer[0];
            return screenHost.Q(className: "screen")
                ?? (screenHost.childCount > 0 ? screenHost[0] : null);
        }

        private static void ApplyTopSafeInsetToContent(VisualElement root, float topInset)
        {
            var overlay = root.Q<VisualElement>("overlay-layer");
            if (overlay != null)
                overlay.style.paddingTop = topInset;

            var screen = GetActiveScreen(root);
            if (screen == null)
                return;

            var chrome = screen.Q<VisualElement>("game-header-overlay")
                ?? screen.Q(className: "game-header-overlay");
            if (chrome != null)
            {
                screen.style.paddingTop = 0;
                chrome.style.paddingTop = topInset;
            }
            else
            {
                screen.style.paddingTop = topInset;
            }
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
    }
}
