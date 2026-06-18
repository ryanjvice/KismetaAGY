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
    public sealed class WinterHubController : ScreenController
    {
        public override string ScreenId => ScreenIds.WinterHub;

        public System.Action? OnOpenUnlock;
        public System.Action? OnOpenWager;

        CommandBridge? _bridge;
        int _localPlayerId;

        protected override void Wire()
        {
            Btn("continue-btn")!.clicked += () => OnOpenUnlock?.Invoke();
            Btn("wager-btn")!.clicked += () => OnOpenWager?.Invoke();
            Btn("menu-btn")!.clicked += () => Debug.Log("[UI] Card table — future work");
            Btn("pass-btn")!.clicked += OnPass;
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
            MainSceneBindings.BindStepRail(
                El("step-rail"), session.Phase.CurrentStepIndex, 4, "step__dot--active");

            var local = MainSceneBindings.LocalPlayer(view, _localPlayerId);
            if (Lbl("limits-line") != null && local != null)
            {
                Lbl("limits-line")!.text =
                    $"Spread {local.Spread.Count}/5 · Hand {local.HandCardCount}/5";
            }

            BindWinterCta(session, bridge);
            RivalStripBuilder.Populate(El("rivals"), view, _localPlayerId);
        }

        void BindWinterCta(GameSession session, CommandBridge bridge)
        {
            bool winterAction = bridge.PendingHint == ActionHint.WinterAction;
            var player = session.Players[_localPlayerId];
            bool canWager = winterAction && player.FatefulWagerSign == ZodiacSign.None;

            SetCtaVisible("continue-btn", winterAction);
            SetCtaVisible("wager-btn", canWager);
            SetCtaVisible("limits-btn", false);
            SetCtaVisible("transit-btn", false);
        }

        void SetCtaVisible(string name, bool visible)
        {
            var btn = Btn(name);
            if (btn == null) return;
            btn.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void OnPass() => _bridge?.SubmitPass();
    }
}
