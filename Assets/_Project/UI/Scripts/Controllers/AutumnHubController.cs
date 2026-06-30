using System;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Views;
using Kismeta.UI;
using Kismeta.UI.Components;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class AutumnHubController : ScreenController
    {
        public override string ScreenId => ScreenIds.AutumnHub;

        public Action? OnOpenCardTable;
        public Action? OnOpenActiveEffects;
        public Action? OnOpenCrucibleCodex;
        public Action? OnOpenProtectiveWards;
        public Action<string>? OnInspectCard;
        public Action<int>? OnRivalSelected;
        public Action? OnOpenForgeInspect;
        public Action? OnDismissForgeInspect;

        public SeasonIntroRecapHost? IntroRecapHost { get; set; }

        GameSession? _session;
        int _localPlayerId;
        DockZone _dockZone = DockZone.Spread;

        protected override void Unwire()
        {
            _dockZone = DockZone.Spread;
            CentralPanelInspectBindings.Unwire();
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
            CentralPanelInspectBindings.Wire(Root, () => OnOpenForgeInspect?.Invoke());
            NarrativeToolbarBindings.WireIntroRecap(Root, Season.Autumn, () => IntroRecapHost);
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
            var player = session.Players[_localPlayerId];

            MainSceneBindings.ApplySeasonClass(Root, session.Phase.CurrentSeason);
            HeaderOverlayBindings.RefreshHeader(
                Root, session, loop, _localPlayerId, loop.ActivePlayerId, session.Phase.CurrentSeason,
                id => OnRivalSelected?.Invoke(id));
            RefreshActionGroupRail();
            MainSceneBindings.BindSpectatorTurnBanner(Root, session, loop, "at the forge");
            RefreshDock();

            CrucibleForgeBindings.ApplyForge(
                El("board-stage"),
                Lbl("stone-label"),
                player,
                AutumnActionBindings.StoneStatusLabel(player));
            CrucibleForgeBindings.ApplyAllPlayerStones(
                El("board-stage"), El("stasis-row"), session, _localPlayerId);
            CrucibleForgeBindings.ApplyCauldronReagents(El("cauldron-mini"), player);

            CentralPanelInspectBindings.SetFabVisible(Root, true, () => OnDismissForgeInspect?.Invoke());

            InventoryOverlayBindings.SetVisible(Root, true);
            InventoryOverlayBindings.OverlayRoot(Root)?.BringToFront();
            HeaderOverlayBindings.ApplyHeaderPad(Root);
        }

        public void RefreshActionGroupRail()
        {
            MainSceneBindings.BindActionGroupRail(El("step-rail"), 3, null);
        }
    }
}
