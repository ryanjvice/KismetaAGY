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

        /// <summary>
        /// Spring Step 3: Deal Harvest cards. Any Adept cards drawn are queued in
        /// <see cref="BoardState.PendingAdeptDecisions"/> for GameLoop to resolve.
        /// Any async Fate effects are queued in <see cref="BoardState.PendingFateDecisions"/>.
        /// </summary>
        void ExecuteHarvest(GameSession session, int playerId);

        /// <summary>Spring Step 4: Player assigns all current Hand+Spread cards to zones.</summary>
        CommandResult HandleCommune(GameSession session, int playerId,
            IReadOnlyList<string> spreadCardIds, IReadOnlyList<string> handCardIds);

        /// <summary>Player purchases an Adept card by spending 3 cards.</summary>
        CommandResult HandleBuyAdept(GameSession session, int playerId, string adeptCardId,
            IReadOnlyList<string> paymentCardIds, string? swapOutAdeptId = null);

        /// <summary>Player declines an Adept card; it is discarded to the Common Discard.</summary>
        CommandResult HandleDeclineAdept(GameSession session, int playerId, string adeptCardId);

        /// <summary>
        /// Routes a single card popped from the Common Deck to the correct zone:
        ///   Fate  → Arcanum immediately; resolves/queues fate effects.
        ///   Adept → PendingAdeptDecisions limbo (Deck zone placeholder).
        ///   Minor → Player's Hand; if <paramref name="minorPool"/> is non-null, appends the id to it.
        /// Returns true only for minor arcana (card placed in Hand).
        /// </summary>
        bool RouteDrawnCard(GameSession session, int playerId, string cardId,
            ICollection<string>? minorPool = null);
    }
}
