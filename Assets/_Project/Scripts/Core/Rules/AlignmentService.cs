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
    /// Hermit resonant doubles element-tier alignment only.
    /// </summary>
    public sealed class AlignmentService : IAlignmentService
    {
        private readonly ICardDatabase _db;

        public AlignmentService(ICardDatabase db) => _db = db;

        public int CalculateAlignmentPoints(GameSession session, int playerId, ZodiacSign referenceSign)
        {
            if (referenceSign == ZodiacSign.None) return 0;

            var player = session.Players[playerId];
            bool hermitResonant = AdeptAttunement.IsHermitResonant(session, player);
            int total  = 0;

            total += ApplyHermitElementDouble(AspectScore(player.CurrentSign, referenceSign), hermitResonant);

            foreach (var id in player.Spread)
            {
                var inst = session.GetCard(id);
                var def  = inst != null ? _db.GetById(inst.DefinitionId) : null;
                if (def == null) continue;
                total += ApplyHermitElementDouble(CardScore(def.Suit, def.Planet, referenceSign), hermitResonant);
            }

            foreach (var id in player.Hand)
            {
                var inst = session.GetCard(id);
                var def  = inst != null ? _db.GetById(inst.DefinitionId) : null;
                if (def == null) continue;
                total += ApplyHermitElementDouble(CardScore(def.Suit, def.Planet, referenceSign), hermitResonant);
            }

            foreach (var id in player.Arcanum)
            {
                if (player.ArrestedAdepts.Contains(id)) continue;
                if (CardEffectSuppressionService.IsSuppressed(session, id)) continue;
                var inst = session.GetCard(id);
                var def  = inst != null ? _db.GetById(inst.DefinitionId) : null;
                if (def?.MajorArcanaType == MajorArcanaType.Adept && def.Sign != ZodiacSign.None)
                    total += ApplyHermitElementDouble(AspectScore(def.Sign, referenceSign), hermitResonant);
            }

            foreach (var houseSign in player.AstralHouses)
                total += ApplyHermitElementDouble(AspectScore(houseSign, referenceSign), hermitResonant);

            return total;
        }

        static int ApplyHermitElementDouble(int score, bool hermitResonant)
            => hermitResonant && score == 1 ? 2 : score;

        private static int AspectScore(ZodiacSign source, ZodiacSign reference)
        {
            if (source == ZodiacSign.None || reference == ZodiacSign.None) return 0;
            if (source == reference)                                         return 3;
            if (Correspondence.PlanetFor(source) == Correspondence.PlanetFor(reference)) return 2;
            if (Correspondence.ElementFor(source) == Correspondence.ElementFor(reference)) return 1;
            return 0;
        }

        /// <summary>Alignment points for a single minor-arcana card vs a reference sign.</summary>
        public static int ScoreCard(Suit suit, Planet planet, ZodiacSign reference) =>
            CardScore(suit, planet, reference);

        private static int CardScore(Suit suit, Planet planet, ZodiacSign reference)
        {
            var refPlanet = Correspondence.PlanetFor(reference);
            if (planet != Planet.None && planet == refPlanet) return 2;

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
