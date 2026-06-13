using Kismeta.Core.Domain;

namespace Kismeta.Core.Entities
{
    /// <summary>
    /// Immutable definition of one cell in the Crucible Codex table.
    /// Each player has 4 of these (one per slot/cauldron) determined by their assigned CodexVariant.
    /// </summary>
    public sealed class CodexFormulaDefinition
    {
        /// <summary>Which Codex card this entry belongs to (A–D).</summary>
        public CodexVariant Codex { get; }

        /// <summary>Index of the Crucible Card slot this formula unlocks (0–3).</summary>
        public int SlotIndex { get; }

        /// <summary>Human-readable cauldron label (e.g. "Red").</summary>
        public string Cauldron { get; }

        /// <summary>The suit of the cauldron that is lit when this formula is satisfied.</summary>
        public Suit CauldronSuit { get; }

        public CodexFormulaType FormulaType { get; }

        /// <summary>Required planet for AnyThreePlanet formulas; None for RankSum.</summary>
        public Planet RequiredPlanet { get; }

        /// <summary>Required suit for RankSum formulas; None for AnyThreePlanet.</summary>
        public Suit RequiredSuit { get; }

        /// <summary>Minimum rank-point sum for RankSum formulas; 0 for AnyThreePlanet.</summary>
        public int MinRankSum { get; }

        /// <summary>Short label shown on the Codex (e.g. "Any Three Mars", "25 Total Ranks · Wands").</summary>
        public string DisplayName { get; }

        public CodexFormulaDefinition(
            CodexVariant codex,
            int slotIndex,
            string cauldron,
            Suit cauldronSuit,
            CodexFormulaType formulaType,
            Planet requiredPlanet,
            Suit requiredSuit,
            int minRankSum,
            string displayName)
        {
            Codex          = codex;
            SlotIndex      = slotIndex;
            Cauldron       = cauldron;
            CauldronSuit   = cauldronSuit;
            FormulaType    = formulaType;
            RequiredPlanet = requiredPlanet;
            RequiredSuit   = requiredSuit;
            MinRankSum     = minRankSum;
            DisplayName    = displayName;
        }
    }
}
