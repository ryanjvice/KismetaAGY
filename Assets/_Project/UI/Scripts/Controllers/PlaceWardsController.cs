using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using System.Linq;
using Kismeta.Core.Entities;
using Kismeta.UI.Components;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class PlaceWardsController : OverlayController
    {
        readonly ReagentStepper _stepper = new() { Cap = 1 };
        readonly Dictionary<int, string> _slotReagentKeys = new();

        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;

        public System.Action OnBack;
        public System.Action OnCompleted;

        protected override void Wire()
        {
            Btn("back-btn")!.clicked += () => OnBack?.Invoke();
            Btn("seal-btn")!.clicked += OnSeal;
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _playerId = SummerActionBindings.ResolvePlayerId(session, bridge);
            if (Root == null || _playerId < 0) return;

            _stepper.Reset();
            _slotReagentKeys.Clear();
            RebuildRows();
        }

        void RebuildRows()
        {
            if (_session == null || _playerId < 0 || Root == null) return;

            var player = _session.Players[_playerId];
            int totalSupply = 0;
            foreach (ReagentType rt in System.Enum.GetValues(typeof(ReagentType)))
            {
                int have = player.GetReagent(rt);
                totalSupply += have;
                _stepper.SetHave(rt.ToString(), have);
            }

            if (Lbl("ward-supply") != null)
                Lbl("ward-supply")!.text = $"{totalSupply} left";

            var host = FindRowsHost();
            if (host == null) return;

            foreach (var row in host.Query(className: "stepper-row").ToList())
                row.RemoveFromHierarchy();

            for (int i = 0; i < player.CrucibleSlots.Count; i++)
            {
                var slot = player.CrucibleSlots[i];
                if (slot.State != CrucibleCardState.Active && slot.State != CrucibleCardState.Fired)
                    continue;

                host.Add(BuildRow(i, slot.WardCount));
            }

            RefreshSealBtn();
        }

        VisualElement? FindRowsHost()
        {
            foreach (var lbl in Root!.Query<Label>().ToList())
            {
                if (lbl.text == "your active cards")
                    return lbl.parent;
            }
            return null;
        }

        VisualElement BuildRow(int slotIndex, int existingWards)
        {
            var row = new VisualElement();
            row.AddToClassList("stepper-row");

            var icon = new VisualElement();
            icon.style.width = 30;
            icon.style.height = 42;
            icon.style.borderTopLeftRadius = 6;
            icon.style.borderTopRightRadius = 6;
            icon.style.borderBottomLeftRadius = 6;
            icon.style.borderBottomRightRadius = 6;
            icon.style.backgroundColor = new StyleColor(new UnityEngine.Color(58f / 255f, 26f / 255f, 14f / 255f));
            icon.style.alignItems = Align.Center;
            icon.style.justifyContent = Justify.Center;
            icon.style.marginRight = 10;
            var iconLbl = new Label(slotIndex.ToString());
            row.Add(icon);
            icon.Add(iconLbl);

            var info = new VisualElement { style = { flexGrow = 1 } };
            info.Add(new Label($"Crucible slot {slotIndex}") { style = { fontSize = 12 } });
            string stateText = existingWards > 0
                ? $"{existingWards} ward(s) placed"
                : "unwarded — free to gambit";
            info.Add(new Label(stateText) { style = { fontSize = 9 } });
            row.Add(info);

            var minus = new Button { name = $"ward-{slotIndex}-minus" };
            minus.AddToClassList("stepper-btn");
            minus.AddToClassList("stepper-btn--minus");
            minus.Add(new Label("−"));
            int captured = slotIndex;
            minus.clicked += () => Adjust(captured, -1);

            var val = new Label("0") { name = $"ward-{slotIndex}-val" };
            val.AddToClassList("stepper-value");

            var plus = new Button { name = $"ward-{slotIndex}-plus" };
            plus.AddToClassList("stepper-btn");
            plus.AddToClassList("stepper-btn--plus");
            plus.Add(new Label("+"));
            plus.clicked += () => Adjust(captured, +1);

            row.Add(minus);
            row.Add(val);
            row.Add(plus);

            return row;
        }

        void Adjust(int slotIndex, int delta)
        {
            var valLbl = Lbl($"ward-{slotIndex}-val");
            if (valLbl == null) return;

            int current = int.TryParse(valLbl.text, out var v) ? v : 0;
            string key = $"slot-{slotIndex}";

            if (delta > 0)
            {
                if (_stepper.Total >= _stepper.Cap) return;
                if (!_stepper.TryAdd(key)) return;
                _slotReagentKeys[slotIndex] = PickDefaultReagent();
                valLbl.text = (current + 1).ToString();
            }
            else
            {
                if (current <= 0) return;
                _stepper.TryRemove(key);
                _slotReagentKeys.Remove(slotIndex);
                valLbl.text = (current - 1).ToString();
            }

            UpdateSupplyLabel();
            RefreshSealBtn();
        }

        string PickDefaultReagent()
        {
            if (_session == null || _playerId < 0) return ReagentType.Salt.ToString();
            var player = _session.Players[_playerId];
            foreach (ReagentType rt in System.Enum.GetValues(typeof(ReagentType)))
            {
                if (player.GetReagent(rt) > 0)
                    return rt.ToString();
            }
            return ReagentType.Salt.ToString();
        }

        void UpdateSupplyLabel()
        {
            if (_session == null || _playerId < 0 || Lbl("ward-supply") == null) return;
            var player = _session.Players[_playerId];
            int remaining = 0;
            foreach (ReagentType rt in System.Enum.GetValues(typeof(ReagentType)))
                remaining += player.GetReagent(rt);
            remaining -= _stepper.Total;
            Lbl("ward-supply")!.text = $"{remaining} left";
        }

        void RefreshSealBtn()
        {
            var btn = Btn("seal-btn");
            if (btn == null) return;
            bool ready = _stepper.Total == 1;
            btn.SetEnabled(ready);
            btn.EnableInClassList("btn--disabled", !ready);
        }

        void OnSeal()
        {
            if (_bridge == null || _session == null || _playerId < 0 || _stepper.Total != 1) return;

            int slotIndex = -1;
            foreach (var kv in _slotReagentKeys)
            {
                slotIndex = kv.Key;
                break;
            }
            if (slotIndex < 0) return;

            var player = _session.Players[_playerId];
            ReagentType rt = ReagentType.Salt;
            foreach (ReagentType type in System.Enum.GetValues(typeof(ReagentType)))
            {
                if (player.GetReagent(type) > 0)
                {
                    rt = type;
                    break;
                }
            }

            if (_bridge.TrySubmit(new PlaceCardWardCommand(_playerId, slotIndex, rt)))
                OnCompleted?.Invoke();
        }
    }
}
