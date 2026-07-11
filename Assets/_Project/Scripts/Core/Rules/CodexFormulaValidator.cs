using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Validates that a set of card instance IDs satisfies a Crucible Codex Activation Formula.
    ///
    /// AnyThreePlanet — exactly 3 cards, every card's planet matches the required planet.
    /// RankSum        — one or more cards, all of the required suit, combined rank points >= minRankSum.
    ///                  Aces are flexible: any combination of 1 or 15 is tried; validation succeeds
    ///                  if at least one combination reaches the threshold.
    ///
    /// Cards must be present in the player's Spread (Hand is not allowed for activation).
    /// </summary>
    public sealed class CodexFormulaValidator
    {
        private readonly ICardDatabase _db;

        public CodexFormulaValidator(ICardDatabase db)
        {
            _db = db;
        }

        /// <summary>
        /// Validates the resolved card definitions against the formula.
        /// This is the primary entry point; CrucibleRules resolves definitions before calling this.
        /// </summary>
        public (bool ok, string reason) ValidateDefs(
            IReadOnlyList<CardDefinition> defs,
            CodexFormulaDefinition formula)
        {
            if (defs == null || defs.Count == 0)
                return (false, "No cards submitted.");

            return formula.FormulaType switch
            {
                CodexFormulaType.AnyThreePlanet => ValidatePlanetWithDb(defs, formula),
                CodexFormulaType.RankSum        => ValidateRankSum(defs, formula),
                _                               => (false, $"Unknown formula type: {formula.FormulaType}.")
            };
        }

        // ─── AnyThreePlanet ───────────────────────────────────────────────────────

        private (bool ok, string reason) ValidatePlanetWithDb(
            IReadOnlyList<CardDefinition> defs, CodexFormulaDefinition formula)
        {
            if (defs.Count != 3)
                return (false, $"Planet formula requires exactly 3 cards (got {defs.Count}).");

            foreach (var def in defs)
            {
                if (!WildcardLinkService.MatchesPlanet(def, formula.RequiredPlanet, _db))
                    return (false,
                        $"Card '{def.Id}' does not match planet {formula.RequiredPlanet}.");
            }

            return (true, "");
        }

        // ─── RankSum ──────────────────────────────────────────────────────────────

        private static (bool ok, string reason) ValidateRankSum(
            IReadOnlyList<CardDefinition> defs, CodexFormulaDefinition formula)
        {
            if (defs.Count == 0)
                return (false, "Rank-sum formula requires at least 1 card.");

            // All cards must match the required suit.
            foreach (var def in defs)
            {
                if (def.Suit != formula.RequiredSuit)
                    return (false,
                        $"Card '{def.Id}' has suit {def.Suit}, but formula requires {formula.RequiredSuit}.");
            }

            // Count how many Aces are in the set; they can each count as 1 or 15.
            int aceCount     = 0;
            int baseRankSum  = 0;
            foreach (var def in defs)
            {
                if (def.Rank == Rank.Ace)
                    aceCount++;
                else
                    baseRankSum += (int)def.Rank;
            }

            // Try all 2^aceCount combinations of ace values (1 or 15).
            // If any combination reaches the threshold, the formula is satisfied.
            int combos = 1 << aceCount; // 2^aceCount
            for (int mask = 0; mask < combos; mask++)
            {
                int total = baseRankSum;
                for (int bit = 0; bit < aceCount; bit++)
                    total += ((mask >> bit) & 1) == 1 ? 15 : 1;

                if (total >= formula.MinRankSum)
                    return (true, "");
            }

            return (false,
                $"Rank sum does not meet the required minimum of {formula.MinRankSum}. " +
                $"Best possible: {BestAceSum(baseRankSum, aceCount)}.");
        }

        private static int BestAceSum(int baseSum, int aceCount) => baseSum + aceCount * 15;
    }
}
