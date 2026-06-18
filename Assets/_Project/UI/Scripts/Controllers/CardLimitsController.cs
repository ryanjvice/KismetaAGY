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

        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;

        protected override void Wire()
        {
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
        }

        void Rebuild()
        {
            if (_session == null || Root == null) return;
            var player = _session.Players[_playerId];
            var db = _session.Rules?.CardDatabase;

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
        }

        void RebuildZone(string containerName, IReadOnlyList<string> cardIds,
            HashSet<string> discardSet, ICardDatabase? db, bool isSpread)
        {
            var zone = El(containerName);
            if (zone == null || _session == null) return;
            zone.Clear();

            int kept = cardIds.Count - discardSet.Count;
            bool zoneOver = kept > (isSpread ? WinterRules.SpreadLimit : WinterRules.HandLimit);
            int overCount = kept - (isSpread ? WinterRules.SpreadLimit : WinterRules.HandLimit);

            foreach (var id in cardIds)
            {
                if (!TapSwapBindings.IsMinorArcana(_session, id)) continue;
                var inst = _session.GetCard(id);
                var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;
                if (def == null) continue;

                bool marked = discardSet.Contains(id);
                var chip = CardChipFactory.Create(def.Rank.ToString(), def.Suit, selected: marked);
                if (!marked && zoneOver && overCount > 0)
                {
                    chip.AddToClassList("card-chip--overlimit");
                    overCount--;
                }

                chip.userData = id;
                chip.RegisterCallback<ClickEvent>(_ =>
                {
                    if (marked) discardSet.Remove(id); else discardSet.Add(id);
                    Rebuild();
                });
                zone.Add(chip);
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
