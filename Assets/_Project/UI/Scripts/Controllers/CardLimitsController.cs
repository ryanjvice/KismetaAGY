using System;
using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Rules;
using Kismeta.UI;
using Kismeta.UI.Components;
using Kismeta.UI.Narrative;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class CardLimitsController : ScreenController
    {
        public override string ScreenId => ScreenIds.CardLimits;

        public Action? OnBack;

        readonly HashSet<string> _discardSpread = new();
        readonly HashSet<string> _discardHand = new();

        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;

        protected override void Wire()
        {
            Btn("back-btn")!.clicked += () => OnBack?.Invoke();
            Btn("transit-btn")!.clicked += OnTransit;
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
            }

            if (Root == null) return;
            Rebuild();
            NarrativeSlotBindings.BindById(Root, "winter.limits");
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
                    ? "Transit The Age · Pass The Key"
                    : $"Discard {toDiscard} More To Transit";
            }
        }

        void PruneStaleSelections(PlayerState player)
        {
            _discardSpread.RemoveWhere(id => !player.Spread.Contains(id));
            _discardHand.RemoveWhere(id => !player.Hand.Contains(id));
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
                Rebuild();
            }
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
        }

        void SetLabelText(string name, string text)
        {
            var lbl = Lbl(name);
            if (lbl != null) lbl.text = text;
        }
    }
}
