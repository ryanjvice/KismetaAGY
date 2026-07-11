using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>Per-player card zone limits (base constants + adept resonant overrides).</summary>
    public static class PlayerLimitService
    {
        public static int GetHandLimit(GameSession session, PlayerState player)
        {
            if (AdeptEffectService.HasAdept(session, player, AdeptEffectService.PriestessArcana)
                && AdeptAttunement.IsPriestessResonant(session, player))
                return 7;
            return WinterRules.HandLimit;
        }

        public static int GetSpreadLimit(GameSession session, PlayerState player)
            => WinterRules.SpreadLimit;
    }
}
