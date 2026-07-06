using System.Collections;
using System.Collections.Generic;
using Kismeta.UI.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    public static class UiMotion
    {
        const float SheetRisePx = 24f;
        const float SheetRiseSec = 0.26f;
        const float ZodiacWheelSpinDegPerSec = 540f;
        const float StaggerFadeSec = 0.26f;
        const float StaggerDelaySec = 0.11f;
        const float StaggerSlidePx = 8f;

        sealed class RevealPickState
        {
            public PickingMode OriginalPickingMode;
            public object? PreviousUserData;
        }

        public static void AnimateSheetRise(VisualElement? sheetRoot)
        {
            if (sheetRoot == null) return;
            sheetRoot.style.translate = new Translate(0, SheetRisePx, 0);
            float start = Time.realtimeSinceStartup;
            void Tick()
            {
                float t = (Time.realtimeSinceStartup - start) / SheetRiseSec;
                if (t >= 1f)
                {
                    sheetRoot.style.translate = new Translate(0, 0, 0);
                    return;
                }
                float y = Mathf.Lerp(SheetRisePx, 0f, t);
                sheetRoot.style.translate = new Translate(0, y, 0);
                sheetRoot.schedule.Execute(Tick).ExecuteLater(16);
            }
            sheetRoot.schedule.Execute(Tick).ExecuteLater(16);
        }

        public static IEnumerator SpinZodiacWheel(VisualElement? wheel, float durationSec)
        {
            if (wheel == null) yield break;

            float start = Time.realtimeSinceStartup;
            float angle = 0f;
            while (Time.realtimeSinceStartup - start < durationSec)
            {
                angle -= ZodiacWheelSpinDegPerSec * Time.deltaTime;
                wheel.style.rotate = new Rotate(angle);
                yield return null;
            }

            wheel.style.rotate = new Rotate(0);
        }

        public static void AnimateWheelSettle(VisualElement wheelHost, System.Action onSettled)
        {
            var wheel = wheelHost.Q("wheel-zodiac");
            if (wheel == null)
            {
                onSettled();
                return;
            }

            wheel.style.rotate = new Rotate(0);
            float start = Time.realtimeSinceStartup;
            const float spinSec = 0.55f;
            void Tick()
            {
                float t = (Time.realtimeSinceStartup - start) / spinSec;
                if (t >= 1f)
                {
                    wheel.style.rotate = new Rotate(0);
                    onSettled();
                    return;
                }
                float angle = Mathf.Lerp(720f, 0f, 1f - (1f - t) * (1f - t));
                wheel.style.rotate = new Rotate(angle);
                wheelHost.schedule.Execute(Tick).ExecuteLater(16);
            }
            wheelHost.schedule.Execute(Tick).ExecuteLater(16);
        }

        public static void AnimateHeaderToggle(VisualElement? overlayRoot, bool expanding)
        {
            if (overlayRoot == null || !expanding) return;
            overlayRoot.style.translate = new Translate(0, -SheetRisePx, 0);
            float start = Time.realtimeSinceStartup;
            void Tick()
            {
                float t = (Time.realtimeSinceStartup - start) / SheetRiseSec;
                if (t >= 1f)
                {
                    overlayRoot.style.translate = new Translate(0, 0, 0);
                    return;
                }
                float y = Mathf.Lerp(-SheetRisePx, 0f, t);
                overlayRoot.style.translate = new Translate(0, y, 0);
                overlayRoot.schedule.Execute(Tick).ExecuteLater(16);
            }
            overlayRoot.schedule.Execute(Tick).ExecuteLater(16);
        }

        public static void AnimateInventoryToggle(VisualElement? overlayRoot, bool expanding)
        {
            if (overlayRoot == null || !expanding) return;
            AnimateSheetRise(overlayRoot);
        }

        public static void PulseChip(VisualElement chip)
        {
            chip.style.scale = new Scale(new Vector3(1.08f, 1.08f, 1f));
            chip.schedule.Execute(() => chip.style.scale = new Scale(Vector3.one)).ExecuteLater(120);
        }

        public static void PulseTrackNode(VisualElement? node)
        {
            if (node == null) return;
            node.style.scale = new Scale(new Vector3(1.12f, 1.12f, 1f));
            node.schedule.Execute(() => node.style.scale = new Scale(Vector3.one)).ExecuteLater(180);
        }

        public static void StaggerFadeIn(
            IReadOnlyList<IReadOnlyList<VisualElement?>> groups,
            float durationSec = StaggerFadeSec,
            float staggerSec = StaggerDelaySec,
            float slidePx = StaggerSlidePx)
        {
            if (groups == null || groups.Count == 0)
                return;

            if (GameSettings.ReducedMotion)
            {
                foreach (var group in groups)
                {
                    if (group == null)
                        continue;
                    foreach (var el in group)
                        RevealInstant(el);
                }
                return;
            }

            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                if (group == null)
                    continue;

                var elements = CollectElements(group);
                if (elements.Count == 0)
                    continue;

                foreach (var el in elements)
                    PrepareHidden(el, slidePx);

                int delayMs = Mathf.RoundToInt(i * staggerSec * 1000f);
                elements[0].schedule.Execute(() => AnimateGroupIn(elements, durationSec, slidePx))
                    .ExecuteLater(delayMs);
            }
        }

        public static void FadeIn(VisualElement? element, float durationSec = StaggerFadeSec, float slidePx = StaggerSlidePx)
        {
            if (element == null)
                return;

            if (GameSettings.ReducedMotion)
            {
                RevealInstant(element);
                return;
            }

            PrepareHidden(element, slidePx);
            element.schedule.Execute(() => AnimateGroupIn(new List<VisualElement> { element }, durationSec, slidePx))
                .ExecuteLater(0);
        }

        static List<VisualElement> CollectElements(IReadOnlyList<VisualElement?> group)
        {
            var elements = new List<VisualElement>();
            foreach (var el in group)
            {
                if (el != null)
                    elements.Add(el);
            }
            return elements;
        }

        static void PrepareHidden(VisualElement el, float slidePx)
        {
            el.userData = new RevealPickState
            {
                OriginalPickingMode = el.pickingMode,
                PreviousUserData = el.userData is RevealPickState ? null : el.userData
            };
            el.style.opacity = 0;
            el.style.translate = new Translate(0, slidePx, 0);
            el.pickingMode = PickingMode.Ignore;
        }

        static void AnimateGroupIn(List<VisualElement> elements, float durationSec, float slidePx)
        {
            if (elements.Count == 0)
                return;

            float start = Time.realtimeSinceStartup;
            var scheduler = elements[0];

            void Tick()
            {
                float t = (Time.realtimeSinceStartup - start) / durationSec;
                if (t >= 1f)
                {
                    foreach (var el in elements)
                        RevealInstant(el);
                    return;
                }

                float opacity = t;
                float y = Mathf.Lerp(slidePx, 0f, t);
                foreach (var el in elements)
                {
                    el.style.opacity = opacity;
                    el.style.translate = new Translate(0, y, 0);
                }

                scheduler.schedule.Execute(Tick).ExecuteLater(16);
            }

            scheduler.schedule.Execute(Tick).ExecuteLater(16);
        }

        static void RevealInstant(VisualElement? el)
        {
            if (el == null)
                return;

            el.style.opacity = 1;
            el.style.translate = new Translate(0, 0, 0);

            if (el.userData is RevealPickState state)
            {
                el.pickingMode = state.OriginalPickingMode;
                el.userData = state.PreviousUserData;
            }
        }
    }
}
