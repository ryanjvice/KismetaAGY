using System;
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

        public Action OnTrade;
        public Action OnDuel;
        public Action OnGambit;

        public bool IsOpen => _layout != null && _layout.IsOverlayVisible;

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

        public void ShowCraftReagent()
        {
            if (!ShowModal(_craftReagent, _craft)) return;
            WireCraft();
            RefreshOpenOverlay();
        }

        public void ShowActivate()
        {
            if (!ShowModal(_activateCard, _activate)) return;
            WireActivate();
            RefreshOpenOverlay();
        }

        public void ShowBuildHouse()
        {
            if (!ShowModal(_buildHouse, _build)) return;
            WireBuild();
            RefreshOpenOverlay();
        }

        public void ShowPlaceWards()
        {
            if (!ShowModal(_placeWards, _wards)) return;
            WireWards();
            RefreshOpenOverlay();
        }

        public void ShowEndSummer()
        {
            EnsureControllers();
            if (!ShowModal(_endSummer, _endSummerCtrl)) return;
            WireEndSummer();
            RefreshOpenOverlay();
        }

        public void ShowLightTip()
        {
            Dismiss();
            if (_layout == null) return;

            var tip = new VisualElement();
            tip.AddToClassList("screen");
            tip.AddToClassList("screen--summer");
            tip.style.paddingLeft = 14;
            tip.style.paddingRight = 14;
            tip.style.paddingTop = 16;
            tip.style.paddingBottom = 16;

            tip.Add(new Label("Light a cauldron")
            {
                style = { fontSize = 14, unityFontStyleAndWeight = FontStyle.Bold }
            });
            tip.Add(new Label(
                "Activating a crucible card moves its coal to the matching cauldron — lighting it. " +
                "A lit cauldron lets you craft that element's reagent.")
            {
                style = { fontSize = 11, whiteSpace = WhiteSpace.Normal, marginTop = 8 }
            });

            var close = new Button { text = "Got it" };
            close.AddToClassList("btn");
            close.AddToClassList("btn--primary");
            close.style.marginTop = 12;
            close.clicked += Dismiss;
            tip.Add(close);

            ShowProgrammaticOverlay(tip);
        }

        void ShowProgrammaticOverlay(VisualElement content)
        {
            _layout?.ShowOverlayElement(content);
        }

        public void Dismiss()
        {
            _craft?.ResetForgeState();
            _craft?.Detach();
            _sheets?.Detach();
            _activate?.Detach();
            _build?.Detach();
            _wards?.Detach();
            _endSummerCtrl?.Detach();
            _layout?.DismissOverlay();
        }

        void ShowSheet(bool craftBuild)
        {
            if (_layout == null || _summerSheets == null || _sheets == null) return;
            _layout.ShowBottomSheet(_summerSheets);
            var root = _layout.OverlayContentRoot;
            if (root == null) return;

            _sheets.AttachTo(root);
            _sheets.ShowCraftBuild(craftBuild);
            WireSheets();
            RefreshOpenOverlay();
        }

        bool ShowModal<T>(VisualTreeAsset? asset, T? controller) where T : OverlayController
        {
            if (_layout == null || asset == null || controller == null) return false;
            _layout.ShowModal(asset);
            var root = _layout.OverlayContentRoot;
            if (root == null) return false;
            controller.AttachTo(root);
            return true;
        }

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
            _sheets.OnLight = () => { Dismiss(); ShowLightTip(); };
            _sheets.OnTrade = () => OnTrade?.Invoke();
            _sheets.OnDuel = () => OnDuel?.Invoke();
            _sheets.OnGambit = () => OnGambit?.Invoke();
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
