using Kismeta.UI.Controllers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>
    /// Shows the Great Year overview as a dismissible overlay from season info scenes.
    /// </summary>
    public sealed class GameOverviewRecapHost : MonoBehaviour
    {
#if UNITY_EDITOR
        const string OverviewPath = "Assets/_Project/UI/UXML/batch1/GameOverviewIntro.uxml";
#endif

        VisualTreeAsset? _overviewAsset;
        GameOverviewIntroController? _controller;
        ViewportLayout? _layout;

        public bool IsOpen => _layout != null && _layout.IsOverlayVisible;

        void Awake()
        {
            _layout ??= GetComponent<ViewportLayout>();
            EnsureAssets();
        }

        public void Configure(VisualTreeAsset? overviewAsset, GameOverviewIntroController? controller)
        {
            _overviewAsset = overviewAsset;
            _controller = controller;
            _layout ??= GetComponent<ViewportLayout>();
            EnsureAssets();
        }

        public void Show()
        {
            _layout ??= GetComponent<ViewportLayout>();
            EnsureAssets();

            if (_layout == null)
            {
                Debug.LogWarning("[GameOverviewRecapHost] ViewportLayout missing — cannot show overview.");
                return;
            }

            if (_overviewAsset == null)
            {
                Debug.LogWarning("[GameOverviewRecapHost] GameOverviewIntro UXML is not assigned.");
                return;
            }

            if (_controller == null)
            {
                Debug.LogWarning("[GameOverviewRecapHost] GameOverviewIntroController missing.");
                return;
            }

            _layout.ShowModal(_overviewAsset);
            _layout.ApplyBoundedOverlaySheet();

            var root = _layout.OverlayContentRoot;
            if (root == null)
                return;

            _controller.ReviewMode = true;
            _controller.OnDismiss = Dismiss;
            _controller.AttachTo(root);
        }

        public void Dismiss()
        {
            _controller?.Detach();
            if (_controller != null)
            {
                _controller.ReviewMode = false;
                _controller.OnDismiss = null;
            }

            _layout?.DismissOverlay();
        }

        void EnsureAssets()
        {
#if UNITY_EDITOR
            _overviewAsset ??= UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(OverviewPath);
#endif
        }
    }
}
