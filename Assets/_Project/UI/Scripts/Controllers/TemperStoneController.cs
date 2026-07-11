using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Rules;
using Kismeta.UI.Components;
using Kismeta.UI.Narrative;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class TemperStoneController : OverlayController
    {
        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;
        int _selectedSlot = -1;

        public System.Action? OnClose;
        public System.Action? OnTempered;

        protected override void Wire()
        {
            Btn("temper-close")!.clicked += () => OnClose?.Invoke();
            Btn("temper-btn")!.clicked += OnTemper;
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _playerId = SummerActionBindings.ResolvePlayerId(session, bridge);
            if (Root == null || _playerId < 0) return;

            _selectedSlot = AutumnActionBindings.FindEligibleTemperSlotIndex(session, session.Players[_playerId]);
            BuildForgedCards();
            RefreshWinTip();
            RefreshTemperBtn();
            UiMotion.PulseTrackNode(Root?.Q(className: "stage-node--now"));
            NarrativeSlotBindings.BindById(Root, "autumn.temper");
            ModifierPreviewBindings.PopulateForContext(
                Root?.Q<VisualElement>("modifier-preview"),
                session,
                _playerId,
                scopeOverride: EffectGlanceScope.Forge);
        }

        void BuildForgedCards()
        {
            var host = El("forged-cards");
            if (host == null || _session == null || _playerId < 0) return;
            host.Clear();

            var player = _session.Players[_playerId];
            var indices = AutumnActionBindings.GetEligibleTemperSlotIndices(_session, player);
            var db = _session.Rules?.CardDatabase;

            foreach (int i in indices)
            {
                var slot = player.CrucibleSlots[i];
                string title = $"Crucible {(char)('A' + i)}";
                bool selected = i == _selectedSlot;

                var btn = new Button();
                btn.AddToClassList("btn");
                btn.style.flexDirection = FlexDirection.Column;
                btn.style.alignItems = Align.Center;
                btn.style.marginRight = 8;
                btn.EnableInClassList("btn--primary", selected);

                btn.Add(new Label(title) { style = { fontSize = 9 } });
                btn.Add(new Label("temper-ready") { style = { fontSize = 7, color = new StyleColor(new UnityEngine.Color(0.5f, 0.77f, 0.66f)) } });
                int captured = i;
                btn.clicked += () => { _selectedSlot = captured; BuildForgedCards(); RefreshTemperBtn(); };
                host.Add(btn);
            }

            if (Lbl("temper-note") != null)
            {
                int used = AutumnActionBindings.FindEligibleTemperSlotIndex(_session, player);
                Lbl("temper-note")!.text = indices.Count > 1
                    ? $"Rules discard slot {(char)('A' + used)} first — tap to preview."
                    : "Discarding the forged card advances your stone one stage.";
            }
        }

        void RefreshWinTip()
        {
            if (_session == null || _playerId < 0) return;
            var player = _session.Players[_playerId];
            bool winning = AutumnActionBindings.IsWinningTemper(player);
            var tip = Root?.Q(className: "tip--ok");
            if (tip != null)
                tip.style.display = winning ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void RefreshTemperBtn()
        {
            var btn = Btn("temper-btn");
            if (btn == null || _session == null || _playerId < 0) return;

            var player = _session.Players[_playerId];
            bool ready = _selectedSlot >= 0 && AutumnActionBindings.CanTemper(_session, player);
            bool winning = AutumnActionBindings.IsWinningTemper(player);

            btn.SetEnabled(ready);
            if (ready)
            {
                btn.RemoveFromClassList("btn--disabled");
                btn.AddToClassList("btn--primary");
                btn.text = winning
                    ? "Temper To The Altar · Complete The Great Work"
                    : $"Temper · Advance To {player.StonePosition.Advance()}";
            }
            else
            {
                btn.AddToClassList("btn--disabled");
                btn.RemoveFromClassList("btn--primary");
                btn.text = "No Forged Card Ready To Temper";
            }
        }

        void OnTemper()
        {
            if (_bridge == null || _playerId < 0 || _session == null) return;
            if (!AutumnActionBindings.CanTemper(_session, _session.Players[_playerId])) return;
            if (_bridge.TrySubmit(new TemperCommand(_playerId)))
                OnTempered?.Invoke();
        }
    }
}
