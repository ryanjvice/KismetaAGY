using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Shared contest modifier / active-effects panel used by attacker and defender UIs.</summary>
    public static class ContestEffectsBindings
    {
        public static void ConfigureDuel(
            VisualElement? host,
            VisualElement? featuredHost,
            VisualElement? listHost,
            Label? subtitleLabel,
            GameSession session,
            int perspectivePlayerId,
            int attackerId,
            int defenderId,
            string? targetCardId,
            string? anteCardId)
        {
            var snapshot = ActiveEffectsService.BuildDuelRelevant(
                session, perspectivePlayerId, attackerId, defenderId, targetCardId, anteCardId);
            Populate(host, featuredHost, listHost, subtitleLabel, session, ContestKind.Duel,
                attackerId, defenderId, snapshot);
        }

        public static void ConfigureGambit(
            VisualElement? host,
            VisualElement? featuredHost,
            VisualElement? listHost,
            Label? subtitleLabel,
            GameSession session,
            int perspectivePlayerId,
            int attackerId,
            int defenderId,
            string? offeredCardId)
        {
            var snapshot = ActiveEffectsService.BuildGambitRelevant(
                session, perspectivePlayerId, attackerId, defenderId, offeredCardId);
            Populate(host, featuredHost, listHost, subtitleLabel, session, ContestKind.Gambit,
                attackerId, defenderId, snapshot);
        }

        public static void ConfigureOpposition(
            VisualElement? host,
            VisualElement? featuredHost,
            VisualElement? listHost,
            Label? subtitleLabel,
            GameSession session,
            int challengerId,
            int defenderId)
        {
            var snapshot = ActiveEffectsService.BuildOppositionRelevant(
                session, challengerId, defenderId);
            Populate(host, featuredHost, listHost, subtitleLabel, session, ContestKind.Opposition,
                challengerId, defenderId, snapshot);
        }

        static void Populate(
            VisualElement? host,
            VisualElement? featuredHost,
            VisualElement? listHost,
            Label? subtitleLabel,
            GameSession session,
            ContestKind kind,
            int attackerId,
            int defenderId,
            ActiveEffectsSnapshot snapshot)
        {
            if (host == null) return;

            var modifiers = ContestModifierService.Build(session, kind, attackerId, defenderId);
            bool boardBestOfThree = session.Board.ContestEffects.IsBestOfThree(kind);
            bool scopedBestOfThree = kind == ContestKind.Duel && modifiers.AttackerForcesBestOfThree;

            bool showFeatured = (boardBestOfThree || scopedBestOfThree)
                && !string.IsNullOrWhiteSpace(snapshot.CosmicAge.Description);
            if (featuredHost != null)
            {
                featuredHost.Clear();
                if (showFeatured)
                    ActiveEffectsRows.PopulateFeatured(featuredHost, snapshot.CosmicAge);
                SetHidden(featuredHost, !showFeatured);
            }

            listHost?.Clear();
            if (listHost != null && snapshot.Sections.Count > 0)
                ActiveEffectsAccordion.Populate(listHost, snapshot.Sections);

            SetHidden(host, !showFeatured && snapshot.Sections.Count == 0
                && string.IsNullOrWhiteSpace(snapshot.Subtitle));

            if (subtitleLabel != null && !string.IsNullOrWhiteSpace(snapshot.Subtitle))
                subtitleLabel.text = snapshot.Subtitle;
        }

        static void SetHidden(VisualElement element, bool hidden)
            => element.EnableInClassList("is-hidden", hidden);
    }
}
