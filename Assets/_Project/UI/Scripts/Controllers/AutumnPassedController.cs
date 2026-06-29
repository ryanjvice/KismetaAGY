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
    public sealed class AutumnPassedController : ScreenController
    {
        public override string ScreenId => ScreenIds.AutumnPassed;

        public Action? OnOpenCardTable;
        public Action? OnOpenActiveEffects;
        public Action<string>? OnInspectCard;
        public Action<int>? OnRivalSelected;
        public Action? OnOpenForgeInspect;
        public Action? OnDismissForgeInspect;

        GameSession? _session;
        int _localPlayerId;
        DockZone _dockZone = DockZone.Spread;
        string? _lastActivity;

        protected override void Unwire()
        {
            _dockZone = DockZone.Spread;
            _lastActivity = null;
            CentralPanelInspectBindings.Unwire();
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
            CentralPanelInspectBindings.Wire(Root, () => OnOpenForgeInspect?.Invoke());
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

            var player = session.Players[_localPlayerId];

            MainSceneBindings.ApplySeasonClass(Root, session.Phase.CurrentSeason);
            HeaderOverlayBindings.RefreshHeader(
                Root, session, loop, _localPlayerId, loop.ActivePlayerId, session.Phase.CurrentSeason,
                id => OnRivalSelected?.Invoke(id));
            RefreshActionGroupRail();
            MainSceneBindings.BindYieldedTurnBanner(
                Root, session, loop,
                "at the forge",
                "You passed — waiting at the forge",
                "Autumn ends when everyone passes in a row");
            NarrativeSlotBindings.BindById(Root, "autumn.passed");
            RefreshDock();

            CrucibleForgeBindings.ApplyForge(
                El("board-stage"),
                Lbl("stone-label"),
                player,
                AutumnActionBindings.StoneStatusLabel(player));
            CrucibleForgeBindings.ApplyAllPlayerStones(
                El("board-stage"), El("stasis-row"), session, _localPlayerId);
            CrucibleForgeBindings.ApplyCauldronReagents(El("cauldron-mini"), player);
            RestoreActivityLabel();

            CentralPanelInspectBindings.SetFabVisible(Root, true, () => OnDismissForgeInspect?.Invoke());
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
