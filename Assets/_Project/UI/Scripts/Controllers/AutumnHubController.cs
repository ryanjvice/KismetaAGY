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
        public Action<string>? OnInspectCard;

        GameSession? _session;
        int _localPlayerId;
        bool _showHand;

        protected override void Unwire()
        {
            _showHand = false;
        }

        protected override void Wire()
        {
            Btn("hand-btn")!.clicked += OnHandToggle;
            Btn("menu-btn")!.clicked += () => OnOpenCardTable?.Invoke();
        }

        void OnHandToggle()
        {
            _showHand = !_showHand;
            MainSceneBindings.SetHandFabActive(Root, _showHand);
            if (_session != null)
                MainSceneBindings.BindDockStrip(Root, _session, _localPlayerId, _showHand, OnInspectCard);
        }

        public void BindState(GameSession session, GameLoop loop, CommandBridge bridge)
        {
            _session = session;
            _localPlayerId = MainSceneBindings.ResolveLocalPlayerId(session, loop, bridge);
            if (Root == null) return;

            var view = GamePublicView.From(session);
            var player = session.Players[_localPlayerId];

            MainSceneBindings.ApplySeasonClass(Root, session.Phase.CurrentSeason);
            MainSceneBindings.BindStatusBar(Root, session, loop);
            MainSceneBindings.BindCosmicAgeBanner(Root, session);
            RefreshActionGroupRail();
            MainSceneBindings.BindSpectatorTurnBanner(Root, session, loop, "at the forge");
            MainSceneBindings.SetHandFabActive(Root, _showHand);
            MainSceneBindings.BindDockStrip(Root, session, _localPlayerId, _showHand, OnInspectCard);

            if (Lbl("stone-label") != null)
                Lbl("stone-label")!.text = AutumnActionBindings.StoneStatusLabel(player);

            RivalStripBuilder.Populate(
                El("rivals"), session, view, _localPlayerId, loop.ActivePlayerId, Season.Autumn);
        }

        public void RefreshActionGroupRail()
        {
            MainSceneBindings.BindActionGroupRail(El("step-rail"), 3, null);
        }
    }
}
