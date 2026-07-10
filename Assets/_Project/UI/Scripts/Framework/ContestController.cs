using System;
using System.Collections;
using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>
    /// Shared wizard plumbing for contest overlays: step panels, die animation, event await.
    /// </summary>
    public abstract class ContestController : OverlayController
    {
        protected void ShowStep(string panelName, params string[] allPanels)
        {
            foreach (var p in allPanels)
            {
                var el = El(p);
                if (el != null)
                    el.style.display = p == panelName ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        protected void SetWizard(string prefix, int total, int activeIndex)
        {
            for (int i = 1; i <= total; i++)
            {
                var dot = El($"{prefix}-{i}");
                if (dot == null) continue;
                dot.RemoveFromClassList("wizard-dot--active");
                dot.RemoveFromClassList("wizard-dot--done");
                if (i < activeIndex) dot.AddToClassList("wizard-dot--done");
                else if (i == activeIndex) dot.AddToClassList("wizard-dot--active");
            }
        }

        /// <summary>Animate a die label through random faces, settling on <paramref name="result"/>.</summary>
        protected IEnumerator RollDie(Label? pip, int result, int frames = DieAnimator.DefaultFrames,
            float step = DieAnimator.DefaultStepSec)
        {
            yield return DieAnimator.RollLabel(pip, result, frames, step);
        }

        /// <summary>
        /// Plays each round of a best-of-three (or single-roll) contest series with score updates.
        /// <paramref name="localIsAttacker"/> determines which pip is "you" vs "foe".
        /// </summary>
        protected IEnumerator AnimateContestSeries(
            Label? youPip, Label? foePip,
            Label? roundLabel, Label? scoreLabel,
            IReadOnlyList<ContestDiceRound> rounds,
            int attackerId, int defenderId, int localPlayerId,
            bool localIsAttacker)
        {
            int localWins = 0, foeWins = 0;
            int foeId = localIsAttacker ? defenderId : attackerId;

            for (int i = 0; i < rounds.Count; i++)
            {
                var round = rounds[i];
                if (roundLabel != null)
                    roundLabel.text = rounds.Count > 1 ? $"Round {i + 1}" : string.Empty;

                int youRoll = localIsAttacker ? round.AttackRoll : round.DefendRoll;
                int foeRoll = localIsAttacker ? round.DefendRoll : round.AttackRoll;

                yield return RollDie(youPip, youRoll);
                yield return RollDie(foePip, foeRoll);

                if (round.RoundWinnerId == localPlayerId) localWins++;
                else if (round.RoundWinnerId == foeId) foeWins++;

                if (scoreLabel != null && rounds.Count > 1)
                    scoreLabel.text = $"Score {localWins}–{foeWins}";
            }
        }

        protected void ResetSeriesLabels(string roundName = "roll-series-round",
            string scoreName = "roll-series-score", string? outcomeName = "roll-outcome")
        {
            var roundLbl = Lbl(roundName);
            var scoreLbl = Lbl(scoreName);
            if (roundLbl != null) roundLbl.style.display = DisplayStyle.None;
            if (scoreLbl != null) scoreLbl.style.display = DisplayStyle.None;
            if (outcomeName != null)
            {
                var outcome = Lbl(outcomeName);
                if (outcome != null) outcome.style.display = DisplayStyle.None;
            }
        }

        protected static void ShowSeriesLabels(Label? roundLbl, Label? scoreLbl, bool multiRound)
        {
            if (roundLbl != null)
                roundLbl.style.display = multiRound ? DisplayStyle.Flex : DisplayStyle.None;
            if (scoreLbl != null)
                scoreLbl.style.display = multiRound ? DisplayStyle.Flex : DisplayStyle.None;
        }

        protected IEnumerator WaitForEvent<T>(GameSession session, Action<T> onReceived, float timeout = 3f)
            where T : class, IGameEvent
        {
            T? captured = null;
            void Handler(IGameEvent e)
            {
                if (e is T t) captured = t;
            }

            session.OnEvent += Handler;
            float elapsed = 0f;
            while (captured == null && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            session.OnEvent -= Handler;
            if (captured != null)
                onReceived(captured);
        }
    }
}
