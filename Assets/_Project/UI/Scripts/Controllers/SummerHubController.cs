using System;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Rules;
using Kismeta.Core.Views;
using Kismeta.UI;
using Kismeta.UI.Components;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class SummerHubController : ScreenController
    {
        public override string ScreenId => ScreenIds.SummerHub;

        public Action? OnOpenCardTable;
        public Action<string>? OnInspectCard;

        GameSession? _session;
        int _localPlayerId;
        DockZone _dockZone = DockZone.Spread;

        protected override void Unwire()
        {
            _dockZone = DockZone.Spread;
        }

        protected override void Wire()
        {
            Btn("hand-btn")!.clicked += OnHandToggle;
            Btn("arcanum-btn")!.clicked += OnArcanumToggle;
            Btn("menu-btn")!.clicked += () => OnOpenCardTable?.Invoke();
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
            MainSceneBindings.SetDockZoneFabActive(Root, _dockZone);
            if (_session != null)
                MainSceneBindings.BindDockStrip(Root, _session, _localPlayerId, _dockZone, OnInspectCard);
        }

        public void BindState(GameSession session, GameLoop loop, CommandBridge bridge)
        {
            _session = session;
            _localPlayerId = MainSceneBindings.ResolveLocalPlayerId(session, loop, bridge);
            if (Root == null) return;

            var view = GamePublicView.From(session);
            MainSceneBindings.ApplySeasonClass(Root, session.Phase.CurrentSeason);
            MainSceneBindings.BindStatusBar(Root, session, loop);
            MainSceneBindings.BindCosmicAgeBanner(Root, session);
            RefreshActionGroupRail();
            MainSceneBindings.BindSpectatorTurnBanner(Root, session, loop, "working in the workshop");
            MainSceneBindings.BindPlayerStrip(Root, session, _localPlayerId);
            RefreshDock();

            var local = MainSceneBindings.LocalPlayer(view, _localPlayerId);
            MainSceneBindings.BindCauldrons(Root, local);

            RivalStripBuilder.Populate(
                El("rivals"), session, view, _localPlayerId, loop.ActivePlayerId, Season.Summer);
        }

        public void RefreshActionGroupRail()
        {
            MainSceneBindings.BindActionGroupRail(El("step-rail"), 3, null);
        }
    }
}
