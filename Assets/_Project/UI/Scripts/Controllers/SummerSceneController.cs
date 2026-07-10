using System;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
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

        public SeasonIntroRecapHost? IntroRecapHost { get; set; }

        CommandBridge? _bridge;
        GameSession? _session;
        int _localPlayerId;
        DockZone _dockZone = DockZone.Spread;
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
        }

        protected override void Wire()
        {
            WireBtn("trade-btn", () => OnTrade?.Invoke());
            WireBtn("duel-btn", () => OnDuel?.Invoke());
            WireBtn("gambit-btn", () => OnGambit?.Invoke());
            WireBtn("build-house-btn", () => OnBuildHouse?.Invoke());
            WireBtn("pass-btn", () => OnPass?.Invoke());
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
                InventoryOverlayBindings.RefreshInventory(Root, _session, _localPlayerId, _dockZone, OnInspectCard);
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

            RefreshRoster();

            var stepId = NarrativeStepResolver.ResolveSummerAction(_summerOverlays, _contestOverlays);
            NarrativeSlotBindings.BindById(Root, stepId, mask: NarrativeSlotMask.Beat);

            HeaderOverlayBindings.ApplyHeaderPad(Root);
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
