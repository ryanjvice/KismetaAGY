using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.UI;
using Kismeta.UI.Components;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class ActivateCardController : OverlayController
    {
        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;
        int _slotIndex = -1;
        List<string>? _activationCards;

        public System.Action OnBack;
        public System.Action? OnCompleted;

        protected override void Wire()
        {
            Btn("back-btn")!.clicked += () => OnBack?.Invoke();
            Btn("activate-btn")!.clicked += OnActivate;
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _playerId = SummerActionHelpers.ResolvePlayerId(session, bridge);
            if (_playerId < 0 || Root == null) return;

            var player = session.Players[_playerId];
            if (_slotIndex < 0)
                _slotIndex = FindFirstDormantSlot(player);

            RebuildSlotPicker(player);
            RefreshFormula();
        }

        int FindFirstDormantSlot(PlayerState player)
        {
            for (int i = 0; i < player.CrucibleSlots.Count; i++)
            {
                var slot = player.CrucibleSlots[i];
                if (slot.State == CrucibleCardState.Dormant && slot.HasCoal)
                    return i;
            }
            return -1;
        }

        void RebuildSlotPicker(PlayerState player)
        {
            var container = El("slot-picker");
            if (container == null)
            {
                var panel = Root?.Q(className: "panel");
                if (panel?.parent == null) return;
                container = new VisualElement { name = "slot-picker" };
                container.style.flexDirection = FlexDirection.Row;
                container.style.flexWrap = Wrap.Wrap;
                container.style.marginTop = 8;
                panel.parent.Insert(panel.parent.IndexOf(panel) + 1, container);
            }
            else
            {
                container.Clear();
            }

            int dormantCount = 0;
            for (int i = 0; i < player.CrucibleSlots.Count; i++)
            {
                var slot = player.CrucibleSlots[i];
                if (slot.State != CrucibleCardState.Dormant || !slot.HasCoal) continue;
                dormantCount++;
                int captured = i;
                var btn = new Button { text = $"Slot {captured}" };
                btn.AddToClassList("btn");
                btn.AddToClassList("btn--secondary");
                if (captured == _slotIndex)
                    btn.AddToClassList("btn--primary");
                btn.clicked += () =>
                {
                    _slotIndex = captured;
                    BindState(_session!, _bridge!);
                };
                container.Add(btn);
            }

            if (dormantCount == 0)
            {
                var lbl = new Label("No dormant crucible cards with coal.");
                lbl.style.fontSize = 11;
                lbl.style.color = new StyleColor(new UnityEngine.Color(0.72f, 0.6f, 0.43f));
                container.Add(lbl);
            }
        }

        void RefreshFormula()
        {
            if (_session == null || Root == null || _playerId < 0) return;
            var player = _session.Players[_playerId];
            var codexDb = _session.Rules?.CodexDatabase;
            var db = _session.Rules?.CardDatabase;

            if (_slotIndex < 0 || codexDb == null)
            {
                SetActivateReady(false, "No dormant slot to activate");
                return;
            }

            var formula = codexDb.GetFormula(player.AssignedCodex, _slotIndex);
            if (formula == null)
            {
                SetActivateReady(false, "No formula for this slot");
                return;
            }

            if (Lbl("formula-name") != null)
                Lbl("formula-name")!.text = $"Crucible {_slotIndex} — {formula.DisplayName}";
            if (Lbl("formula-req") != null)
                Lbl("formula-req")!.text = formula.DisplayName;

            _activationCards = SummerActionHelpers.FindActivationCards(_session, player, _slotIndex);
            var slots = El("formula-slots");
            if (slots != null && db != null)
            {
                slots.Clear();
                if (_activationCards != null)
                {
                    foreach (var id in _activationCards)
                    {
                        var inst = _session.GetCard(id);
                        var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                        if (def != null)
                            slots.Add(CardChipFactory.Create(def.Rank.ToString(), def.Suit));
                    }
                }

                int need = formula.FormulaType == CodexFormulaType.AnyThreePlanet ? 3 : 1;
                int have = _activationCards?.Count ?? 0;
                if (Lbl("formula-progress") != null)
                    Lbl("formula-progress")!.text = formula.FormulaType == CodexFormulaType.AnyThreePlanet
                        ? $"{have} / 3"
                        : have > 0 ? "ready" : "missing cards";

                bool ready = _activationCards != null && _activationCards.Count > 0;
                SetActivateReady(ready, ready
                    ? "Activate · light the cauldron"
                    : "Need more spread cards to activate");
            }
        }

        void SetActivateReady(bool ready, string label)
        {
            var b = Btn("activate-btn");
            if (b == null) return;
            b.text = label;
            b.EnableInClassList("btn--disabled", !ready);
            b.EnableInClassList("btn--primary", ready);
        }

        void OnActivate()
        {
            if (_bridge == null || _playerId < 0 || _slotIndex < 0) return;
            if (_activationCards == null || _activationCards.Count == 0) return;
            if (_bridge.TrySubmit(new ActivateCrucibleCommand(_playerId, _slotIndex, _activationCards)))
                OnCompleted?.Invoke();
        }
    }
}
