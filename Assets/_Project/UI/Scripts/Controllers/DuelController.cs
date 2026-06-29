using System.Collections;
using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Entities;
using Kismeta.UI.Components;
using Kismeta.UI.Narrative;
using Kismeta.UI.Controllers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class DuelController : ContestController
    {
        static readonly string[] Panels = { "step-setup", "step-roll" };

        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;
        int _rivalId = -1;
        int? _preselectedRival;
        readonly HashSet<string> _target = new();
        readonly HashSet<string> _ante = new();
        bool _rolling;

        public System.Action? OnBack;
        public System.Action? OnCompleted;

        protected override void Wire()
        {
            Btn("back-btn")!.clicked += () => OnBack?.Invoke();
            Btn("setup-next")!.clicked += OnSetupNext;
            Btn("roll-btn")!.clicked += () => { if (!_rolling) StartCoroutine(DoRoll()); };
        }

        public void SetPreselectedRival(int? rivalId) => _preselectedRival = rivalId;

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _playerId = SummerActionBindings.ResolvePlayerId(session, bridge);
            _rivalId = _preselectedRival ?? -1;
            _preselectedRival = null;
            _target.Clear();
            _ante.Clear();
            _rolling = false;

            if (Root == null || _playerId < 0) return;

            if (_rivalId < 0)
                _rivalId = ContestBindings.FirstEligibleRival(_session, _playerId,
                    ContestBindings.RivalEligibleForSummerContest);

            ShowStep("step-setup", Panels);
            SetWizard("wd", 2, 1);
            RefreshSetup();
            Lbl("roll-outcome")!.style.display = DisplayStyle.None;
            NarrativeSlotBindings.BindById(Root, "summer.duel");
        }

        void RefreshSetup()
        {
            if (_session == null || Root == null || _playerId < 0) return;

            ContestBindings.SetContestTitle(Root, "Duel", _rivalId, _session);
            ContestBindings.BuildRivalChips(El("rival-strip"), _session, _playerId,
                ContestBindings.RivalEligibleForSummerContest, _rivalId, id =>
                {
                    _rivalId = id;
                    _target.Clear();
                    RefreshSetup();
                });

            var targetTitle = Lbl("target-tray-title");
            if (targetTitle != null)
            {
                targetTitle.text = _rivalId >= 0
                    ? $"{ContestBindings.RivalName(_session, _rivalId)} spread — pick a card to win"
                    : "Rival spread — pick a card to win";
            }

            var targetCards = _rivalId >= 0
                ? ContestBindings.PublicRivalSpread(_session, _rivalId)
                : new List<string>();
            ContestBindings.BuildCardChips(El("target-cards"), _session, targetCards, _target, false,
                _ => RefreshSetup());

            var emptyMsg = Lbl("target-empty-msg");
            if (emptyMsg != null)
            {
                bool showEmpty = _rivalId >= 0 && _rivalId < _session.Players.Count && targetCards.Count == 0;
                emptyMsg.style.display = showEmpty ? DisplayStyle.Flex : DisplayStyle.None;
                if (showEmpty)
                    emptyMsg.text = $"{ContestBindings.RivalName(_session, _rivalId)} has no public spread cards.";
            }

            var player = _session.Players[_playerId];
            var anteCards = ContestBindings.MinorSpreadCards(_session, player);
            ContestBindings.BuildCardChips(El("ante-cards"), _session, anteCards, _ante, false,
                _ => RefreshSetup());

            var next = Btn("setup-next");
            if (next != null)
            {
                bool ready = _rivalId >= 0 && _target.Count == 1 && _ante.Count == 1;
                next.SetEnabled(ready);
            }
        }

        void OnSetupNext()
        {
            if (_rivalId < 0 || _target.Count == 0 || _ante.Count == 0) return;
            ShowStep("step-roll", Panels);
            SetWizard("wd", 2, 2);
            Lbl("die-you-pip")!.text = "?";
            Lbl("die-foe-pip")!.text = "?";
            Lbl("roll-outcome")!.style.display = DisplayStyle.None;

            var foeName = Lbl("die-foe-name");
            if (foeName != null && _session != null)
                foeName.text = ContestBindings.RivalName(_session, _rivalId);
        }

        IEnumerator DoRoll()
        {
            if (_session == null || _bridge == null || _playerId < 0 || _rivalId < 0
                || _target.Count == 0 || _ante.Count == 0)
                yield break;

            _rolling = true;
            Btn("roll-btn")?.SetEnabled(false);

            DuelResolvedEvent? resolved = null;
            var cmd = new InitiateDuelCommand(_playerId, _rivalId, GetFirst(_target), GetFirst(_ante));
            _bridge.TrySubmit(cmd);

            yield return WaitForEvent(_session, (DuelResolvedEvent e) => resolved = e);

            if (resolved != null)
            {
                yield return RollDie(Lbl("die-you-pip"), resolved.AttackRoll);
                yield return RollDie(Lbl("die-foe-pip"), resolved.DefendRoll);
                OnCompleted?.Invoke();
            }
            else
            {
                var outcome = Lbl("roll-outcome");
                if (outcome != null)
                {
                    outcome.style.display = DisplayStyle.Flex;
                    outcome.text = "Duel could not resolve — try again.";
                }
                Btn("roll-btn")?.SetEnabled(true);
            }

            _rolling = false;
        }

        static string GetFirst(HashSet<string> set)
        {
            foreach (var id in set) return id;
            return "";
        }
    }
}
