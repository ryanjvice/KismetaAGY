using Kismeta.Core.Domain;
using Kismeta.Core.Players;

namespace Kismeta.UI.Narrative
{
    public static class NarrativeStepResolver
    {
        public static string ResolveSeasonIntro(Season season) =>
            $"{season.ToString().ToLowerInvariant()}.intro";

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
