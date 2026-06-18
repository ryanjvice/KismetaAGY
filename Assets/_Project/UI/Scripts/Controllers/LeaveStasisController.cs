using System.Collections;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class LeaveStasisController : OverlayController
    {
        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;

        public System.Action? OnBack;
        public System.Action? OnStay;
        public System.Action? OnReturned;

        protected override void Wire()
        {
            Btn("back-btn")!.clicked += () => OnBack?.Invoke();
            Btn("stay-btn")!.clicked += () => OnStay?.Invoke();
            Btn("pay-salt-btn")!.clicked += () => StartCoroutine(PaySaltAndResolve());
            Btn("done-btn")!.clicked += () => OnReturned?.Invoke();
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            _playerId = SummerActionBindings.ResolvePlayerId(session, bridge);
            if (Root == null || _playerId < 0) return;

            El("escape-view")!.style.display = DisplayStyle.Flex;
            El("result-view")!.style.display = DisplayStyle.None;

            bool spotOpen = !AutumnActionBindings.ForgeSpotOccupied(session, _playerId);
            var saltEscape = Root?.Q(className: "escape--live")
                ?? FindEscapePanel(true);
            var oppEscape = Root?.Q(className: "escape--inactive")
                ?? FindEscapePanel(false);

            if (saltEscape != null)
            {
                saltEscape.EnableInClassList("escape--live", spotOpen);
                saltEscape.EnableInClassList("escape--inactive", !spotOpen);
            }
            if (oppEscape != null)
            {
                oppEscape.EnableInClassList("escape--inactive", spotOpen);
                oppEscape.EnableInClassList("escape--live", !spotOpen);
            }

            var player = session.Players[_playerId];
            int salt = player.GetReagent(ReagentType.Salt);
            if (Lbl("salt-check") != null)
                Lbl("salt-check")!.text = salt >= AutumnActionBindings.StasisSaltCost
                    ? $"you hold {salt} · ok"
                    : $"only {salt} — short";

            var payBtn = Btn("pay-salt-btn");
            if (payBtn != null)
                payBtn.SetEnabled(AutumnActionBindings.HasSaltForStasis(player));
        }

        VisualElement? FindEscapePanel(bool saltPanel)
        {
            if (Root == null) return null;
            foreach (var el in Root.Query(className: "escape").ToList())
            {
                var payBtn = el.Q<Button>("pay-salt-btn");
                if (saltPanel && payBtn != null) return el;
                if (!saltPanel && payBtn == null) return el;
            }
            return null;
        }

        IEnumerator PaySaltAndResolve()
        {
            if (_session == null || _bridge == null || _playerId < 0) yield break;
            if (!AutumnActionBindings.HasSaltForStasis(_session.Players[_playerId])) yield break;

            StasisOppositionEvent? clash = null;
            void Handler(IGameEvent e)
            {
                if (e is StasisOppositionEvent so) clash = so;
            }

            _session.OnEvent += Handler;
            _bridge.TrySubmit(new LeaveStasisCommand(_playerId));

            float elapsed = 0f;
            while (elapsed < 2f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            _session.OnEvent -= Handler;

            El("escape-view")!.style.display = DisplayStyle.None;
            El("result-view")!.style.display = DisplayStyle.Flex;

            var player = _session.Players[_playerId];
            if (Lbl("result-position") != null)
                Lbl("result-position")!.text = $"back in the forge at {player.StonePosition}";

            if (Lbl("result-progress") != null)
            {
                int fired = 0;
                foreach (var slot in player.CrucibleSlots)
                    if (slot.State == CrucibleCardState.Fired) fired++;
                Lbl("result-progress")!.text = $"Crucible progress intact — {fired} of {player.CrucibleSlots.Count} cards fired";
            }

            if (Lbl("result-salt") != null)
                Lbl("result-salt")!.text = $"{player.GetReagent(ReagentType.Salt)} Salt remaining";

            if (clash != null && Lbl("result-clash") != null)
            {
                Lbl("result-clash")!.style.display = DisplayStyle.Flex;
                bool won = clash.LoserId != _playerId;
                Lbl("result-clash")!.text = won
                    ? $"Stasis Opposition won ({clash.ReturnerRoll} vs {clash.OccupierRoll})"
                    : $"Stasis Opposition lost ({clash.ReturnerRoll} vs {clash.OccupierRoll}) — back to Stasis";
            }
        }
    }
}
