using Kismeta.UI;

namespace Kismeta.UI.Controllers
{
    /// <summary>Maps open Summer/Autumn overlays to action-group rail indices.</summary>
    public static class ActionGroupRailBindings
    {
        public static int? ResolveSummerActiveGroup(SummerOverlayHost? summer, ContestOverlayHost? contest)
        {
            if (contest != null && contest.IsOpen)
            {
                var contestIndex = contest.ActiveSummerGroupIndex;
                if (contestIndex.HasValue)
                    return contestIndex;
            }

            if (summer != null && summer.IsOpen)
                return summer.ActiveActionGroupIndex;

            return null;
        }

        public static int? ResolveAutumnActiveGroup(AutumnOverlayHost? autumn, ContestOverlayHost? contest)
        {
            if (contest != null && contest.IsOpen)
            {
                var contestIndex = contest.ActiveAutumnGroupIndex;
                if (contestIndex.HasValue)
                    return contestIndex;
            }

            if (autumn != null && autumn.IsOpen)
                return autumn.ActiveActionGroupIndex;

            return null;
        }
    }
}
