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

        OverlayController? _active;

        public bool IsOpen => _layout != null && _layout.IsOverlayVisible;

        public void Configure(
            VisualTreeAsset? summerSheets,
            VisualTreeAsset? craftReagent,
            VisualTreeAsset? activateCard,
            VisualTreeAsset? buildHouse,
            VisualTreeAsset? placeWards,
            VisualTreeAsset? endSummer)
        {
            _summerSheets = summerSheets;
            _craftReagent = craftReagent;
            _activateCard = activateCard;
            _buildHouse = buildHouse;
            _placeWards = placeWards;
            _endSummer = endSummer;
        }

        public void Bind(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            if (_active != null && _session != null)
                RefreshActive();
        }

        public void Dismiss()
        {
            DetachActive();
            _layout?.DismissOverlay();
        }

        public void DismissIfOpen()
        {
            if (IsOpen)
                Dismiss();
        }

        public void ShowCraftBuildSheet()
        {
            ShowSheet(true);
        }

        public void ShowConsortSheet()
        {
            ShowSheet(false);
        }

        public void ShowCraftReagent() => ShowModal(_craftReagent, _craft);
        public void ShowActivate() => ShowModal(_activateCard, _activate);
        public void ShowBuildHouse() => ShowModal(_buildHouse, _build);
        public void ShowPlaceWards() => ShowModal(_placeWards, _wards);
        public void ShowEndSummer() => ShowModal(_endSummer, _endSummerCtrl);

        public void ShowLightTip()
        {
            Dismiss();
            Debug.Log("[UI] Light cauldron — activating a crucible card lights its matching cauldron.");
        }

        void Awake()
        {
            _layout = GetComponent<ViewportLayout>();
            EnsureControllers();
        }

        void EnsureControllers()
        {
            _sheets = Ensure<SummerSheetsController>();
            _craft = Ensure<CraftReagentController>();
            _activate = Ensure<ActivateCardController>();
            _build = Ensure<BuildHouseController>();
            _wards = Ensure<PlaceWardsController>();
            _endSummerCtrl = Ensure<EndSummerController>();
            WireNavigation();
        }

        T Ensure<T>() where T : Component
        {
            return GetComponent<T>() ?? gameObject.AddComponent<T>();
        }

        void WireNavigation()
        {
            if (_sheets != null)
            {
                _sheets.OnCraft = () => { Dismiss(); ShowCraftReagent(); };
                _sheets.OnBuild = () => { Dismiss(); ShowBuildHouse(); };
                _sheets.OnWard = () => { Dismiss(); ShowPlaceWards(); };
                _sheets.OnLight = ShowLightTip;
                _sheets.OnClose = Dismiss;
            }

            if (_craft != null)
            {
                _craft.OnBack = Dismiss;
                _craft.OnDone = Dismiss;
            }

            if (_activate != null)
            {
                _activate.OnBack = Dismiss;
                _activate.OnCompleted = Dismiss;
            }

            if (_build != null)
            {
                _build.OnBack = Dismiss;
                _build.OnCompleted = Dismiss;
            }

            if (_wards != null)
            {
                _wards.OnBack = Dismiss;
                _wards.OnCompleted = Dismiss;
            }

            if (_endSummerCtrl != null)
            {
                _endSummerCtrl.OnKeep = Dismiss;
                _endSummerCtrl.OnEnd = () =>
                {
                    _endSummerCtrl.SubmitEnd();
                    Dismiss();
                };
            }
        }

        void ShowSheet(bool craftBuild)
        {
            if (_layout == null || _summerSheets == null || _session == null || _bridge == null) return;
            DetachActive();
            _layout.ShowBottomSheet(_summerSheets);
            var root = _layout.OverlayContentRoot;
            if (root == null) return;
            _sheets!.AttachTo(root);
            _sheets.ShowCraftBuild(craftBuild);
            _active = _sheets;
            RefreshActive();
        }

        void ShowModal<T>(VisualTreeAsset? asset, T? controller) where T : OverlayController
        {
            if (_layout == null || asset == null || controller == null || _session == null || _bridge == null)
                return;
            DetachActive();
            _layout.ShowModal(asset);
            var root = _layout.OverlayContentRoot;
            if (root == null) return;
            controller.AttachTo(root);
            _active = controller;
            RefreshActive();
        }

        void RefreshActive()
        {
            if (_session == null || _bridge == null || _active == null) return;

            switch (_active)
            {
                case CraftReagentController craft:
                    craft.BindState(_session, _bridge);
                    break;
                case ActivateCardController activate:
                    activate.BindState(_session, _bridge);
                    break;
                case BuildHouseController build:
                    build.BindState(_session, _bridge);
                    break;
                case PlaceWardsController wards:
                    wards.BindState(_session, _bridge);
                    break;
                case EndSummerController end:
                    end.BindState(_session, _bridge);
                    break;
            }
        }

        void DetachActive()
        {
            _active?.Detach();
            _active = null;
        }
    }
}
