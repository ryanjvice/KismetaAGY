using System.Text;
using Kismeta.Core.Rules;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Active-effects glance state on inventory/header HUD surfaces.</summary>
    public static class EffectGlanceBindings
    {
        static readonly string[] FabPolarityClasses =
        {
            "effects-fab--debuff",
            "effects-fab--buff",
            "effects-fab--pending",
            "effects-fab--neutral"
        };

        public static void Populate(VisualElement? root, EffectGlanceSnapshot glance)
        {
            var effectsFab = root?.Q<Button>("effects-fab");
            if (effectsFab == null)
                return;

            PopulateEffectsFab(effectsFab, glance);
        }

        static void PopulateEffectsFab(Button effectsFab, EffectGlanceSnapshot glance)
        {
            PopulateFabBadge(effectsFab, glance.TotalCount);
            ApplyFabPolarity(effectsFab, glance);
            effectsFab.tooltip = BuildTooltip(glance);
        }

        static void PopulateFabBadge(Button effectsFab, int totalCount)
        {
            var badge = effectsFab.Q<Label>("effects-fab-count");
            if (badge == null)
            {
                badge = new Label();
                badge.name = "effects-fab-count";
                badge.AddToClassList("effects-fab__count");
                badge.pickingMode = PickingMode.Ignore;
                effectsFab.Add(badge);
            }

            bool show = totalCount > 0;
            badge.text = show ? totalCount.ToString() : string.Empty;
            badge.EnableInClassList("is-hidden", !show);
            effectsFab.EnableInClassList("effects-fab--has-count", show);
        }

        static void ApplyFabPolarity(Button effectsFab, EffectGlanceSnapshot glance)
        {
            foreach (var cls in FabPolarityClasses)
                effectsFab.EnableInClassList(cls, false);

            var polarityClass = DominantFabClass(glance);
            if (polarityClass != null)
                effectsFab.AddToClassList(polarityClass);
        }

        static string? DominantFabClass(EffectGlanceSnapshot glance)
        {
            if (glance.TotalCount == 0)
                return null;

            if (glance.Chips.Count == 0)
                return "effects-fab--neutral";

            var chip = glance.Chips[0];
            if (chip.Polarity == ActiveEffectPolarity.Debuff
                || chip.Badge.Tone == ActiveEffectBadgeTone.Arrested)
                return "effects-fab--debuff";

            if (chip.Badge.Tone == ActiveEffectBadgeTone.Pending)
                return "effects-fab--pending";

            if (chip.Polarity == ActiveEffectPolarity.Buff)
                return "effects-fab--buff";

            return "effects-fab--neutral";
        }

        static string BuildTooltip(EffectGlanceSnapshot glance)
        {
            if (glance.TotalCount == 0)
                return string.Empty;

            var sb = new StringBuilder();
            sb.Append($"{glance.TotalCount} active effect{(glance.TotalCount == 1 ? "" : "s")}");

            int shown = 0;
            foreach (var chip in glance.Chips)
            {
                if (shown >= 3)
                    break;
                if (string.IsNullOrWhiteSpace(chip.Title))
                    continue;
                sb.Append('\n').Append(chip.Title);
                shown++;
            }

            if (glance.OverflowCount > 0)
                sb.Append($"\n+{glance.OverflowCount} more");

            return sb.ToString();
        }
    }
}
