using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Spread/arcanum chip halos driven by ActiveEffects evaluators.</summary>
    public static class CardChipEffectBindings
    {
        public static void ApplySpreadEffect(
            GameSession session,
            PlayerState player,
            VisualElement chip,
            string cardId,
            CardDefinition def,
            string? crucibleSlotLabel = null)
        {
            ClearIndicators(chip);

            var cosmic = session.Board.CosmicAgeSign;
            int alignPts = AlignmentService.ScoreCard(def.Suit, def.Planet, cosmic);
            var state = SpreadCardEffectEvaluator.Evaluate(
                session, player, cardId, def, cosmic, alignPts, crucibleSlotLabel);
            if (state.IsInactive)
                return;

            ApplyPolarityDot(chip, state.Polarity, state.Description);
        }

        public static void ApplyAdeptEffect(
            GameSession session,
            PlayerState player,
            VisualElement chip,
            string cardId,
            CardDefinition def)
        {
            ClearIndicators(chip);

            if (def.MajorArcanaType != MajorArcanaType.Adept)
                return;

            bool attuned = AdeptAttunement.IsResonant(player, def);
            bool arrested = player.ArrestedAdepts.Contains(cardId);
            bool used = player.UsedAdeptInstanceIdsThisAge.Contains(cardId);
            var badge = AdeptEffectCatalog.BadgeFor(def.ArcanaNumber, arrested, used, attuned);
            if (string.IsNullOrWhiteSpace(badge.Text))
                return;

            var toneClass = badge.Tone switch
            {
                ActiveEffectBadgeTone.Attuned => "card-chip__effect-badge--attuned",
                ActiveEffectBadgeTone.Arrested => "card-chip__effect-badge--debuff",
                ActiveEffectBadgeTone.Used => "card-chip__effect-badge--used",
                ActiveEffectBadgeTone.Available => "card-chip__effect-badge--available",
                _ => "card-chip__effect-badge--active"
            };

            var badgeLbl = new Label(ShortBadge(badge.Text));
            badgeLbl.AddToClassList("card-chip__effect-badge");
            badgeLbl.AddToClassList(toneClass);
            badgeLbl.pickingMode = PickingMode.Ignore;
            badgeLbl.tooltip = badge.Text;
            chip.Add(badgeLbl);
        }

        static void ApplyPolarityDot(VisualElement chip, ActiveEffectPolarity polarity, string description)
        {
            var dot = new VisualElement();
            dot.AddToClassList("card-chip__effect-dot");
            dot.AddToClassList(polarity switch
            {
                ActiveEffectPolarity.Buff => "card-chip__effect-dot--buff",
                ActiveEffectPolarity.Debuff => "card-chip__effect-dot--debuff",
                _ => "card-chip__effect-dot--neutral"
            });
            dot.pickingMode = PickingMode.Ignore;
            if (!string.IsNullOrWhiteSpace(description))
                dot.tooltip = description;
            chip.Add(dot);
        }

        static void ClearIndicators(VisualElement chip)
        {
            chip.Q(className: "card-chip__effect-dot")?.RemoveFromHierarchy();
            chip.Q(className: "card-chip__effect-badge")?.RemoveFromHierarchy();
        }

        static string ShortBadge(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "?";
            if (text.StartsWith("attuned", System.StringComparison.OrdinalIgnoreCase))
                return "attuned";
            if (text.StartsWith("arrested", System.StringComparison.OrdinalIgnoreCase))
                return "arrested";
            if (text.StartsWith("once per age", System.StringComparison.OrdinalIgnoreCase))
                return usedLabel(text);
            if (text.StartsWith("active", System.StringComparison.OrdinalIgnoreCase))
                return "active";
            return text.Length <= 10 ? text : text[..8] + "…";
        }

        static string usedLabel(string text) =>
            text.Contains("used", System.StringComparison.OrdinalIgnoreCase) ? "used" : "ready";
    }
}
