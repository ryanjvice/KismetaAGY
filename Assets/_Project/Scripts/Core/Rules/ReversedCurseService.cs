using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Reversed curse gate: active when in Spread, misaligned with Cosmic Age, and not Magician-resonant.
    /// </summary>
    public static class ReversedCurseService
    {
        public static bool IsCurseActive(
            GameSession session,
            PlayerState owner,
            string cardInstanceId,
            CardDefinition def)
        {
            if (!def.EffectType.Equals("Reversed", System.StringComparison.OrdinalIgnoreCase))
                return false;
            if (!owner.Spread.Contains(cardInstanceId))
                return false;
            if (MagicianResonantNegates(session, owner))
                return false;

            int align = AlignmentService.ScoreCard(def.Suit, def.Planet, session.Board.CosmicAgeSign);
            return align <= 0;
        }

        public static bool MagicianResonantNegates(GameSession session, PlayerState owner)
            => AdeptAttunement.IsMagicianResonant(session, owner);

        public static bool HasActiveCurse(GameSession session, PlayerState player, ReversedCurseKind kind)
        {
            var db = session.Rules?.CardDatabase;
            if (db == null)
                return false;

            foreach (var cardId in player.Spread)
            {
                var inst = session.GetCard(cardId);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def == null)
                    continue;
                if (!IsCurseActive(session, player, cardId, def))
                    continue;
                if (ReversedCurseCatalog.KindFor(def.Id) == kind)
                    return true;
            }

            return false;
        }

        public static int HandLimitPenalty(GameSession session, PlayerState player)
            => HasActiveCurse(session, player, ReversedCurseKind.HandLimitPenalty) ? 1 : 0;

        public static int CraftExtraCardCost(GameSession session, int playerId)
            => HasActiveCurse(session, session.Players[playerId], ReversedCurseKind.CraftExtraCard) ? 1 : 0;

        public static int AdeptBuyExtraCardCost(GameSession session, int playerId)
            => HasActiveCurse(session, session.Players[playerId], ReversedCurseKind.AdeptBuyExtraCard) ? 1 : 0;

        public static bool RequiresDualAnte(GameSession session, PlayerState attacker)
            => HasActiveCurse(session, attacker, ReversedCurseKind.DualAnte);

        public static bool RequiresOpponentChooseAnte(GameSession session, PlayerState attacker)
            => HasActiveCurse(session, attacker, ReversedCurseKind.OpponentChoosesAnte);

        public static bool RequiresSaltForContestStart(GameSession session, PlayerState attacker, ContestKind kind)
        {
            if (kind is not (ContestKind.Gambit or ContestKind.Opposition))
                return false;
            return HasActiveCurse(session, attacker, ReversedCurseKind.SaltCostOppositionGambit);
        }

        public static bool IsTradeOfferRatioValid(
            GameSession session,
            int initiatorId,
            int offerCount,
            int requestCount)
        {
            var initiator = session.Players[initiatorId];
            if (!HasActiveCurse(session, initiator, ReversedCurseKind.TradeDoubleOffer))
                return true;
            if (requestCount == 0)
                return true;
            return offerCount >= requestCount * 2;
        }
    }
}
