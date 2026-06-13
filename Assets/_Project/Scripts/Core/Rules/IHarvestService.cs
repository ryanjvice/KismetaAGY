using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Handles all Spring phase steps: Cosmic Age roll, personal Zodiac roll,
    /// Harvest card dealing, and the Commune zone-assignment step.
    /// </summary>
    public interface IHarvestService : IRuleService
    {
        /// <summary>Spring Step 1: Agekeeper rolls the Cosmic Age Die.</summary>
        void RollCosmicAge(GameSession session);

        /// <summary>Spring Step 2: One player rolls their personal Zodiac Die.</summary>
        void RollZodiac(GameSession session, int playerId);

        /// <summary>Returns the number of cards the player draws this Harvest.</summary>
        int CalculateHarvestCount(GameSession session, int playerId);

        /// <summary>Spring Step 3: Deal Harvest cards from the common deck into the player's Hand.</summary>
        void ExecuteHarvest(GameSession session, int playerId);

        /// <summary>Spring Step 4: Player assigns all current Hand cards to Spread or Hand zones.</summary>
        CommandResult HandleCommune(GameSession session, int playerId,
            IReadOnlyList<string> spreadCardIds, IReadOnlyList<string> handCardIds);
    }
}
