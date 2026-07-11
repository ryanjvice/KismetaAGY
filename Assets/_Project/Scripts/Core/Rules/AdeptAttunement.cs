using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Attuned (resonant) predicate: adept Sign matches player CurrentSign or a built Astral House.
    /// </summary>
    public static class AdeptAttunement
    {
        public static bool IsAttuned(PlayerState player, CardDefinition adeptDef)
            => IsResonant(player, adeptDef);

        public static bool IsResonant(PlayerState player, CardDefinition adeptDef)
        {
            if (adeptDef.Sign == ZodiacSign.None)
                return false;
            if (player.CurrentSign == adeptDef.Sign)
                return true;
            return player.AstralHouses.Contains(adeptDef.Sign);
        }

        public static bool IsResonant(GameSession session, int playerId, int arcanaNumber)
        {
            var player = session.Players[playerId];
            var db = session.Rules?.CardDatabase;
            if (db == null)
                return false;

            var adeptId = AdeptEffectService.FindAdeptInstance(session, player, arcanaNumber);
            if (adeptId == null)
                return false;

            var inst = session.GetCard(adeptId);
            var def = inst != null ? db.GetById(inst.DefinitionId) : null;
            return def != null && IsResonant(player, def);
        }

        public static bool IsPriestessResonant(GameSession session, PlayerState player)
            => IsResonant(session, player.PlayerId, AdeptEffectService.PriestessArcana);

        public static bool IsEmperorResonant(GameSession session, PlayerState player)
            => IsResonant(session, player.PlayerId, AdeptEffectService.EmperorArcana);

        public static bool IsHierophantResonant(GameSession session, PlayerState player)
            => IsResonant(session, player.PlayerId, AdeptEffectService.HierophantArcana);

        public static bool IsHermitResonant(GameSession session, PlayerState player)
            => IsResonant(session, player.PlayerId, 9);

        public static bool IsStarResonant(GameSession session, PlayerState player)
            => IsResonant(session, player.PlayerId, AdeptEffectService.StarArcana);

        public static bool IsDevilResonant(GameSession session, PlayerState player)
            => IsResonant(session, player.PlayerId, AdeptEffectService.DevilArcana);

        public static int HierophantMaxShiftDelta(GameSession session, PlayerState player)
            => IsHierophantResonant(session, player) ? 2 : 1;
    }
}
