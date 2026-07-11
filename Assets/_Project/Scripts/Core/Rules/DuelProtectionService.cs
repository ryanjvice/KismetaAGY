using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>King V2 spread passives: suit-wide duel protection (king itself remains vulnerable).</summary>
    public static class DuelProtectionService
    {
        public static bool IsKingV2Passive(CardDefinition def)
            => def.Rank == Rank.King && def.Variant == CardVariant.Two
               && def.EffectType.Equals("Passive", System.StringComparison.OrdinalIgnoreCase);

        public static bool IsSuitProtectedFromDuel(
            GameSession session,
            PlayerState defender,
            string targetCardId)
        {
            var db = session.Rules?.CardDatabase;
            if (db == null)
                return false;

            var targetInst = session.GetCard(targetCardId);
            var targetDef = targetInst != null ? db.GetById(targetInst.DefinitionId) : null;
            if (targetDef == null || targetDef.Suit == Suit.None)
                return false;

            foreach (var cardId in defender.Spread)
            {
                if (cardId == targetCardId)
                    continue;

                var inst = session.GetCard(cardId);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def == null || !IsKingV2Passive(def))
                    continue;

                if (def.Suit == targetDef.Suit)
                    return true;
            }

            return false;
        }

        public static bool IsDuelTargetAllowed(
            GameSession session,
            PlayerState defender,
            string targetCardId)
            => !AdeptEffectService.IsEmperorProtected(session, defender, targetCardId)
               && !IsSuitProtectedFromDuel(session, defender, targetCardId);

        public static System.Collections.Generic.List<string> FilterDuelTargets(
            GameSession session,
            PlayerState defender,
            System.Collections.Generic.IEnumerable<string> cardIds)
        {
            var list = new System.Collections.Generic.List<string>();
            foreach (var id in cardIds)
            {
                if (IsDuelTargetAllowed(session, defender, id))
                    list.Add(id);
            }

            return list;
        }
    }
}
