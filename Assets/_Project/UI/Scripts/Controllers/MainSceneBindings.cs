using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Views;
using Kismeta.UI;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    /// <summary>Shared bind helpers for season main scenes.</summary>
    internal static class MainSceneBindings
    {
        public static void ApplySeasonClass(VisualElement? root, Season season)
        {
            if (root == null) return;
            root.RemoveFromClassList("screen--spring");
            root.RemoveFromClassList("screen--summer");
            root.RemoveFromClassList("screen--autumn");
            root.RemoveFromClassList("screen--winter");
            root.AddToClassList($"screen--{season.ToString().ToLowerInvariant()}");
        }

        public static void BindStatusBar(VisualElement? root, GameSession session, GameLoop loop)
        {
            if (root == null) return;

            var seasonLbl = root.Q<Label>("status-season");
            if (seasonLbl != null)
                seasonLbl.text = session.Phase.CurrentSeason.ToString();

            var stepLbl = root.Q<Label>("status-step");
            if (stepLbl != null)
                stepLbl.text = session.Phase.CurrentStep.Name;

            var roundLbl = root.Q<Label>("status-round");
            if (roundLbl != null)
                roundLbl.text = $"Round {session.Board.RoundNumber} · {session.Board.CosmicAgeSign}";

            var hintLbl = root.Q<Label>("hint-label");
            if (hintLbl != null)
            {
                hintLbl.text = session.IsOver
                    ? $"Game over — winner P{session.WinnerPlayerId}"
                    : $"Your turn · {loop.PendingHint} · P{loop.ActivePlayerId}";
            }
        }

        public static void BindPassButton(VisualElement? root, GameSession session, CommandBridge bridge)
        {
            var pass = root?.Q<Button>("pass-btn");
            if (pass == null) return;

            var hint = bridge.PendingHint;
            bool canPass = bridge.CanSubmit && !session.IsOver &&
                hint is ActionHint.SummerAction or ActionHint.AutumnAction or ActionHint.WinterAction;
            pass.SetEnabled(canPass);
        }

        public static void BindStepRail(VisualElement? rail, int currentStepIndex, int stepCount, string activeAccentClass)
        {
            if (rail == null) return;

            for (int i = 0; i < stepCount; i++)
            {
                var step = rail.Q<VisualElement>($"step-{i}");
                if (step == null) continue;

                var dot = step.Q(className: "step__dot");
                if (dot == null) continue;

                dot.RemoveFromClassList("step__dot--done");
                dot.RemoveFromClassList("step__dot--active");
                dot.RemoveFromClassList("step__dot--locked");
                dot.RemoveFromClassList(activeAccentClass);

                if (i < currentStepIndex)
                    dot.AddToClassList("step__dot--done");
                else if (i == currentStepIndex)
                {
                    dot.AddToClassList("step__dot--active");
                    dot.AddToClassList(activeAccentClass);
                }
                else
                    dot.AddToClassList("step__dot--locked");
            }
        }

        public static PublicPlayerView? LocalPlayer(GamePublicView view, int localPlayerId)
        {
            foreach (var p in view.Players)
            {
                if (p.PlayerId == localPlayerId)
                    return p;
            }
            return view.Players.Count > 0 ? view.Players[0] : null;
        }
    }
}
