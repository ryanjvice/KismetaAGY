using System;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Views;
using Kismeta.UI;
using Kismeta.UI.Components;
using Kismeta.UI.Narrative;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class SummerPassedController : ScreenController
    {
        public override string ScreenId => ScreenIds.SummerPassed;

        public Action? OnOpenCardTable;
        public Action? OnOpenActiveEffects;
        public Action? OnOpenCrucibleCodex;
        public Action? OnOpenProtectiveWards;
        public Action<string>? OnInspectCard;
        public Action<int>? OnRivalSelected;

        public SeasonIntroRecapHost? IntroRecapHost { get; set; }

        GameSession? _session;
        int _localPlayerId;
        DockZone _dockZone = DockZone.Spread;
        string? _lastActivity;

        protected override void Unwire()
        {
            _dockZone = DockZone.Spread;
            _lastActivity = null;
        }

        protected override void Wire()
        {
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
            NarrativeToolbarBindings.WireIntroRecap(Root, Season.Summer, () => IntroRecapHost);
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
            _localPlayerId = MainSceneBindings.ResolveLocalPlayerId(session, loop, bridge);
            if (Root == null) return;

            var view = GamePublicView.From(session);
            MainSceneBindings.ApplySeasonClass(Root, session.Phase.CurrentSeason);
            HeaderOverlayBindings.RefreshHeader(
                Root, session, loop, _localPlayerId, loop.ActivePlayerId, session.Phase.CurrentSeason,
                id => OnRivalSelected?.Invoke(id));
            RefreshActionGroupRail();
            MainSceneBindings.BindYieldedTurnBanner(
                Root, session, loop,
                "taking their summer turn",
                "Waiting for your turn…",
                "End Turn when you are ready to pass");
            NarrativeSlotBindings.BindById(Root, "summer.passed");
            RefreshDock();

            var local = MainSceneBindings.LocalPlayer(view, _localPlayerId);
            MainSceneBindings.BindCauldrons(Root, local, session);
            RestoreActivityLabel();
        }

        public void NotifyActivity(string message)
        {
            _lastActivity = message;
            if (Lbl("activity-label") != null)
                Lbl("activity-label")!.text = message;
        }

        void RestoreActivityLabel()
        {
            if (Lbl("activity-label") != null && !string.IsNullOrEmpty(_lastActivity))
                Lbl("activity-label")!.text = _lastActivity;
        }

        public void RefreshActionGroupRail()
        {
            MainSceneBindings.BindActionGroupRail(El("step-rail"), 3, null);
        }
    }
}
