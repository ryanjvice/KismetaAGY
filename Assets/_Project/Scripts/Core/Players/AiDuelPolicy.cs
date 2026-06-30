using System;
using Kismeta.Core.Commands;

namespace Kismeta.Core.Players
{
    /// <summary>
    /// Test-focused AI policy for initiating Duels during Summer.
    /// </summary>
    public static class AiDuelPolicy
    {
        public const double InitiateChance = 0.30;
        public const int MinSpreadForAnte = 2;

        public static bool ShouldInitiate(
            GameContext ctx,
            int ownPid,
            Random rng,
            out InitiateDuelCommand? command)
        {
            return ShouldInitiate(ctx, ownPid, rng.NextDouble(), out command);
        }

        public static bool ShouldInitiate(
            GameContext ctx,
            int ownPid,
            double rollValue,
            out InitiateDuelCommand? command)
        {
            command = null;

            var player = ctx.PublicView.Players[ownPid];
            if (player.Spread.Count < MinSpreadForAnte)
                return false;

            if (player.DuelChallengedRivalId >= 0)
                return false;

            var targetId = FindDuelTarget(ctx, ownPid);
            if (targetId < 0)
                return false;

            var rivalSpread = ctx.PublicView.Players[targetId].Spread;
            if (rivalSpread.Count == 0)
                return false;

            if (rollValue >= InitiateChance)
                return false;

            command = new InitiateDuelCommand(
                ownPid, targetId, rivalSpread[0], player.Spread[0]);
            return true;
        }

        /// <summary>
        /// Returns the ID of a Duel target (first opponent with spread cards), or -1 if none.
        /// </summary>
        public static int FindDuelTarget(GameContext ctx, int ownPid)
        {
            foreach (var opp in ctx.PublicView.Players)
            {
                if (opp.PlayerId == ownPid) continue;
                if (opp.Spread.Count > 0)
                    return opp.PlayerId;
            }
            return -1;
        }
    }
}
