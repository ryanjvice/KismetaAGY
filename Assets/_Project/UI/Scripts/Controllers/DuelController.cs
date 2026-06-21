using System.Collections;
using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Entities;
using Kismeta.UI.Components;
using Kismeta.UI.Controllers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class DuelController : ContestController
    {
        static readonly string[] Panels = { "step-target", "step-ante", "step-roll" };

        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;
        int _rivalId = -1;
        int? _preselectedRival;
        readonly HashSet<string> _ante = new();
        bool _rolling;

        public System.Action? OnBack;
        public System.Action? OnCompleted;

        protected override void Wire()
        {
            Btn("back-btn")!.clicked += () => OnBack?.Invoke();
            Btn("target-next")!.clicked += OnTargetNext;
            Btn("ante-next")!.clicked += OnAnteNext;
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
            _ante.Clear();
            _rolling = false;

            if (Root == null || _playerId < 0) return;

            if (_rivalId >= 0)
            {
                ShowStep("step-ante", Panels);
                SetWizard("wd", 3, 2);
                RefreshAnteStep();
            }
            else
            {
                ShowStep("step-target", Panels);
                SetWizard("wd", 3, 1);
                RefreshTargetStep();
            }
            Lbl("roll-outcome")!.style.display = DisplayStyle.None;
        }

        void RefreshTargetStep()
        {
            if (_session == null || Root == null) return;
            ContestBindings.SetContestTitle(Root, "Duel", _rivalId, _session);
            ContestBindings.BuildRivalChips(El("rival-cards"), _session, _playerId,
                ContestBindings.RivalEligibleForSummerContest, _rivalId, id =>
                {
                    _rivalId = id;
                    RefreshTargetStep();
                });

            var next = Btn("target-next");
            if (next != null)
                next.SetEnabled(_rivalId >= 0);
        }

        void OnTargetNext()
        {
            if (_rivalId < 0) return;
            ShowStep("step-ante", Panels);
            SetWizard("wd", 3, 2);
            RefreshAnteStep();
        }

        void RefreshAnteStep()
        {
            if (_session == null || _playerId < 0) return;
            var player = _session.Players[_playerId];
            var cards = ContestBindings.MinorSpreadCards(_session, player);
            ContestBindings.BuildCardChips(El("ante-cards"), _session, cards, _ante, false, _ =>
            {
                RefreshAnteStep();
                Btn("ante-next")?.SetEnabled(_ante.Count > 0);
            });
            Btn("ante-next")?.SetEnabled(_ante.Count > 0);
        }

        void OnAnteNext()
        {
            if (_ante.Count == 0) return;
            ShowStep("step-roll", Panels);
            SetWizard("wd", 3, 3);
            Lbl("die-you-pip")!.text = "?";
            Lbl("die-foe-pip")!.text = "?";
            Lbl("roll-outcome")!.style.display = DisplayStyle.None;
        }

        IEnumerator DoRoll()
        {
            if (_session == null || _bridge == null || _playerId < 0 || _rivalId < 0 || _ante.Count == 0)
                yield break;

            _rolling = true;
            Btn("roll-btn")?.SetEnabled(false);

            DuelResolvedEvent? resolved = null;
            var cmd = new InitiateDuelCommand(_playerId, _rivalId, GetFirstAnte());
            _bridge.TrySubmit(cmd);

            yield return WaitForEvent(_session, (DuelResolvedEvent e) => resolved = e);

            if (resolved != null)
            {
                yield return RollDie(Lbl("die-you-pip"), resolved.AttackRoll);
                yield return RollDie(Lbl("die-foe-pip"), resolved.DefendRoll);
                bool won = resolved.WinnerId == _playerId;
                var outcome = Lbl("roll-outcome");
                if (outcome != null)
                {
                    outcome.style.display = DisplayStyle.Flex;
                    outcome.text = won
                        ? $"You win ({resolved.AttackRoll} vs {resolved.DefendRoll}) — keep the ante."
                        : $"You lose ({resolved.AttackRoll} vs {resolved.DefendRoll}) — rival takes your ante.";
                }
                yield return new WaitForSeconds(1.2f);
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

        string GetFirstAnte()
        {
            foreach (var id in _ante) return id;
            return "";
        }
    }
}
