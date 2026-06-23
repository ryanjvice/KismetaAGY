using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    public static class UiMotion
    {
        const float SheetRisePx = 24f;
        const float SheetRiseSec = 0.26f;

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

        public static void AnimateWheelSettle(VisualElement wheelHost, System.Action onSettled)
        {
            if (wheelHost.childCount == 0)
            {
                onSettled();
                return;
            }

            var glyph = wheelHost[0];
            glyph.style.rotate = new Rotate(0);
            float start = Time.realtimeSinceStartup;
            const float spinSec = 0.55f;
            void Tick()
            {
                float t = (Time.realtimeSinceStartup - start) / spinSec;
                if (t >= 1f)
                {
                    glyph.style.rotate = new Rotate(0);
                    onSettled();
                    return;
                }
                float angle = Mathf.Lerp(720f, 0f, 1f - (1f - t) * (1f - t));
                glyph.style.rotate = new Rotate(angle);
                wheelHost.schedule.Execute(Tick).ExecuteLater(16);
            }
            wheelHost.schedule.Execute(Tick).ExecuteLater(16);
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
    }
}
