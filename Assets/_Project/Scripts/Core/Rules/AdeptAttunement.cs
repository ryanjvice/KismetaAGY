using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Minimal resonant predicate for Phase 1 combat adepts (Strength, Chariot).
    /// Full resonant layer arrives in Phase 4.
    /// </summary>
    public static class AdeptAttunement
    {
        public static bool IsResonant(PlayerState player, CardDefinition adeptDef)
        {
            if (adeptDef.Sign == ZodiacSign.None)
                return false;
            if (player.CurrentSign == adeptDef.Sign)
                return true;
            return player.AstralHouses.Contains(adeptDef.Sign);
        }
    }
}
