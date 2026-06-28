using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.UI.Controllers;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    public static class AutumnBoardBindings
    {
        public static void PopulateCruciblePills(VisualElement? host, GameSession session, PlayerState player)
        {
            if (host == null) return;
            host.Clear();

            var db = session.Rules?.CardDatabase;
            for (int i = 0; i < player.CrucibleSlots.Count; i++)
            {
                var slot = player.CrucibleSlots[i];
                string label = SlotLetter(i);
                string stateLabel = PillStateLabel(slot, session.Board.RoundNumber);
                string css = PillClass(slot, session.Board.RoundNumber);

                var pill = new VisualElement();
                pill.AddToClassList("crucible-pill");
                pill.AddToClassList(css);
                pill.Add(new Label(label) { style = { fontSize = 10, marginTop = 2 } });
                pill.Add(new Label(stateLabel) { style = { fontSize = 7 } });
                host.Add(pill);
            }
        }

        public static void PopulateCardStrip(VisualElement? host, GameSession session, IReadOnlyList<string> cardIds)
        {
            if (host == null) return;
            host.Clear();
            var db = session.Rules?.CardDatabase;
            if (db == null) return;

            foreach (var cardId in cardIds)
            {
                var inst = session.GetCard(cardId);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def == null) continue;
                var chip = CardChipFactory.CreateFromDefinition(def);
                chip.style.marginRight = 5;
                host.Add(chip);
            }
        }

        public static void PopulateReagentPanel(VisualElement? host, PlayerState player)
        {
            if (host == null) return;
            host.Clear();
            host.style.flexDirection = FlexDirection.Row;
            host.style.alignItems = Align.Center;

            foreach (ReagentType rt in System.Enum.GetValues(typeof(ReagentType)))
            {
                int n = player.GetReagent(rt);
                if (n <= 0) continue;
                host.Add(BuildReagentRow(rt, n));
            }
        }

        public static void PopulateReagentCost(VisualElement? host, ReagentCost cost)
        {
            if (host == null) return;
            host.Clear();
            host.style.flexDirection = FlexDirection.Row;
            host.style.alignItems = Align.Center;
            host.style.flexWrap = Wrap.Wrap;

            if (cost.Total == 0)
            {
                host.Add(new Label("no reagent cost") { style = { fontSize = 11, color = new UnityEngine.Color(184f / 255f, 154f / 255f, 110f / 255f) } });
                return;
            }

            if (cost.Sulphur > 0) host.Add(BuildReagentRow(ReagentType.Sulphur, cost.Sulphur));
            if (cost.AquaRegia > 0) host.Add(BuildReagentRow(ReagentType.AquaRegia, cost.AquaRegia));
            if (cost.Vitriol > 0) host.Add(BuildReagentRow(ReagentType.Vitriol, cost.Vitriol));
            if (cost.Quicksilver > 0) host.Add(BuildReagentRow(ReagentType.Quicksilver, cost.Quicksilver));
            if (cost.Salt > 0) host.Add(BuildReagentRow(ReagentType.Salt, cost.Salt));
        }

        static VisualElement BuildReagentRow(ReagentType rt, int count)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginRight = 14;

            var dot = new VisualElement();
            dot.AddToClassList("reagent-dot");
            dot.AddToClassList(ReagentDotClass(rt));
            dot.style.marginRight = 4;
            row.Add(dot);
            row.Add(new Label(count.ToString()) { style = { fontSize = 11 } });
            return row;
        }

        static string ReagentDotClass(ReagentType rt) => rt switch
        {
            ReagentType.Sulphur => "reagent-dot--sulphur",
            ReagentType.Vitriol => "reagent-dot--vitriol",
            ReagentType.AquaRegia => "reagent-dot--aqua",
            ReagentType.Quicksilver => "reagent-dot--quick",
            _ => "reagent-dot--salt"
        };

        public static string BuildContextNote(GameSession session, PlayerState player)
        {
            int fired = 0;
            bool temperReady = AutumnActionBindings.CanTemper(session, player);
            foreach (var slot in player.CrucibleSlots)
                if (slot.State == CrucibleCardState.Fired) fired++;

            if (temperReady)
                return "A crucible card is temper-ready — you can advance a stage this Autumn.";
            if (fired > 0)
                return $"{fired} crucible card(s) forged — temper unlocks after a full forging round.";
            return "Review your locked tableau before Fire, Temper, or Oppose.";
        }

        static string SlotLetter(int index) => ((char)('A' + index)).ToString();

        static string PillStateLabel(PlayerCrucibleSlot slot, int round)
        {
            if (slot.State == CrucibleCardState.Fired && slot.FiredAtRound >= 0 && slot.FiredAtRound < round)
                return "temper-ready";
            return slot.State switch
            {
                CrucibleCardState.Fired => "forged",
                CrucibleCardState.Active => "active",
                CrucibleCardState.Dormant => "dormant",
                CrucibleCardState.Discarded => "discarded",
                CrucibleCardState.Arrested => "arrested",
                _ => slot.State.ToString().ToLowerInvariant()
            };
        }

        static string PillClass(PlayerCrucibleSlot slot, int round)
        {
            if (slot.State == CrucibleCardState.Fired && slot.FiredAtRound >= 0 && slot.FiredAtRound < round)
                return "crucible-pill--ready";
            return slot.State switch
            {
                CrucibleCardState.Fired => "crucible-pill--forged",
                CrucibleCardState.Active => "crucible-pill--active",
                _ => "crucible-pill--dormant"
            };
        }
    }
}
