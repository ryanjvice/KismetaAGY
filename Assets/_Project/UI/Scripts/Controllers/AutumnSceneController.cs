using System;
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

        public Action? OnOppose;

        CommandBridge? _bridge;
        int _localPlayerId;

        protected override void Wire()
        {
            Btn("fire-btn")!.clicked += () => Debug.Log("[UI] Fire — Phase 4");
            Btn("temper-btn")!.clicked += () => Debug.Log("[UI] Temper — Phase 4");
            Btn("oppose-btn")!.clicked += OnOpposeClicked;
            Btn("manage-cards-btn")!.clicked += () => Debug.Log("[UI] Manage cards — Phase 4");
            Btn("menu-btn")!.clicked += () => Debug.Log("[UI] Card table — Phase 4");
            Btn("pass-btn")!.clicked += OnPass;
        }

        void OnOpposeClicked()
        {
            if (_bridge != null && _bridge.CanSubmit && _bridge.PendingHint == ActionHint.AutumnAction)
                OnOppose?.Invoke();
        }

        public void BindState(GameSession session, GameLoop loop, CommandBridge bridge)
        {
            _bridge = bridge;
            _localPlayerId = bridge.ActivePlayerId;
            if (Root == null) return;

            var view = GamePublicView.From(session);
            MainSceneBindings.ApplySeasonClass(Root, session.Phase.CurrentSeason);
            MainSceneBindings.BindStatusBar(Root, session, loop);
            MainSceneBindings.BindPassButton(Root, session, bridge);

            bool canOppose = bridge.CanSubmit && bridge.PendingHint == ActionHint.AutumnAction;
            Btn("oppose-btn")?.SetEnabled(canOppose);

            var local = MainSceneBindings.LocalPlayer(view, _localPlayerId);
            if (Lbl("stone-label") != null && local != null)
                Lbl("stone-label")!.text = $"stone · {local.StoneState.ToString().ToLowerInvariant()}";

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

        void OnPass() => _bridge?.SubmitPass();
    }
}
