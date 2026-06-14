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
