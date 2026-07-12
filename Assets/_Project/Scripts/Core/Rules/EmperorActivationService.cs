using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;

namespace Kismeta.Core.Rules
{
    /// <summary>Whether The Emperor protection can be activated from the UI this moment.</summary>
    public static class EmperorActivationService
    {
        public static bool CanActivate(
            GameSession session,
            int playerId,
            ActionHint hint,
            bool humanCanSubmit)
        {
            if (!humanCanSubmit || hint != ActionHint.SpringAction)
                return false;
            if (session.Phase.CurrentSeason != Season.Spring)
                return false;
            if (playerId < 0 || playerId >= session.Players.Count)
                return false;

            var player = session.Players[playerId];
            return AdeptEffectService.CanUseOncePerAge(session, player, AdeptEffectService.EmperorArcana);
        }
    }
}
