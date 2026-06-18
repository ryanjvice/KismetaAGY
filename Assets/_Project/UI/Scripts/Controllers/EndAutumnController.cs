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
        }

        static void AddRecapRow(VisualElement list, string text)
        {
            var row = new VisualElement();
            row.AddToClassList("benefit-row");
            row.Add(new Label(text) { style = { fontSize = 11, whiteSpace = WhiteSpace.Normal } });
            list.Add(row);
        }

        void BuildStillAvailable()
        {
            if (_session == null || _playerId < 0) return;
            var player = _session.Players[_playerId];
            var tip = Root?.Q(className: "tip--caution");
            if (tip == null) return;

            tip.Clear();
            bool any = false;

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

            if (player.StoneWardCount == 0 && player.StoneState == StoneState.Forging)
            {
                any = true;
                tip.Add(MakeHintRow("Your stone is unwarded — rivals pay less to Oppose"));
            }

            if (!any)
                tip.Add(MakeHintRow("No obvious forge moves remain — safe to pass"));
        }

        static VisualElement MakeHintRow(string text)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 7;
            row.Add(new Label(text) { style = { fontSize = 11, whiteSpace = WhiteSpace.Normal } });
            return row;
        }
    }
}
