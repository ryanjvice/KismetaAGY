using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.UI.Components;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class ActivateCardController : OverlayController
    {
        readonly HashSet<string> _selected = new();
        readonly List<Button> _slotButtons = new();

        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;
        int _slotIndex = -1;

        public System.Action OnBack;
        public System.Action OnCompleted;

        protected override void Wire()
        {
            Btn("back-btn")!.clicked += () => OnBack?.Invoke();
            Btn("activate-btn")!.clicked += OnActivate;
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _playerId = SummerActionBindings.ResolvePlayerId(session, bridge);
            if (Root == null || _playerId < 0) return;

            _selected.Clear();
            BuildSlotPicker();
            SelectFirstDormantSlot();
        }

        void BuildSlotPicker()
        {
            if (_session == null || _playerId < 0 || Root == null) return;

            var player = _session.Players[_playerId];
            var statusbar = Root.Q(className: "statusbar");
            if (statusbar == null) return;

            var existing = Root.Q("slot-picker");
            existing?.RemoveFromHierarchy();

            foreach (var btn in _slotButtons)
                btn.clicked -= null;
            _slotButtons.Clear();

            var picker = new VisualElement { name = "slot-picker" };
            picker.style.flexDirection = FlexDirection.Row;
            picker.style.flexWrap = Wrap.Wrap;
            picker.style.paddingLeft = 14;
            picker.style.paddingRight = 14;
            picker.style.paddingBottom = 4;

            for (int i = 0; i < player.CrucibleSlots.Count; i++)
            {
                var slot = player.CrucibleSlots[i];
                if (slot.State != CrucibleCardState.Dormant || !slot.HasCoal) continue;

                int captured = i;
                var btn = new Button { text = $"Slot {captured}" };
                btn.userData = captured;
                btn.AddToClassList("btn");
                btn.AddToClassList("btn--secondary");
                btn.clicked += () => SelectSlot(captured);
                picker.Add(btn);
                _slotButtons.Add(btn);
            }

            statusbar.parent?.Insert(statusbar.parent.IndexOf(statusbar) + 1, picker);
        }

        void SelectFirstDormantSlot()
        {
            if (_session == null || _playerId < 0) return;
            var player = _session.Players[_playerId];
            for (int i = 0; i < player.CrucibleSlots.Count; i++)
            {
                var slot = player.CrucibleSlots[i];
                if (slot.State == CrucibleCardState.Dormant && slot.HasCoal)
                {
                    SelectSlot(i);
                    return;
                }
            }
        }

        void SelectSlot(int slotIndex)
        {
            _slotIndex = slotIndex;
            _selected.Clear();
            HighlightSlotButton(slotIndex);
            RefreshFormula();
        }

        void HighlightSlotButton(int slotIndex)
        {
            foreach (var btn in _slotButtons)
            {
                bool active = btn.userData is int idx && idx == slotIndex;
                btn.EnableInClassList("btn--primary", active);
            }
        }

        void RefreshFormula()
        {
            if (_session == null || _playerId < 0 || _slotIndex < 0 || Root == null) return;

            var player = _session.Players[_playerId];
            var codexDb = _session.Rules?.CodexDatabase;
            var db = _session.Rules?.CardDatabase;
            if (codexDb == null || db == null) return;

            var formula = codexDb.GetFormula(player.AssignedCodex, _slotIndex);
            if (formula == null) return;

            if (Lbl("formula-name") != null)
                Lbl("formula-name")!.text = $"Crucible {_slotIndex} — {formula.DisplayName}";

            if (Lbl("formula-req") != null)
            {
                Lbl("formula-req")!.text = formula.FormulaType == CodexFormulaType.AnyThreePlanet
                    ? $"needs 3 {formula.RequiredPlanet} cards in your Spread"
                    : $"needs {formula.MinRankSum} rank points of {formula.RequiredSuit} in your Spread";
            }

            RebuildFormulaSlots(formula, db, player);
            RefreshActivateBtn(formula, db);
        }

        void RebuildFormulaSlots(CodexFormulaDefinition formula, ICardDatabase db, PlayerState player)
        {
            var slots = El("formula-slots");
            if (slots == null) return;
            slots.Clear();

            var spreadCards = new List<(string id, CardDefinition def)>();
            foreach (var id in player.Spread)
            {
                var inst = _session!.GetCard(id);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def != null) spreadCards.Add((id, def));
            }

            if (formula.FormulaType == CodexFormulaType.AnyThreePlanet)
            {
                int matched = 0;
                foreach (var (id, def) in spreadCards)
                {
                    bool match = def.Planet == formula.RequiredPlanet;
                    bool sel = _selected.Contains(id);
                    if (match)
                    {
                        matched++;
                        var chip = CardChipFactory.Create(def.Rank.ToString(), def.Suit, selected: sel);
                        chip.userData = id;
                        chip.RegisterCallback<ClickEvent>(_ => ToggleSpreadCard(id, formula));
                        slots.Add(chip);
                    }
                }
                for (int i = matched; i < 3; i++)
                    slots.Add(EmptySlot());
            }
            else
            {
                foreach (var (id, def) in spreadCards)
                {
                    if (def.Suit != formula.RequiredSuit) continue;
                    bool sel = _selected.Contains(id);
                    var chip = CardChipFactory.Create(def.Rank.ToString(), def.Suit, selected: sel);
                    chip.userData = id;
                    chip.RegisterCallback<ClickEvent>(_ => ToggleSpreadCard(id, formula));
                    slots.Add(chip);
                }

                int sum = RankSum(_selected, db);
                if (sum < formula.MinRankSum)
                    slots.Add(EmptySlot());
            }

            if (Lbl("formula-progress") != null)
            {
                if (formula.FormulaType == CodexFormulaType.AnyThreePlanet)
                    Lbl("formula-progress")!.text = $"{_selected.Count} / 3";
                else
                    Lbl("formula-progress")!.text = $"{RankSum(_selected, db)} / {formula.MinRankSum}";
            }
        }

        static VisualElement EmptySlot()
        {
            var slot = new VisualElement();
            slot.AddToClassList("formula-slot");
            slot.AddToClassList("formula-slot--empty");
            var lbl = new Label("?");
            lbl.style.fontSize = 14;
            lbl.style.color = new StyleColor(new UnityEngine.Color(90f / 255f, 74f / 255f, 58f / 255f));
            slot.Add(lbl);
            return slot;
        }

        void ToggleSpreadCard(string cardId, CodexFormulaDefinition formula)
        {
            if (_selected.Contains(cardId))
                _selected.Remove(cardId);
            else
                _selected.Add(cardId);

            if (_session == null || _playerId < 0) return;
            var db = _session.Rules?.CardDatabase;
            if (db == null) return;
            RebuildFormulaSlots(formula, db, _session.Players[_playerId]);
            RefreshActivateBtn(formula, db);
        }

        void RefreshActivateBtn(CodexFormulaDefinition formula, ICardDatabase db)
        {
            var btn = Btn("activate-btn");
            if (btn == null) return;

            bool ready = IsFormulaReady(formula, db);
            btn.EnableInClassList("btn--disabled", !ready);
            btn.EnableInClassList("btn--primary", ready);

            if (ready)
                btn.text = "Activate · light the cauldron";
            else if (formula.FormulaType == CodexFormulaType.AnyThreePlanet)
                btn.text = $"Need {3 - _selected.Count} more matching card(s)";
            else
                btn.text = $"Need more rank points ({RankSum(_selected, db)} / {formula.MinRankSum})";
        }

        bool IsFormulaReady(CodexFormulaDefinition formula, ICardDatabase db)
        {
            if (_selected.Count == 0) return false;
            var defs = new List<CardDefinition>();
            foreach (var id in _selected)
            {
                var inst = _session?.GetCard(id);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def != null) defs.Add(def);
            }

            var validator = new CodexFormulaValidator(db);
            return validator.ValidateDefs(defs, formula).ok;
        }

        int RankSum(HashSet<string> ids, ICardDatabase db)
        {
            int sum = 0;
            foreach (var id in ids)
            {
                var inst = _session?.GetCard(id);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def == null) continue;
                sum += def.Rank == Rank.Ace ? 15 : (int)def.Rank;
            }
            return sum;
        }

        void OnActivate()
        {
            if (_bridge == null || _playerId < 0 || _slotIndex < 0) return;
            var db = _session?.Rules?.CardDatabase;
            var codexDb = _session?.Rules?.CodexDatabase;
            if (db == null || codexDb == null || _session == null) return;

            var formula = codexDb.GetFormula(_session.Players[_playerId].AssignedCodex, _slotIndex);
            if (formula == null || !IsFormulaReady(formula, db)) return;

            var ids = new List<string>(_selected);
            if (_bridge.TrySubmit(new ActivateCrucibleCommand(_playerId, _slotIndex, ids)))
                OnCompleted?.Invoke();
        }
    }
}
