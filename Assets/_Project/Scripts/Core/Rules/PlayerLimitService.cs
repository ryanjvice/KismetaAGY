using System;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>Per-player card zone limits (base constants + adept resonant overrides).</summary>
    public static class PlayerLimitService
    {
        public static int GetHandLimit(GameSession session, PlayerState player)
        {
            int limit = WinterRules.HandLimit;
            if (AdeptEffectService.HasAdept(session, player, AdeptEffectService.PriestessArcana)
                && AdeptAttunement.IsPriestessResonant(session, player))
                limit = 7;

            limit -= ReversedCurseService.HandLimitPenalty(session, player);
            return Math.Max(1, limit);
        }

        public static int GetSpreadLimit(GameSession session, PlayerState player)
            => WinterRules.SpreadLimit;
    }
}
