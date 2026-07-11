using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>Post-Fire spread forge effects (rank 7 reagent grants).</summary>
    public static class ForgeEffectService
    {
        public static void TryGrantRank7Reagents(GameSession session, int playerId, ICardDatabase db)
        {
            var player = session.Players[playerId];
            foreach (var cardId in player.Spread)
            {
                var inst = session.GetCard(cardId);
                if (inst == null) continue;
                var def = db.GetById(inst.DefinitionId);
                if (def == null || !SpreadForgeEffectCatalog.IsRank7ForgeReagent(def))
                    continue;

                player.AddReagent(SpreadForgeEffectCatalog.ReagentForRank7(def));
            }
        }
    }
}
