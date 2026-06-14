using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Implements Aspect Alignment scoring for Opposition contests.
    ///
    /// Each card in the player's Spread (and optionally Hand) is scored independently
    /// against a reference sign (the Cosmic Age sign). The highest matching aspect
    /// per card applies:
    ///   +3  if the card's suit/planet aligns by Sign   (same ZodiacSign)
    ///   +2  if aligned by Planet                        (same Planet, different sign)
    ///   +1  if aligned by Element                       (same Element, different planet)
    ///    0  otherwise
    ///
    /// Adept cards in Arcanum each contribute their Sign as an independent source.
    /// Astral Houses each contribute their ZodiacSign as an independent source.
    /// The player's own personal Zodiac roll is scored once.
    /// </summary>
    public sealed class AlignmentService : IAlignmentService
    {
        private readonly ICardDatabase _db;

        public AlignmentService(ICardDatabase db) => _db = db;

        public int CalculateAlignmentPoints(GameSession session, int playerId, ZodiacSign referenceSign)
        {
            if (referenceSign == ZodiacSign.None) return 0;

            var player = session.Players[playerId];
            int total  = 0;

            // Personal Zodiac sign
            total += AspectScore(player.CurrentSign, referenceSign);

            // Spread cards (each card scored independently)
            foreach (var id in player.Spread)
            {
                var inst = session.GetCard(id);
                var def  = inst != null ? _db.GetById(inst.DefinitionId) : null;
                if (def == null) continue;
                total += CardScore(def.Suit, def.Planet, referenceSign);
            }

            // Hand cards also count toward Opposition (all cards are active for Opposition)
            foreach (var id in player.Hand)
            {
                var inst = session.GetCard(id);
                var def  = inst != null ? _db.GetById(inst.DefinitionId) : null;
                if (def == null) continue;
                total += CardScore(def.Suit, def.Planet, referenceSign);
            }

            // Adept cards in Arcanum (arrested Adepts do not contribute until refreshed)
            foreach (var id in player.Arcanum)
            {
                if (player.ArrestedAdepts.Contains(id)) continue;
                var inst = session.GetCard(id);
                var def  = inst != null ? _db.GetById(inst.DefinitionId) : null;
                if (def?.MajorArcanaType == MajorArcanaType.Adept && def.Sign != ZodiacSign.None)
                    total += AspectScore(def.Sign, referenceSign);
            }

            // Astral Houses
            foreach (var houseSign in player.AstralHouses)
                total += AspectScore(houseSign, referenceSign);

            return total;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static int AspectScore(ZodiacSign source, ZodiacSign reference)
        {
            if (source == ZodiacSign.None || reference == ZodiacSign.None) return 0;
            if (source == reference)                                         return 3;
            if (Correspondence.PlanetFor(source) == Correspondence.PlanetFor(reference)) return 2;
            if (Correspondence.ElementFor(source) == Correspondence.ElementFor(reference)) return 1;
            return 0;
        }

        private static int CardScore(Suit suit, Planet planet, ZodiacSign reference)
        {
            // Planet match (+2)
            var refPlanet = Correspondence.PlanetFor(reference);
            if (planet != Planet.None && planet == refPlanet) return 2;

            // Element match (+1) via suit
            if (suit != Suit.None)
            {
                var cardElement = Correspondence.ElementFor(suit);
                var refElement  = Correspondence.ElementFor(reference);
                if (cardElement != Element.None && cardElement == refElement) return 1;
            }

            return 0;
        }
    }
}
