using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.UI.Components;
using Kismeta.UI.Narrative;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class FireStoneController : OverlayController
    {
        readonly HashSet<string> _selected = new();
        readonly List<Button> _slotButtons = new();

        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;
        int _slotIndex = -1;

        public System.Action? OnClose;
        public System.Action? OnFired;

        protected override void Wire()
        {
            Btn("fire-close")!.clicked += () => OnClose?.Invoke();
            Btn("fire-btn")!.clicked += OnFire;
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _playerId = SummerActionBindings.ResolvePlayerId(session, bridge);
            if (Root == null || _playerId < 0) return;

            _selected.Clear();
            BuildSlotPicker();
            SelectFirstFireableSlot();
            RefreshTrack();
            NarrativeSlotBindings.BindById(Root, "autumn.fire");
            ModifierPreviewBindings.PopulateForContext(
                Root?.Q<VisualElement>("modifier-preview"),
                session,
                _playerId,
                scopeOverride: EffectGlanceScope.Forge);
        }

        void BuildSlotPicker()
        {
            if (_session == null || _playerId < 0 || Root == null) return;

            var host = El("crucible-slots");
            if (host == null) return;
            host.Clear();
            _slotButtons.Clear();

            var indices = AutumnActionBindings.GetFireableSlotIndices(_session, _session.Players[_playerId]);
            foreach (int i in indices)
            {
                int captured = i;
                var btn = new Button { text = $"Slot {captured}" };
                btn.userData = captured;
                btn.AddToClassList("btn");
                btn.AddToClassList("btn--secondary");
                btn.style.marginRight = 6;
                btn.style.marginBottom = 6;
                btn.clicked += () => SelectSlot(captured);
                host.Add(btn);
                _slotButtons.Add(btn);
            }
        }

        void SelectFirstFireableSlot()
        {
            if (_session == null || _playerId < 0) return;
            var indices = AutumnActionBindings.GetFireableSlotIndices(_session, _session.Players[_playerId]);
            if (indices.Count > 0)
                SelectSlot(indices[0]);
        }

        void SelectSlot(int slotIndex)
        {
            _slotIndex = slotIndex;
            _selected.Clear();
            foreach (var btn in _slotButtons)
            {
                bool active = btn.userData is int idx && idx == slotIndex;
                btn.EnableInClassList("btn--primary", active);
            }
            RefreshAlignmentAndCost();
        }

        void RefreshTrack()
        {
            if (_session == null || _playerId < 0) return;
            var player = _session.Players[_playerId];
            int now = player.StonePosition.Value;
            int target = player.StonePosition.Advance().Value;

            if (Lbl("track-now-val") != null) Lbl("track-now-val")!.text = now.ToString();
            if (Lbl("track-target-val") != null) Lbl("track-target-val")!.text = target.ToString();
            if (Lbl("track-now-label") != null) Lbl("track-now-label")!.text = $"{player.StonePosition} · now";
            if (Lbl("track-target-label") != null) Lbl("track-target-label")!.text = "target";
            UiMotion.PulseTrackNode(Root?.Q(className: "track-node--target"));
        }

        void RefreshAlignmentAndCost()
        {
            if (_session == null || _playerId < 0 || _slotIndex < 0) return;
            var player = _session.Players[_playerId];
            var db = _session.Rules?.CardDatabase;
            if (db == null) return;

            var slot = player.CrucibleSlots[_slotIndex];
            var inst = _session.GetCard(slot.CardInstanceId);
            var def = inst != null ? db.GetById(inst.DefinitionId) : null;

            if (Lbl("align-formula") != null && def != null)
                Lbl("align-formula")!.text = def.AlchemicalFormula;

            if (Lbl("cost-summary") != null && def != null)
            {
                bool canPay = AutumnActionBindings.CanPayCost(_session, player, def.AlchemicalCost);
                Lbl("cost-summary")!.text = $"Alchemical cost: {AutumnActionBindings.FormatReagentCost(def.AlchemicalCost)}"
                    + AutumnActionBindings.FormatWildReagentNote(_session, player)
                    + (canPay ? "" : " — insufficient");
            }

            RebuildAlignmentCards(def);
            RefreshFireBtn(def);
        }

        void RebuildAlignmentCards(CardDefinition? crucibleDef)
        {
            var host = El("align-cards");
            if (host == null || _session == null || _playerId < 0) return;
            host.Clear();

            var db = _session.Rules?.CardDatabase;
            if (db == null) return;

            var player = _session.Players[_playerId];
            foreach (var cardId in player.Spread)
            {
                var inst = _session.GetCard(cardId);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def == null) continue;

                bool sel = _selected.Contains(cardId);
                var chip = CardChipFactory.CreateFromDefinition(def, selected: sel);
                chip.userData = cardId;
                chip.RegisterCallback<ClickEvent>(_ => ToggleCard(cardId, crucibleDef));
                host.Add(chip);
            }
        }

        void ToggleCard(string cardId, CardDefinition? crucibleDef)
        {
            if (_selected.Contains(cardId))
                _selected.Remove(cardId);
            else
                _selected.Add(cardId);
            RebuildAlignmentCards(crucibleDef);
            RefreshFireBtn(crucibleDef);
        }

        void RefreshFireBtn(CardDefinition? crucibleDef)
        {
            var btn = Btn("fire-btn");
            if (btn == null || _session == null || _playerId < 0) return;

            var player = _session.Players[_playerId];
            bool ready = _slotIndex >= 0
                && crucibleDef != null
                && AutumnActionBindings.CanPayCost(_session, player, crucibleDef.AlchemicalCost)
                && IsAlignmentReady(crucibleDef);

            btn.SetEnabled(ready);
            if (ready)
            {
                btn.RemoveFromClassList("btn--disabled");
                btn.AddToClassList("btn--primary");
                btn.text = $"Fire — Advance To {player.StonePosition.Advance()}";
            }
            else
            {
                btn.AddToClassList("btn--disabled");
                btn.RemoveFromClassList("btn--primary");
                btn.text = crucibleDef == null ? "Select A Crucible Slot" : "Complete Alignment & Cost";
            }
        }

        bool IsAlignmentReady(CardDefinition? crucibleDef)
        {
            if (crucibleDef == null || _session == null) return false;
            var validator = _session.Rules?.AlchemicalValidator;
            if (validator == null) return true;

            var db = _session.Rules!.CardDatabase;
            var player = _session.Players[_playerId];
            var defs = new List<CardDefinition>();
            foreach (var id in _selected)
            {
                var inst = _session.GetCard(id);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def != null) defs.Add(def);
            }

            if (player.WorldWildcardFlipped
                && AdeptAttunement.IsWorldResonant(_session, player)
                && crucibleDef.ArcanaNumber >= 0)
            {
                defs.Add(WildcardLinkService.CreateVirtualWildcard(crucibleDef.ArcanaNumber));
            }

            var (ok, _) = validator.Validate(crucibleDef.AlchemicalFormula, defs, db);
            return ok;
        }

        void OnFire()
        {
            if (_bridge == null || _playerId < 0 || _slotIndex < 0 || _session == null) return;
            var db = _session.Rules?.CardDatabase;
            if (db == null) return;

            var slot = _session.Players[_playerId].CrucibleSlots[_slotIndex];
            var inst = _session.GetCard(slot.CardInstanceId);
            var def = inst != null ? db.GetById(inst.DefinitionId) : null;
            if (def == null || !IsAlignmentReady(def)) return;

            var ids = new List<string>(_selected);
            if (_bridge.TrySubmit(new FireStoneCommand(_playerId, _slotIndex, ids)))
                OnFired?.Invoke();
        }
    }
}
