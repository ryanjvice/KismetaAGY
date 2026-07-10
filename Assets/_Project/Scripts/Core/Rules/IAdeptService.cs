using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    public interface IAdeptService : IRuleService
    {
        CommandResult TryMagicianSwap(GameSession session, int playerId, string cardId, bool toSpread);
        CommandResult TryProtectSpreadCards(GameSession session, int playerId, IReadOnlyList<string> cardIds);
        CommandResult TryShiftZodiac(GameSession session, int playerId, int delta);
        CommandResult TryShiftOppositionZodiac(GameSession session, int playerId, int delta);
        CommandResult TryDevilSteal(GameSession session, int playerId, string sacrificeCardId,
            int targetPlayerId, string stolenCardId);
        CommandResult TryCompletePriestessHarvest(GameSession session, int playerId,
            IReadOnlyList<string> returnCardIds);
        bool IsPriestessHarvestPending(GameSession session, int playerId);
    }
}
