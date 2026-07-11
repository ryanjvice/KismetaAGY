using System;
using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Rules;
using Kismeta.UI.Components;
using Kismeta.UI.Narrative;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class SpringHarvestController : ScreenController
    {
        public enum HarvestUiPhase { Tally, Dealing, Commune }

        public override string ScreenId => ScreenIds.SpringHarvest;

        public System.Action<string>? OnInspectCard;
        public Action? NotifyCardAnimated { get; set; }

        GameSession? _session;
        GameLoop? _loop;
        CommandBridge? _bridge;
        int _playerId = -1;
        int _bindKey = int.MinValue;
        HarvestUiPhase _phase = HarvestUiPhase.Tally;

        readonly List<string> _spreadIds = new();
        readonly List<string> _handIds = new();

        bool _dealing;
        bool _communeInitialized;
        bool _communeZonesBuilt;
        VisualElement? _communeBuiltForRoot;
        int _communePlayerId = -1;

        protected override void Wire()
        {
            Btn("deal-btn")!.clicked += OnDeal;
            Btn("commune-lock-btn")!.clicked += OnCommuneLock;
        }

        protected override void Unwire()
        {
            _bindKey = int.MinValue;
            _dealing = false;
            _communeInitialized = false;
            _communeZonesBuilt = false;
            _communeBuiltForRoot = null;
            _communePlayerId = -1;
        }

        public HarvestUiPhase Phase => _phase;

        public void BindState(GameSession session, GameLoop loop, CommandBridge bridge)
        {
            _session = session;
            _loop = loop;
            _bridge = bridge;
            _playerId = ResolvePlayerId(session, bridge);
            if (Root == null || _playerId < 0) return;

            MainSceneBindings.ApplySeasonClass(Root, session.Phase.CurrentSeason);
            MainSceneBindings.BindStatusBar(Root, session, loop);
            MainSceneBindings.BindCosmicAgeBanner(Root, session);

            var hint = bridge.PendingHint;
            if (hint == ActionHint.HarvestCommune)
            {
                if (_phase != HarvestUiPhase.Commune)
                {
                    _phase = HarvestUiPhase.Commune;
                    _dealing = false;
                    _communeInitialized = false;
                    _communeZonesBuilt = false;
                }
            }
            else if (session.Board.ActiveHarvestDeal?.PlayerId == _playerId)
            {
                _phase = HarvestUiPhase.Dealing;
                _communeInitialized = false;
                _communeZonesBuilt = false;
            }
            else if (hint != ActionHint.ConfirmHarvest)
            {
                _phase = HarvestUiPhase.Tally;
                _communeInitialized = false;
                _communeZonesBuilt = false;
            }

            BindStepRail(session);
            BindPhaseVisibility();
            BindTableauHeading();

            int bindKey = ComputeBindKey(session, _playerId);
            if (_phase == HarvestUiPhase.Tally && bindKey != _bindKey)
            {
                _bindKey = bindKey;
                BindTally(session);
            }

            if (_phase == HarvestUiPhase.Dealing)
                RefreshDealingTableau(session);
            else if (_phase == HarvestUiPhase.Commune)
                BindCommuneTableau(session, bridge);
        }

        public void OnHarvestCardRouted(HarvestCardRoutedEvent routed)
        {
            if (_session == null || _playerId < 0 || routed.PlayerId != _playerId) return;
            if (_phase != HarvestUiPhase.Dealing) return;

            var tableau = El("harvest-tableau");
            TableauBindings.AppendRoutedCard(tableau, _session, routed, _session.Board.CosmicAgeSign, OnInspectCard);

            if (routed.Target == HarvestRouteTarget.DiscardedDuplicate)
                return;

            NotifyCardAnimated?.Invoke();
        }

        public void OnHarvestHandsCleared(HarvestHandsClearedEvent evt)
        {
            if (_session == null || _playerId < 0) return;
            if (_phase != HarvestUiPhase.Dealing) return;
            TableauBindings.ClearHandZone(El("harvest-tableau"), _session, _playerId, _session.Board.CosmicAgeSign);
        }

        public void OnFateResolvedDuringDeal(FateResolvedEvent fate)
        {
            if (_session == null || _playerId < 0 || fate.PlayerId != _playerId) return;
            if (_phase != HarvestUiPhase.Dealing) return;
            RefreshTableau(_session);
        }

        void BindTally(GameSession session)
        {
            HarvestCompareBindings.Bind(Root, session, _playerId);
            var breakdown = HarvestBreakdownService.Build(session, _playerId);

            if (Lbl("harvest-summary") != null)
                Lbl("harvest-summary")!.text = $"BASE {breakdown.Base} + BONUS {breakdown.BonusSubtotal} + BOON {breakdown.Boon}";

            if (Lbl("harvest-total-line") != null)
                Lbl("harvest-total-line")!.text = $"{breakdown.Total} cards this harvest";

            HarvestSourceRows.Populate(El("source-list"), breakdown.Sources);

            if (Lbl("deal-cta") != null)
                Lbl("deal-cta")!.text = $"→ Deal {breakdown.Total} cards";

            var dealBtn = Btn("deal-btn");
            if (dealBtn != null && _bridge != null)
            {
                bool canDeal = _bridge.CanSubmit && _bridge.PendingHint == ActionHint.ConfirmHarvest;
                dealBtn.SetEnabled(canDeal);
                dealBtn.EnableInClassList("btn--disabled", !canDeal);
            }
        }

        void BindPhaseVisibility()
        {
            El("harvest-tally")?.EnableInClassList("harvest-tableau--hidden", _phase != HarvestUiPhase.Tally);
            El("harvest-tableau")?.EnableInClassList("harvest-tableau--hidden", _phase == HarvestUiPhase.Tally);

            var lockBtn = Btn("commune-lock-btn");
            if (lockBtn != null)
                lockBtn.style.display = _phase == HarvestUiPhase.Commune ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void BindTableauHeading()
        {
            var heading = Root?.Q<Label>("tableau-heading");
            if (heading == null) return;
            heading.text = _phase switch
            {
                HarvestUiPhase.Dealing => "Receiving your harvest",
                HarvestUiPhase.Commune => "Commune with your Tableau",
                _ => "Commune with your Tableau"
            };
        }

        void BindStepRail(GameSession session)
        {
            int step = _phase switch
            {
                HarvestUiPhase.Tally => 2,
                HarvestUiPhase.Dealing => 2,
                HarvestUiPhase.Commune => 3,
                _ => 2
            };
            MainSceneBindings.BindStepRail(El("step-rail"), step, 5, "step__dot--active");
        }

        void BindNarrative()
        {
            var stepId = _phase switch
            {
                HarvestUiPhase.Commune => "spring.commune",
                HarvestUiPhase.Dealing => "spring.harvest.deal",
                _ => "spring.harvest"
            };
            NarrativeSlotBindings.BindById(Root, stepId);
        }

        void BindCommuneTableau(GameSession session, CommandBridge bridge)
        {
            if (_playerId != _communePlayerId)
            {
                _communePlayerId = _playerId;
                _communeInitialized = false;
                _communeZonesBuilt = false;
            }

            if (!_communeInitialized)
                SeedCommuneFromPlayer(session);

            RenderCommuneZonesIfNeeded(session);
            BindCommuneLockCta(bridge);
            BindNarrative();
        }

        void RefreshDealingTableau(GameSession session)
        {
            var tableau = El("harvest-tableau");
            if (tableau == null) return;
            TableauBindings.RebuildDealingZones(tableau, session, _playerId, session.Board.CosmicAgeSign, OnInspectCard);
        }

        void RenderCommuneZonesIfNeeded(GameSession session)
        {
            var tableau = El("harvest-tableau");
            if (tableau == null) return;
            if (_communeZonesBuilt && ReferenceEquals(tableau, _communeBuiltForRoot)) return;
            RefreshCommuneZones(session);
        }

        void RefreshCommuneZones(GameSession session)
        {
            var tableau = El("harvest-tableau");
            if (tableau == null) return;

            TableauBindings.RebuildCommuneZones(tableau, session, _playerId, _spreadIds, _handIds,
                session.Board.CosmicAgeSign, OnCommuneTapMove, OnInspectCard);
            _communeZonesBuilt = true;
            _communeBuiltForRoot = tableau;
        }

        void RefreshTableau(GameSession session)
        {
            if (_phase == HarvestUiPhase.Commune)
                RefreshCommuneZones(session);
            else
                RefreshDealingTableau(session);
        }

        void SeedCommuneFromPlayer(GameSession session)
        {
            _spreadIds.Clear();
            _handIds.Clear();
            var player = session.Players[_playerId];
            foreach (var id in player.Spread)
                if (TapSwapBindings.IsMinorArcana(session, id)) _spreadIds.Add(id);
            foreach (var id in player.Hand)
                if (TapSwapBindings.IsMinorArcana(session, id)) _handIds.Add(id);
            _communeInitialized = true;
            _communeZonesBuilt = false;
        }

        void OnCommuneTapMove(string cardId, bool fromSpread)
        {
            if (_phase != HarvestUiPhase.Commune || _session == null) return;

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

            RefreshCommuneZones(_session);
            BindCommuneLockCta(_bridge!);
        }

        void BindCommuneLockCta(CommandBridge bridge)
        {
            var btn = Btn("commune-lock-btn");
            if (btn == null || _session == null) return;

            int handLimit = PlayerLimitService.GetHandLimit(_session, _session.Players[_playerId]);
            bool overLimit = _handIds.Count > handLimit;
            btn.SetEnabled(bridge.CanSubmit && _phase == HarvestUiPhase.Commune && !overLimit);
        }

        void OnDeal()
        {
            if (_bridge == null || _playerId < 0 || _dealing) return;
            _bridge.TrySubmit(new BeginHarvestCommand(_playerId));
        }

        public void BeginDealingUi()
        {
            if (_session == null) return;
            _phase = HarvestUiPhase.Dealing;
            _dealing = true;
            _communeInitialized = false;
            _communeZonesBuilt = false;
            BindPhaseVisibility();
            BindTableauHeading();
            BindStepRail(_session);
            BindNarrative();
            RefreshTableau(_session);
        }

        public void EnterCommuneUi()
        {
            if (_session == null) return;
            _phase = HarvestUiPhase.Commune;
            _dealing = false;
            _communeInitialized = false;
            _communeZonesBuilt = false;
            BindPhaseVisibility();
            BindTableauHeading();
            BindStepRail(_session);
            BindCommuneTableau(_session, _bridge!);
        }

        void OnCommuneLock()
        {
            if (_bridge == null || _session == null || _phase != HarvestUiPhase.Commune) return;

            int handLimit = PlayerLimitService.GetHandLimit(_session, _session.Players[_playerId]);
            if (_handIds.Count > handLimit) return;

            _bridge.TrySubmit(new CommuneCommand(_playerId, _spreadIds, _handIds));
        }

        static int ResolvePlayerId(GameSession session, CommandBridge bridge)
        {
            int pid = bridge.ActivePlayerId;
            if (pid >= 0 && pid < session.Players.Count)
                return pid;

            var hs = bridge.PendingController;
            if (hs != null && hs.Slot.Index >= 0 && hs.Slot.Index < session.Players.Count)
                return hs.Slot.Index;

            return -1;
        }

        static int ComputeBindKey(GameSession session, int playerId)
        {
            var player = session.Players[playerId];
            int spreadHash = 0;
            foreach (var id in player.Spread)
                spreadHash = spreadHash * 31 + id.GetHashCode();
            return playerId * 1000
                   + (int)session.Board.CosmicAgeSign * 10
                   + (int)player.CurrentSign
                   + spreadHash;
        }
    }
}
