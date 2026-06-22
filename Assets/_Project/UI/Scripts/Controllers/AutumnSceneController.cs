using System;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Views;
using Kismeta.UI;
using Kismeta.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class AutumnSceneController : ScreenController
    {
        public override string ScreenId => ScreenIds.AutumnMain;

        public Action? OnFire;
        public Action? OnTemper;
        public Action? OnManageCards;
        public Action? OnPass;
        public Action? OnLeaveStasis;
        public Action? OnOppose;
        public Action? OnOpenCardTable;
        public Action<string>? OnInspectCard;

        CommandBridge? _bridge;
        GameSession? _session;
        int _localPlayerId;
        bool _inStasis;
        bool _showHand;

        protected override void Unwire()
        {
            _showHand = false;
        }

        protected override void Wire()
        {
            WireBtn("fire-btn", OnFireClicked);
            WireBtn("temper-btn", OnTemperClicked);
            WireBtn("oppose-btn", OnOpposeClicked);
            WireBtn("manage-cards-btn", OnManageCardsClicked);
            WireBtn("pass-btn", OnPassClicked);
            Btn("hand-btn")?.RegisterCallback<ClickEvent>(_ => OnHandToggle());
            Btn("menu-btn")?.RegisterCallback<ClickEvent>(_ => OnOpenCardTable?.Invoke());
        }

        void OnHandToggle()
        {
            _showHand = !_showHand;
            MainSceneBindings.SetHandFabActive(Root, _showHand);
            if (_session != null)
                MainSceneBindings.BindDockStrip(Root, _session, _localPlayerId, _showHand, OnInspectCard);
        }

        void WireBtn(string name, Action handler)
        {
            var btn = Btn(name);
            if (btn == null)
            {
                Debug.LogWarning($"[UI] AutumnMain missing button '{name}'.");
                return;
            }
            btn.clicked += () => handler();
        }

        void OnFireClicked()
        {
            if (!CanAutumnAction()) return;
            if (_inStasis)
                OnLeaveStasis?.Invoke();
            else
                OnFire?.Invoke();
        }

        void OnTemperClicked()
        {
            if (CanAutumnAction()) OnTemper?.Invoke();
        }

        void OnManageCardsClicked()
        {
            if (CanAutumnAction()) OnManageCards?.Invoke();
        }

        void OnPassClicked()
        {
            if (CanAutumnAction()) OnPass?.Invoke();
        }

        void OnOpposeClicked()
        {
            if (CanAutumnAction()) OnOppose?.Invoke();
        }

        bool CanAutumnAction() =>
            _bridge != null && _bridge.CanSubmit && _bridge.PendingHint == ActionHint.AutumnAction;

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

            var player = session.Players[_localPlayerId];
            bool autumnAction = bridge.CanSubmit && bridge.PendingHint == ActionHint.AutumnAction;
            _inStasis = player.StoneState == StoneState.Stasis;

            var fireBtn = Btn("fire-btn");
            if (fireBtn != null)
            {
                if (_inStasis)
                {
                    fireBtn.text = "Leave Stasis";
                    fireBtn.SetEnabled(autumnAction && AutumnActionBindings.CanLeaveStasis(player));
                }
                else
                {
                    fireBtn.text = "Fire";
                    fireBtn.SetEnabled(autumnAction && AutumnActionBindings.CanFire(session, player));
                }
            }

            Btn("temper-btn")?.SetEnabled(
                autumnAction && !_inStasis && AutumnActionBindings.CanTemper(session, player));
            Btn("oppose-btn")?.SetEnabled(
                autumnAction && !_inStasis && AutumnActionBindings.HasOpposeTargets(session, _localPlayerId));
            Btn("manage-cards-btn")?.SetEnabled(autumnAction);
            Btn("pass-btn")?.SetEnabled(autumnAction);

            if (Lbl("stone-label") != null)
                Lbl("stone-label")!.text = AutumnActionBindings.StoneStatusLabel(player);

            if (Lbl("hint-label") != null)
            {
                Lbl("hint-label")!.text = autumnAction
                    ? (_inStasis ? "In Stasis — pay Salt or wait" : "Your turn at the forge")
                    : "Waiting…";
            }

            var local = MainSceneBindings.LocalPlayer(view, _localPlayerId);
            MainSceneBindings.SetHandFabActive(Root, _showHand);
            MainSceneBindings.BindDockStrip(Root, session, _localPlayerId, _showHand, OnInspectCard);
            RivalStripBuilder.Populate(El("rivals"), session, view, _localPlayerId, loop.ActivePlayerId, session.Phase.CurrentSeason);
        }
    }
}
