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
    public sealed class SpringHubController : ScreenController
    {
        public override string ScreenId => ScreenIds.SpringHub;

        int _localPlayerId;

        protected override void Wire()
        {
            Btn("commune-btn")!.clicked += () => Debug.Log("[UI] Commune — Phase 4");
            Btn("menu-btn")!.clicked += () => Debug.Log("[UI] Card table — Phase 4");
        }

        public void BindState(GameSession session, GameLoop loop, CommandBridge bridge)
        {
            _localPlayerId = bridge.ActivePlayerId;
            if (Root == null) return;

            var view = GamePublicView.From(session);
            MainSceneBindings.ApplySeasonClass(Root, session.Phase.CurrentSeason);
            MainSceneBindings.BindStatusBar(Root, session, loop);
            MainSceneBindings.BindStepRail(
                El("step-rail"), session.Phase.CurrentStepIndex, 5, "step__dot--active");

            var local = MainSceneBindings.LocalPlayer(view, _localPlayerId);
            if (Lbl("harvest-count") != null && local != null)
                Lbl("harvest-count")!.text = $"{local.Spread.Count} in spread · {local.HandCardCount} in hand";

            if (Lbl("spread-count") != null && local != null)
                Lbl("spread-count")!.text = local.Spread.Count.ToString();

            RivalStripBuilder.Populate(El("rivals"), view, _localPlayerId);
            PopulateSpreadStrip(session, local);
        }

        void PopulateSpreadStrip(GameSession session, PublicPlayerView? local)
        {
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
                strip.Add(CardChipFactory.CreateFromDefinition(
                    def.Rank.ToString(), def.Id, db));
            }
        }
    }
}
