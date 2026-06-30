using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Returns Adept ward reagents to a player's supply when the Adept leaves Arcanum.
    /// Crucible wards are permanent and are not refunded.
    /// </summary>
    public static class WardRefundHelper
    {
        public static void RefundAdeptWards(PlayerState player, string adeptCardId)
        {
            int count = player.ClearAdeptWards(adeptCardId);
            if (count > 0)
                player.AddReagent(ReagentType.Salt, count);
        }
    }
}
