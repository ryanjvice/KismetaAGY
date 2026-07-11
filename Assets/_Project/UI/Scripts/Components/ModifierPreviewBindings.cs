using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Rules;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Contextual modifier strip for action overlays (craft, build, forge, etc.).</summary>
    public static class ModifierPreviewBindings
    {
        public static void PopulateForContext(
            VisualElement? host,
            GameSession session,
            int playerId,
            ActionHint hint = ActionHint.None,
            EffectGlanceScope? scopeOverride = null)
        {
            if (host == null || playerId < 0 || playerId >= session.Players.Count)
                return;

            var context = EffectGlanceContext.From(session, hint);
            if (scopeOverride.HasValue)
            {
                context = new EffectGlanceContext(
                    context.Season, context.Hint, scopeOverride.Value, context.SpreadOverride);
            }

            var glance = ActiveEffectsService.BuildGlance(session, playerId, context, maxChips: 5);
            Populate(host, glance);
        }

        public static void Populate(VisualElement? host, EffectGlanceSnapshot glance)
        {
            if (host == null)
                return;

            host.Clear();
            if (glance.Chips.Count == 0 && glance.OverflowCount == 0)
            {
                host.EnableInClassList("is-hidden", true);
                return;
            }

            host.EnableInClassList("is-hidden", false);

            var eyebrow = new Label("with current effects");
            eyebrow.AddToClassList("modifier-preview__eyebrow");
            host.Add(eyebrow);

            var row = new VisualElement();
            row.AddToClassList("modifier-preview__row");
            foreach (var chip in glance.Chips)
                row.Add(EffectGlanceChip(chip));

            if (glance.OverflowCount > 0)
            {
                var more = new Label($"+{glance.OverflowCount}");
                more.AddToClassList("effect-glance-chip");
                more.AddToClassList("effect-glance-chip--overflow");
                row.Add(more);
            }

            host.Add(row);
        }

        static VisualElement EffectGlanceChip(ActiveEffectItem item)
        {
            var chip = new Label(item.Title);
            chip.AddToClassList("effect-glance-chip");
            chip.AddToClassList(item.Polarity switch
            {
                ActiveEffectPolarity.Buff => "effect-glance-chip--buff",
                ActiveEffectPolarity.Debuff => "effect-glance-chip--debuff",
                _ => "effect-glance-chip--neutral"
            });
            if (!string.IsNullOrWhiteSpace(item.Description))
                chip.tooltip = item.Description;
            return chip;
        }
    }
}
