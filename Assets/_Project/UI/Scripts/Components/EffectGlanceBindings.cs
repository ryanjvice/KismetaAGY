using System;
using Kismeta.Core.Rules;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Compact ambient effect chips for inventory/header HUD surfaces.</summary>
    public static class EffectGlanceBindings
    {
        const string WiredKey = "effect-glance-wired";

        public static void Wire(VisualElement? root, Action? onOpenFullPanel)
        {
            if (root == null || ReferenceEquals(root.userData, WiredKey))
                return;

            root.userData = WiredKey;
            root.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target is VisualElement target
                    && target.ClassListContains("effect-glance-chip"))
                {
                    onOpenFullPanel?.Invoke();
                }
            });
        }

        public static void Populate(
            VisualElement? root,
            EffectGlanceSnapshot glance,
            Action? onOpenFullPanel = null)
        {
            var strip = root?.Q<VisualElement>("effect-glance-strip");
            var effectsFab = root?.Q<Button>("effects-fab");
            if (strip == null && effectsFab == null)
                return;

            Wire(root, onOpenFullPanel);

            if (strip != null)
            {
                strip.Clear();
                foreach (var chip in glance.Chips)
                    strip.Add(BuildChip(chip));

                if (glance.OverflowCount > 0)
                {
                    var more = new Label($"+{glance.OverflowCount}");
                    more.AddToClassList("effect-glance-chip");
                    more.AddToClassList("effect-glance-chip--overflow");
                    strip.Add(more);
                }

                strip.EnableInClassList("is-hidden", glance.Chips.Count == 0 && glance.OverflowCount == 0);
            }

            if (effectsFab != null)
                PopulateFabBadge(effectsFab, glance.TotalCount);
        }

        static VisualElement BuildChip(ActiveEffectItem item)
        {
            var chip = new Label(item.Title);
            chip.AddToClassList("effect-glance-chip");
            chip.AddToClassList(PolarityClass(item.Polarity));
            chip.AddToClassList(BadgeToneClass(item.Badge.Tone));
            if (!string.IsNullOrWhiteSpace(item.Description))
                chip.tooltip = item.Description;
            return chip;
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

        static string PolarityClass(ActiveEffectPolarity polarity) => polarity switch
        {
            ActiveEffectPolarity.Buff => "effect-glance-chip--buff",
            ActiveEffectPolarity.Debuff => "effect-glance-chip--debuff",
            _ => "effect-glance-chip--neutral"
        };

        static string BadgeToneClass(ActiveEffectBadgeTone tone) => tone switch
        {
            ActiveEffectBadgeTone.Pending => "effect-glance-chip--pending",
            ActiveEffectBadgeTone.Arrested => "effect-glance-chip--debuff",
            ActiveEffectBadgeTone.Used => "effect-glance-chip--used",
            _ => string.Empty
        };
    }
}
