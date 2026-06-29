using System;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.UI;
using Kismeta.UI.Components;
using Kismeta.UI.Narrative;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class SpringPassedController : ScreenController
    {
        public override string ScreenId => ScreenIds.SpringPassed;

        public Action? OnOpenCardTable;
        public Action? OnOpenActiveEffects;
        public Action<string>? OnInspectCard;
        public Action<int>? OnRivalSelected;

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
                OnOpenActiveEffects = () => OnOpenActiveEffects?.Invoke()
            });
            HeaderOverlayBindings.Wire(Root, id => OnRivalSelected?.Invoke(id));
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

            MainSceneBindings.ApplySeasonClass(Root, session.Phase.CurrentSeason);
            HeaderOverlayBindings.RefreshHeader(
                Root, session, loop, _localPlayerId, loop.ActivePlayerId, session.Phase.CurrentSeason,
                id => OnRivalSelected?.Invoke(id));
            MainSceneBindings.BindStepRail(El("step-rail"), 4, 5, "step__dot--active");
            MainSceneBindings.BindYieldedTurnBanner(
                Root, session, loop,
                "taking their spring turn",
                "Waiting for your turn…",
                "End Turn when you are ready to pass");
            NarrativeSlotBindings.BindById(
                Root,
                "spring.passed",
                mask: NarrativeSlotMask.Beat | NarrativeSlotMask.Stakes);
            NarrativeSlotBindings.BindById(
                El("spring-board-panel") ?? Root,
                "spring.passed",
                mask: NarrativeSlotMask.Charge);
            SpringBoardBindings.BindBoard(El("spring-board"), session);
            RefreshDock();
            RestoreActivityLabel();
            HeaderOverlayBindings.ApplyHeaderPad(Root);
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
    }
}
