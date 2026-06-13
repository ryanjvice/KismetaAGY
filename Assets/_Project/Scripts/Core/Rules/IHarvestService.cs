using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Calculates and executes the Harvest step (Spring Step 3).
    /// Base 3 cards + bonuses from Zodiac sign, Astral Houses, Adepts, Agekeeper's Boon.
    /// </summary>
    public interface IHarvestService : IRuleService
    {
        int CalculateHarvestCount(GameSession session, int playerId);
        void ExecuteHarvest(GameSession session, int playerId);
    }
}
