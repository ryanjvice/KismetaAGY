using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Binds local player reagent counts into the persistent player HUD.</summary>
    public static class PlayerHudBindings
    {
        public static void Bind(VisualElement root, PlayerState player)
        {
            foreach (ReagentType rt in System.Enum.GetValues(typeof(ReagentType)))
                SetCount(root, ReagentLabelName(rt), player.GetReagent(rt));
        }

        static void SetCount(VisualElement root, string name, int count)
        {
            var lbl = root.Q<Label>(name);
            if (lbl != null)
                lbl.text = count.ToString();
        }

        static string ReagentLabelName(ReagentType type) => type switch
        {
            ReagentType.Sulphur => "hud-reagent-sulphur",
            ReagentType.AquaRegia => "hud-reagent-aquaregia",
            ReagentType.Vitriol => "hud-reagent-vitriol",
            ReagentType.Quicksilver => "hud-reagent-quicksilver",
            _ => "hud-reagent-salt"
        };
    }
}
