using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.UI.Components;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class EndSummerController : OverlayController
    {
        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;

        public System.Action OnKeep;
        public System.Action OnConfirm;

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
            int activeCards = 0;
            foreach (var slot in player.CrucibleSlots)
                if (slot.State == CrucibleCardState.Active || slot.State == CrucibleCardState.Fired)
                    activeCards++;
            activeCards += player.Arcanum.Count;

            AddRecapRow(list, $"Reagents in supply: {SummarizeReagents(player)}");
            AddRecapRow(list, $"{activeCards} active crucible & adept card(s)");
        }

        static void AddRecapRow(VisualElement list, string text)
        {
            var row = new VisualElement();
            row.AddToClassList("end-summer__recap-row");
            var label = new Label(text);
            label.AddToClassList("end-summer__line");
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

            if (HasUnwardedActive(player))
            {
                any = true;
                tip.Add(MakeHintRow("An active crucible card is unwarded — rivals can gambit it"));
            }

            if (_session.Players.Count > 1)
            {
                any = true;
                tip.Add(MakeHintRow("You can still Trade, Duel, or Gambit with rivals"));
            }

            if (!any)
                tip.Add(MakeHintRow("No obvious contest moves remain — safe to pass"));
        }

        VisualElement MakeHintRow(string text)
        {
            var row = new VisualElement();
            row.AddToClassList("end-summer__hint-row");
            var label = new Label(text);
            label.AddToClassList("end-summer__line");
            row.Add(label);
            return row;
        }

        static bool HasUnwardedActive(PlayerState player)
        {
            foreach (var slot in player.CrucibleSlots)
                if ((slot.State == CrucibleCardState.Active || slot.State == CrucibleCardState.Fired)
                    && slot.WardCount == 0)
                    return true;
            return false;
        }
    }
}
