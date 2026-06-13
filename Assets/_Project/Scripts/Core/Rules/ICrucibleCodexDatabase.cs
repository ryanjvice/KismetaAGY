using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>Read-only access to the Crucible Codex formula table loaded from crucible-codex.json.</summary>
    public interface ICrucibleCodexDatabase
    {
        /// <summary>Returns the formula for the given codex variant and slot index, or null if not found.</summary>
        CodexFormulaDefinition? GetFormula(CodexVariant codex, int slotIndex);

        /// <summary>Returns all 4 formulas for the given codex variant, ordered by slot index.</summary>
        IReadOnlyList<CodexFormulaDefinition> GetEntriesForCodex(CodexVariant codex);
    }
}
