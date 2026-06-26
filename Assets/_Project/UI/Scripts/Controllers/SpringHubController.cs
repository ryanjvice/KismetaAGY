using System.Collections;
using System.Collections.Generic;
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

        public System.Action? OnOpenCardTable;
        public System.Action<string>? OnInspectCard;

        readonly List<string> _spreadIds = new();
        readonly List<string> _handIds = new();

        GameSession? _session;
        GameLoop? _loop;
        CommandBridge? _bridge;
        int _localPlayerId;
        int _wheelBindKey = int.MinValue;
        bool _rolling;
        bool _justFinishedRollSpin;
        bool _communeInitialized;
        bool _communeZonesBuilt;
        int _communePlayerId = -1;
        VisualElement? _communeBuiltForRoot;
        DockZone _dockZone = DockZone.Spread;

        protected override void Unwire()
        {
            _wheelBindKey = int.MinValue;
            _rolling = false;
            _justFinishedRollSpin = false;
            _dockZone = DockZone.Spread;
            _communeZonesBuilt = false;
            _communeBuiltForRoot = null;
            _communePlayerId = -1;
        }

        protected override void Wire()
        {
            Btn("commune-btn")!.clicked += OnPrimaryAction;
            Btn("hand-btn")!.clicked += OnHandToggle;
            Btn("arcanum-btn")!.clicked += OnArcanumToggle;
            Btn("menu-btn")!.clicked += () => OnOpenCardTable?.Invoke();
        }

        void OnHandToggle()
        {
            _dockZone = _dockZone == DockZone.Hand ? DockZone.Spread : DockZone.Hand;
            RefreshDock();
        }

        void OnArcanumToggle()
        {
            _dockZone = _dockZone == DockZone.Arcanum ? DockZone.Spread : DockZone.Arcanum;
            RefreshDock();
        }

        void RefreshDock()
        {
            MainSceneBindings.SetDockZoneFabActive(Root, _dockZone);
            if (_session != null)
                MainSceneBindings.BindDockStrip(Root, _session, _localPlayerId, _dockZone, OnInspectCard);
        }

        public void BindState(GameSession session, GameLoop loop, CommandBridge bridge)
        {
            _session = session;
            _loop = loop;
            _bridge = bridge;
            int resolvedPlayerId = MainSceneBindings.ResolveLocalPlayerId(session, loop, bridge);
            if (resolvedPlayerId != _localPlayerId)
            {
                _wheelBindKey = int.MinValue;
                _communeInitialized = false;
                _communeZonesBuilt = false;
            }
            _localPlayerId = resolvedPlayerId;
            if (Root == null) return;

            var view = GamePublicView.From(session);
            var player = session.Players[_localPlayerId];
            var hint = bridge.PendingHint;
            bool isCommune = hint == ActionHint.Commune;

            MainSceneBindings.ApplySeasonClass(Root, session.Phase.CurrentSeason);
            MainSceneBindings.BindStatusBar(Root, session, loop);
            MainSceneBindings.BindCosmicAgeBanner(Root, session);
            MainSceneBindings.BindStepRail(
                El("step-rail"), ResolveStepIndex(session, player, hint), 5, "step__dot--active");

            BindPhaseVisibility(isCommune);

            if (isCommune)
                BindCommune(session, bridge);
            else
                BindWheelAndHarvest(session, player, hint);

            BindHintLabel(hint, bridge, player);
            BindSpringCta(bridge, loop);

            MainSceneBindings.BindPlayerStrip(Root, session, _localPlayerId);

            if (!isCommune)
                RefreshDock();

            RivalStripBuilder.Populate(El("rivals"), session, view, _localPlayerId, loop.ActivePlayerId, session.Phase.CurrentSeason);
        }

        void BindPhaseVisibility(bool isCommune)
        {
            El("commune-stage")?.EnableInClassList("commune-stage--hidden", !isCommune);
            El("wheel-stage")?.EnableInClassList("spring-hub__stage--hidden", isCommune);
            Root?.Q(className: "screen__inventory")?.EnableInClassList("screen__inventory--hidden", isCommune);
        }

        void BindCommune(GameSession session, CommandBridge bridge)
        {
            int pid = ResolveCommunePlayerId(session, bridge);
            if (pid < 0) return;

            if (pid != _communePlayerId)
            {
                _communePlayerId = pid;
                _communeInitialized = false;
                _communeZonesBuilt = false;
            }

            if (!_communeInitialized)
                SeedCommuneFromPlayer(session, pid);

            RenderCommuneZonesIfNeeded(session);
        }

        static int ResolveCommunePlayerId(GameSession session, CommandBridge bridge)
        {
            int pid = bridge.ActivePlayerId;
            if (pid >= 0 && pid < session.Players.Count)
                return pid;

            var hs = bridge.PendingController;
            if (hs != null && hs.Slot.Index >= 0 && hs.Slot.Index < session.Players.Count)
                return hs.Slot.Index;

            return -1;
        }

        void SeedCommuneFromPlayer(GameSession session, int playerId)
        {
            _spreadIds.Clear();
            _handIds.Clear();
            var player = session.Players[playerId];
            foreach (var id in player.Spread)
                if (TapSwapBindings.IsMinorArcana(session, id)) _spreadIds.Add(id);
            foreach (var id in player.Hand)
                if (TapSwapBindings.IsMinorArcana(session, id)) _handIds.Add(id);
            _communeInitialized = true;
            _communeZonesBuilt = false;
        }

        void RenderCommuneZonesIfNeeded(GameSession session)
        {
            var communeStage = El("commune-stage");
            if (communeStage == null) return;
            if (_communeZonesBuilt && ReferenceEquals(communeStage, _communeBuiltForRoot)) return;
            RefreshCommuneZones(session);
        }

        void RefreshCommuneZones(GameSession session)
        {
            var communeStage = El("commune-stage");
            if (communeStage == null) return;

            TapSwapBindings.RebuildZones(communeStage, session, _spreadIds, _handIds,
                session.Board.CosmicAgeSign, OnCommuneTapMove,
                "commune-spread-count", "commune-hand-count");
            _communeZonesBuilt = true;
            _communeBuiltForRoot = communeStage;
        }

        void OnCommuneTapMove(string cardId, bool fromSpread)
        {
            if (fromSpread)
            {
                if (!_spreadIds.Remove(cardId)) return;
                _handIds.Add(cardId);
            }
            else
            {
                if (!_handIds.Remove(cardId)) return;
                _spreadIds.Add(cardId);
            }

            if (_session != null)
            {
                RefreshCommuneZones(_session);
                BindSpringCta(_bridge!, _loop!);
            }
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

        void BindHintLabel(ActionHint hint, CommandBridge bridge, PlayerState player)
        {
            var label = Lbl("hint-label");
            if (label == null) return;

            if (hint == ActionHint.RollZodiac && bridge.CanSubmit)
                label.text = "Your turn";
            else if (hint == ActionHint.AcknowledgeSign && bridge.CanSubmit)
                label.text = "Review your sign when ready";
            else if (hint == ActionHint.Commune && bridge.CanSubmit)
                label.text = "Arrange spread and hand, then lock";
            else
                label.text = "Your turn";
        }

        void OnPrimaryAction()
        {
            if (_bridge == null) return;

            switch (_bridge.PendingHint)
            {
                case ActionHint.RollZodiac:
                    if (_bridge.ActivePlayerId >= 0 && !_rolling)
                        StartCoroutine(DoRollZodiac());
                    break;
                case ActionHint.AcknowledgeSign:
                    if (_bridge.ActivePlayerId >= 0)
                        _bridge.TrySubmit(new PassActionCommand(_bridge.ActivePlayerId));
                    break;
                case ActionHint.Commune:
                    OnCommuneLock();
                    break;
            }
        }

        void OnCommuneLock()
        {
            if (_bridge == null || _handIds.Count > WinterRules.HandLimit) return;
            int pid = ResolveCommunePlayerId(_session!, _bridge);
            if (pid < 0) return;
            _bridge.TrySubmit(new CommuneCommand(pid, _spreadIds, _handIds));
        }

        const float RollSpinSec = 2f;

        IEnumerator DoRollZodiac()
        {
            if (_rolling || _bridge == null || _session == null || _loop == null
                || _bridge.PendingHint != ActionHint.RollZodiac
                || _bridge.ActivePlayerId < 0)
                yield break;

            _rolling = true;
            Btn("commune-btn")?.SetEnabled(false);
            if (Lbl("harvest-count") != null)
                Lbl("harvest-count")!.text = "Rolling...";

            _bridge.TrySubmit(new RollZodiacCommand(_bridge.ActivePlayerId));

            yield return UiMotion.SpinZodiacWheel(El("wheel-zodiac"), RollSpinSec);

            _justFinishedRollSpin = true;
            _rolling = false;
            _wheelBindKey = int.MinValue;

            var player = _session.Players[_localPlayerId];
            BindWheelAndHarvest(_session, player, _bridge.PendingHint);
            BindHintLabel(_bridge.PendingHint, _bridge, player);
            BindSpringCta(_bridge, _loop);
            _justFinishedRollSpin = false;
        }

        void BindSpringCta(CommandBridge bridge, GameLoop loop)
        {
            var btn = Btn("commune-btn");
            if (btn == null) return;

            var hint = bridge.PendingHint;
            bool canAct = bridge.CanSubmit && loop.PendingHumanController != null;

            if (_rolling)
            {
                btn.style.display = DisplayStyle.Flex;
                btn.text = "Roll your zodiac die";
                btn.SetEnabled(false);
                btn.EnableInClassList("btn--disabled", true);
                return;
            }

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
                    btn.text = "Lock the tableau · to Summer";
                    bool valid = _handIds.Count <= WinterRules.HandLimit;
                    btn.SetEnabled(valid);
                    btn.EnableInClassList("btn--disabled", !valid);
                    break;
                default:
                    btn.style.display = DisplayStyle.None;
                    break;
            }
        }

        void BindWheelAndHarvest(GameSession session, PlayerState player, ActionHint hint)
        {
            var wheel = El("wheel-stack") ?? El("wheel-host");
            if (wheel == null) return;

            UiArtBindings.ApplyWheelStack(wheel);

            if (_rolling)
            {
                BindWheelGlyph(ZodiacSign.None);
                SetLabelVisible("rolled-sign", false);
                SetLabelVisible("sign-match", false);
                if (Lbl("harvest-count") != null)
                    Lbl("harvest-count")!.text = "Rolling...";
                return;
            }

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

            if (hint == ActionHint.AcknowledgeSign && !_justFinishedRollSpin)
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
            SymbolGlyphs.TagZodiac(glyph);
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
