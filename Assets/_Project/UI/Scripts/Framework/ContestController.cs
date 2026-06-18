using System;
using System.Collections;
using Kismeta.Core.Commands;
using Kismeta.Core.Entities;
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
        protected IEnumerator RollDie(Label? pip, int result, int frames = 12, float step = 0.07f)
        {
            if (pip == null) yield break;
            for (int i = 0; i < frames; i++)
            {
                pip.text = UnityEngine.Random.Range(1, 13).ToString();
                yield return new WaitForSeconds(step);
            }
            pip.text = result.ToString();
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
