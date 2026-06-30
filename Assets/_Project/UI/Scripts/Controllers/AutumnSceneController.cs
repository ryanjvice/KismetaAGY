using System;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Views;
using Kismeta.UI;
using Kismeta.UI.Components;
using Kismeta.UI.Narrative;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class AutumnSceneController : ScreenController
    {
        public override string ScreenId => ScreenIds.AutumnMain;

        enum CentralView { Forge, Cauldron }

        // Top action buttons
        public Action? OnCraft;
        public Action? OnActivate;
        public Action<int>? OnActivateSlot;
        public Action<Suit>? OnCauldronClicked;
        public Action<int>? OnCrucibleDetail;

        // Contextual forge actions
        public Action? OnFire;
        public Action? OnTemper;

        // Footer
        public Action? OnLeaveStasis;
        public Action? OnPass;

        // Inventory / table
        public Action? OnOpenCardTable;
        public Action? OnOpenActiveEffects;
        public Action? OnOpenCrucibleCodex;
        public Action<string>? OnInspectCard;
        public Action<int>? OnRivalSelected;
        public Action? OnOpenForgeInspect;
        public Action? OnDismissForgeInspect;

        CommandBridge? _bridge;
        GameSession? _session;
        int _localPlayerId;
        bool _inStasis;
        DockZone _dockZone = DockZone.Spread;
        CentralView _centralView = CentralView.Forge;
        AutumnOverlayHost? _autumnOverlays;
        ContestOverlayHost? _contestOverlays;

        public void ConfigureOverlays(AutumnOverlayHost? autumn, ContestOverlayHost? contest)
        {
            _autumnOverlays = autumn;
            _contestOverlays = contest;
        }

        protected override void Unwire()
        {
            _dockZone = DockZone.Spread;
            CauldronHubBindings.UnwireCauldrons(Root);
            SummerCrucibleRowBindings.Unwire(Root);
            CentralPanelInspectBindings.Unwire();
        }

        protected override void Wire()
        {
            WireBtn("craft-btn", OnCraftClicked);
            WireBtn("activate-btn", OnActivateClicked);
            WireBtn("forge-btn", OnForgeClicked);
            WireBtn("stasis-btn", OnStasisClicked);
            WireBtn("pass-btn", OnPassClicked);
            WireBtn("info-btn", () => SetInfoPopup(true));
            WireBtn("info-close-btn", () => SetInfoPopup(false));

            var scrim = El("info-scrim");
            if (scrim != null)
                scrim.RegisterCallback<ClickEvent>(_ => SetInfoPopup(false));

            InventoryOverlayBindings.Wire(Root, new InventoryOverlayBindings.Callbacks
            {
                OnHandToggle = OnHandToggle,
                OnArcanumToggle = OnArcanumToggle,
                OnOpenCardTable = () => OnOpenCardTable?.Invoke(),
                OnOpenActiveEffects = () => OnOpenActiveEffects?.Invoke(),
                OnOpenCrucibleCodex = () => OnOpenCrucibleCodex?.Invoke()
            });
            HeaderOverlayBindings.Wire(Root, id => OnRivalSelected?.Invoke(id));
            CentralPanelInspectBindings.Wire(Root, () => OnOpenForgeInspect?.Invoke());
            CauldronHubBindings.WireCauldrons(
                Root,
                suit => OnCauldronClicked?.Invoke(suit),
                onCodexTap: OnActivateClicked);

            ApplyCentralView();
        }

        void OnHandToggle()
        {
            _dockZone = _dockZone == DockZone.Hand ? DockZone.Spread : DockZone.Hand;
            RefreshDock();
        }

        void OnArcanumToggle()
        {
            _dockZone = _dockZone == DockZone.Arcanum ? DockZone.Spread : DockZone.Arcanum;
            RefreshDock();
        }

        void RefreshDock()
        {
            if (_session != null)
                InventoryOverlayBindings.RefreshInventory(Root, _session, _localPlayerId, _dockZone, OnInspectCard);
        }

        void WireBtn(string name, Action handler)
        {
            var btn = Btn(name);
            if (btn == null)
            {
                Debug.LogWarning($"[UI] AutumnMain missing button '{name}'.");
                return;
            }
            btn.clicked += () => handler();
        }

        void OnCraftClicked()
        {
            _centralView = CentralView.Cauldron;
            ApplyCentralView();
            if (CanAutumnAction()) OnCraft?.Invoke();
        }

        void OnActivateClicked()
        {
            _centralView = CentralView.Cauldron;
            ApplyCentralView();
            if (CanAutumnAction()) OnActivate?.Invoke();
        }

        void OnForgeClicked()
        {
            _centralView = CentralView.Forge;
            ApplyCentralView();
        }

        void OnStasisClicked()
        {
            if (CanAutumnAction() && _inStasis) OnLeaveStasis?.Invoke();
        }

        void OnPassClicked()
        {
            if (CanAutumnAction()) OnPass?.Invoke();
        }

        void OnCrucibleCardTapped(int slotIndex)
        {
            if (_session == null || _localPlayerId < 0) return;
            var slots = _session.Players[_localPlayerId].CrucibleSlots;
            if (slotIndex < 0 || slotIndex >= slots.Count) return;

            switch (slots[slotIndex].State)
            {
                case CrucibleCardState.Dormant:
                    if (CanAutumnAction()) OnActivateSlot?.Invoke(slotIndex);
                    break;
                case CrucibleCardState.Fired:
                    if (CanAutumnAction()) OnTemper?.Invoke();
                    break;
                case CrucibleCardState.Active:
                    OnCrucibleDetail?.Invoke(slotIndex);
                    break;
            }
        }

        void SetInfoPopup(bool visible)
        {
            var popup = El("autumn-info-popup");
            if (popup != null)
                popup.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void ApplyCentralView()
        {
            var cauldron = El("autumn-cauldron-stage");
            if (cauldron != null)
                cauldron.style.display = _centralView == CentralView.Cauldron ? DisplayStyle.Flex : DisplayStyle.None;

            var forge = El("autumn-forge-stage");
            if (forge != null)
                forge.style.display = _centralView == CentralView.Forge ? DisplayStyle.Flex : DisplayStyle.None;

            CentralPanelInspectBindings.SetFabVisible(
                Root,
                _centralView == CentralView.Forge,
                () => OnDismissForgeInspect?.Invoke());
        }

        bool CanAutumnAction() =>
            _bridge != null && _bridge.CanSubmit && _bridge.PendingHint == ActionHint.AutumnAction;

        public void BindState(GameSession session, GameLoop loop, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _localPlayerId = MainSceneBindings.ResolveLocalPlayerId(session, loop, bridge);
            if (Root == null) return;

            var view = GamePublicView.From(session);
            MainSceneBindings.ApplySeasonClass(Root, session.Phase.CurrentSeason);
            HeaderOverlayBindings.RefreshHeader(
                Root, session, loop, _localPlayerId, loop.ActivePlayerId, session.Phase.CurrentSeason,
                id => OnRivalSelected?.Invoke(id));
            MainSceneBindings.BindPassButton(Root, session, bridge);
            RefreshActionGroupRail();

            var player = session.Players[_localPlayerId];
            bool autumnAction = bridge.CanSubmit && bridge.PendingHint == ActionHint.AutumnAction;
            _inStasis = player.StoneState == StoneState.Stasis;

            Btn("craft-btn")?.SetEnabled(autumnAction && !_inStasis);
            Btn("activate-btn")?.SetEnabled(autumnAction && !_inStasis);
            Btn("forge-btn")?.SetEnabled(true);
            Btn("pass-btn")?.SetEnabled(autumnAction);

            var stasisBtn = Btn("stasis-btn");
            if (stasisBtn != null)
            {
                stasisBtn.text = _inStasis ? "Leave Stasis" : "Stasis";
                stasisBtn.SetEnabled(autumnAction && _inStasis && AutumnActionBindings.CanLeaveStasis(player));
            }

            ApplyCentralView();

            // Cauldron (Activate) view bindings
            var local = MainSceneBindings.LocalPlayer(view, _localPlayerId);
            MainSceneBindings.BindCauldrons(Root, local, session);

            // Persistent crucible row — tap is context-aware (Dormant→Activate, Fired→Temper, Active→Detail)
            SummerCrucibleRowBindings.Bind(Root, local, session, OnCrucibleCardTapped);

            // Forge view bindings
            CrucibleForgeBindings.ApplyForge(
                El("board-stage"),
                Lbl("stone-label"),
                player,
                AutumnActionBindings.StoneStatusLabel(player));
            CrucibleForgeBindings.ApplyAllPlayerStones(
                El("board-stage"), El("stasis-row"), session, _localPlayerId);
            CrucibleForgeBindings.ApplyCauldronReagents(El("cauldron-mini"), player);

            BindLocalStoneTap(session, player, autumnAction);

            var stepId = NarrativeStepResolver.ResolveAutumnAction(
                player.StoneState, _autumnOverlays, _contestOverlays);
            NarrativeSlotBindings.BindById(Root, stepId, mask: NarrativeSlotMask.Beat);

            var infoPopup = El("autumn-info-popup");
            if (infoPopup != null)
            {
                NarrativeSlotBindings.BindById(
                    infoPopup,
                    stepId,
                    mask: NarrativeSlotMask.Stakes | NarrativeSlotMask.Charge);
            }

            RefreshDock();

            InventoryOverlayBindings.SetVisible(Root, true);
            InventoryOverlayBindings.OverlayRoot(Root)?.BringToFront();
            HeaderOverlayBindings.ApplyHeaderPad(Root);
        }

        // Tap your own Stone in the forge to Fire (advance into the Forge).
        void BindLocalStoneTap(GameSession session, PlayerState player, bool autumnAction)
        {
            var stone = Root?.Q<VisualElement>(className: "player-stone--local");
            if (stone == null) return;

            bool canFire = autumnAction && !_inStasis && AutumnActionBindings.CanFire(session, player);
            stone.EnableInClassList("player-stone--tappable", canFire);
            if (canFire)
                stone.RegisterCallback<ClickEvent>(_ => { if (CanAutumnAction()) OnFire?.Invoke(); });
        }

        public void RefreshActionGroupRail()
        {
            MainSceneBindings.BindActionGroupRail(
                El("step-rail"), 3,
                ActionGroupRailBindings.ResolveAutumnActiveGroup(_autumnOverlays, _contestOverlays));
        }
    }
}
