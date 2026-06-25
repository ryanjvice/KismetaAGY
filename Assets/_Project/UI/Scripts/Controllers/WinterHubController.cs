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
        public System.Action? OnOpenCraft;
        public System.Action? OnOpenWager;
        public System.Action? OnOpenCardTable;
        public System.Action<string>? OnInspectCard;

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
            Btn("continue-btn")!.clicked += () => OnOpenUnlock?.Invoke();
            Btn("craft-btn")!.clicked += () => OnOpenCraft?.Invoke();
            Btn("wager-btn")!.clicked += () => OnOpenWager?.Invoke();
            Btn("hand-btn")!.clicked += OnHandToggle;
            Btn("menu-btn")!.clicked += () => OnOpenCardTable?.Invoke();
            Btn("pass-btn")!.clicked += OnPass;
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
            _localPlayerId = MainSceneBindings.ResolveLocalPlayerId(session, loop, bridge);
            if (Root == null) return;

            UiArtBindings.ApplyWinterSeal(El("winter-stage"));

            var view = GamePublicView.From(session);
            MainSceneBindings.ApplySeasonClass(Root, session.Phase.CurrentSeason);
            MainSceneBindings.BindStatusBar(Root, session, loop);
            MainSceneBindings.BindCosmicAgeBanner(Root, session);
            MainSceneBindings.BindPassButton(Root, session, bridge);
            MainSceneBindings.BindStepRail(
                El("step-rail"), session.Phase.CurrentStepIndex, 4, "step__dot--active");

            var local = MainSceneBindings.LocalPlayer(view, _localPlayerId);
            if (Lbl("limits-line") != null && local != null)
            {
                Lbl("limits-line")!.text =
                    $"Spread {local.Spread.Count}/5 · Hand {local.HandCardCount}/5";
            }

            MainSceneBindings.SetHandFabActive(Root, _showHand);
            MainSceneBindings.BindDockStrip(Root, session, _localPlayerId, _showHand, OnInspectCard);

            BindWinterCta(session, bridge);
            RivalStripBuilder.Populate(El("rivals"), session, view, _localPlayerId, loop.ActivePlayerId, session.Phase.CurrentSeason);
        }

        void BindWinterCta(GameSession session, CommandBridge bridge)
        {
            bool winterAction = bridge.PendingHint == ActionHint.WinterAction;
            var player = session.Players[_localPlayerId];
            bool canWager = winterAction && player.FatefulWagerSign == ZodiacSign.None;

            SetCtaVisible("continue-btn", winterAction);
            SetCtaVisible("craft-btn", winterAction);
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
