using System;
using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Rules;
using Kismeta.UI;
using Kismeta.UI.Components;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class CardLimitsController : ScreenController
    {
        public override string ScreenId => ScreenIds.CardLimits;

        readonly HashSet<string> _discardSpread = new();
        readonly HashSet<string> _discardHand = new();
        readonly HashSet<string> _craftSelected = new();

        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;
        ReagentType _reagent = ReagentType.Salt;

        protected override void Wire()
        {
            Btn("transit-btn")!.clicked += OnTransit;
            Btn("forge-btn")!.clicked += OnForge;

            foreach (var key in CraftReagentPanelBindings.ReagentKeys)
                Btn($"pick-{key}")?.RegisterCallback<ClickEvent>(_ => PickReagent(key));
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            int pid = bridge.ActivePlayerId;
            if (pid != _playerId)
            {
                _playerId = pid;
                _discardSpread.Clear();
                _discardHand.Clear();
                _craftSelected.Clear();
                _reagent = ReagentType.Salt;
            }

            if (Root == null) return;
            Rebuild();
        }

        void Rebuild()
        {
            if (_session == null || Root == null) return;
            var player = _session.Players[_playerId];
            var db = _session.Rules?.CardDatabase;

            PruneStaleSelections(player);

            RebuildZone("spread-cards", player.Spread, _discardSpread, db, isSpread: true);
            RebuildZone("hand-cards", player.Hand, _discardHand, db, isSpread: false);

            int newSpread = player.Spread.Count - _discardSpread.Count;
            int newHand = player.Hand.Count - _discardHand.Count;
            int toDiscard = Math.Max(0, newSpread - WinterRules.SpreadLimit)
                          + Math.Max(0, newHand - WinterRules.HandLimit);

            SetLabelText("spread-limit", $"{newSpread} / {WinterRules.SpreadLimit}");
            SetLabelText("hand-limit", $"{newHand} / {WinterRules.HandLimit}");
            SetLabelText("hand-tally", $"{newHand} / {WinterRules.HandLimit}");
            SetLabelText("spread-tally", $"{newSpread} / {WinterRules.SpreadLimit}");
            SetLabelText("to-discard", toDiscard.ToString());

            bool valid = newSpread <= WinterRules.SpreadLimit && newHand <= WinterRules.HandLimit;
            var transit = Btn("transit-btn");
            if (transit != null)
            {
                transit.EnableInClassList("btn--disabled", !valid);
                transit.EnableInClassList("btn--primary", valid);
                transit.SetEnabled(valid);
                transit.text = valid
                    ? "Transit the age · pass the key"
                    : $"Discard {toDiscard} more to transit";
            }

            RebuildCraftPanel(player);
        }

        void PruneStaleSelections(PlayerState player)
        {
            _discardSpread.RemoveWhere(id => !player.Spread.Contains(id));
            _discardHand.RemoveWhere(id => !player.Hand.Contains(id));
            _craftSelected.RemoveWhere(id =>
                !player.Spread.Contains(id) && !player.Hand.Contains(id));
        }

        void RebuildZone(string containerName, IReadOnlyList<string> cardIds,
            HashSet<string> discardSet, ICardDatabase? db, bool isSpread)
        {
            var zone = El(containerName);
            if (zone == null || _session == null) return;
            zone.Clear();

            int kept = cardIds.Count - discardSet.Count;
            int limit = isSpread ? WinterRules.SpreadLimit : WinterRules.HandLimit;
            bool zoneOver = kept > limit;
            int overCount = kept - limit;

            foreach (var id in cardIds)
            {
                if (!TapSwapBindings.IsMinorArcana(_session, id)) continue;
                var inst = _session.GetCard(id);
                var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;
                if (def == null) continue;

                bool marked = discardSet.Contains(id);
                bool isOverLimit = !marked && zoneOver && overCount > 0;
                if (isOverLimit) overCount--;

                var chip = CardChipFactory.CreateFromDefinition(def, selected: marked);
                if (isOverLimit)
                    chip.AddToClassList("card-chip--overlimit");

                chip.userData = id;
                chip.RegisterCallback<ClickEvent>(_ =>
                    OnChipClick(id, isSpread, marked, isOverLimit));
                zone.Add(chip);
            }
        }

        void OnChipClick(string cardId, bool isSpread, bool marked, bool isOverLimit)
        {
            var discardSet = isSpread ? _discardSpread : _discardHand;

            if (marked)
            {
                discardSet.Remove(cardId);
                Rebuild();
                return;
            }

            if (isOverLimit)
            {
                discardSet.Add(cardId);
                Rebuild();
                return;
            }

            if (_bridge == null) return;
            if (_bridge.TryApplySideEffect(new WinterMoveCardCommand(_playerId, cardId, toSpread: !isSpread)))
            {
                _discardSpread.Remove(cardId);
                _discardHand.Remove(cardId);
                _craftSelected.Remove(cardId);
                Rebuild();
            }
        }

        void RebuildCraftPanel(PlayerState player)
        {
            if (_session == null || Root == null) return;

            var section = El("craft-section");
            if (section != null)
            {
                bool show = CraftReagentPanelBindings.HasAnyCraftableReagent(player);
                section.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                if (!show) return;
            }

            CraftReagentPanelBindings.LockReagentPicks(Root, player);
            CraftReagentPanelBindings.SetActiveReagentPick(Root, _reagent);
            CraftReagentPanelBindings.UpdateCauldronNote(Root, _session, player, _reagent);

            int need = CraftReagentPanelBindings.EffectiveCost(_session, player, _reagent);
            var pickLabel = Lbl("craft-pick-label");
            if (pickLabel != null)
            {
                if (_reagent == ReagentType.Salt)
                    pickLabel.text = $"tap {need} cards to pay";
                else
                    pickLabel.text = $"tap {need} {Correspondence.SuitFor(_reagent)} cards to pay";
            }

            var cards = SummerCardPickBindings.CollectMinorCards(_session, player);
            Suit? filter = _reagent == ReagentType.Salt ? null : Correspondence.SuitFor(_reagent);
            SummerCardPickBindings.RebuildPool(Root, _session, cards, _craftSelected, filter, OnCraftCardToggle);

            CraftReagentPanelBindings.RefreshForgeButton(Root, _session, player, _reagent, _craftSelected.Count);
        }

        void PickReagent(string key)
        {
            if (Root != null && CraftReagentPanelBindings.IsReagentPickLocked(Root, key))
                return;

            _reagent = CraftReagentPanelBindings.KeyToReagent(key);
            _craftSelected.Clear();
            Rebuild();
        }

        void OnCraftCardToggle(string cardId)
        {
            if (_session == null) return;
            int need = CraftReagentPanelBindings.EffectiveCost(_session, _session.Players[_playerId], _reagent);

            if (_craftSelected.Contains(cardId))
                _craftSelected.Remove(cardId);
            else if (_craftSelected.Count < need)
                _craftSelected.Add(cardId);

            Rebuild();
        }

        void OnForge()
        {
            if (_bridge == null || _session == null) return;

            int need = CraftReagentPanelBindings.EffectiveCost(_session, _session.Players[_playerId], _reagent);
            if (_craftSelected.Count < need) return;

            var ids = new List<string>(_craftSelected);
            if (!_bridge.TryApplySideEffect(new CraftReagentCommand(_playerId, _reagent, ids)))
                return;

            foreach (var id in ids)
            {
                _discardSpread.Remove(id);
                _discardHand.Remove(id);
            }
            _craftSelected.Clear();
            Rebuild();
        }

        void OnTransit()
        {
            if (_bridge == null || _session == null) return;
            var player = _session.Players[_playerId];
            int newSpread = player.Spread.Count - _discardSpread.Count;
            int newHand = player.Hand.Count - _discardHand.Count;
            if (newSpread > WinterRules.SpreadLimit || newHand > WinterRules.HandLimit) return;

            _bridge.TrySubmit(new DiscardToLimitCommand(_playerId,
                new List<string>(_discardSpread), new List<string>(_discardHand)));
            _discardSpread.Clear();
            _discardHand.Clear();
            _craftSelected.Clear();
        }

        void SetLabelText(string name, string text)
        {
            var lbl = Lbl(name);
            if (lbl != null) lbl.text = text;
        }
    }
}
