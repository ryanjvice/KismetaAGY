using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Player-to-player zodiac aspect checks used by Magnus trade ratio rules.
    /// Aligned when both current signs share sign, planet, or element.
    /// </summary>
    public static class PlayerAspectAlignment
    {
        public static bool ShareAspect(ZodiacSign a, ZodiacSign b)
        {
            if (a == ZodiacSign.None || b == ZodiacSign.None) return false;
            if (a == b) return true;
            if (Correspondence.PlanetFor(a) == Correspondence.PlanetFor(b)) return true;
            if (Correspondence.ElementFor(a) == Correspondence.ElementFor(b)) return true;
            return false;
        }

        public static bool ArePlayersAlignedForTrade(GameSession session, int traderId, int rivalId)
        {
            if (traderId < 0 || traderId >= session.Players.Count) return true;
            if (rivalId < 0 || rivalId >= session.Players.Count) return true;
            return ShareAspect(session.Players[traderId].CurrentSign, session.Players[rivalId].CurrentSign);
        }

        public static bool IsMagnusTradeRatioValid(GameSession session, int initiatorId, int targetId,
            int offerCount, int requestCount)
        {
            if (session.Mode != GameMode.MagnusAlchemist) return true;
            if (ArePlayersAlignedForTrade(session, initiatorId, targetId)) return true;
            return offerCount >= 2 * requestCount;
        }

        /// <summary>
        /// Magnus Alchemist: misaligned challenger gains +1 dice (Duel/Gambit) or +1 alignment (Opposition).
        /// </summary>
        public static bool IsMagnusMisalignedChallenger(GameSession session, int challengerId, int defenderId)
        {
            if (session.Mode != GameMode.MagnusAlchemist) return false;
            return !ArePlayersAlignedForTrade(session, challengerId, defenderId);
        }

        public static int MagnusContestDiceBonus(GameSession session, int challengerId, int defenderId)
            => IsMagnusMisalignedChallenger(session, challengerId, defenderId) ? 1 : 0;

        public static int MagnusOppositionAlignmentBonus(GameSession session, int challengerId, int defenderId)
            => MagnusContestDiceBonus(session, challengerId, defenderId);
    }
}
