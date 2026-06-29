using Kismeta.Core.Entities;
using Kismeta.UI;
using Kismeta.UI.Components;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class CrucibleCardDetailController : OverlayController
    {
        GameSession? _session;

        public int? SlotIndex { get; set; }
        public string? CardInstanceId { get; set; }
        public System.Action? OnClose;

        protected override void Wire()
        {
            Btn("close-btn")!.clicked += () => OnClose?.Invoke();
            Btn("back-btn")!.clicked += () => OnClose?.Invoke();
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            if (Root == null) return;

            if (!TryResolveBinding(session, bridge, out var def, out int playerId, out int slotIndex, out string slotState))
                return;

            BindDetail(def, playerId, slotIndex, slotState, instanceMode: !string.IsNullOrEmpty(CardInstanceId));
        }

        bool TryResolveBinding(
            GameSession session,
            CommandBridge bridge,
            out CardDefinition def,
            out int playerId,
            out int slotIndex,
            out string slotState)
        {
            def = null!;
            playerId = -1;
            slotIndex = -1;
            slotState = string.Empty;

            var db = session.Rules?.CardDatabase;
            if (db == null) return false;

            if (!string.IsNullOrEmpty(CardInstanceId))
            {
                for (int p = 0; p < session.Players.Count; p++)
                {
                    var slots = session.Players[p].CrucibleSlots;
                    for (int i = 0; i < slots.Count; i++)
                    {
                        if (slots[i].CardInstanceId != CardInstanceId)
                            continue;

                        var inst = session.GetCard(CardInstanceId);
                        var resolved = inst != null ? db.GetById(inst.DefinitionId) : null;
                        if (resolved == null) return false;

                        def = resolved;
                        playerId = p;
                        slotIndex = i;
                        slotState = slots[i].State.ToString().ToLowerInvariant();
                        return true;
                    }
                }

                return false;
            }

            if (!SlotIndex.HasValue) return false;

            playerId = SummerActionBindings.ResolvePlayerId(session, bridge);
            if (playerId < 0 || playerId >= session.Players.Count) return false;

            slotIndex = SlotIndex.Value;
            if (slotIndex < 0 || slotIndex >= session.Players[playerId].CrucibleSlots.Count) return false;

            var slot = session.Players[playerId].CrucibleSlots[slotIndex];
            var cardInst = session.GetCard(slot.CardInstanceId);
            var slotDef = cardInst != null ? db.GetById(cardInst.DefinitionId) : null;
            if (slotDef == null) return false;

            def = slotDef;
            slotState = slot.State.ToString().ToLowerInvariant();
            return true;
        }

        void BindDetail(CardDefinition def, int playerId, int slotIndex, string slotState, bool instanceMode)
        {
            var numeral = RomanNumerals.ToArcanaLabel(def.ArcanaNumber);
            var slotLetter = (char)('A' + slotIndex);

            if (Lbl("crucible-slot-label") != null)
            {
                Lbl("crucible-slot-label")!.text = instanceMode
                    ? $"{_session!.Players[playerId].Color} · slot {slotLetter} · {numeral} · {slotState}"
                    : $"slot {slotLetter} · {numeral} · {slotState}";
            }

            if (Lbl("crucible-name") != null)
                Lbl("crucible-name")!.text = def.Name;

            if (Lbl("crucible-number") != null)
                Lbl("crucible-number")!.text = numeral;

            if (Lbl("crucible-formula") != null)
            {
                Lbl("crucible-formula")!.text = string.IsNullOrWhiteSpace(def.AlchemicalFormula)
                    ? "—"
                    : def.AlchemicalFormula;
            }

            if (Btn("back-btn") != null)
                Btn("back-btn")!.text = instanceMode ? "Done" : "Back To The Forge";

            AutumnBoardBindings.PopulateReagentCost(El("crucible-cost"), def.AlchemicalCost);
        }
    }
}
