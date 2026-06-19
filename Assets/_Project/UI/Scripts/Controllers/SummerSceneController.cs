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
        int _localPlayerId;

        protected override void Wire()
        {
            Btn("craftbuild-btn")!.clicked += () => OnCraftBuild?.Invoke();
            Btn("consort-btn")!.clicked += () => OnConsort?.Invoke();
            Btn("activate-btn")!.clicked += () => OnActivate?.Invoke();
            Btn("pass-btn")!.clicked += () => OnPass?.Invoke();
            Btn("menu-btn")!.clicked += () => OnOpenCardTable?.Invoke();
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

            var local = MainSceneBindings.LocalPlayer(view, _localPlayerId);
            BindCauldrons(local);
            BindSpread(session, local);

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
                strip.Add(CardChipFactory.CreateFromDefinition(
                    def.Rank.ToString(), def.Id, db,
                    instanceId: cardId, onInspect: OnInspectCard));
            }
        }
    }
}
