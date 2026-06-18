using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.UI;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class EndSummerController : OverlayController
    {
        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;

        public System.Action OnKeep;
        public System.Action OnEnd;

        protected override void Wire()
        {
            Btn("keep-btn")!.clicked += () => OnKeep?.Invoke();
            Btn("close-btn")!.clicked += () => OnKeep?.Invoke();
            Btn("end-btn")!.clicked += () => OnEnd?.Invoke();
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _playerId = SummerActionHelpers.ResolvePlayerId(session, bridge);
            if (_playerId < 0 || Root == null) return;

            RebuildRecap(session.Players[_playerId]);
            RebuildStillAvailable(session, session.Players[_playerId]);
        }

        void RebuildRecap(PlayerState player)
        {
            var list = El("recap-list");
            if (list == null) return;
            list.Clear();

            AddRecapRow(list, "\u25cf", "Your spread holds " + player.Spread.Count + " cards");
            if (player.GetReagent(ReagentType.Salt) > 0)
                AddRecapRow(list, "\uebd9", $"{player.GetReagent(ReagentType.Salt)} Salt in stockpile");
        }

        void RebuildStillAvailable(GameSession session, PlayerState player)
        {
            var hintsHost = El("still-available-hints");
            if (hintsHost == null)
            {
                var caution = Root?.Q(className: "tip--caution");
                if (caution == null) return;
                hintsHost = new VisualElement { name = "still-available-hints" };
                hintsHost.style.flexDirection = FlexDirection.Column;
                hintsHost.style.alignItems = Align.Stretch;
                caution.Clear();
                caution.Add(hintsHost);
            }
            else
            {
                hintsHost.Clear();
            }

            var hints = new List<string>();
            var cards = SummerActionHelpers.CollectMinorCards(session, player);
            if (cards.Count >= 3)
                hints.Add($"You can still craft — {cards.Count} minors in hand/spread");

            for (int i = 0; i < player.CrucibleSlots.Count; i++)
            {
                var slot = player.CrucibleSlots[i];
                if (slot.State == CrucibleCardState.Active && slot.WardCount == 0)
                {
                    hints.Add($"Crucible {i} is unwarded — rivals can gambit it free");
                    break;
                }
            }

            for (int i = 0; i < player.CrucibleSlots.Count; i++)
            {
                var slot = player.CrucibleSlots[i];
                if (slot.State == CrucibleCardState.Dormant && slot.HasCoal)
                {
                    hints.Add($"Crucible {i} can still be activated");
                    break;
                }
            }

            if (hints.Count == 0)
                hints.Add("No obvious moves left — passing ends your Summer actions");

            foreach (var hint in hints)
                AddHintRow(hintsHost, hint);
        }

        static void AddRecapRow(VisualElement list, string icon, string text)
        {
            var row = new VisualElement();
            row.AddToClassList("benefit-row");
            row.Add(new Label(icon) { style = { marginRight = 9 } });
            row.Add(new Label(text)
            {
                style =
                {
                    fontSize = 11,
                    color = new StyleColor(new UnityEngine.Color(0.81f, 0.77f, 0.66f)),
                    whiteSpace = WhiteSpace.Normal
                }
            });
            list.Add(row);
        }

        static void AddHintRow(VisualElement host, string text)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 7;
            row.Add(new Label(text)
            {
                style =
                {
                    fontSize = 11,
                    color = new StyleColor(new UnityEngine.Color(0.95f, 0.91f, 0.82f)),
                    whiteSpace = WhiteSpace.Normal
                }
            });
            host.Add(row);
        }

        public void SubmitEnd()
        {
            if (_bridge == null || _playerId < 0) return;
            _bridge.TrySubmit(new PassCrucibleActionCommand(_playerId));
        }
    }
}
