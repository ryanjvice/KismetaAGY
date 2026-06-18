using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.UI;
using Kismeta.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class PlaceWardsController : OverlayController
    {
        readonly ReagentStepper _stepper = new() { Cap = 1 };

        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;
        ReagentType _reagentType = ReagentType.Salt;
        int _pendingSlot = -1;

        public System.Action OnBack;
        public System.Action? OnCompleted;

        protected override void Wire()
        {
            Btn("back-btn")!.clicked += () => OnBack?.Invoke();
            Btn("seal-btn")!.clicked += OnSeal;
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _playerId = SummerActionHelpers.ResolvePlayerId(session, bridge);
            if (_playerId < 0 || Root == null) return;

            var player = session.Players[_playerId];
            _stepper.Reset();
            if (_pendingSlot >= 0)
                _stepper.TryAdd($"slot-{_pendingSlot}");

            RebuildReagentPicker(player);
            RebuildSlotRows(player);
            RefreshSealButton(player);
        }

        void RebuildReagentPicker(PlayerState player)
        {
            var picker = El("reagent-type-picker");
            if (picker == null)
            {
                var supplySection = Root?.Q(className: "eyebrow");
                if (supplySection?.parent == null) return;
                picker = new VisualElement { name = "reagent-type-picker" };
                picker.style.flexDirection = FlexDirection.Row;
                picker.style.flexWrap = Wrap.Wrap;
                picker.style.marginTop = 6;
                supplySection.parent.Insert(supplySection.parent.IndexOf(supplySection) + 1, picker);
            }
            else
            {
                picker.Clear();
            }

            bool any = false;
            foreach (ReagentType rt in System.Enum.GetValues(typeof(ReagentType)))
            {
                int have = player.GetReagent(rt);
                if (have <= 0) continue;
                any = true;
                var btn = new Button { text = $"{rt} ({have})" };
                btn.AddToClassList("reagent-pick");
                if (rt == _reagentType)
                    btn.AddToClassList("reagent-pick--active");
                var captured = rt;
                btn.clicked += () =>
                {
                    _reagentType = captured;
                    BindState(_session!, _bridge!);
                };
                picker.Add(btn);
            }

            if (!any)
            {
                var lbl = new Label("No reagents in supply.");
                lbl.style.fontSize = 10;
                picker.Add(lbl);
            }
        }

        void RebuildSlotRows(PlayerState player)
        {
            var listHost = El("ward-slot-list");
            if (listHost == null)
            {
                var eyebrow = Root?.Q<Label>(className: "eyebrow");
                while (eyebrow != null && eyebrow.text != "your active cards")
                    eyebrow = eyebrow.parent?.Q<Label>(className: "eyebrow");

                if (eyebrow?.parent == null) return;
                listHost = new VisualElement { name = "ward-slot-list" };
                eyebrow.parent.Insert(eyebrow.parent.IndexOf(eyebrow) + 1, listHost);

                foreach (var row in eyebrow.parent.Query(className: "stepper-row").ToList())
                    row.RemoveFromHierarchy();
            }
            else
            {
                listHost.Clear();
            }

            if (Lbl("ward-supply") != null)
                Lbl("ward-supply")!.text = $"{player.GetReagent(_reagentType)} {_reagentType}";

            for (int i = 0; i < player.CrucibleSlots.Count; i++)
            {
                var slot = player.CrucibleSlots[i];
                if (slot.State != CrucibleCardState.Active && slot.State != CrucibleCardState.Fired)
                    continue;

                int captured = i;
                int existing = slot.WardCount;
                int pending = _pendingSlot == captured ? 1 : 0;
                int display = existing + pending;

                var row = new VisualElement();
                row.AddToClassList("stepper-row");

                var badge = new VisualElement();
                badge.style.width = 30;
                badge.style.height = 42;
                badge.style.borderTopLeftRadius = badge.style.borderTopRightRadius =
                    badge.style.borderBottomLeftRadius = badge.style.borderBottomRightRadius = 6;
                badge.style.backgroundColor = new StyleColor(new UnityEngine.Color(0.23f, 0.1f, 0.055f));
                badge.style.borderTopWidth = badge.style.borderBottomWidth =
                    badge.style.borderLeftWidth = badge.style.borderRightWidth = 1;
                badge.style.borderTopColor = badge.style.borderBottomColor =
                    badge.style.borderLeftColor = badge.style.borderRightColor =
                        new StyleColor(new UnityEngine.Color(0.79f, 0.59f, 0.18f));
                badge.style.alignItems = Align.Center;
                badge.style.justifyContent = Justify.Center;
                badge.style.marginRight = 10;
                badge.Add(new Label(captured.ToString())
                {
                    style =
                    {
                        fontSize = 12,
                        color = new StyleColor(new UnityEngine.Color(0.91f, 0.73f, 0.29f)),
                        unityFontStyleAndWeight = FontStyle.Bold
                    }
                });

                var info = new VisualElement { style = { flexGrow = 1 } };
                info.Add(new Label($"Crucible {captured}") { style = { fontSize = 12, color = new StyleColor(new UnityEngine.Color(0.95f, 0.91f, 0.82f)) } });
                var stateLbl = new Label(display > 0 ? $"{display} ward(s)" : "unwarded — free to gambit")
                {
                    style =
                    {
                        fontSize = 9,
                        color = new StyleColor(display > 0
                            ? new UnityEngine.Color(0.5f, 0.77f, 0.66f)
                            : new UnityEngine.Color(0.94f, 0.56f, 0.54f))
                    }
                };
                info.Add(stateLbl);

                var minus = new Button();
                minus.AddToClassList("stepper-btn");
                minus.AddToClassList("stepper-btn--minus");
                minus.Add(new Label("−"));
                minus.clicked += () =>
                {
                    if (_pendingSlot == captured)
                        _pendingSlot = -1;
                    BindState(_session!, _bridge!);
                };

                var val = new Label(pending.ToString());
                val.AddToClassList("stepper-value");

                var plus = new Button();
                plus.AddToClassList("stepper-btn");
                plus.AddToClassList("stepper-btn--plus");
                plus.Add(new Label("+"));
                plus.SetEnabled(player.GetReagent(_reagentType) > 0 && _pendingSlot < 0);
                plus.clicked += () =>
                {
                    if (player.GetReagent(_reagentType) <= 0) return;
                    _pendingSlot = captured;
                    BindState(_session!, _bridge!);
                };

                row.Add(badge);
                row.Add(info);
                row.Add(minus);
                row.Add(val);
                row.Add(plus);
                listHost.Add(row);
            }
        }

        void RefreshSealButton(PlayerState player)
        {
            var btn = Btn("seal-btn");
            if (btn == null) return;
            bool ready = _pendingSlot >= 0 && player.GetReagent(_reagentType) > 0;
            btn.SetEnabled(ready);
            btn.EnableInClassList("btn--disabled", !ready);
            btn.text = ready
                ? $"Seal ward on slot {_pendingSlot}"
                : "Assign 1 ward to seal";
        }

        void OnSeal()
        {
            if (_bridge == null || _playerId < 0 || _pendingSlot < 0) return;
            if (_bridge.TrySubmit(new PlaceCardWardCommand(_playerId, _pendingSlot, _reagentType)))
                OnCompleted?.Invoke();
        }
    }
}
