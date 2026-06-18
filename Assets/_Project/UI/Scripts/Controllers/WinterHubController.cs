using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Views;
using Kismeta.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class WinterHubController : ScreenController
    {
        public override string ScreenId => ScreenIds.WinterHub;

        CommandBridge? _bridge;
        int _localPlayerId;

        protected override void Wire()
        {
            HookAdvance("continue-btn");
            HookAdvance("limits-btn");
            HookAdvance("transit-btn");
            Btn("menu-btn")!.clicked += () => Debug.Log("[UI] Card table — Phase 4");
            Btn("pass-btn")!.clicked += OnPass;
        }

        void HookAdvance(string name)
        {
            var b = Btn(name);
            if (b != null)
                b.clicked += () => Debug.Log($"[UI] Winter advance ({name}) — Phase 4");
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

            BindWinterCta(session.Phase.CurrentStepIndex);
            RivalStripBuilder.Populate(El("rivals"), view, _localPlayerId);
        }

        void BindWinterCta(int stepIndex)
        {
            SetCtaVisible("continue-btn", stepIndex == 0);
            SetCtaVisible("limits-btn", stepIndex == 2);
            SetCtaVisible("transit-btn", stepIndex == 3);
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
