using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class EndAutumnController : OverlayController
    {
        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;

        public System.Action? OnKeep;
        public System.Action? OnConfirm;

        protected override void Wire()
        {
            Btn("keep-btn")!.clicked += () => OnKeep?.Invoke();
            Btn("close-btn")!.clicked += () => OnKeep?.Invoke();
            Btn("end-btn")!.clicked += () => OnConfirm?.Invoke();
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _playerId = SummerActionBindings.ResolvePlayerId(session, bridge);
            if (Root == null || _playerId < 0) return;

            BuildRecap();
            BuildStillAvailable();
        }

        void BuildRecap()
        {
            var list = El("recap-list");
            if (list == null || _session == null || _playerId < 0) return;
            list.Clear();

            var player = _session.Players[_playerId];
            int fired = 0;
            foreach (var slot in player.CrucibleSlots)
                if (slot.State == CrucibleCardState.Fired) fired++;

            AddRecapRow(list, $"Stone at {player.StonePosition} ({player.StoneState})");
            AddRecapRow(list, $"{fired} crucible card(s) forged this age");
            AddRecapRow(list, $"Stone wards: {player.StoneWardCount}");
            AddRecapRow(list, $"Reagents in supply: {SummarizeReagents(player)}");
        }

        static void AddRecapRow(VisualElement list, string text)
        {
            var row = new VisualElement();
            row.AddToClassList("end-autumn__recap-row");
            var label = new Label(text);
            label.AddToClassList("end-autumn__line");
            row.Add(label);
            list.Add(row);
        }

        static string SummarizeReagents(PlayerState player)
        {
            var parts = new List<string>();
            foreach (ReagentType rt in System.Enum.GetValues(typeof(ReagentType)))
            {
                int n = player.GetReagent(rt);
                if (n > 0) parts.Add($"{n} {rt}");
            }
            return parts.Count > 0 ? string.Join(", ", parts) : "none yet";
        }

        void BuildStillAvailable()
        {
            if (_session == null || _playerId < 0) return;
            var player = _session.Players[_playerId];
            var tip = El("still-available");
            if (tip == null) return;

            tip.Clear();
            bool any = false;

            if (SummerActionBindings.CanStillCraftReagent(_session, player))
            {
                any = true;
                tip.Add(MakeHintRow("You can still craft reagents from your cards"));
            }

            if (SummerActionBindings.HasActivatableCrucible(player))
            {
                any = true;
                tip.Add(MakeHintRow("A dormant crucible slot still has coal — you can activate"));
            }

            if (AutumnActionBindings.CanFire(_session, player))
            {
                any = true;
                tip.Add(MakeHintRow("You can still Fire from a mantle position"));
            }

            if (AutumnActionBindings.CanTemper(_session, player))
            {
                any = true;
                tip.Add(MakeHintRow("A temper-ready forged card can advance your stage"));
            }

            if (AutumnActionBindings.CanLeaveStasis(player))
            {
                any = true;
                tip.Add(MakeHintRow("You can pay Salt to Leave Stasis"));
            }

            if (!any)
                tip.Add(MakeHintRow("No obvious forge moves remain — safe to pass"));
        }

        static VisualElement MakeHintRow(string text)
        {
            var row = new VisualElement();
            row.AddToClassList("end-autumn__hint-row");
            var label = new Label(text);
            label.AddToClassList("end-autumn__line");
            row.Add(label);
            return row;
        }
    }
}
