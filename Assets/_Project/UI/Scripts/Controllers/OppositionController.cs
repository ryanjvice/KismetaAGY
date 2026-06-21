using System;
using System.Collections;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class OppositionController : ContestController
    {
        static readonly string[] Panels = { "step-target", "step-fee", "step-roll", "step-tally", "stasis-result" };

        readonly ReagentStepper _fee = new();
        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;
        int _rivalId = -1;
        bool _rolling;
        bool _resolving;

        public System.Action? OnBack;
        public System.Action? OnCompleted;

        protected override void Wire()
        {
            Btn("back-btn")!.clicked += () => OnBack?.Invoke();
            Btn("target-next")!.clicked += OnTargetNext;
            Btn("o-fee-next")!.clicked += OnFeeNext;
            Btn("o-roll-btn")!.clicked += OnRollAdvance;
            Btn("o-resolve-btn")!.clicked += () => { if (!_resolving) StartCoroutine(DoResolve()); };
            Btn("stasis-done-btn")!.clicked += () => OnCompleted?.Invoke();
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _playerId = SummerActionBindings.ResolvePlayerId(session, bridge);
            _rivalId = -1;
            _rolling = false;
            _resolving = false;
            _fee.Reset();

            if (Root == null || _playerId < 0) return;

            ShowStep("step-target", Panels);
            SetWizard("wo", 4, 1);
            El("stasis-result")!.style.display = DisplayStyle.None;
            RefreshTargetStep();
        }

        void RefreshTargetStep()
        {
            if (_session == null || Root == null) return;
            ContestBindings.SetContestTitle(Root, "Oppose", _rivalId, _session);
            ContestBindings.BuildRivalChips(El("rival-cards"), _session, _playerId,
                rival => ContestBindings.CanTargetForOpposition(_session, _playerId, rival.PlayerId),
                _rivalId, id =>
                {
                    _rivalId = id;
                    RefreshTargetStep();
                });
            Btn("target-next")?.SetEnabled(_rivalId >= 0);
        }

        void OnTargetNext()
        {
            if (_rivalId < 0 || _session == null || _playerId < 0) return;
            int cap = _session.Players[_rivalId].StoneWardCount;
            if (cap == 0)
            {
                ShowStep("step-roll", Panels);
                SetWizard("wo", 4, 3);
                Lbl("die-set-pip")!.text = "?";
                return;
            }

            int have = ContestBindings.TotalReagents(_session.Players[_playerId]);
            if (have < cap) return;

            ShowStep("step-fee", Panels);
            SetWizard("wo", 4, 2);
            RefreshFeeStep(cap);
        }

        void RefreshFeeStep(int cap)
        {
            if (_session == null || _playerId < 0) return;
            _fee.Reset();
            _fee.Cap = cap;

            var player = _session.Players[_playerId];
            foreach (ReagentType rt in Enum.GetValues(typeof(ReagentType)))
                _fee.SetHave(rt.ToString(), player.GetReagent(rt));

            if (Lbl("fee-count") != null)
                Lbl("fee-count")!.text = cap == 0 ? "no ward fee" : $"0 / {cap}";

            BuildFeeRows(cap);
            RefreshFeeBtn(cap);
        }

        void BuildFeeRows(int cap)
        {
            var host = El("fee-supply");
            if (host == null || _session == null || _playerId < 0) return;
            host.Clear();

            var player = _session.Players[_playerId];
            foreach (ReagentType rt in Enum.GetValues(typeof(ReagentType)))
            {
                int have = player.GetReagent(rt);
                if (have <= 0 && cap > 0) continue;
                host.Add(BuildFeeRow(rt, have, cap));
            }
        }

        VisualElement BuildFeeRow(ReagentType rt, int have, int cap)
        {
            string key = rt.ToString();
            var row = new VisualElement();
            row.AddToClassList("stepper-row");

            var dot = new VisualElement();
            dot.AddToClassList("reagent-dot");
            dot.AddToClassList($"reagent-dot--{key.ToLowerInvariant()}");
            row.Add(dot);

            row.Add(new Label(key) { style = { flexGrow = 1, fontSize = 12 } });

            var minus = new Button { name = $"o-fee-{key.ToLowerInvariant()}-minus" };
            minus.AddToClassList("stepper-btn");
            minus.AddToClassList("stepper-btn--minus");
            minus.clicked += () => { if (_fee.TryRemove(key)) RefreshFeeValues(cap); };
            row.Add(minus);

            var val = new Label("0") { name = $"o-fee-{key.ToLowerInvariant()}-val" };
            val.AddToClassList("stepper-value");
            row.Add(val);

            var plus = new Button { name = $"o-fee-{key.ToLowerInvariant()}-plus" };
            plus.AddToClassList("stepper-btn");
            plus.AddToClassList("stepper-btn--plus");
            plus.clicked += () => { if (_fee.TryAdd(key)) RefreshFeeValues(cap); };
            row.Add(plus);

            return row;
        }

        void RefreshFeeValues(int cap)
        {
            if (Root == null) return;
            foreach (ReagentType rt in Enum.GetValues(typeof(ReagentType)))
            {
                var val = Lbl($"o-fee-{rt.ToString().ToLowerInvariant()}-val");
                if (val != null) val.text = _fee.Of(rt.ToString()).ToString();
            }
            if (Lbl("o-fee-val") != null)
                Lbl("o-fee-val")!.text = _fee.Of(ReagentType.Vitriol.ToString()).ToString();
            if (Lbl("fee-count") != null)
                Lbl("fee-count")!.text = cap == 0 ? "no ward fee" : $"{_fee.Total} / {cap}";
            RefreshFeeBtn(cap);
        }

        void RefreshFeeBtn(int cap)
        {
            var btn = Btn("o-fee-next");
            if (btn == null) return;
            bool ok = cap == 0 || _fee.Total >= cap;
            if (ok)
            {
                btn.RemoveFromClassList("btn--disabled");
                btn.AddToClassList("btn--primary");
                btn.text = "Continue";
                btn.SetEnabled(true);
            }
            else
            {
                btn.AddToClassList("btn--disabled");
                btn.text = $"Pay {cap - _fee.Total} more";
                btn.SetEnabled(false);
            }
        }

        void OnFeeNext()
        {
            ShowStep("step-roll", Panels);
            SetWizard("wo", 4, 3);
            Lbl("die-set-pip")!.text = "?";
            if (Lbl("set-sign") != null)
                Lbl("set-sign")!.style.display = DisplayStyle.None;
        }

        void OnRollAdvance()
        {
            if (_rolling) return;
            StartCoroutine(ShowTallyPreview());
        }

        IEnumerator ShowTallyPreview()
        {
            _rolling = true;
            Btn("o-roll-btn")?.SetEnabled(false);

            if (_session != null)
            {
                var cosmic = _session.Board.CosmicAgeSign;
                yield return RollDie(Lbl("die-set-pip"), UnityEngine.Random.Range(1, 13));
                if (Lbl("set-sign") != null)
                {
                    Lbl("set-sign")!.style.display = DisplayStyle.Flex;
                    Lbl("set-sign")!.text = cosmic == ZodiacSign.None
                        ? "Cosmic age sign pending"
                        : $"Age sign: {cosmic}";
                }
            }

            ShowStep("step-tally", Panels);
            SetWizard("wo", 4, 4);
            if (_session != null && _playerId >= 0 && _rivalId >= 0)
                ContestAlignmentRows.Populate(Root!, _session, _playerId, _rivalId);

            _rolling = false;
        }

        IEnumerator DoResolve()
        {
            if (_session == null || _bridge == null || _playerId < 0 || _rivalId < 0)
                yield break;

            _resolving = true;
            Btn("o-resolve-btn")?.SetEnabled(false);

            OppositionResolvedEvent? resolved = null;
            _bridge.TrySubmit(new InitiateOppositionCommand(_playerId, _rivalId));
            yield return WaitForEvent(_session, (OppositionResolvedEvent e) => resolved = e);

            if (resolved != null)
            {
                bool won = resolved.LoserId == _rivalId;
                if (won)
                {
                    ShowStep("stasis-result", Panels);
                    var banner = Root?.Q(className: "verdict-banner");
                    if (banner != null)
                    {
                        var sub = banner.Q<Label>();
                        if (sub != null && sub.text.Contains("stone"))
                            sub.text = $"{ContestBindings.RivalName(_session, _rivalId)}'s stone is sent to Stasis";
                    }
                }
                else
                {
                    yield return new WaitForSeconds(0.8f);
                    OnCompleted?.Invoke();
                }
            }
            else
                Btn("o-resolve-btn")?.SetEnabled(true);

            _resolving = false;
        }
    }
}
