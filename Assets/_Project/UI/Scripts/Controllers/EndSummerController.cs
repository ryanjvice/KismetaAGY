using System.Collections.Generic;
using System.Linq;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
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
            int active = 0;
            foreach (var slot in player.CrucibleSlots)
                if (slot.State >= CrucibleCardState.Active) active++;

            AddRecapRow(list, "Activated crucible cards", $"{active} active slot(s) this summer");
            AddRecapRow(list, "Reagents crafted", SummarizeReagents(player));
        }

        static void AddRecapRow(VisualElement list, string icon, string text)
        {
            var row = new VisualElement();
            row.AddToClassList("benefit-row");
            row.Add(new Label(text) { style = { fontSize = 11, whiteSpace = WhiteSpace.Normal } });
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
            var tip = Root?.Q(className: "tip--caution");
            if (tip == null) return;

            foreach (var child in tip.Query(className: "benefit-row").ToList())
                child.RemoveFromHierarchy();

            foreach (var child in tip.Children().ToList())
                if (child.ClassListContains("benefit-row") == false && child is Label == false)
                    continue;

            tip.Clear();

            bool any = false;

            if (CanStillCraft(player))
            {
                any = true;
                tip.Add(MakeHintRow("You can still craft reagents from your cards"));
            }

            if (HasUnwardedActive(player))
            {
                any = true;
                tip.Add(MakeHintRow("An active crucible card is unwarded — rivals can gambit it"));
            }

            if (HasDormantWithCoal(player))
            {
                any = true;
                tip.Add(MakeHintRow("A dormant crucible slot still has coal — you can activate"));
            }

            if (!any)
                tip.Add(MakeHintRow("No obvious productive moves remain — safe to pass"));
        }

        VisualElement MakeHintRow(string text)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 7;
            row.Add(new Label(text) { style = { fontSize = 11, whiteSpace = WhiteSpace.Normal } });
            return row;
        }

        bool CanStillCraft(PlayerState player)
        {
            if (_session == null) return false;
            var cards = SummerCardPickBindings.CollectMinorCards(_session, player);
            int need = SummerActionBindings.CraftEffectiveCost(_session, player, ReagentType.Salt);
            return cards.Count >= need;
        }

        static bool HasUnwardedActive(PlayerState player)
        {
            foreach (var slot in player.CrucibleSlots)
                if ((slot.State == CrucibleCardState.Active || slot.State == CrucibleCardState.Fired)
                    && slot.WardCount == 0)
                    return true;
            return false;
        }

        static bool HasDormantWithCoal(PlayerState player)
        {
            foreach (var slot in player.CrucibleSlots)
                if (slot.State == CrucibleCardState.Dormant && slot.HasCoal)
                    return true;
            return false;
        }
    }
}
