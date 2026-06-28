using System.Collections.Generic;
using Kismeta.Core.Domain;

namespace Kismeta.Core.Phases
{
    /// <summary>
    /// Static factory returning the canonical step lists per season, derived from
    /// the Round at a Glance table in Docs/Rules/round-overview.md.
    /// </summary>
    public static class PhaseDefinitions
    {
        public static IReadOnlyList<PhaseStepDefinition> StepsFor(Season season) => season switch
        {
            Season.Spring => SpringSteps,
            Season.Summer => SummerSteps,
            Season.Autumn => AutumnSteps,
            Season.Winter => WinterSteps,
            _             => SpringSteps
        };

        public static readonly IReadOnlyList<PhaseStepDefinition> SpringSteps = new[]
        {
            new PhaseStepDefinition(1, "SetCosmicAge",
                "Agekeeper rolls the Cosmic Age Die. Read the Sign & Aspects aloud."),
            new PhaseStepDefinition(2, "DetermineSign",
                "All players roll their Zodiac Die and move their Meeple to their Sign."),
            new PhaseStepDefinition(3, "Harvest",
                "Base Harvest + Bonus Cards + Agekeeper's Boon."),
            new PhaseStepDefinition(4, "SpringHub",
                "Commune with cards, build an Astral House, and review the table before Summer."),
            new PhaseStepDefinition(5, "CardLock",
                "Cards are locked between Hand and Spread until Phase 4: Winter.")
        };

        // Summer steps are a free-order pool of alchemist interactions. IsOrdered = false marks each as pooled.
        public static readonly IReadOnlyList<PhaseStepDefinition> SummerSteps = new[]
        {
            new PhaseStepDefinition(1, "Trade",
                "Exchange Kismeta Cards, Reagents, or Active Crucible Cards freely.",
                isOrdered: false),
            new PhaseStepDefinition(2, "Duel",
                "Wager cards and roll dice against a rival to steal a card from their Spread.",
                isOrdered: false),
            new PhaseStepDefinition(3, "Gambit",
                "Pay any Ward cost and roll dice to seize a rival's Active Crucible Card or Adept.",
                isOrdered: false),
            new PhaseStepDefinition(4, "Opposition",
                "Attempt to send an opponent's Forging Stone into Stasis via an Alignment contest.",
                isOrdered: false)
        };

        public static readonly IReadOnlyList<PhaseStepDefinition> AutumnSteps = new[]
        {
            new PhaseStepDefinition(1, "CraftReagent",
                "Discard 3 matching-Suit cards into a lit Cauldron to craft 1 Reagent."),
            new PhaseStepDefinition(2, "ActivateCrucible",
                "Collect card sets listed on the Codex & discard to activate a Crucible Card."),
            new PhaseStepDefinition(3, "FireStone",
                "Complete an active Crucible card and move your Stone into the Forge."),
            new PhaseStepDefinition(4, "Temper",
                "After a full round in the Forge, move your Stone to the next Mantle Ring space."),
            new PhaseStepDefinition(5, "LeaveStasis",
                "Move your Stone out of Stasis back to its previous Forge spot.")
        };

        public static readonly IReadOnlyList<PhaseStepDefinition> WinterSteps = new[]
        {
            new PhaseStepDefinition(1, "CardUnlock",
                "Move cards freely between your Hand and Spread."),
            new PhaseStepDefinition(2, "FatefulWager",
                "Bet on the next Cosmic Age with cards; double or lose your Wager."),
            new PhaseStepDefinition(3, "EnforceCardLimits",
                "Spread: 5 cards | Hand: 5 cards | Arcanum: Adept cards only."),
            new PhaseStepDefinition(4, "TransitAge",
                "Agekeeper shuffles the Common Deck; passes the Key clockwise to end the round.")
        };
    }
}
