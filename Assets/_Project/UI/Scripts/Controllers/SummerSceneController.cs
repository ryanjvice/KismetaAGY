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
    public sealed class SummerSceneController : ScreenController
    {
        public override string ScreenId => ScreenIds.SummerMain;

        public Action? OnTrade;
        public Action? OnDuel;
        public Action? OnGambit;
        public Action? OnBuildHouse;
        public Action? OnPass;

        public Action? OnOpenCardTable;
        public Action? OnOpenActiveEffects;
        public Action? OnOpenCrucibleCodex;
        public Action? OnOpenProtectiveWards;
        public Action<int>? OnOpenPlayerEffects;
        public Action<string>? OnInspectCard;
        public Action<int>? OnRivalSelected;
        public Action? OnOpenBoardInspect;
        public Action? OnDismissBoardInspect;
        public Action? OnOpenForgeInspect;
        public Action? OnDismissForgeInspect;

        public SeasonIntroRecapHost? IntroRecapHost { get; set; }

        CommandBridge? _bridge;
        GameSession? _session;
        int _localPlayerId;
        DockZone _dockZone = DockZone.Spread;
        SummerConsultView _consultView = SummerConsultView.Table;
        SummerOverlayHost? _summerOverlays;
        ContestOverlayHost? _contestOverlays;

        public void ConfigureOverlays(SummerOverlayHost? summer, ContestOverlayHost? contest)
        {
            _summerOverlays = summer;
            _contestOverlays = contest;
        }

        protected override void Unwire()
        {
            _dockZone = DockZone.Spread;
            _consultView = SummerConsultView.Table;
            CentralPanelInspectBindings.Unwire();
            if (Root != null)
                SummerCrucibleRowBindings.Unwire(Root);
        }

        protected override void Wire()
        {
            WireBtn("trade-btn", () => OnTrade?.Invoke());
            WireBtn("duel-btn", () => OnDuel?.Invoke());
            WireBtn("gambit-btn", () => OnGambit?.Invoke());
            WireBtn("build-house-btn", () => OnBuildHouse?.Invoke());
            WireBtn("pass-btn", () => OnPass?.Invoke());
            WireBtn("consult-table-btn", () => SetConsultView(SummerConsultView.Table));
            WireBtn("consult-zodiac-btn", () => SetConsultView(SummerConsultView.Zodiac));
            WireBtn("consult-crucible-btn", () => SetConsultView(SummerConsultView.Crucible));
            CentralPanelInspectBindings.Wire(Root, OnInspectFabClicked);
            NarrativeToolbarBindings.WireIntroRecap(Root, Season.Summer, () => IntroRecapHost);

            InventoryOverlayBindings.Wire(Root, new InventoryOverlayBindings.Callbacks
            {
                OnHandToggle = OnHandToggle,
                OnArcanumToggle = OnArcanumToggle,
                OnOpenCardTable = () => OnOpenCardTable?.Invoke(),
                OnOpenActiveEffects = () => OnOpenActiveEffects?.Invoke(),
                OnOpenCrucibleCodex = () => OnOpenCrucibleCodex?.Invoke(),
                OnOpenProtectiveWards = () => OnOpenProtectiveWards?.Invoke()
            });
            HeaderOverlayBindings.Wire(Root, id => OnRivalSelected?.Invoke(id));
        }

        void WireBtn(string name, Action handler)
        {
            var btn = Btn(name);
            if (btn == null)
            {
                Debug.LogWarning($"[UI] SummerMain missing button '{name}'.");
                return;
            }
            btn.clicked += () => handler();
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
                InventoryOverlayBindings.RefreshInventory(
                    Root, _session, _localPlayerId, _dockZone, OnInspectCard,
                    hint: _bridge?.PendingHint ?? ActionHint.None,
                    onOpenActiveEffects: OnOpenActiveEffects);
        }

        void SetConsultView(SummerConsultView view)
        {
            if (_consultView == view) return;

            DismissInspectForView(_consultView);
            _consultView = view;
            ApplyConsultView();
            RefreshConsultView();
        }

        void ApplyConsultView()
        {
            SummerConsultBindings.ApplyView(Root, _consultView, DismissActiveInspect);
            SummerConsultBindings.BindButtonStates(Root, _consultView);
        }

        void DismissActiveInspect() => DismissInspectForView(_consultView);

        void DismissInspectForView(SummerConsultView view)
        {
            switch (view)
            {
                case SummerConsultView.Zodiac:
                    OnDismissBoardInspect?.Invoke();
                    break;
                case SummerConsultView.Crucible:
                    OnDismissForgeInspect?.Invoke();
                    break;
            }
        }

        void OnInspectFabClicked()
        {
            switch (_consultView)
            {
                case SummerConsultView.Zodiac:
                    OnOpenBoardInspect?.Invoke();
                    break;
                case SummerConsultView.Crucible:
                    OnOpenForgeInspect?.Invoke();
                    break;
            }
        }

        public void BindState(GameSession session, GameLoop loop, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _localPlayerId = MainSceneBindings.ResolveLocalPlayerId(session, loop, bridge);
            if (Root == null) return;

            MainSceneBindings.ApplySeasonClass(Root, session.Phase.CurrentSeason);
            HeaderOverlayBindings.RefreshHeader(
                Root, session, loop, _localPlayerId, loop.ActivePlayerId, session.Phase.CurrentSeason,
                id => OnRivalSelected?.Invoke(id));
            MainSceneBindings.BindPassButton(Root, session, bridge);
            RefreshActionGroupRail();
            RefreshDock();

            bool summerAction = bridge.CanSubmit && bridge.PendingHint == ActionHint.SummerAction;
            Btn("trade-btn")?.SetEnabled(summerAction);
            Btn("duel-btn")?.SetEnabled(summerAction);
            Btn("gambit-btn")?.SetEnabled(summerAction);
            Btn("build-house-btn")?.SetEnabled(summerAction);

            ApplyConsultView();
            RefreshConsultView();

            var stepId = NarrativeStepResolver.ResolveSummerAction(_summerOverlays, _contestOverlays);
            NarrativeSlotBindings.BindById(Root, stepId, mask: NarrativeSlotMask.Beat);

            HeaderOverlayBindings.ApplyHeaderPad(Root);
        }

        void RefreshConsultView()
        {
            if (Root == null || _session == null) return;

            switch (_consultView)
            {
                case SummerConsultView.Table:
                    RefreshRoster();
                    break;
                case SummerConsultView.Zodiac:
                    SpringBoardBindings.BindBoard(El("spring-board"), _session);
                    break;
                case SummerConsultView.Crucible:
                    RefreshCrucibleConsult();
                    break;
            }
        }

        void RefreshCrucibleConsult()
        {
            if (Root == null || _session == null) return;

            var view = GamePublicView.From(_session);
            var local = MainSceneBindings.LocalPlayer(view, _localPlayerId);
            var player = _session.Players[_localPlayerId];

            CrucibleForgeBindings.ApplyForge(
                El("board-stage"),
                null,
                player,
                AutumnActionBindings.StoneStatusLabel(player));
            CrucibleForgeBindings.ApplyAllPlayerStones(
                El("board-stage"), El("stasis-row"), _session, _localPlayerId);
            CrucibleForgeBindings.ApplyCauldronReagents(El("cauldron-mini"), player);
            SummerCrucibleRowBindings.Bind(Root, local, _session, onCardTap: null);
        }

        void RefreshRoster()
        {
            if (Root == null || _session == null) return;
            SummerRosterBindings.Populate(
                Root,
                _session,
                _localPlayerId,
                OnInspectCard,
                id => OnOpenPlayerEffects?.Invoke(id));
        }

        public void RefreshActionGroupRail()
        {
            MainSceneBindings.BindActionGroupRail(
                El("step-rail"), 4,
                ActionGroupRailBindings.ResolveSummerActiveGroup(_summerOverlays, _contestOverlays));
        }
    }
}
