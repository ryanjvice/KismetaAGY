using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;

namespace Kismeta.Core.Players
{
    /// <summary>Shared contest modifier heuristics for AI duel/gambit/opposition decisions.</summary>
    public static class AiContestPolicy
    {
        public static bool HasFavorableAttackModifiers(
            GameSession session, ContestKind kind, int attackerId, int defenderId)
        {
            var mods = ContestModifierService.Build(session, kind, attackerId, defenderId);
            return mods.Attacker.AttackBonus >= mods.Defender.DefendBonus;
        }

        public static bool CanAffordContestStart(
            GameSession session, PlayerState attacker, ContestKind kind)
        {
            if (!ReversedCurseService.RequiresSaltForContestStart(session, attacker, kind))
                return true;
            return attacker.GetReagent(ReagentType.Salt) >= 1;
        }
    }
}
