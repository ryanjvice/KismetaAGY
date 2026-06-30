using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.UI;
using Kismeta.UI.Components;
using Kismeta.UI.Diagnostics;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class WaitingHudController : ScreenController
    {
        public override string ScreenId => ScreenIds.Waiting;

        protected override void Wire() { }

        public void BindState(GameSession session, GameLoop loop)
        {
            if (Lbl("waiting-label") != null)
            {
                Lbl("waiting-label")!.text = FormatWaitingLabel(session, loop);
            }

            if (Lbl("season-label") != null)
                Lbl("season-label")!.text =
                    $"{session.Phase.CurrentSeason} — {session.Phase.CurrentStep.Name}";

            if (loop.PendingHumanController != null)
            {
                // #region agent log
                DebugSessionLog.Write("E", "WaitingHudController.BindState", "waiting hud during human turn",
                    "{\"hint\":\"" + loop.PendingHint + "\",\"step\":\"" + session.Phase.CurrentStep.Name +
                    "\",\"label\":\"" + FormatWaitingLabel(session, loop).Replace("\"", "'") + "\"}");
                // #endregion
            }
        }

        static string FormatWaitingLabel(GameSession session, GameLoop loop)
        {
            if (loop.PendingHumanController != null && loop.ActivePlayerId >= 0)
            {
                var hint = loop.PendingHint;
                if (hint is ActionHint.TradeResponse or ActionHint.DuelResponse
                    or ActionHint.GambitResponse or ActionHint.OppositionResponse)
                    return $"Respond to {hint} · P{loop.ActivePlayerId}";

                return $"Your turn · P{loop.ActivePlayerId}";
            }

            if (loop.TurnPlayerId >= 0)
            {
                var name = CeremonyBindings.PlayerName(session, loop.TurnPlayerId);
                return $"Waiting · {name}'s turn…";
            }

            if (loop.ActivePlayerId >= 0)
                return $"Waiting · P{loop.ActivePlayerId} is deciding…";

            return "Waiting for the next step…";
        }
    }
}
