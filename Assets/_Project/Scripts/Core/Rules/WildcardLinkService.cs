using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Resolves minor V2 WildcardLink cards for crucible requirement matching.
    /// Spread-only enforcement is the caller's responsibility.
    /// </summary>
    public static class WildcardLinkService
    {
        public static bool IsWildcardLink(CardDefinition def)
            => def.EffectType.Equals("WildcardLink", System.StringComparison.OrdinalIgnoreCase)
               && def.WildcardArcanaNumber >= 0;

        public static bool MatchesLinkedArcana(CardDefinition def, int arcanaNumber)
            => IsWildcardLink(def) && def.WildcardArcanaNumber == arcanaNumber;

        public static bool MatchesPlanet(CardDefinition def, Planet required, ICardDatabase db)
        {
            if (required == Planet.None)
                return false;
            if (IsWildcardLink(def))
                return LinkedMajorPlanet(def.WildcardArcanaNumber, db) == required;
            return def.Planet == required;
        }

        public static Planet LinkedMajorPlanet(int arcanaNumber, ICardDatabase db)
        {
            if (arcanaNumber < 0 || db == null)
                return Planet.None;

            var major = db.GetByArcanaNumber(arcanaNumber);
            if (major == null)
                return Planet.None;

            if (major.Planet != Planet.None)
                return major.Planet;

            if (major.Sign != ZodiacSign.None)
                return Correspondence.PlanetFor(major.Sign);

            return PlanetFromMajorName(major.Name);
        }

        static Planet PlanetFromMajorName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Planet.None;

            foreach (Planet planet in System.Enum.GetValues(typeof(Planet)))
            {
                if (planet == Planet.None)
                    continue;
                if (name.Contains(planet.ToString(), System.StringComparison.OrdinalIgnoreCase))
                    return planet;
            }

            return Planet.None;
        }

        public static IEnumerable<Planet> EffectivePlanets(CardDefinition def, ICardDatabase db)
        {
            if (def.Planet != Planet.None)
                yield return def.Planet;

            if (!IsWildcardLink(def))
                yield break;

            var linked = LinkedMajorPlanet(def.WildcardArcanaNumber, db);
            if (linked != Planet.None && linked != def.Planet)
                yield return linked;
        }

        /// <summary>Virtual wildcard card for World resonant (linked to fired crucible arcana).</summary>
        public static CardDefinition CreateVirtualWildcard(int arcanaNumber)
            => new CardDefinition(
                id: $"virtual.wildcard.{arcanaNumber}",
                deck: Deck.Kismeta,
                suit: Suit.None,
                rank: Rank.None,
                variant: CardVariant.Two,
                majorArcanaType: MajorArcanaType.None,
                arcanaNumber: -1,
                sign: ZodiacSign.None,
                planet: Planet.None,
                name: "World wildcard",
                effectType: "WildcardLink",
                effectText: "",
                effectTextResonant: "",
                isCurse: false,
                nullifiesCard: "",
                wildcardArcanaNumber: arcanaNumber,
                wildcardArcanaMajorName: "",
                crucibleGroup: CrucibleGroup.None,
                alchemicalFormula: "",
                alchemicalCost: ReagentCost.Zero);
    }
}
