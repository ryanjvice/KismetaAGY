using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>Applies rank-9 Social draw rewards after successful contests (owner-only).</summary>
    public static class SpreadSocialEffectService
    {
        public static void TryApplyTradeDraws(GameSession session, int initiatorId, int targetId, ICardDatabase db)
        {
            TryApplyContestDraws(session, initiatorId, ContestKind.Trade, db);
            TryApplyContestDraws(session, targetId, ContestKind.Trade, db);
        }

        public static void TryApplyContestWinDraw(GameSession session, int winnerId, ContestKind kind, ICardDatabase db)
            => TryApplyContestDraws(session, winnerId, kind, db);

        static void TryApplyContestDraws(GameSession session, int playerId, ContestKind kind, ICardDatabase db)
        {
            var player = session.Players[playerId];
            foreach (var cardId in player.Spread)
            {
                var inst = session.GetCard(cardId);
                if (inst == null) continue;
                var def = db.GetById(inst.DefinitionId);
                if (def == null) continue;
                if (!SpreadSocialEffectCatalog.TryGetDrawOnSuccess(def, kind, out int drawCount))
                    continue;

                AdeptEffectService.DrawCardsToHand(session, playerId, drawCount);
            }
        }
    }
}
