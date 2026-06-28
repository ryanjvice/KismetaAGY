using Kismeta.Core.Domain;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    public static class StepRailBuilder
    {
        public const int MaxStepSlots = 5;

        public static void EnsureBuilt(VisualElement? rail, Season season)
        {
            if (rail == null) return;

            var labels = LabelsFor(season);
            if (rail.userData is Season builtFor && builtFor == season && rail.childCount == labels.Length)
                return;

            rail.Clear();
            for (int i = 0; i < labels.Length; i++)
            {
                var step = new VisualElement { name = $"step-{i}" };
                step.AddToClassList("step");
                step.AddToClassList("step--inline");

                var dot = new VisualElement();
                dot.AddToClassList("step__dot");
                step.Add(dot);

                var label = new Label(labels[i]);
                label.AddToClassList("step__label");
                step.Add(label);

                rail.Add(step);
            }

            for (int i = labels.Length; i < MaxStepSlots; i++)
            {
                var hidden = rail.Q<VisualElement>($"step-{i}");
                if (hidden != null)
                    hidden.style.display = DisplayStyle.None;
            }

            rail.userData = season;
        }

        static string[] LabelsFor(Season season) => season switch
        {
            Season.Spring => new[] { "age", "sign", "harvest", "hub", "lock" },
            Season.Summer => new[] { "craft", "consort", "activate" },
            Season.Autumn => new[] { "fire", "temper", "oppose" },
            Season.Winter => new[] { "unlock", "wager", "limits", "transit" },
            _ => new[] { "step" }
        };
    }
}
