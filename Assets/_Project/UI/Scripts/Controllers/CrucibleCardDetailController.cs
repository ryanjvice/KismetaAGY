using Kismeta.Core.Entities;
using Kismeta.UI;
using Kismeta.UI.Components;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class CrucibleCardDetailController : OverlayController
    {
        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;

        public int? SlotIndex { get; set; }
        public System.Action? OnClose;

        protected override void Wire()
        {
            Btn("close-btn")!.clicked += () => OnClose?.Invoke();
            Btn("back-btn")!.clicked += () => OnClose?.Invoke();
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _playerId = SummerActionBindings.ResolvePlayerId(session, bridge);
            if (Root == null || _playerId < 0 || !SlotIndex.HasValue) return;

            int slotIndex = SlotIndex.Value;
            if (slotIndex < 0 || slotIndex >= session.Players[_playerId].CrucibleSlots.Count) return;

            var player = session.Players[_playerId];
            var slot = player.CrucibleSlots[slotIndex];
            var db = session.Rules?.CardDatabase;
            if (db == null) return;

            var inst = session.GetCard(slot.CardInstanceId);
            var def = inst != null ? db.GetById(inst.DefinitionId) : null;
            if (def == null) return;

            if (Lbl("crucible-slot-label") != null)
                Lbl("crucible-slot-label")!.text = $"slot {(char)('A' + slotIndex)} · active";

            if (Lbl("crucible-name") != null)
                Lbl("crucible-name")!.text = def.Name;

            if (Lbl("crucible-formula") != null)
            {
                Lbl("crucible-formula")!.text = string.IsNullOrWhiteSpace(def.AlchemicalFormula)
                    ? "—"
                    : def.AlchemicalFormula;
            }

            AutumnBoardBindings.PopulateReagentCost(El("crucible-cost"), def.AlchemicalCost);
        }
    }
}
