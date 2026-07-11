using System;
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
using Kismeta.UI.Narrative;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class SpringHubController : ScreenController
    {
        public override string ScreenId => ScreenIds.SpringHub;

        public System.Action? OnOpenCardTable;
        public System.Action? OnOpenActiveEffects;
        public System.Action? OnOpenCrucibleCodex;
        public System.Action? OnOpenProtectiveWards;
        public System.Action<string>? OnInspectCard;
        public System.Action<int>? OnRivalSelected;
        public System.Action? OnOpenBoardInspect;
        public System.Action? OnDismissBoardInspect;
        public System.Action? OnOpenForgeInspect;
        public System.Action? OnDismissForgeInspect;

        public SeasonIntroRecapHost? IntroRecapHost { get; set; }

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
        bool _communeSubviewOpen;
        int _communePlayerId = -1;
        VisualElement? _communeBuiltForRoot;
        DockZone _dockZone = DockZone.Spread;
        SummerConsultView _consultView = SummerConsultView.Table;

        protected override void Unwire()
        {
            UnwireClick(Btn("wheel-action-btn"), OnWheelAction);
            UnwireClick(Btn("review-tableau-btn"), OnOpenCommuneSubview);
            UnwireClick(Btn("proceed-btn"), OnProceedToSummer);
            UnwireClick(Btn("review-effects-btn"), OnReviewEffects);
            UnwireClick(Btn("commune-lock-btn"), OnCommuneLock);
            UnwireClick(Btn("consult-table-btn"), OnConsultTable);
            UnwireClick(Btn("consult-zodiac-btn"), OnConsultZodiac);
            UnwireClick(Btn("consult-crucible-btn"), OnConsultCrucible);
            CentralPanelInspectBindings.Unwire();
            if (Root != null)
                SummerCrucibleRowBindings.Unwire(Root);
            _wheelBindKey = int.MinValue;
            _rolling = false;
            _justFinishedRollSpin = false;
            _dockZone = DockZone.Spread;
            _communeZonesBuilt = false;
            _communeBuiltForRoot = null;
            _communePlayerId = -1;
            _communeSubviewOpen = false;
            _consultView = SummerConsultView.Table;
        }

        protected override void Wire()
        {
            WireClick(Btn("wheel-action-btn"), OnWheelAction);
            WireClick(Btn("review-tableau-btn"), OnOpenCommuneSubview);
            WireClick(Btn("proceed-btn"), OnProceedToSummer);
            WireClick(Btn("review-effects-btn"), OnReviewEffects);
            WireClick(Btn("commune-lock-btn"), OnCommuneLock);
            WireClick(Btn("consult-table-btn"), OnConsultTable);
            WireClick(Btn("consult-zodiac-btn"), OnConsultZodiac);
            WireClick(Btn("consult-crucible-btn"), OnConsultCrucible);
            NarrativeToolbarBindings.WireIntroRecap(Root, Season.Spring, () => IntroRecapHost);

            InventoryOverlayBindings.Wire(Root, new InventoryOverlayBindings.Callbacks
            {
                OnHandToggle = OnHandToggle,
                OnArcanumToggle = OnArcanumToggle,
                OnOpenCardTable = () => OnOpenCardTable?.Invoke(),
                OnOpenActiveEffects = () => OnOpenActiveEffects?.Invoke(),
                OnOpenCrucibleCodex = () => OnOpenCrucibleCodex?.Invoke(),
                OnOpenProtectiveWards = () => OnOpenProtectiveWards?.Invoke()
            });
            HeaderOverlayBindings.Wire(Root, id => OnRivalSelected?.Invoke(id));
            CentralPanelInspectBindings.Wire(Root, OnInspectFabClicked);
        }

        void OnConsultTable() => SetConsultView(SummerConsultView.Table);
        void OnConsultZodiac() => SetConsultView(SummerConsultView.Zodiac);
        void OnConsultCrucible() => SetConsultView(SummerConsultView.Crucible);

        static void WireClick(Button? btn, Action handler)
        {
            if (btn != null)
                btn.clicked += handler;
        }

        static void UnwireClick(Button? btn, Action handler)
        {
            if (btn != null)
                btn.clicked -= handler;
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
            if (_session != null)
                InventoryOverlayBindings.RefreshInventory(Root, _session, _localPlayerId, _dockZone, OnInspectCard);
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
                _communeSubviewOpen = false;
            }
            _localPlayerId = resolvedPlayerId;
            if (Root == null) return;

            var player = session.Players[_localPlayerId];
            var hint = bridge.PendingHint;
            bool isCommune = hint == ActionHint.Commune;
            bool isHub = hint == ActionHint.SpringAction;
            bool isWheel = hint is ActionHint.RollZodiac or ActionHint.AcknowledgeSign;
            bool showCommune = isCommune || (_communeSubviewOpen && isHub);

            if (!isHub && !isCommune)
                _communeSubviewOpen = false;

            MainSceneBindings.ApplySeasonClass(Root, session.Phase.CurrentSeason);
            HeaderOverlayBindings.RefreshHeader(
                Root, session, loop, _localPlayerId, loop.ActivePlayerId, session.Phase.CurrentSeason,
                id => OnRivalSelected?.Invoke(id));
            MainSceneBindings.BindStepRail(
                El("step-rail"), ResolveStepIndex(session, player, hint), 5, "step__dot--active");

            BindPhaseVisibility(isHub, isWheel, showCommune);

            if (showCommune)
                BindCommune(session, bridge);
            else if (isHub)
            {
                ApplyConsultView();
                RefreshConsultView();
            }
            else
                CentralPanelInspectBindings.SetFabVisible(Root, false, DismissActiveInspect);

            BindHintLabel(hint, bridge, player, isHub, showCommune);
            BindCtas(bridge, loop, hint, isHub, showCommune);

            var stepId = NarrativeStepResolver.ResolveSpringHub(hint, showCommune);
            NarrativeSlotBindings.BindById(Root, stepId, mask: NarrativeSlotMask.Beat);

            VisualElement? chargeRoot = showCommune ? El("commune-stage")
                : isHub ? El("spring-consult-zodiac")
                : El("wheel-stage");
            NarrativeSlotBindings.BindById(
                chargeRoot,
                stepId,
                mask: NarrativeSlotMask.Charge);

            if (isWheel)
                BindWheelAndHarvest(session, player, hint);

            if (isWheel || isHub || showCommune)
                RefreshDock();

            HeaderOverlayBindings.ApplyHeaderPad(Root);
        }

        void BindPhaseVisibility(bool isHub, bool isWheel, bool showCommune)
        {
            bool showConsult = isHub && !showCommune;
            El("wheel-stage")?.EnableInClassList("spring-hub__stage--hidden", !isWheel);
            El("spring-consult-frame")?.EnableInClassList("spring-hub__stage--hidden", !showConsult);
            El("commune-stage")?.EnableInClassList("commune-stage--hidden", !showCommune);
            El("spring-consult-crucible-section")?.EnableInClassList(
                "spring-consult-crucible-section--hidden",
                !showConsult || _consultView != SummerConsultView.Crucible);
            El("wheel-cta")?.EnableInClassList("spring-hub__hub-cta--hidden", !isWheel);
            El("hub-cta")?.EnableInClassList("spring-hub__hub-cta--hidden", !isHub || showCommune);
            El("commune-cta")?.EnableInClassList("spring-hub__commune-cta--hidden", !showCommune);
            InventoryOverlayBindings.SetVisible(Root, isWheel || isHub || showCommune);
        }

        void SetConsultView(SummerConsultView view)
        {
            if (_consultView == view) return;

            DismissInspectForView(_consultView);
            _consultView = view;
            ApplyConsultView();
            RefreshConsultView();
        }

        void ApplyConsultView()
        {
            SpringConsultBindings.ApplyView(Root, _consultView, DismissActiveInspect);
            SpringConsultBindings.BindButtonStates(Root, _consultView);
        }

        void DismissActiveInspect() => DismissInspectForView(_consultView);

        void DismissInspectForView(SummerConsultView view)
        {
            switch (view)
            {
                case SummerConsultView.Zodiac:
                    OnDismissBoardInspect?.Invoke();
                    break;
                case SummerConsultView.Crucible:
                    OnDismissForgeInspect?.Invoke();
                    break;
            }
        }

        void OnInspectFabClicked()
        {
            switch (_consultView)
            {
                case SummerConsultView.Zodiac:
                    OnOpenBoardInspect?.Invoke();
                    break;
                case SummerConsultView.Crucible:
                    OnOpenForgeInspect?.Invoke();
                    break;
            }
        }

        void RefreshConsultView()
        {
            if (Root == null || _session == null) return;

            switch (_consultView)
            {
                case SummerConsultView.Table:
                    RefreshRoster();
                    break;
                case SummerConsultView.Zodiac:
                    SpringBoardBindings.BindBoard(El("spring-board"), _session);
                    break;
                case SummerConsultView.Crucible:
                    RefreshCrucibleConsult();
                    break;
            }
        }

        void RefreshCrucibleConsult()
        {
            if (Root == null || _session == null) return;

            var view = GamePublicView.From(_session);
            var local = MainSceneBindings.LocalPlayer(view, _localPlayerId);
            var player = _session.Players[_localPlayerId];

            CrucibleForgeBindings.ApplyForge(
                El("board-stage"),
                null,
                player,
                AutumnActionBindings.StoneStatusLabel(player));
            CrucibleForgeBindings.ApplyAllPlayerStones(
                El("board-stage"), El("stasis-row"), _session, _localPlayerId);
            CrucibleForgeBindings.ApplyCauldronReagents(El("cauldron-mini"), player);
            SummerCrucibleRowBindings.Bind(Root, local, _session, onCardTap: null);
        }

        void RefreshRoster()
        {
            if (Root == null || _session == null) return;
            SummerRosterBindings.Populate(Root, _session, _localPlayerId, OnInspectCard);
        }

        void BindCommune(GameSession session, CommandBridge bridge)
        {
            int pid = ResolvePendingPlayerId(session, bridge);
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
            BindCommuneLockCta(bridge);
        }

        static int ResolvePendingPlayerId(GameSession session, CommandBridge bridge)
        {
            var hs = bridge.PendingController;
            if (hs != null && hs.Slot.Index >= 0 && hs.Slot.Index < session.Players.Count)
                return hs.Slot.Index;

            int pid = bridge.ActivePlayerId;
            if (pid >= 0 && pid < session.Players.Count)
                return pid;

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

            int pid = ResolvePendingPlayerId(session, _bridge!);
            TableauBindings.RebuildCommuneZones(communeStage, session, pid, _spreadIds, _handIds,
                session.Board.CosmicAgeSign, OnCommuneTapMove, OnInspectCard);
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
                BindCommuneLockCta(_bridge!);
            }
        }

        static int ResolveStepIndex(GameSession session, PlayerState player, ActionHint hint)
        {
            if (hint == ActionHint.RollZodiac)
                return 1;
            if (hint == ActionHint.AcknowledgeSign)
                return 2;
            if (hint == ActionHint.SpringAction || hint == ActionHint.Commune)
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

        void BindHintLabel(ActionHint hint, CommandBridge bridge, PlayerState player, bool isHub, bool showCommune)
        {
            var label = Lbl("hint-label");
            if (label == null) return;

            if (showCommune)
                label.text = "Arrange spread and hand, then lock";
            else if (isHub && bridge.CanSubmit)
                label.text = "Review the wheel, then take an action or proceed";
            else if (hint == ActionHint.RollZodiac && bridge.CanSubmit)
                label.text = "Your turn";
            else if (hint == ActionHint.AcknowledgeSign && bridge.CanSubmit)
                label.text = "Review your sign when ready";
            else
                label.text = "Your turn";

            var showHint = isHub || hint is ActionHint.RollZodiac or ActionHint.AcknowledgeSign;
            label.style.display = showHint ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void OnWheelAction()
        {
            if (_bridge == null || _session == null) return;

            var hint = _loop?.PendingHint ?? _bridge.PendingHint;
            switch (hint)
            {
                case ActionHint.RollZodiac:
                    if (!_rolling)
                        StartCoroutine(DoRollZodiac());
                    break;
                case ActionHint.AcknowledgeSign:
                    TrySubmitPassForPendingPlayer();
                    break;
            }
        }

        bool TrySubmitPassForPendingPlayer()
        {
            if (_bridge == null || !_bridge.CanSubmit)
                return false;
            return _bridge.TrySubmitPass();
        }

        void OnReviewEffects() => OnOpenActiveEffects?.Invoke();

        void OnOpenCommuneSubview()
        {
            if (_session == null || _bridge == null) return;
            _communeSubviewOpen = true;
            _communeInitialized = false;
            BindState(_session, _loop!, _bridge);
        }

        void OnProceedToSummer()
        {
            if (_bridge == null || _loop == null || !_bridge.CanSubmit)
                return;

            var proceedBtn = Btn("proceed-btn");
            proceedBtn?.SetEnabled(false);
            proceedBtn?.EnableInClassList("btn--disabled", true);

            if (TrySubmitPassForPendingPlayer())
                return;

            BindHubCtas(_bridge, _loop);
        }

        void OnCommuneLock()
        {
            if (_bridge == null || _session == null) return;
            int pid = ResolvePendingPlayerId(_session, _bridge);
            if (pid < 0) return;
            int handLimit = PlayerLimitService.GetHandLimit(_session, _session.Players[pid]);
            if (_handIds.Count > handLimit) return;
            if (_bridge.TrySubmit(new CommuneCommand(pid, _spreadIds, _handIds)))
            {
                _communeSubviewOpen = false;
                _communeInitialized = false;
            }
        }

        const float RollSpinSec = 2f;

        IEnumerator DoRollZodiac()
        {
            if (_rolling || _bridge == null || _session == null || _loop == null)
                yield break;

            var hint = _loop.PendingHint;
            if (hint != ActionHint.RollZodiac)
                yield break;

            int pid = ResolvePendingPlayerId(_session, _bridge);
            if (pid < 0)
                yield break;

            _rolling = true;
            Btn("wheel-action-btn")?.SetEnabled(false);
            if (Lbl("harvest-count") != null)
                Lbl("harvest-count")!.text = "Rolling...";

            if (!_bridge.TrySubmit(new RollZodiacCommand(pid)))
            {
                _rolling = false;
                BindCtas(_bridge, _loop, hint, isHub: false, showCommune: false);
                yield break;
            }

            yield return UiMotion.SpinZodiacWheel(El("wheel-zodiac"), RollSpinSec);

            _justFinishedRollSpin = true;
            _rolling = false;
            _wheelBindKey = int.MinValue;

            var player = _session.Players[_localPlayerId];
            BindWheelAndHarvest(_session, player, _bridge.PendingHint);
            BindHintLabel(_bridge.PendingHint, _bridge, player, isHub: false, showCommune: false);
            BindCtas(_bridge, _loop, _bridge.PendingHint, isHub: false, showCommune: false);
            _justFinishedRollSpin = false;
        }

        void BindCtas(CommandBridge bridge, GameLoop loop, ActionHint hint, bool isHub, bool showCommune)
        {
            if (showCommune)
            {
                BindCommuneCtas(bridge);
                return;
            }

            if (isHub)
                BindHubCtas(bridge, loop);
            else
                BindWheelCta(bridge, loop, hint);
        }

        void BindWheelCta(CommandBridge bridge, GameLoop loop, ActionHint hint)
        {
            var btn = Btn("wheel-action-btn");
            if (btn == null) return;

            bool canAct = bridge.CanSubmit && loop.PendingHumanController != null;

            if (_rolling)
            {
                btn.text = "Roll Your Zodiac Die";
                btn.SetEnabled(false);
                btn.EnableInClassList("btn--disabled", true);
                return;
            }

            if (!canAct || hint is not (ActionHint.RollZodiac or ActionHint.AcknowledgeSign))
            {
                btn.SetEnabled(false);
                btn.EnableInClassList("btn--disabled", true);
                return;
            }

            switch (hint)
            {
                case ActionHint.RollZodiac:
                    btn.text = "Roll Your Zodiac Die";
                    btn.SetEnabled(true);
                    btn.EnableInClassList("btn--disabled", false);
                    break;
                case ActionHint.AcknowledgeSign:
                    btn.text = "Continue To Harvest";
                    btn.SetEnabled(true);
                    btn.EnableInClassList("btn--disabled", false);
                    break;
            }
        }

        void BindHubCtas(CommandBridge bridge, GameLoop loop)
        {
            bool canAct = bridge.CanSubmit && loop.PendingHumanController != null;

            var reviewBtn = Btn("review-tableau-btn");
            var proceedBtn = Btn("proceed-btn");
            var tableBtn = Btn("consult-table-btn");
            var zodiacBtn = Btn("consult-zodiac-btn");
            var crucibleBtn = Btn("consult-crucible-btn");

            reviewBtn?.SetEnabled(canAct);
            reviewBtn?.EnableInClassList("btn--disabled", !canAct);
            proceedBtn?.SetEnabled(canAct);
            proceedBtn?.EnableInClassList("btn--disabled", !canAct);
            tableBtn?.SetEnabled(canAct);
            tableBtn?.EnableInClassList("btn--disabled", !canAct);
            zodiacBtn?.SetEnabled(canAct);
            zodiacBtn?.EnableInClassList("btn--disabled", !canAct);
            crucibleBtn?.SetEnabled(canAct);
            crucibleBtn?.EnableInClassList("btn--disabled", !canAct);
        }

        void BindCommuneCtas(CommandBridge bridge)
        {
            var reviewBtn = Btn("review-effects-btn");
            reviewBtn?.SetEnabled(true);
            reviewBtn?.EnableInClassList("btn--disabled", false);
            BindCommuneLockCta(bridge);
        }

        void BindCommuneLockCta(CommandBridge bridge)
        {
            var btn = Btn("commune-lock-btn");
            if (btn == null || _session == null) return;

            int pid = ResolvePendingPlayerId(_session, bridge);
            int handLimit = pid >= 0
                ? PlayerLimitService.GetHandLimit(_session, _session.Players[pid])
                : WinterRules.HandLimit;
            bool valid = _handIds.Count <= handLimit;
            bool canAct = bridge.CanSubmit;
            btn.text = "Lock The Tableau";
            btn.SetEnabled(canAct && valid);
            btn.EnableInClassList("btn--disabled", !canAct || !valid);
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
            int bindKey = WheelBindKey(player.PlayerId, sign, hint);

            BindWheelGlyph(sign);

            if (bindKey == _wheelBindKey)
            {
                UpdateWheelResultText(session, player);
                UpdateHarvestLabel(session, player, hint);
                return;
            }

            _wheelBindKey = bindKey;

            if (sign == ZodiacSign.None)
            {
                SetLabelVisible("rolled-sign", false);
                UpdateWheelResultText(session, player);
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

            UpdateWheelResultText(session, player);
            UpdateHarvestLabel(session, player, hint);
        }

        void UpdateWheelResultText(GameSession session, PlayerState player)
        {
            var chargeLbl = El("wheel-stage")?.Q<Label>("narrative-charge");
            if (chargeLbl == null)
                return;

            if (_rolling || player.CurrentSign == ZodiacSign.None)
            {
                SetLabelVisible("sign-match", false);
                return;
            }

            var cosmic = session.Board.CosmicAgeSign;
            chargeLbl.text = DescribeAlignment(player.CurrentSign, cosmic);
            chargeLbl.style.display = DisplayStyle.Flex;
            SetLabelVisible("sign-match", false);
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

            if (hint == ActionHint.AcknowledgeSign)
            {
                SetLabelVisible("harvest-count", false);
                return;
            }

            SetLabelVisible("harvest-count", true);

            if (player.CurrentSign == ZodiacSign.None)
            {
                Lbl("harvest-count")!.text = hint == ActionHint.RollZodiac
                    ? "Awaiting your roll"
                    : "Awaiting harvest";
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
                return $"Your meeple moves to {playerSign} · sign match · +{bonus} alignment";
            if (bonus == 2)
                return $"Your meeple moves to {playerSign} · planet match · +{bonus} alignment";
            if (bonus == 1)
                return $"Your meeple moves to {playerSign} · element match · +{bonus} alignment";
            return $"Your meeple moves to {playerSign} · no aspect match";
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
