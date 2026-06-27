using Kismeta.Core.Domain;
using Kismeta.Core.Players;

namespace Kismeta.UI.Narrative
{
    public static class NarrativeStepResolver
    {
        public static string ResolveSeasonIntro(Season season) =>
            $"{season.ToString().ToLowerInvariant()}.intro";

        public static string ResolveSpringHub(ActionHint hint, bool isCommune)
        {
            if (isCommune || hint == ActionHint.Commune)
                return "spring.commune";
            if (hint is ActionHint.RollZodiac or ActionHint.AcknowledgeSign)
                return "spring.sign";
            return "spring.harvest";
        }

        public static string ResolveSummerAction(
            SummerOverlayHost? summerOverlays,
            ContestOverlayHost? contestOverlays)
        {
            var contestId = contestOverlays?.ActiveNarrativeStepId;
            if (!string.IsNullOrEmpty(contestId))
                return contestId;

            var summerId = summerOverlays?.ActiveNarrativeStepId;
            if (!string.IsNullOrEmpty(summerId))
                return summerId;

            return "summer.hub";
        }

        public static string ResolveAutumnAction(
            StoneState stoneState,
            AutumnOverlayHost? autumnOverlays,
            ContestOverlayHost? contestOverlays)
        {
            var contestId = contestOverlays?.ActiveNarrativeStepId;
            if (!string.IsNullOrEmpty(contestId))
                return contestId;

            var autumnId = autumnOverlays?.ActiveNarrativeStepId;
            if (!string.IsNullOrEmpty(autumnId))
                return autumnId;

            if (stoneState == StoneState.Stasis)
                return "autumn.leavestasis";

            return "autumn.fire";
        }

        public static string ResolveWinterScreen(string screenId) => screenId switch
        {
            ScreenIds.WinterUnlock => "winter.unlock",
            ScreenIds.FatefulWager => "winter.wager",
            ScreenIds.CardLimits => "winter.limits",
            ScreenIds.AgeClosing => "winter.transit",
            ScreenIds.CraftReagent => "summer.craft",
            _ => "winter.unlock"
        };

        public static string? StoneBreadcrumbLabel(StoneState state) => state switch
        {
            StoneState.Tempering => "Tempering",
            StoneState.Forging => "Forging",
            StoneState.Stasis => "In Stasis",
            StoneState.Complete => "Altar",
            _ => null
        };
    }
}
