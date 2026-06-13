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

        public int CommonDeckCount    => CommonDeck.Count;
        public int CommonDiscardCount => CommonDiscard.Count;
    }
}
