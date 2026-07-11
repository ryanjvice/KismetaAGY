using System;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>Per-player card zone limits (base constants + adept resonant + spread passives).</summary>
    public static class PlayerLimitService
    {
        public static int GetHandLimit(GameSession session, PlayerState player)
        {
            int limit = WinterRules.HandLimit;
            if (AdeptEffectService.HasAdept(session, player, AdeptEffectService.PriestessArcana)
                && AdeptAttunement.IsPriestessResonant(session, player))
                limit = 7;

            limit += CountQueenHandBonus(session, player);
            limit -= ReversedCurseService.HandLimitPenalty(session, player);
            return Math.Max(1, limit);
        }

        public static int GetSpreadLimit(GameSession session, PlayerState player)
        {
            int limit = WinterRules.SpreadLimit;
            limit += CountQueenSpreadBonus(session, player);
            return Math.Max(1, limit);
        }

        static int CountQueenHandBonus(GameSession session, PlayerState player)
            => CountQueenBonus(session, player, SpreadLimitEffectCatalog.GrantsHandBonus);

        static int CountQueenSpreadBonus(GameSession session, PlayerState player)
            => CountQueenBonus(session, player, SpreadLimitEffectCatalog.GrantsSpreadBonus);

        static int CountQueenBonus(
            GameSession session,
            PlayerState player,
            System.Func<CardDefinition, bool> predicate)
        {
            var db = session.Rules?.CardDatabase;
            if (db == null)
                return 0;

            int bonus = 0;
            foreach (var cardId in player.Spread)
            {
                var inst = session.GetCard(cardId);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def != null && predicate(def))
                    bonus++;
            }

            return bonus;
        }
    }
}
