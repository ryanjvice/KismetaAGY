using System.Collections.Generic;
using Kismeta.Core.Domain;

namespace Kismeta.Core.Entities
{
    /// <summary>
    /// All mutable state for one Alchemist player. Represents the full per-player model —
    /// public zones (Spread, Arcanum), private zone (Hand), reagents, board pieces.
    /// </summary>
    public sealed class PlayerState
    {
        public int PlayerId { get; }
        public PlayerColor Color { get; }
        public bool IsAgekeeper { get; set; }
        public ZodiacSign CurrentSign { get; set; }

        /// <summary>Crucible Codex variant assigned to this player at game start (persists whole game).</summary>
        public CodexVariant AssignedCodex { get; set; } = CodexVariant.None;

        // Card zones
        public List<string> Spread { get; } = new();
        public List<string> Hand   { get; } = new();
        public List<string> Arcanum{ get; } = new();

        // Reagent inventory (indexed by ReagentType)
        private readonly int[] _reagents = new int[5];

        // Philosopher's Stone
        public StonePosition StonePosition { get; set; } = StonePosition.Start;
        public StoneState StoneState       { get; set; } = StoneState.Tempering;
        public int StoneWardCount          { get; set; }
        /// <summary>
        /// True if the stone left Stasis this Autumn. Blocks Temper for the round —
        /// the stone must complete a full round of Forging before it may advance.
        /// Cleared by WinterRules.Transit at round end.
        /// </summary>
        public bool ReturnedFromStasisThisRound { get; set; }

        // Cauldrons lit by Coal (indexed by Suit — Wands/Cups/Pentacles/Swords)
        private readonly bool[] _cauldronLit = new bool[4];

        // Remaining unplaced Coals (start = 4; 1 per active Crucible Card)
        public int UnplacedCoals { get; set; } = 4;

        // Fateful Wager (reset each Transit)
        /// <summary>The predicted sign for the current Fateful Wager, or None if no wager was placed.</summary>
        public ZodiacSign FatefulWagerSign { get; set; } = ZodiacSign.None;
        /// <summary>Cards held out of play as the wager stake (cleared at resolution).</summary>
        public List<string> FatefulWagerCards { get; } = new();

        // Tower Fate: arrested Adept card IDs (must spend 1 Salt each to refresh)
        /// <summary>Adept cards in Arcanum that are arrested by The Tower and cannot contribute to alignment.</summary>
        public HashSet<string> ArrestedAdepts { get; } = new();

        // Astral Houses placed (ZodiacSign values the player has claimed)
        public HashSet<ZodiacSign> AstralHouses { get; } = new();
        public int UnplacedAstralHouses { get; set; } = 4;

        // Crucible Card slots (4 per player)
        public List<PlayerCrucibleSlot> CrucibleSlots { get; } = new();

        public PlayerState(int playerId, PlayerColor color)
        {
            PlayerId = playerId;
            Color = color;
        }

        public int GetReagent(ReagentType type) => _reagents[(int)type];
        public void AddReagent(ReagentType type, int amount = 1)
        {
            _reagents[(int)type] += amount;
            if (_reagents[(int)type] < 0) _reagents[(int)type] = 0;
        }
        public bool SpendReagent(ReagentType type, int amount = 1)
        {
            if (_reagents[(int)type] < amount) return false;
            _reagents[(int)type] -= amount;
            return true;
        }

        public bool IsCauldronLit(Suit suit) => suit != Suit.None && _cauldronLit[(int)suit - 1];
        public void LightCauldron(Suit suit)
        {
            if (suit != Suit.None)
                _cauldronLit[(int)suit - 1] = true;
        }
    }
}
