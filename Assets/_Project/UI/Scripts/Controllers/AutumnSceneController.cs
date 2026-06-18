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

        CommandBridge? _bridge;
        GameSession? _session;
        int _localPlayerId;
        bool _inStasis;

        protected override void Wire()
        {
            Btn("fire-btn")!.clicked += OnFireClicked;
            Btn("temper-btn")!.clicked += OnTemperClicked;
            Btn("oppose-btn")!.clicked += OnOpposeClicked;
            Btn("manage-cards-btn")!.clicked += OnManageCardsClicked;
            Btn("menu-btn")!.clicked += () => Debug.Log("[UI] Card table — Batch 7");
            Btn("pass-btn")!.clicked += OnPassClicked;
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
            _localPlayerId = bridge.ActivePlayerId;
            if (Root == null) return;

            var view = GamePublicView.From(session);
            MainSceneBindings.ApplySeasonClass(Root, session.Phase.CurrentSeason);
            MainSceneBindings.BindStatusBar(Root, session, loop);
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
            BindSpread(session, local);
            RivalStripBuilder.Populate(El("rivals"), view, _localPlayerId);
        }

        void BindSpread(GameSession session, PublicPlayerView? local)
        {
            if (Lbl("spread-count") != null && local != null)
                Lbl("spread-count")!.text = local.Spread.Count.ToString();

            var strip = El("spread-strip");
            if (strip == null || local == null) return;
            strip.Clear();

            var db = session.Rules?.CardDatabase;
            foreach (var cardId in local.Spread)
            {
                var inst = session.GetCard(cardId);
                if (inst == null || db == null) continue;
                var def = db.GetById(inst.DefinitionId);
                if (def == null) continue;
                strip.Add(CardChipFactory.CreateFromDefinition(def.Rank.ToString(), def.Id, db));
            }
        }
    }
}
