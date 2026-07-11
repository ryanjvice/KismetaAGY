using System;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;

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

            if (ctx.Session != null)
            {
                var attacker = ctx.Session.Players[ownPid];
                if (ReversedCurseService.RequiresDualAnte(ctx.Session, attacker)
                    && player.Spread.Count < 2)
                    return false;
                if (!AiContestPolicy.HasFavorableAttackModifiers(
                        ctx.Session, ContestKind.Duel, ownPid, targetId))
                    return false;
            }

            if (rollValue >= InitiateChance)
                return false;

            var targetCardId = FindDuelTargetCard(ctx, targetId);
            if (targetCardId == null)
                return false;

            command = new InitiateDuelCommand(
                ownPid, targetId, targetCardId, player.Spread[0]);
            return true;
        }

        public static bool ShouldAcceptAsDefender(GameSession session, ContestKind kind,
            int attackerId, int defenderId)
        {
            var mods = ContestModifierService.Build(session, kind, attackerId, defenderId);
            int attackNet = mods.Attacker.AttackBonus;
            int defendNet = mods.Defender.DefendBonus;
            return defendNet >= attackNet - 1;
        }

        static string? FindDuelTargetCard(GameContext ctx, int defenderId)
        {
            if (ctx.Session == null)
            {
                var spread = ctx.PublicView.Players[defenderId].Spread;
                return spread.Count > 0 ? spread[0] : null;
            }

            var defender = ctx.Session.Players[defenderId];
            var duelable = DuelProtectionService.FilterDuelTargets(
                ctx.Session, defender, ctx.PublicView.Players[defenderId].Spread);
            return duelable.Count > 0 ? duelable[0] : null;
        }

        /// <summary>
        /// Returns the ID of a Duel target (first opponent with duelable spread cards), or -1 if none.
        /// </summary>
        public static int FindDuelTarget(GameContext ctx, int ownPid)
        {
            foreach (var opp in ctx.PublicView.Players)
            {
                if (opp.PlayerId == ownPid) continue;
                if (ctx.Session != null)
                {
                    var defender = ctx.Session.Players[opp.PlayerId];
                    if (DuelProtectionService.FilterDuelTargets(ctx.Session, defender, opp.Spread).Count > 0)
                        return opp.PlayerId;
                }
                else if (opp.Spread.Count > 0)
                {
                    return opp.PlayerId;
                }
            }
            return -1;
        }
    }
}
