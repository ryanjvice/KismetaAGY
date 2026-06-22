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

        CommandBridge? _bridge;
        GameSession? _session;
        int _localPlayerId;
        bool _showHand;

        protected override void Unwire()
        {
            _showHand = false;
        }

        protected override void Wire()
        {
            Btn("craftbuild-btn")!.clicked += () => OnCraftBuild?.Invoke();
            Btn("consort-btn")!.clicked += () => OnConsort?.Invoke();
            Btn("activate-btn")!.clicked += () => OnActivate?.Invoke();
            Btn("pass-btn")!.clicked += () => OnPass?.Invoke();
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
            _bridge = bridge;
            _localPlayerId = bridge.ActivePlayerId;
            if (Root == null) return;

            var view = GamePublicView.From(session);
            MainSceneBindings.ApplySeasonClass(Root, session.Phase.CurrentSeason);
            MainSceneBindings.BindStatusBar(Root, session, loop);
            MainSceneBindings.BindCosmicAgeBanner(Root, session);
            MainSceneBindings.BindPassButton(Root, session, bridge);
            MainSceneBindings.SetHandFabActive(Root, _showHand);
            MainSceneBindings.BindDockStrip(Root, session, _localPlayerId, _showHand, OnInspectCard);

            var local = MainSceneBindings.LocalPlayer(view, _localPlayerId);
            BindCauldrons(local);

            if (Lbl("codex-label") != null && local != null)
            {
                int active = 0;
                foreach (var slot in local.CrucibleSlots)
                    if (slot.State >= CrucibleCardState.Active) active++;
                Lbl("codex-label")!.text = $"codex · {active} active";
            }

            RivalStripBuilder.Populate(El("rivals"), view, _localPlayerId);
        }

        void BindCauldrons(PublicPlayerView? local)
        {
            var ids = new[] { "cauldron-n", "cauldron-e", "cauldron-s", "cauldron-w" };
            for (int i = 0; i < ids.Length; i++)
            {
                var el = El(ids[i]);
                if (el == null) continue;

                bool lit = local != null && i < local.CrucibleSlots.Count &&
                    local.CrucibleSlots[i].State >= CrucibleCardState.Active;
                el.EnableInClassList("cauldron--lit", lit);
                el.EnableInClassList("cauldron--dormant", !lit);
            }
        }
    }
}
