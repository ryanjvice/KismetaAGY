using System;
using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Rules;
using Kismeta.UI.Components;
using Kismeta.UI.Narrative;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class SpringHarvestController : ScreenController
    {
        public enum HarvestUiPhase { Tally, Dealing }

        public override string ScreenId => ScreenIds.SpringHarvest;

        public System.Action<string>? OnInspectCard;
        public Action? NotifyCardAnimated { get; set; }

        GameSession? _session;
        GameLoop? _loop;
        CommandBridge? _bridge;
        int _playerId = -1;
        int _bindKey = int.MinValue;
        HarvestUiPhase _phase = HarvestUiPhase.Tally;

        bool _dealing;

        protected override void Wire()
        {
            Btn("deal-btn")!.clicked += OnDeal;
        }

        protected override void Unwire()
        {
            _bindKey = int.MinValue;
            _dealing = false;
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

            if (session.Board.ActiveHarvestDeal?.PlayerId == _playerId)
                _phase = HarvestUiPhase.Dealing;
            else if (bridge.PendingHint == ActionHint.ConfirmHarvest)
                _phase = HarvestUiPhase.Tally;

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

            BindNarrative();
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
            RefreshDealingTableau(_session);
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
        }

        void BindTableauHeading()
        {
            var heading = Root?.Q<Label>("tableau-heading");
            if (heading == null) return;
            heading.text = _phase == HarvestUiPhase.Dealing
                ? "Receiving your harvest"
                : "Receiving your harvest";
        }

        void BindStepRail(GameSession session)
        {
            int step = _phase == HarvestUiPhase.Dealing ? 2 : 2;
            MainSceneBindings.BindStepRail(El("step-rail"), step, 5, "step__dot--active");
        }

        void BindNarrative()
        {
            var stepId = _phase == HarvestUiPhase.Dealing ? "spring.harvest.deal" : "spring.harvest";
            NarrativeSlotBindings.BindById(Root, stepId);
        }

        void RefreshDealingTableau(GameSession session)
        {
            var tableau = El("harvest-tableau");
            if (tableau == null) return;
            TableauBindings.RebuildDealingZones(tableau, session, _playerId, session.Board.CosmicAgeSign, OnInspectCard);
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
            BindPhaseVisibility();
            BindTableauHeading();
            BindStepRail(_session);
            BindNarrative();
            RefreshDealingTableau(_session);
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
