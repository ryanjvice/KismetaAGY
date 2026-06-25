using System;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Rules;
using Kismeta.Core.Views;
using Kismeta.UI;
using Kismeta.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class SummerSceneController : ScreenController
    {
        public override string ScreenId => ScreenIds.SummerMain;

        public Action OnCraftBuild;
        public Action OnConsort;
        public Action OnActivate;
        public Action OnPass;
        public Action OnOpenCardTable;
        public Action<string> OnInspectCard;
        public Action<Suit>? OnCauldronClicked;

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
            CauldronHubBindings.UnwireCauldrons(Root);
        }

        protected override void Wire()
        {
            Btn("craftbuild-btn")!.clicked += () => OnCraftBuild?.Invoke();
            Btn("consort-btn")!.clicked += () => OnConsort?.Invoke();
            Btn("activate-btn")!.clicked += () => OnActivate?.Invoke();
            Btn("pass-btn")!.clicked += () => OnPass?.Invoke();
            Btn("hand-btn")!.clicked += OnHandToggle;
            Btn("arcanum-btn")!.clicked += OnArcanumToggle;
            Btn("menu-btn")!.clicked += () => OnOpenCardTable?.Invoke();
            CauldronHubBindings.WireCauldrons(
                Root,
                suit => OnCauldronClicked?.Invoke(suit),
                onCodexTap: () => OnActivate?.Invoke());
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
            _bridge = bridge;
            _localPlayerId = MainSceneBindings.ResolveLocalPlayerId(session, loop, bridge);
            if (Root == null) return;

            var view = GamePublicView.From(session);
            MainSceneBindings.ApplySeasonClass(Root, session.Phase.CurrentSeason);
            MainSceneBindings.BindStatusBar(Root, session, loop);
            MainSceneBindings.BindCosmicAgeBanner(Root, session);
            MainSceneBindings.BindPassButton(Root, session, bridge);
            RefreshActionGroupRail();
            MainSceneBindings.BindPlayerStrip(Root, session, _localPlayerId);
            RefreshDock();

            var local = MainSceneBindings.LocalPlayer(view, _localPlayerId);
            MainSceneBindings.BindCauldrons(Root, local);

            RivalStripBuilder.Populate(El("rivals"), session, view, _localPlayerId, loop.ActivePlayerId, session.Phase.CurrentSeason);
        }

        public void RefreshActionGroupRail()
        {
            MainSceneBindings.BindActionGroupRail(
                El("step-rail"), 3,
                ActionGroupRailBindings.ResolveSummerActiveGroup(_summerOverlays, _contestOverlays));
        }
    }
}
