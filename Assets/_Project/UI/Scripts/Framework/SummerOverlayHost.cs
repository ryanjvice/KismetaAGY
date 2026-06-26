using System;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.UI.Controllers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>
    /// Manages Summer action overlays (sheets + modals) without changing ScreenRouter's active screen.
    /// </summary>
    public sealed class SummerOverlayHost : MonoBehaviour
    {
        VisualTreeAsset? _summerSheets;
        VisualTreeAsset? _craftReagent;
        VisualTreeAsset? _activateCard;
        VisualTreeAsset? _buildHouse;
        VisualTreeAsset? _placeWards;
        VisualTreeAsset? _endSummer;

        ViewportLayout? _layout;
        GameSession? _session;
        CommandBridge? _bridge;

        SummerSheetsController? _sheets;
        CraftReagentController? _craft;
        ActivateCardController? _activate;
        BuildHouseController? _build;
        PlaceWardsController? _wards;
        EndSummerController? _endSummerCtrl;

        enum ActiveOverlay
        {
            None,
            CraftBuildSheet,
            ConsortSheet,
            CraftReagent,
            BuildHouse,
            PlaceWards,
            Activate,
            EndSummer
        }

        ActiveOverlay _active = ActiveOverlay.None;

        public Action OnTrade;
        public Action OnDuel;
        public Action OnGambit;
        public Action? OverlayChanged;

        public bool IsOpen => _active != ActiveOverlay.None
            && _layout != null && _layout.IsOverlayVisible;

        public int? ActiveActionGroupIndex => _active switch
        {
            ActiveOverlay.CraftBuildSheet or ActiveOverlay.CraftReagent or ActiveOverlay.BuildHouse
                or ActiveOverlay.PlaceWards => 0,
            ActiveOverlay.ConsortSheet => 1,
            ActiveOverlay.Activate => 2,
            _ => null
        };

        void Awake() => EnsureControllers();

        public void Configure(
            VisualTreeAsset summerSheets,
            VisualTreeAsset craftReagent,
            VisualTreeAsset activateCard,
            VisualTreeAsset buildHouse,
            VisualTreeAsset placeWards,
            VisualTreeAsset endSummer)
        {
            _summerSheets = summerSheets;
            _craftReagent = craftReagent;
            _activateCard = activateCard;
            _buildHouse = buildHouse;
            _placeWards = placeWards;
            _endSummer = endSummer;
            EnsureControllers();
        }

        void EnsureControllers()
        {
            _layout ??= GetComponent<ViewportLayout>();
            _sheets ??= GetComponent<SummerSheetsController>();
            _craft ??= GetComponent<CraftReagentController>();
            _activate ??= GetComponent<ActivateCardController>();
            _build ??= GetComponent<BuildHouseController>();
            _wards ??= GetComponent<PlaceWardsController>();
            _endSummerCtrl ??= GetComponent<EndSummerController>();
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

        public void ShowCraftBuildSheet() => ShowSheet(craftBuild: true);
        public void ShowConsortSheet() => ShowSheet(craftBuild: false);

        public void ShowCraftReagent() => ShowCraftReagent(null);

        public void ShowCraftReagent(ReagentType? initialReagent)
        {
            if (!ShowModal(_craftReagent, _craft, ActiveOverlay.CraftReagent)) return;
            if (_craft != null)
                _craft.InitialReagent = initialReagent;
            WireCraft();
            RefreshOpenOverlay();
        }

        public void ShowActivate() => ShowActivate(null);

        public void ShowActivate(int? initialSlotIndex)
        {
            if (!ShowModal(_activateCard, _activate, ActiveOverlay.Activate)) return;
            if (_activate != null)
                _activate.InitialSlotIndex = initialSlotIndex;
            WireActivate();
            RefreshOpenOverlay();
        }

        public void ShowBuildHouse()
        {
            if (!ShowModal(_buildHouse, _build, ActiveOverlay.BuildHouse)) return;
            WireBuild();
            RefreshOpenOverlay();
        }

        public void ShowPlaceWards()
        {
            if (!ShowModal(_placeWards, _wards, ActiveOverlay.PlaceWards)) return;
            WireWards();
            RefreshOpenOverlay();
        }

        public void ShowEndSummer()
        {
            EnsureControllers();
            if (!ShowModal(_endSummer, _endSummerCtrl, ActiveOverlay.EndSummer)) return;
            WireEndSummer();
            RefreshOpenOverlay();
        }

        /// <summary>
        /// Detaches the action sheet without clearing the overlay layer so a contest modal can replace it.
        /// </summary>
        internal void ReleaseSheetForContest()
        {
            _sheets?.Detach();
            _active = ActiveOverlay.None;
            NotifyOverlayChanged();
        }

        public void Dismiss()
        {
            _active = ActiveOverlay.None;
            _craft?.ResetForgeState();
            _craft?.Detach();
            _sheets?.Detach();
            _activate?.Detach();
            _build?.Detach();
            _wards?.Detach();
            _endSummerCtrl?.Detach();
            _layout?.DismissOverlay();
            NotifyOverlayChanged();
        }

        void ShowSheet(bool craftBuild)
        {
            if (_layout == null || _summerSheets == null || _sheets == null) return;
            _layout.ShowBottomSheet(_summerSheets, ViewportLayout.SheetVerticalAlign.Bottom);
            var root = _layout.OverlayContentRoot;
            if (root == null) return;

            _sheets.AttachTo(root);
            _sheets.ShowCraftBuild(craftBuild);
            _active = craftBuild ? ActiveOverlay.CraftBuildSheet : ActiveOverlay.ConsortSheet;
            WireSheets();
            RefreshOpenOverlay();
            NotifyOverlayChanged();
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
            NotifyOverlayChanged();
            return true;
        }

        void NotifyOverlayChanged() => OverlayChanged?.Invoke();

        void RefreshOpenOverlay()
        {
            if (_session == null || _bridge == null) return;
            _craft?.BindState(_session, _bridge);
            _activate?.BindState(_session, _bridge);
            _build?.BindState(_session, _bridge);
            _wards?.BindState(_session, _bridge);
            _endSummerCtrl?.BindState(_session, _bridge);
            _sheets?.BindState(_session, _bridge);
        }

        void WireSheets()
        {
            if (_sheets == null) return;
            _sheets.OnCraft = () => { Dismiss(); ShowCraftReagent(); };
            _sheets.OnBuild = () => { Dismiss(); ShowBuildHouse(); };
            _sheets.OnWard = () => { Dismiss(); ShowPlaceWards(); };
            _sheets.OnTrade = () => { ReleaseSheetForContest(); OnTrade?.Invoke(); };
            _sheets.OnDuel = () => { ReleaseSheetForContest(); OnDuel?.Invoke(); };
            _sheets.OnGambit = () => { ReleaseSheetForContest(); OnGambit?.Invoke(); };
            _sheets.OnClose = Dismiss;
        }

        void WireCraft()
        {
            if (_craft == null) return;
            _craft.OnBack = Dismiss;
            _craft.OnDone = Dismiss;
        }

        void WireActivate()
        {
            if (_activate == null) return;
            _activate.OnBack = Dismiss;
            _activate.OnCompleted = Dismiss;
        }

        void WireBuild()
        {
            if (_build == null) return;
            _build.OnBack = Dismiss;
            _build.OnCompleted = Dismiss;
        }

        void WireWards()
        {
            if (_wards == null) return;
            _wards.OnBack = Dismiss;
            _wards.OnCompleted = Dismiss;
        }

        void WireEndSummer()
        {
            if (_endSummerCtrl == null) return;
            _endSummerCtrl.OnKeep = Dismiss;
            _endSummerCtrl.OnConfirm = () =>
            {
                if (_bridge?.TrySubmitPass() == true)
                    Dismiss();
            };
        }
    }
}
