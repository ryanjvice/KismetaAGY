using Kismeta.Core.Entities;
using Kismeta.UI.Controllers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>
    /// Manages Spring hub overlays (Build a House modal) without changing ScreenRouter's active screen.
    /// </summary>
    public sealed class SpringOverlayHost : MonoBehaviour
    {
        VisualTreeAsset? _buildHouse;

        ViewportLayout? _layout;
        GameSession? _session;
        CommandBridge? _bridge;

        BuildHouseController? _build;

        enum ActiveOverlay { None, BuildHouse }

        ActiveOverlay _active = ActiveOverlay.None;

        public bool IsOpen => _active != ActiveOverlay.None
            && _layout != null && _layout.IsOverlayVisible;

        void Awake() => EnsureControllers();

        public void Configure(VisualTreeAsset buildHouse)
        {
            _buildHouse = buildHouse;
            EnsureControllers();
        }

        void EnsureControllers()
        {
            _layout ??= GetComponent<ViewportLayout>();
            _build ??= GetComponent<BuildHouseController>();
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;

            if (_layout != null && _layout.IsOverlayVisible && session != null)
                RefreshOpenOverlay();
        }

        public void DismissIfNotHumanTurn()
        {
            if (_bridge != null && _bridge.CanSubmit) return;
            Dismiss();
        }

        public void ShowBuildHouse()
        {
            if (!ShowModal(_buildHouse, _build, ActiveOverlay.BuildHouse)) return;
            WireBuild();
            RefreshOpenOverlay();
        }

        public void Dismiss()
        {
            _active = ActiveOverlay.None;
            _build?.Detach();
            _layout?.DismissOverlay();
        }

        bool ShowModal<T>(VisualTreeAsset? asset, T? controller, ActiveOverlay kind)
            where T : class, IVisualRootController
        {
            if (_layout == null || asset == null || controller == null) return false;
            _layout.ShowModal(asset);
            var root = _layout.OverlayContentRoot;
            if (root == null) return false;
            controller.AttachTo(root);
            _active = kind;
            return true;
        }

        void RefreshOpenOverlay()
        {
            if (_session == null || _bridge == null) return;
            _build?.BindState(_session, _bridge);
        }

        void WireBuild()
        {
            if (_build == null) return;
            _build.OnBack = Dismiss;
            _build.OnCompleted = Dismiss;
        }
    }
}
