using System;
using Kismeta.Core.Entities;
using Kismeta.UI.Controllers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>
    /// Manages Autumn action overlays (Fire, Temper, Manage Cards, Leave Stasis, End Autumn, forge inspect).
    /// </summary>
    public sealed class AutumnOverlayHost : MonoBehaviour
    {
        VisualTreeAsset? _fireStone;
        VisualTreeAsset? _temperStone;
        VisualTreeAsset? _manageCards;
        VisualTreeAsset? _leaveStasis;
        VisualTreeAsset? _endAutumn;
        VisualTreeAsset? _forgeInspect;

        ViewportLayout? _layout;
        GameSession? _session;
        CommandBridge? _bridge;

        FireStoneController? _fire;
        TemperStoneController? _temper;
        ManageCardsController? _manage;
        LeaveStasisController? _leaveStasisCtrl;
        EndAutumnController? _endAutumnCtrl;
        AutumnForgeInspectController? _forgeInspectCtrl;

        enum ActiveOverlay { None, Fire, Temper, Manage, LeaveStasis, EndAutumn, ForgeInspect }
        ActiveOverlay _active = ActiveOverlay.None;

        public bool IsOpen => _layout != null && _layout.IsOverlayVisible;

        // Autumn rail groups: Craft (0), Activate (1), Forge (2).
        // Fire / Temper / Leave Stasis are all forge-stage actions.
        public int? ActiveActionGroupIndex => _active switch
        {
            ActiveOverlay.Fire or ActiveOverlay.Temper or ActiveOverlay.LeaveStasis => 2,
            _ => null
        };

        public string? ActiveNarrativeStepId => _active switch
        {
            ActiveOverlay.Fire => "autumn.fire",
            ActiveOverlay.Temper => "autumn.temper",
            ActiveOverlay.LeaveStasis => "autumn.leavestasis",
            _ => null
        };

        public Action? OverlayChanged;

        void Awake() => EnsureControllers();

        public void Configure(
            VisualTreeAsset fireStone,
            VisualTreeAsset temperStone,
            VisualTreeAsset manageCards,
            VisualTreeAsset leaveStasis,
            VisualTreeAsset endAutumn,
            VisualTreeAsset? forgeInspect = null)
        {
            _fireStone = fireStone;
            _temperStone = temperStone;
            _manageCards = manageCards;
            _leaveStasis = leaveStasis;
            _endAutumn = endAutumn;
            _forgeInspect = forgeInspect;
            EnsureControllers();
        }

        void EnsureControllers()
        {
            _layout ??= GetComponent<ViewportLayout>();
            _fire ??= GetComponent<FireStoneController>();
            _temper ??= GetComponent<TemperStoneController>();
            _manage ??= GetComponent<ManageCardsController>();
            _leaveStasisCtrl ??= GetComponent<LeaveStasisController>();
            _endAutumnCtrl ??= GetComponent<EndAutumnController>();
            _forgeInspectCtrl ??= GetComponent<AutumnForgeInspectController>();
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

        public void ShowFire() { EnsureControllers(); ShowOverlay(_fireStone, _fire, WireFire, ActiveOverlay.Fire); }
        public void ShowTemper() { EnsureControllers(); ShowOverlay(_temperStone, _temper, WireTemper, ActiveOverlay.Temper); }
        public void ShowManageCards() { EnsureControllers(); ShowOverlay(_manageCards, _manage, WireManage, ActiveOverlay.Manage); }
        public void ShowLeaveStasis() { EnsureControllers(); ShowOverlay(_leaveStasis, _leaveStasisCtrl, WireLeaveStasis, ActiveOverlay.LeaveStasis); }
        public void ShowEndAutumn() { EnsureControllers(); ShowOverlay(_endAutumn, _endAutumnCtrl, WireEndAutumn, ActiveOverlay.EndAutumn); }

        public void ShowForgeInspect()
        {
            EnsureControllers();
            ShowOverlay(_forgeInspect, _forgeInspectCtrl, WireForgeInspect, ActiveOverlay.ForgeInspect);
        }

        public void DismissForgeInspect()
        {
            if (_active != ActiveOverlay.ForgeInspect) return;
            Dismiss();
        }

        public void Dismiss()
        {
            _active = ActiveOverlay.None;
            _fire?.Detach();
            _temper?.Detach();
            _manage?.Detach();
            _leaveStasisCtrl?.Detach();
            _endAutumnCtrl?.Detach();
            _forgeInspectCtrl?.Detach();
            _layout?.DismissOverlay();
            NotifyOverlayChanged();
        }

        void ShowOverlay<T>(VisualTreeAsset? asset, T? controller, System.Action wire, ActiveOverlay kind)
            where T : OverlayController
        {
            if (_layout == null || asset == null || controller == null) return;
            _layout.ShowModal(asset);
            var root = _layout.OverlayContentRoot;
            if (root == null) return;
            controller.AttachTo(root);
            _active = kind;
            wire();
            RefreshOpenOverlay();
            NotifyOverlayChanged();
        }

        void NotifyOverlayChanged() => OverlayChanged?.Invoke();

        void RefreshOpenOverlay()
        {
            if (_session == null) return;

            switch (_active)
            {
                case ActiveOverlay.Fire when _bridge != null:
                    _fire?.BindState(_session, _bridge);
                    break;
                case ActiveOverlay.Temper when _bridge != null:
                    _temper?.BindState(_session, _bridge);
                    break;
                case ActiveOverlay.Manage when _bridge != null:
                    _manage?.BindState(_session, _bridge);
                    break;
                case ActiveOverlay.LeaveStasis when _bridge != null:
                    _leaveStasisCtrl?.BindState(_session, _bridge);
                    break;
                case ActiveOverlay.EndAutumn when _bridge != null:
                    _endAutumnCtrl?.BindState(_session, _bridge);
                    break;
                case ActiveOverlay.ForgeInspect:
                    _forgeInspectCtrl?.BindState(_session);
                    break;
            }
        }

        void WireFire()
        {
            if (_fire == null) return;
            _fire.OnClose = Dismiss;
            _fire.OnFired = Dismiss;
        }

        void WireTemper()
        {
            if (_temper == null) return;
            _temper.OnClose = Dismiss;
            _temper.OnTempered = Dismiss;
        }

        void WireManage()
        {
            if (_manage == null) return;
            _manage.OnClose = Dismiss;
        }

        void WireLeaveStasis()
        {
            if (_leaveStasisCtrl == null) return;
            _leaveStasisCtrl.OnBack = Dismiss;
            _leaveStasisCtrl.OnStay = Dismiss;
            _leaveStasisCtrl.OnReturned = Dismiss;
        }

        void WireEndAutumn()
        {
            if (_endAutumnCtrl == null) return;
            _endAutumnCtrl.OnKeep = Dismiss;
            _endAutumnCtrl.OnConfirm = () =>
            {
                if (_bridge?.TrySubmitPass() == true)
                    Dismiss();
            };
        }

        void WireForgeInspect()
        {
            if (_forgeInspectCtrl == null) return;
            _forgeInspectCtrl.OnBack = Dismiss;
        }
    }
}
