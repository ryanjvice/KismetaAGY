using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.UI;
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
                Lbl("waiting-label")!.text = loop.ActivePlayerId >= 0
                    ? $"Waiting · P{loop.ActivePlayerId} is deciding…"
                    : "Waiting for the next step…";
            }

            if (Lbl("season-label") != null)
                Lbl("season-label")!.text =
                    $"{session.Phase.CurrentSeason} — {session.Phase.CurrentStep.Name}";
        }
    }
}
