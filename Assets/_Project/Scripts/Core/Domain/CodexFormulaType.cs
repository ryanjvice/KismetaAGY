namespace Kismeta.Core.Domain
{
    /// <summary>
    /// The two formula types that appear on the Crucible Codex.
    /// AnyThreePlanet — discard exactly 3 cards that share the specified planet.
    /// RankSum        — discard any number of cards of the specified suit whose rank-point total is >= minRankSum.
    /// </summary>
    public enum CodexFormulaType
    {
        None = 0,
        AnyThreePlanet,
        RankSum
    }
}
