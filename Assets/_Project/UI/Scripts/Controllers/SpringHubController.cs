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
        public System.Action? OnOpenCardTable;
        public System.Action<string>? OnInspectCard;

        GameSession? _session;
        GameLoop? _loop;
        CommandBridge? _bridge;
        int _localPlayerId;
        int _wheelBindKey = int.MinValue;
        bool _showHand;

        protected override void Unwire()
        {
            _wheelBindKey = int.MinValue;
            _showHand = false;
        }

        protected override void Wire()
        {
            Btn("commune-btn")!.clicked += OnPrimaryAction;
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
            _loop = loop;
            _bridge = bridge;
            int resolvedPlayerId = MainSceneBindings.ResolveLocalPlayerId(session, loop, bridge);
            if (resolvedPlayerId != _localPlayerId)
                _wheelBindKey = int.MinValue;
            _localPlayerId = resolvedPlayerId;
            if (Root == null) return;

            var view = GamePublicView.From(session);
            var player = session.Players[_localPlayerId];
            var hint = bridge.PendingHint;

            MainSceneBindings.ApplySeasonClass(Root, session.Phase.CurrentSeason);
            MainSceneBindings.BindStatusBar(Root, session, loop);
            MainSceneBindings.BindCosmicAgeBanner(Root, session);
            MainSceneBindings.BindStepRail(
                El("step-rail"), ResolveStepIndex(session, player, hint), 5, "step__dot--active");

            BindSceneSubtitle(hint, player);
            BindWheelAndHarvest(session, player, hint);
            BindHintLabel(hint, bridge, player);
            BindSpringCta(bridge, loop);

            MainSceneBindings.SetHandFabActive(Root, _showHand);
            MainSceneBindings.BindDockStrip(Root, session, _localPlayerId, _showHand, OnInspectCard);

            RivalStripBuilder.Populate(El("rivals"), session, view, _localPlayerId, loop.ActivePlayerId, session.Phase.CurrentSeason);
        }

        static int ResolveStepIndex(GameSession session, PlayerState player, ActionHint hint)
        {
            if (hint == ActionHint.RollZodiac)
                return 1;
            if (hint == ActionHint.AcknowledgeSign)
                return 2;
            if (hint == ActionHint.Commune)
                return 3;
            if (player.CurrentSign != ZodiacSign.None)
            {
                int harvestCount = player.Spread.Count + player.Hand.Count;
                if (harvestCount > 0)
                    return 3;
                return 2;
            }
            return session.Phase.CurrentStepIndex;
        }

        void BindSceneSubtitle(ActionHint hint, PlayerState player)
        {
            var subtitle = Lbl("scene-subtitle");
            if (subtitle == null) return;

            if (hint == ActionHint.RollZodiac && player.CurrentSign == ZodiacSign.None)
                subtitle.text = "roll your zodiac die to claim a sign";
            else if (hint == ActionHint.AcknowledgeSign)
                subtitle.text = "your sign is set — gather your harvest next";
            else if (player.CurrentSign != ZodiacSign.None)
                subtitle.text = "your sign is set — harvest and commune follow";
            else
                subtitle.text = "spring — set your sign, gather, commune";
        }

        void BindHintLabel(ActionHint hint, CommandBridge bridge, PlayerState player)
        {
            var label = Lbl("hint-label");
            if (label == null) return;

            if (hint == ActionHint.RollZodiac && bridge.CanSubmit)
                label.text = "Your turn — roll your zodiac die";
            else if (hint == ActionHint.AcknowledgeSign && bridge.CanSubmit)
                label.text = "Review your sign — continue when ready";
            else if (hint == ActionHint.Commune && bridge.CanSubmit)
                label.text = "Your turn — commune when ready";
            else
                label.text = "Your turn";
        }

        void OnPrimaryAction()
        {
            if (_bridge == null) return;

            switch (_bridge.PendingHint)
            {
                case ActionHint.RollZodiac:
                    if (_bridge.ActivePlayerId >= 0)
                        _bridge.TrySubmit(new RollZodiacCommand(_bridge.ActivePlayerId));
                    break;
                case ActionHint.AcknowledgeSign:
                    if (_bridge.ActivePlayerId >= 0)
                        _bridge.TrySubmit(new PassActionCommand(_bridge.ActivePlayerId));
                    break;
                case ActionHint.Commune:
                    OnOpenCommune?.Invoke();
                    break;
            }
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
                case ActionHint.RollZodiac:
                    btn.style.display = DisplayStyle.Flex;
                    btn.text = "Roll your zodiac die";
                    btn.SetEnabled(true);
                    btn.EnableInClassList("btn--disabled", false);
                    break;
                case ActionHint.AcknowledgeSign:
                    btn.style.display = DisplayStyle.Flex;
                    btn.text = "Continue";
                    btn.SetEnabled(true);
                    btn.EnableInClassList("btn--disabled", false);
                    break;
                case ActionHint.Commune:
                    btn.style.display = DisplayStyle.Flex;
                    btn.text = "Commune";
                    btn.SetEnabled(true);
                    btn.EnableInClassList("btn--disabled", false);
                    break;
                default:
                    btn.style.display = DisplayStyle.None;
                    break;
            }
        }

        void BindWheelAndHarvest(GameSession session, PlayerState player, ActionHint hint)
        {
            var wheel = El("wheel-host");
            if (wheel == null) return;

            var sign = player.CurrentSign;
            var cosmic = session.Board.CosmicAgeSign;
            int bindKey = WheelBindKey(player.PlayerId, sign, hint);

            BindWheelGlyph(sign);

            if (bindKey == _wheelBindKey)
            {
                UpdateHarvestLabel(session, player, hint);
                return;
            }

            _wheelBindKey = bindKey;

            if (sign == ZodiacSign.None)
            {
                SetLabelVisible("rolled-sign", false);
                SetLabelVisible("sign-match", false);
                UpdateHarvestLabel(session, player, hint);
                return;
            }

            if (hint == ActionHint.AcknowledgeSign)
                UiMotion.AnimateWheelSettle(wheel, () => { });

            if (Lbl("rolled-sign") != null)
            {
                Lbl("rolled-sign")!.text = sign.ToString();
                Lbl("rolled-sign")!.style.display = DisplayStyle.Flex;
            }

            if (Lbl("sign-match") != null)
            {
                Lbl("sign-match")!.text = DescribeAlignment(sign, cosmic);
                Lbl("sign-match")!.style.display = DisplayStyle.Flex;
            }

            UpdateHarvestLabel(session, player, hint);
        }

        void BindWheelGlyph(ZodiacSign sign)
        {
            var glyph = Lbl("wheel-glyph");
            if (glyph == null) return;
            SymbolGlyphs.TagEmoji(glyph);
            glyph.text = sign == ZodiacSign.None ? "?" : SymbolGlyphs.Zodiac(sign);
            glyph.style.display = DisplayStyle.Flex;
        }

        static int WheelBindKey(int playerId, ZodiacSign sign, ActionHint hint)
            => playerId * 1000 + (int)sign * 10 + (int)hint;

        void UpdateHarvestLabel(GameSession session, PlayerState player, ActionHint hint)
        {
            if (Lbl("harvest-count") == null) return;

            if (player.CurrentSign == ZodiacSign.None)
            {
                Lbl("harvest-count")!.text = hint == ActionHint.RollZodiac
                    ? "Awaiting your roll"
                    : "Awaiting harvest";
                return;
            }

            if (hint == ActionHint.AcknowledgeSign)
            {
                Lbl("harvest-count")!.text = "Sign locked — tap below to gather";
                return;
            }

            int harvestCount = CountMinorCards(session, player);
            Lbl("harvest-count")!.text = harvestCount > 0
                ? $"{harvestCount} card(s) harvested"
                : "Awaiting harvest";
        }

        void SetLabelVisible(string elementName, bool visible)
        {
            var lbl = Lbl(elementName);
            if (lbl != null)
                lbl.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        static string DescribeAlignment(ZodiacSign playerSign, ZodiacSign cosmicSign)
        {
            int bonus = AlignmentBonus(playerSign, cosmicSign);
            if (bonus >= 3)
                return $"your meeple moves to {playerSign} · sign match · +{bonus} alignment";
            if (bonus == 2)
                return $"your meeple moves to {playerSign} · planet match · +{bonus} alignment";
            if (bonus == 1)
                return $"your meeple moves to {playerSign} · element match · +{bonus} alignment";
            return $"your meeple moves to {playerSign} · no aspect match";
        }

        static int AlignmentBonus(ZodiacSign playerSign, ZodiacSign cosmicSign)
        {
            if (playerSign == ZodiacSign.None || cosmicSign == ZodiacSign.None) return 0;
            if (playerSign == cosmicSign) return 3;
            if (Correspondence.PlanetFor(playerSign) == Correspondence.PlanetFor(cosmicSign)) return 2;
            if (Correspondence.ElementFor(playerSign) == Correspondence.ElementFor(cosmicSign)) return 1;
            return 0;
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
    }
}
