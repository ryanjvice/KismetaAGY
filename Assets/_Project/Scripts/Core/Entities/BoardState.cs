using System.Collections.Generic;
using Kismeta.Core.Domain;

namespace Kismeta.Core.Entities
{
    /// <summary>
    /// Shared board state: current Cosmic Age, common deck, stasis occupancy.
    /// </summary>
    public sealed class BoardState
    {
        public int RoundNumber { get; set; } = 1;
        public ZodiacSign CosmicAgeSign { get; set; } = ZodiacSign.None;

        // Common Kismeta deck and discard pile (card instance IDs)
        public Stack<string> CommonDeck    { get; } = new();
        public List<string>  CommonDiscard { get; } = new();

        // Crucible deck for this game (instance IDs, shuffled per group)
        public Stack<string> CrucibleDeck    { get; } = new();
        public List<string>  CrucibleDiscard { get; } = new();

        // Stasis zones indexed by player id
        public Dictionary<int, bool> StasisOccupancy { get; } = new();

        // Pending Adept purchase decisions dequeued by GameLoop after each player's Harvest
        // Each entry is (playerId, adeptCardId)
        public List<(int PlayerId, string AdeptCardId)> PendingAdeptDecisions { get; } = new();

        // Pending Fate card async decisions (Moon, Fool, Lovers) after all Harvests
        // Each entry is (playerId, fateCardId, arcanaNumber)
        public List<(int PlayerId, string FateCardId, int ArcanaNumber)> PendingFateDecisions { get; } = new();

        /// <summary>
        /// Temporarily holds the 4 card instance IDs drawn by The Moon so the UI can
        /// present exactly those cards for the keep-2 decision. Cleared after resolution.
        /// </summary>
        public List<string> FateMoonDrawnCardIds { get; } = new();

        /// <summary>Players who must return 2 Priestess harvest cards before Spring continues.</summary>
        public HashSet<int> PendingPriestessReturns { get; } = new();

        /// <summary>Active stepped harvest deal for UI-driven dealing; null when idle.</summary>
        public ActiveHarvestDeal? ActiveHarvestDeal { get; set; }

        // Per-round Cosmic Effect flags set by CosmicEffectService; cleared each Transit
        public CosmicEffectFlags CosmicEffect { get; set; }

        // Per-age contest modifiers (Justice, future card effects); cleared each Transit
        public ContestEffectFlags ContestEffects { get; set; }

        /// <summary>Contest awaiting defender response, or null when none is in flight.</summary>
        public PendingContest? PendingContest { get; set; }

        public int CommonDeckCount    => CommonDeck.Count;
        public int CommonDiscardCount => CommonDiscard.Count;
    }

    /// <summary>
    /// Flags derived from the active Cosmic Age sign that modify harvest and crafting rules.
    /// Reset to default each Winter Transit.
    /// </summary>
    public struct CosmicEffectFlags
    {
        /// <summary>Bonus added to the base Harvest of 3 (Aries +1, Libra +1).</summary>
        public int HarvestBaseBonus;

        /// <summary>Court Cards of this Suit count as any Suit for crafting/activation. None = no effect.</summary>
        public Suit WildCourtSuit;

        /// <summary>Craft Salt for 2 cards instead of 3 (Cancer, Capricorn).</summary>
        public bool SaltCostsTwo;

        /// <summary>Craft this Suit's Reagent for 2 cards instead of 3 (requires lit Cauldron).</summary>
        public Suit CheapCraftSuit;

        /// <summary>The Reagent type that benefits from the cheap-craft rule.</summary>
        public ReagentType CheapCraftReagent;

        public static CosmicEffectFlags Default => new()
        {
            HarvestBaseBonus  = 0,
            WildCourtSuit     = Suit.None,
            SaltCostsTwo      = false,
            CheapCraftSuit    = Suit.None,
            CheapCraftReagent = ReagentType.Salt,
        };
    }
}
