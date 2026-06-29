using Kismeta.Core.Entities;
using Kismeta.UI.Controllers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>
    /// Manages Spring hub overlays (Build a House, board inspect) without changing ScreenRouter's active screen.
    /// </summary>
    public sealed class SpringOverlayHost : MonoBehaviour
    {
        VisualTreeAsset? _buildHouse;
        VisualTreeAsset? _boardInspect;

        ViewportLayout? _layout;
        GameSession? _session;
        CommandBridge? _bridge;

        BuildHouseController? _build;
        SpringBoardInspectController? _inspect;

        enum ActiveOverlay { None, BuildHouse, BoardInspect }

        ActiveOverlay _active = ActiveOverlay.None;

        public bool IsOpen => _active != ActiveOverlay.None
            && _layout != null && _layout.IsOverlayVisible;

        void Awake() => EnsureControllers();

        public void Configure(VisualTreeAsset buildHouse, VisualTreeAsset? boardInspect = null)
        {
            _buildHouse = buildHouse;
            _boardInspect = boardInspect;
            EnsureControllers();
        }

        void EnsureControllers()
        {
            _layout ??= GetComponent<ViewportLayout>();
            _build ??= GetComponent<BuildHouseController>();
            _inspect ??= GetComponent<SpringBoardInspectController>();
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

        public void ShowBoardInspect()
        {
            if (!ShowModal(_boardInspect, _inspect, ActiveOverlay.BoardInspect)) return;
            WireInspect();
            RefreshOpenOverlay();
        }

        public void DismissBoardInspect()
        {
            if (_active != ActiveOverlay.BoardInspect) return;
            Dismiss();
        }

        public void Dismiss()
        {
            _active = ActiveOverlay.None;
            _build?.Detach();
            _inspect?.Detach();
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
            if (_session == null) return;

            switch (_active)
            {
                case ActiveOverlay.BuildHouse when _bridge != null:
                    _build?.BindState(_session, _bridge);
                    break;
                case ActiveOverlay.BoardInspect:
                    _inspect?.BindState(_session);
                    break;
            }
        }

        void WireBuild()
        {
            if (_build == null) return;
            _build.OnBack = Dismiss;
            _build.OnCompleted = Dismiss;
        }

        void WireInspect()
        {
            if (_inspect == null) return;
            _inspect.OnBack = Dismiss;
        }
    }
}
