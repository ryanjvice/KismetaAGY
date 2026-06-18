using Kismeta.Core.Commands;
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

        public System.Action? OnOpenCommune;

        GameSession? _session;
        GameLoop? _loop;
        CommandBridge? _bridge;
        int _localPlayerId;

        protected override void Wire()
        {
            Btn("commune-btn")!.clicked += OnPrimaryAction;
            Btn("menu-btn")!.clicked += () => Debug.Log("[UI] Card table — future work");
        }

        public void BindState(GameSession session, GameLoop loop, CommandBridge bridge)
        {
            _session = session;
            _loop = loop;
            _bridge = bridge;
            _localPlayerId = ResolveLocalPlayerId(session, loop, bridge);
            if (Root == null) return;

            var view = GamePublicView.From(session);
            MainSceneBindings.ApplySeasonClass(Root, session.Phase.CurrentSeason);
            MainSceneBindings.BindStatusBar(Root, session, loop);
            MainSceneBindings.BindStepRail(
                El("step-rail"), session.Phase.CurrentStepIndex, 5, "step__dot--active");

            var player = session.Players[_localPlayerId];
            BindWheelAndHarvest(session, player);
            BindSpringCta(bridge, loop);

            var local = MainSceneBindings.LocalPlayer(view, _localPlayerId);
            if (Lbl("spread-count") != null && local != null)
                Lbl("spread-count")!.text = local.Spread.Count.ToString();

            RivalStripBuilder.Populate(El("rivals"), view, _localPlayerId);
            PopulateSpreadStrip(session, local);
        }

        static int ResolveLocalPlayerId(GameSession session, GameLoop loop, CommandBridge bridge)
        {
            if (bridge.ActivePlayerId >= 0 && bridge.ActivePlayerId < session.Players.Count)
                return bridge.ActivePlayerId;
            if (loop.ActivePlayerId >= 0 && loop.ActivePlayerId < session.Players.Count)
                return loop.ActivePlayerId;
            var hs = bridge.PendingController;
            if (hs != null && hs.Slot.Index >= 0 && hs.Slot.Index < session.Players.Count)
                return hs.Slot.Index;
            return 0;
        }

        void OnPrimaryAction()
        {
            if (_bridge == null) return;

            switch (_bridge.PendingHint)
            {
                case ActionHint.Commune:
                    OnOpenCommune?.Invoke();
                    break;
                case ActionHint.AdeptDecision:
                    SubmitDeclineAdept();
                    break;
            }
        }

        void SubmitDeclineAdept()
        {
            if (_bridge == null || _loop == null) return;
            var adeptId = _loop.PendingCardId;
            if (string.IsNullOrEmpty(adeptId)) return;

            int pid = _bridge.PendingController?.Slot.Index ?? _localPlayerId;
            if (pid < 0) return;
            _bridge.TrySubmit(new DeclineAdeptCommand(pid, adeptId));
        }

        void BindSpringCta(CommandBridge bridge, GameLoop loop)
        {
            var btn = Btn("commune-btn");
            if (btn == null) return;

            var hint = bridge.PendingHint;
            bool canAct = bridge.CanSubmit && loop.PendingHumanController != null;

            if (!canAct)
            {
                btn.style.display = DisplayStyle.None;
                return;
            }

            switch (hint)
            {
                case ActionHint.Commune:
                    btn.style.display = DisplayStyle.Flex;
                    btn.text = "Commune";
                    btn.SetEnabled(true);
                    btn.EnableInClassList("btn--disabled", false);
                    break;
                case ActionHint.AdeptDecision:
                    btn.style.display = DisplayStyle.Flex;
                    btn.text = "Decline Adept";
                    btn.SetEnabled(!string.IsNullOrEmpty(loop.PendingCardId));
                    btn.EnableInClassList("btn--disabled", string.IsNullOrEmpty(loop.PendingCardId));
                    break;
                default:
                    btn.style.display = DisplayStyle.None;
                    break;
            }
        }

        void BindWheelAndHarvest(GameSession session, PlayerState player)
        {
            var wheel = El("wheel-host");
            if (wheel != null)
            {
                wheel.Clear();
                var sign = player.CurrentSign;
                var glyph = SignGlyph(sign);
                var signLbl = new Label(glyph);
                signLbl.style.fontSize = 36;
                signLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
                signLbl.style.flexGrow = 1;
                wheel.Add(signLbl);

                if (sign != ZodiacSign.None)
                {
                    var nameLbl = new Label(sign.ToString());
                    nameLbl.style.fontSize = 10;
                    nameLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
                    nameLbl.style.color = new StyleColor(new Color(0.5f, 0.77f, 0.66f));
                    wheel.Add(nameLbl);
                }
            }

            int harvestCount = CountMinorCards(session, player);
            if (Lbl("harvest-count") != null)
                Lbl("harvest-count")!.text = harvestCount > 0
                    ? $"{harvestCount} card(s) harvested"
                    : "Awaiting harvest";
        }

        static int CountMinorCards(GameSession session, PlayerState player)
        {
            int count = 0;
            foreach (var id in player.Spread)
                if (TapSwapBindings.IsMinorArcana(session, id)) count++;
            foreach (var id in player.Hand)
                if (TapSwapBindings.IsMinorArcana(session, id)) count++;
            return count;
        }

        static string SignGlyph(ZodiacSign sign) => sign switch
        {
            ZodiacSign.Aries => "\u2648",
            ZodiacSign.Taurus => "\u2649",
            ZodiacSign.Gemini => "\u264A",
            ZodiacSign.Cancer => "\u264B",
            ZodiacSign.Leo => "\u264C",
            ZodiacSign.Virgo => "\u264D",
            ZodiacSign.Libra => "\u264E",
            ZodiacSign.Scorpio => "\u264F",
            ZodiacSign.Sagittarius => "\u2650",
            ZodiacSign.Capricorn => "\u2651",
            ZodiacSign.Aquarius => "\u2652",
            ZodiacSign.Pisces => "\u2653",
            _ => "?"
        };

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
