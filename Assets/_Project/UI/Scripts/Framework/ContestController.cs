using System;
using System.Collections;
using Kismeta.Core.Commands;
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
