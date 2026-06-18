using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.UI;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    /// <summary>
    /// Phase 1 gameplay placeholder until season main scenes land in later batches.
    /// Shows season/hint state and exposes Pass for free-action phases.
    /// </summary>
    public sealed class GameplayHudController : ScreenController
    {
        public override string ScreenId => ScreenIds.GameplayHud;

        private CommandBridge? _bridge;

        protected override void Wire()
        {
            Btn("pass-btn")!.clicked += OnPass;
        }

        public void BindState(GameSession session, GameLoop loop, CommandBridge bridge)
        {
            _bridge = bridge;
            if (Root == null) return;

            var season = session.Phase.CurrentSeason;
            ApplySeasonClass(season);

            if (Lbl("status-season") != null)
                Lbl("status-season")!.text = season.ToString();

            if (Lbl("status-step") != null)
                Lbl("status-step")!.text = session.Phase.CurrentStep.Name;

            if (Lbl("status-round") != null)
                Lbl("status-round")!.text = $"Round {session.Board.RoundNumber} · {session.Board.CosmicAgeSign}";

            if (Lbl("hint-label") != null)
            {
                var hint = loop.PendingHint;
                Lbl("hint-label")!.text = session.IsOver
                    ? $"Game over — winner P{session.WinnerPlayerId}"
                    : $"Your turn · {hint} · P{loop.ActivePlayerId}";
            }

            if (Btn("pass-btn") != null)
            {
                bool canPass = bridge.CanSubmit && !session.IsOver &&
                    (loop.PendingHint is ActionHint.SummerAction
                        or ActionHint.AutumnAction
                        or ActionHint.WinterAction);
                Btn("pass-btn")!.SetEnabled(canPass);
            }
        }

        private void OnPass() => _bridge?.SubmitPass();

        private void ApplySeasonClass(Season season)
        {
            if (Root == null) return;
            Root.RemoveFromClassList("screen--spring");
            Root.RemoveFromClassList("screen--summer");
            Root.RemoveFromClassList("screen--autumn");
            Root.RemoveFromClassList("screen--winter");
            Root.AddToClassList($"screen--{season.ToString().ToLowerInvariant()}");
        }
    }
}
