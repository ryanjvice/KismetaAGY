using Kismeta.Core.Domain;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
  public static class ReagentChipFactory
    {
        public static VisualElement Create(ReagentType type, int count)
        {
            var chip = new VisualElement();
            chip.AddToClassList("reagent-chip");
            chip.AddToClassList(ReagentChipClass(type));
            var lbl = new Label(count.ToString());
            lbl.AddToClassList("reagent-chip__count");
            chip.Add(lbl);
            return chip;
        }

        public static string ReagentChipClass(ReagentType type) => type switch
        {
            ReagentType.Sulphur => "reagent-chip--sulphur",
            ReagentType.AquaRegia => "reagent-chip--aqua",
            ReagentType.Vitriol => "reagent-chip--vitriol",
            ReagentType.Quicksilver => "reagent-chip--quick",
            _ => "reagent-chip--salt"
        };
    }
}
