using System.Collections;
using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Entities;
using Kismeta.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class GambitController : ContestController
    {
        static readonly string[] Panels = { "step-target", "step-fee", "step-stake", "step-roll" };

        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;
        int _rivalId = -1;
        int? _preselectedRival;
        readonly HashSet<string> _stake = new();
        bool _rolling;

        public System.Action? OnBack;
        public System.Action? OnCompleted;

        protected override void Wire()
        {
            Btn("back-btn")!.clicked += () => OnBack?.Invoke();
            Btn("g-target-next")!.clicked += OnTargetNext;
            Btn("g-fee-next")!.clicked += OnFeeNext;
            Btn("g-stake-next")!.clicked += OnStakeNext;
            Btn("g-roll-btn")!.clicked += () => { if (!_rolling) StartCoroutine(DoRoll()); };
        }

        public void SetPreselectedRival(int? rivalId) => _preselectedRival = rivalId;

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _playerId = SummerActionBindings.ResolvePlayerId(session, bridge);
            _rivalId = _preselectedRival ?? -1;
            _preselectedRival = null;
            _stake.Clear();
            _rolling = false;

            if (Root == null || _playerId < 0) return;

            if (_rivalId >= 0)
            {
                int wards = _session.Players[_rivalId].StoneWardCount;
                if (wards > 0)
                {
                    ShowStep("step-fee", Panels);
                    SetWizard("wg", 4, 2);
                    RefreshFeeStep(wards);
                }
                else
                {
                    ShowStep("step-stake", Panels);
                    SetWizard("wg", 4, 3);
                    RefreshStakeStep();
                }
            }
            else
            {
                ShowStep("step-target", Panels);
                SetWizard("wg", 4, 1);
                RefreshTargetStep();
            }
        }

        void RefreshTargetStep()
        {
            if (_session == null || Root == null) return;
            ContestBindings.SetContestTitle(Root, "Gambit", _rivalId, _session);
            ContestBindings.BuildRivalChips(El("rival-cards"), _session, _playerId,
                ContestBindings.RivalEligibleForSummerContest, _rivalId, id =>
                {
                    _rivalId = id;
                    RefreshTargetStep();
                });

            var wardNote = Lbl("ward-note");
            if (wardNote != null && _rivalId >= 0)
            {
                int wards = _session.Players[_rivalId].StoneWardCount;
                wardNote.text = wards > 0
                    ? $"warded · rival pays {wards} reagent(s) to enter"
                    : "unwarded · no entry fee";
            }

            Btn("g-target-next")?.SetEnabled(_rivalId >= 0);
        }

        void OnTargetNext()
        {
            if (_rivalId < 0 || _session == null) return;
            int wards = _session.Players[_rivalId].StoneWardCount;
            if (wards > 0)
            {
                ShowStep("step-fee", Panels);
                SetWizard("wg", 4, 2);
                RefreshFeeStep(wards);
            }
            else
            {
                ShowStep("step-stake", Panels);
                SetWizard("wg", 4, 3);
                RefreshStakeStep();
            }
        }

        void RefreshFeeStep(int fee)
        {
            if (Lbl("fee-count") != null)
                Lbl("fee-count")!.text = $"rival pays {fee}";
            var next = Btn("g-fee-next");
            if (next != null)
            {
                next.RemoveFromClassList("btn--disabled");
                next.AddToClassList("btn--primary");
                next.text = "Continue";
                next.SetEnabled(true);
            }
        }

        void OnFeeNext()
        {
            ShowStep("step-stake", Panels);
            SetWizard("wg", 4, 3);
            RefreshStakeStep();
        }

        void RefreshStakeStep()
        {
            if (_session == null || _playerId < 0) return;
            var cards = ContestBindings.GambitStakeCards(_session, _playerId);
            ContestBindings.BuildCardChips(El("stake-cards"), _session, cards, _stake, false, _ =>
            {
                RefreshStakeStep();
                Btn("g-stake-next")?.SetEnabled(_stake.Count > 0);
            });
            Btn("g-stake-next")?.SetEnabled(_stake.Count > 0);
        }

        void OnStakeNext()
        {
            if (_stake.Count == 0) return;
            ShowStep("step-roll", Panels);
            SetWizard("wg", 4, 4);
            Lbl("die-you-pip")!.text = "?";
            Lbl("die-foe-pip")!.text = "?";
        }

        IEnumerator DoRoll()
        {
            if (_session == null || _bridge == null || _playerId < 0 || _rivalId < 0 || _stake.Count == 0)
                yield break;

            _rolling = true;
            Btn("g-roll-btn")?.SetEnabled(false);

            GambitResolvedEvent? resolved = null;
            string offered = "";
            foreach (var id in _stake) { offered = id; break; }

            _bridge.TrySubmit(new InitiateGambitCommand(_playerId, _rivalId, offered));
            yield return WaitForEvent(_session, (GambitResolvedEvent e) => resolved = e);

            if (resolved != null)
            {
                yield return RollDie(Lbl("die-you-pip"), resolved.AttackRoll);
                yield return RollDie(Lbl("die-foe-pip"), resolved.DefendRoll);
                yield return new WaitForSeconds(1.2f);
                OnCompleted?.Invoke();
            }
            else
                Btn("g-roll-btn")?.SetEnabled(true);

            _rolling = false;
        }
    }
}
