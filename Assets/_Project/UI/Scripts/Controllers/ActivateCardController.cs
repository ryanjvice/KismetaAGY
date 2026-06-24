using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class ActivateCardController : OverlayController
    {
        static readonly string[] SlotIds = { "A", "B", "C", "D" };
        static readonly string[] ColorKeys = { "red", "blue", "green", "yellow" };

        static readonly Color CountReadyColor = new(232f / 255f, 185f / 255f, 74f / 255f);
        static readonly Color CountMutedColor = new(184f / 255f, 154f / 255f, 110f / 255f);
        static readonly Color ProgressReadyColor = new(93f / 255f, 202f / 255f, 165f / 255f);

        readonly HashSet<string> _selected = new();

        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;
        int _slotIndex = -1;
        bool _uiInitialized;

        public System.Action OnBack;
        public System.Action OnCompleted;

        public int? InitialSlotIndex { get; set; }

        protected override void Bind()
        {
            _uiInitialized = false;
        }

        protected override void Wire()
        {
            Btn("back-btn")!.clicked += () => OnBack?.Invoke();
            Btn("activate-btn")!.clicked += OnActivate;

            for (int i = 0; i < SlotIds.Length; i++)
            {
                int captured = i;
                Btn($"slot-{SlotIds[i]}")?.RegisterCallback<ClickEvent>(_ => SelectSlot(captured));
            }
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _playerId = SummerActionBindings.ResolvePlayerId(session, bridge);
            if (Root == null || _playerId < 0) return;

            if (!_uiInitialized)
            {
                _slotIndex = -1;
                _selected.Clear();

                var detail = El("formula-detail");
                if (detail != null)
                    detail.style.display = DisplayStyle.None;

                RefreshAllSlots();
                _uiInitialized = true;
            }
            else
                RefreshAllSlots();

            if (InitialSlotIndex.HasValue)
            {
                SelectSlot(InitialSlotIndex.Value);
                InitialSlotIndex = null;
            }
            else if (_slotIndex >= 0)
                RefreshFormulaDetail();
            else
                DisableActivate("Select a card to activate");
        }

        static bool CanActivateSlot(PlayerCrucibleSlot slot) =>
            slot.State == CrucibleCardState.Dormant && slot.HasCoal;

        static bool IsSlotActive(PlayerCrucibleSlot slot) =>
            slot.State != CrucibleCardState.Dormant;

        void RefreshAllSlots()
        {
            if (_session == null || _playerId < 0) return;

            var player = _session.Players[_playerId];
            var codexDb = _session.Rules?.CodexDatabase;
            var db = _session.Rules?.CardDatabase;
            if (codexDb == null || db == null) return;

            var spreadCards = CollectSpreadCards(db, player);

            for (int i = 0; i < SlotIds.Length; i++)
            {
                var btn = Btn($"slot-{SlotIds[i]}");
                if (btn == null) continue;

                bool selected = i == _slotIndex;
                btn.EnableInClassList("crucible-slot--selected", selected);

                foreach (var key in ColorKeys)
                    btn.EnableInClassList($"slot-sel--{key}", false);

                if (i >= player.CrucibleSlots.Count) continue;

                var slot = player.CrucibleSlots[i];
                var formula = codexDb.GetFormula(player.AssignedCodex, i);
                if (formula == null) continue;

                var colorKey = ColorKey(formula.Cauldron);
                if (selected)
                    btn.AddToClassList($"slot-sel--{colorKey}");

                bool active = IsSlotActive(slot);
                btn.EnableInClassList("crucible-slot--active", active);

                var reagentLbl = Lbl($"slot-{SlotIds[i]}-reagent");
                var progressLbl = Lbl($"slot-{SlotIds[i]}-progress");
                if (reagentLbl == null || progressLbl == null) continue;

                if (active)
                {
                    reagentLbl.text = "\u2014";
                    progressLbl.text = "active";
                    progressLbl.style.color = new StyleColor(new Color(93f / 255f, 202f / 255f, 165f / 255f));
                    continue;
                }

                reagentLbl.text = ReagentDisplayName(Correspondence.ReagentFor(formula.CauldronSuit));
                var (progressText, ready) = SlotProgressText(formula, spreadCards);
                progressLbl.text = progressText;
                progressLbl.style.color = ready
                    ? new StyleColor(ProgressReadyColor)
                    : new StyleColor(CountMutedColor);
            }
        }

        void SelectSlot(int slotIndex)
        {
            if (_session == null || _playerId < 0 || slotIndex < 0 || slotIndex >= SlotIds.Length) return;

            var player = _session.Players[_playerId];
            if (slotIndex >= player.CrucibleSlots.Count) return;

            var slot = player.CrucibleSlots[slotIndex];

            _slotIndex = slotIndex;
            _selected.Clear();

            RefreshAllSlots();
            RefreshFormulaDetail();
        }

        void RefreshFormulaDetail()
        {
            if (_session == null || _playerId < 0 || _slotIndex < 0 || Root == null) return;

            var player = _session.Players[_playerId];
            var codexDb = _session.Rules?.CodexDatabase;
            var db = _session.Rules?.CardDatabase;
            if (codexDb == null || db == null) return;

            var formula = codexDb.GetFormula(player.AssignedCodex, _slotIndex);
            if (formula == null) return;

            var slot = player.CrucibleSlots[_slotIndex];
            var colorKey = ColorKey(formula.Cauldron);
            var cauldron = formula.Cauldron;
            var reagent = ReagentDisplayName(Correspondence.ReagentFor(formula.CauldronSuit));

            var detail = El("formula-detail");
            if (detail != null)
                detail.style.display = DisplayStyle.Flex;

            var panel = El("formula-card-panel");
            if (panel != null)
            {
                foreach (var key in ColorKeys)
                    panel.EnableInClassList($"slot-sel--{key}", false);
                panel.AddToClassList($"slot-sel--{colorKey}");
            }

            if (Lbl("formula-reagent") != null)
                Lbl("formula-reagent")!.text = reagent;

            if (Lbl("formula-req") != null)
            {
                Lbl("formula-req")!.text = formula.FormulaType == CodexFormulaType.AnyThreePlanet
                    ? $"any 3 {formula.RequiredPlanet} · {cauldron} cauldron"
                    : $"{formula.MinRankSum} total ranks · {formula.RequiredSuit} · {cauldron} cauldron";
            }

            if (Lbl("formula-consequence") != null)
            {
                Lbl("formula-consequence")!.text =
                    $"Activating moves the coal to the {cauldron} cauldron, lighting it — then you can craft {reagent}.";
            }

            RebuildFormulaChips(formula, db, player);
            RefreshFormulaStatus(formula, db, player);
            RefreshActivateBtn(formula, db, slot, player);
        }

        void RefreshFormulaStatus(CodexFormulaDefinition formula, ICardDatabase db, PlayerState player)
        {
            var statusLbl = Lbl("formula-status");
            if (statusLbl == null) return;

            var spreadCards = CollectSpreadCards(db, player);

            if (IsActivationReady(formula, spreadCards, db))
            {
                statusLbl.text = $"formula complete — ready to light the {formula.Cauldron} cauldron";
                return;
            }

            if (formula.FormulaType == CodexFormulaType.AnyThreePlanet)
            {
                int have = _selected.Count > 0
                    ? _selected.Count
                    : CountMatchingPlanet(spreadCards, formula.RequiredPlanet);
                int missing = 3 - have;
                statusLbl.text = missing <= 0
                    ? $"select 3 {formula.RequiredPlanet} cards from your spread"
                    : $"need {missing} more {formula.RequiredPlanet} card{(missing > 1 ? "s" : "")} from your spread";
            }
            else
            {
                int sum = _selected.Count > 0
                    ? BestEffectiveRankSumForSelection(db)
                    : ActivationCardSuggester.BestRankSumFromSpread(spreadCards, formula.RequiredSuit);
                int missing = formula.MinRankSum - sum;
                statusLbl.text = missing > 0
                    ? $"need {missing} more rank points from your spread"
                    : $"select {formula.RequiredSuit} cards from your spread";
            }
        }

        void RebuildFormulaChips(CodexFormulaDefinition formula, ICardDatabase db, PlayerState player)
        {
            var chips = El("formula-chips");
            if (chips == null) return;
            chips.Clear();

            var spreadCards = CollectSpreadCards(db, player);
            var colorKey = ColorKey(formula.Cauldron);
            bool ready = IsActivationReady(formula, spreadCards, db);

            if (formula.FormulaType == CodexFormulaType.AnyThreePlanet)
            {
                foreach (var (id, def) in spreadCards)
                {
                    if (def.Planet != formula.RequiredPlanet) continue;

                    bool sel = _selected.Contains(id);
                    var chip = BuildPlanetChip(def, colorKey, sel);
                    chip.userData = id;
                    chip.RegisterCallback<ClickEvent>(_ => ToggleSpreadCard(id, formula));
                    chips.Add(chip);
                }

                int matched = 0;
                foreach (var (_, def) in spreadCards)
                {
                    if (def.Planet == formula.RequiredPlanet)
                        matched++;
                }

                for (int i = matched; i < 3; i++)
                    chips.Add(EmptySlot());
            }
            else
            {
                foreach (var (id, def) in spreadCards)
                {
                    if (def.Suit != formula.RequiredSuit) continue;

                    bool sel = _selected.Contains(id);
                    var chip = CardChipFactory.CreateFromDefinition(def, selected: sel);
                    chip.userData = id;
                    chip.RegisterCallback<ClickEvent>(_ => ToggleSpreadCard(id, formula));
                    chips.Add(chip);
                }
            }

            var countLbl = Lbl("formula-count");
            if (countLbl != null)
            {
                countLbl.text = FormulaCountText(formula, spreadCards, db);
                countLbl.style.color = new StyleColor(ready ? CountReadyColor : CountMutedColor);
            }
        }

        static VisualElement BuildPlanetChip(CardDefinition def, string colorKey, bool selected)
        {
            var chip = new VisualElement();
            chip.AddToClassList("card-chip");
            if (selected)
            {
                chip.AddToClassList($"fslot-fill--{colorKey}");
                chip.style.borderTopWidth = chip.style.borderRightWidth =
                    chip.style.borderBottomWidth = chip.style.borderLeftWidth = 1;
                chip.style.borderTopColor = chip.style.borderRightColor =
                    chip.style.borderBottomColor = chip.style.borderLeftColor =
                        new StyleColor(new Color(201f / 255f, 150f / 255f, 47f / 255f));
            }

            var rank = new Label(SymbolGlyphs.CompactRank(def.Rank));
            rank.AddToClassList("card-chip__rank");
            chip.Add(rank);
            return chip;
        }

        static VisualElement EmptySlot()
        {
            var slot = new VisualElement();
            slot.AddToClassList("formula-slot");
            slot.AddToClassList("formula-slot--empty");
            slot.style.marginRight = 6;
            var lbl = new Label("?");
            lbl.style.fontSize = 13;
            lbl.style.color = new StyleColor(new Color(106f / 255f, 90f / 255f, 74f / 255f));
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
            var slot = _session.Players[_playerId].CrucibleSlots[_slotIndex];
            var player = _session.Players[_playerId];
            RebuildFormulaChips(formula, db, player);
            RefreshFormulaStatus(formula, db, player);
            RefreshActivateBtn(formula, db, slot, player);
        }

        void RefreshActivateBtn(CodexFormulaDefinition formula, ICardDatabase db, PlayerCrucibleSlot slot, PlayerState player)
        {
            var btn = Btn("activate-btn");
            if (btn == null) return;

            if (_slotIndex < 0)
            {
                DisableActivate("Select a card to activate");
                return;
            }

            if (!CanActivateSlot(slot))
            {
                btn.EnableInClassList("btn--disabled", true);
                btn.EnableInClassList("btn--primary", false);
                btn.text = SlotBlockedMessage(slot);
                return;
            }

            var spreadCards = CollectSpreadCards(db, player);
            bool ready = IsActivationReady(formula, spreadCards, db);
            btn.EnableInClassList("btn--disabled", !ready);
            btn.EnableInClassList("btn--primary", ready);

            if (ready)
                btn.text = $"Activate · light the {formula.Cauldron} cauldron";
            else if (formula.FormulaType == CodexFormulaType.AnyThreePlanet)
            {
                int have = _selected.Count > 0
                    ? _selected.Count
                    : CountMatchingPlanet(spreadCards, formula.RequiredPlanet);
                int missing = 3 - have;
                btn.text = missing > 0
                    ? $"Need {missing} more {formula.RequiredPlanet} to activate"
                    : $"Select 3 {formula.RequiredPlanet} cards to activate";
            }
            else
            {
                int sum = _selected.Count > 0
                    ? BestEffectiveRankSumForSelection(db)
                    : ActivationCardSuggester.BestRankSumFromSpread(spreadCards, formula.RequiredSuit);
                int missing = formula.MinRankSum - sum;
                btn.text = missing > 0
                    ? $"Need {missing} more rank points to activate"
                    : $"Need more rank points ({sum} / {formula.MinRankSum})";
            }
        }

        void DisableActivate(string label)
        {
            var btn = Btn("activate-btn");
            if (btn == null) return;
            btn.EnableInClassList("btn--disabled", true);
            btn.EnableInClassList("btn--primary", false);
            btn.text = label;
        }

        static string SlotBlockedMessage(PlayerCrucibleSlot slot) => slot.State switch
        {
            CrucibleCardState.Dormant when !slot.HasCoal => "No coal on this card",
            CrucibleCardState.Active => "Already activated",
            CrucibleCardState.Fired => "Already forged",
            CrucibleCardState.Discarded => "Discarded",
            CrucibleCardState.Arrested => "Arrested",
            _ => "Cannot activate this slot"
        };

        bool IsSelectedSetValid(CodexFormulaDefinition formula, ICardDatabase db) =>
            ActivationCardSuggester.IsSelectedSetValid(CollectSelectedDefs(db), formula, db);

        bool IsActivationReady(
            CodexFormulaDefinition formula,
            List<(string id, CardDefinition def)> spreadCards,
            ICardDatabase db) =>
            ActivationCardSuggester.CanSpreadSatisfyFormula(spreadCards, formula)
            || IsSelectedSetValid(formula, db);

        string FormulaCountText(
            CodexFormulaDefinition formula,
            List<(string id, CardDefinition def)> spreadCards,
            ICardDatabase db)
        {
            if (formula.FormulaType == CodexFormulaType.AnyThreePlanet)
            {
                int have = _selected.Count > 0
                    ? _selected.Count
                    : CountMatchingPlanet(spreadCards, formula.RequiredPlanet);
                return $"{System.Math.Min(have, 3)}/3";
            }

            int sum = _selected.Count > 0
                ? BestEffectiveRankSumForSelection(db)
                : ActivationCardSuggester.BestRankSumFromSpread(spreadCards, formula.RequiredSuit);
            return $"{System.Math.Min(sum, formula.MinRankSum)}/{formula.MinRankSum}";
        }

        static int CountMatchingPlanet(
            List<(string id, CardDefinition def)> spreadCards,
            Planet planet)
        {
            int count = 0;
            foreach (var (_, def) in spreadCards)
            {
                if (def.Planet == planet)
                    count++;
            }
            return count;
        }

        List<CardDefinition> CollectSelectedDefs(ICardDatabase db)
        {
            var defs = new List<CardDefinition>();
            foreach (var id in _selected)
            {
                var inst = _session?.GetCard(id);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def != null)
                    defs.Add(def);
            }
            return defs;
        }

        int BestEffectiveRankSumForSelection(ICardDatabase db) =>
            ActivationCardSuggester.BestEffectiveRankSum(CollectSelectedDefs(db));

        List<string>? ResolveActivationIds(
            CodexFormulaDefinition formula,
            List<(string id, CardDefinition def)> spreadCards,
            ICardDatabase db)
        {
            if (IsSelectedSetValid(formula, db))
                return new List<string>(_selected);

            if (ActivationCardSuggester.CanSpreadSatisfyFormula(spreadCards, formula))
                return ActivationCardSuggester.SuggestActivationCards(spreadCards, formula, db);

            return null;
        }

        List<(string id, CardDefinition def)> CollectSpreadCards(ICardDatabase db, PlayerState player)
        {
            var spreadCards = new List<(string id, CardDefinition def)>();
            foreach (var id in player.Spread)
            {
                var inst = _session?.GetCard(id);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def != null) spreadCards.Add((id, def));
            }
            return spreadCards;
        }

        static (string text, bool ready) SlotProgressText(
            CodexFormulaDefinition formula,
            List<(string id, CardDefinition def)> spreadCards)
        {
            if (formula.FormulaType == CodexFormulaType.AnyThreePlanet)
            {
                int count = CountMatchingPlanet(spreadCards, formula.RequiredPlanet);
                return ($"{count} / 3 {formula.RequiredPlanet}", count >= 3);
            }

            int best = ActivationCardSuggester.BestRankSumFromSpread(spreadCards, formula.RequiredSuit);
            return ($"{best} / {formula.MinRankSum} {formula.RequiredSuit}", best >= formula.MinRankSum);
        }

        void OnActivate()
        {
            if (_bridge == null || _playerId < 0 || _slotIndex < 0) return;
            var db = _session?.Rules?.CardDatabase;
            var codexDb = _session?.Rules?.CodexDatabase;
            if (db == null || codexDb == null || _session == null) return;

            var player = _session.Players[_playerId];
            var slot = player.CrucibleSlots[_slotIndex];
            if (!CanActivateSlot(slot)) return;

            var formula = codexDb.GetFormula(player.AssignedCodex, _slotIndex);
            if (formula == null) return;

            var spreadCards = CollectSpreadCards(db, player);
            var ids = ResolveActivationIds(formula, spreadCards, db);
            if (ids == null || ids.Count == 0) return;

            if (_bridge.TrySubmit(new ActivateCrucibleCommand(_playerId, _slotIndex, ids)))
                OnCompleted?.Invoke();
        }

        static string ColorKey(string cauldron) =>
            string.IsNullOrEmpty(cauldron) ? "red" : cauldron.ToLowerInvariant();

        static string ReagentDisplayName(ReagentType reagent) => reagent switch
        {
            ReagentType.Sulphur => "Sulphur",
            ReagentType.AquaRegia => "Aqua Regia",
            ReagentType.Vitriol => "Vitriol",
            ReagentType.Quicksilver => "Quicksilver",
            _ => reagent.ToString()
        };
    }
}
