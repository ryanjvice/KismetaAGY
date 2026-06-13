namespace Kismeta.Core.Domain
{
    /// <summary>
    /// Static lookup helpers for the four-way correspondence between
    /// Element, Suit, ReagentType, and PlayerColor (Cauldron color).
    /// </summary>
    public static class Correspondence
    {
        public static Element ElementFor(Suit suit) => suit switch
        {
            Suit.Wands     => Element.Fire,
            Suit.Cups      => Element.Water,
            Suit.Pentacles => Element.Earth,
            Suit.Swords    => Element.Air,
            _              => Element.None
        };

        public static ReagentType ReagentFor(Suit suit) => suit switch
        {
            Suit.Wands     => ReagentType.Sulphur,
            Suit.Cups      => ReagentType.AquaRegia,
            Suit.Pentacles => ReagentType.Vitriol,
            Suit.Swords    => ReagentType.Quicksilver,
            _              => ReagentType.Salt
        };

        public static Suit SuitFor(ReagentType reagent) => reagent switch
        {
            ReagentType.Sulphur      => Suit.Wands,
            ReagentType.AquaRegia    => Suit.Cups,
            ReagentType.Vitriol      => Suit.Pentacles,
            ReagentType.Quicksilver  => Suit.Swords,
            _                        => Suit.None
        };

        public static Element ElementFor(ZodiacSign sign) => sign switch
        {
            ZodiacSign.Aries or ZodiacSign.Leo or ZodiacSign.Sagittarius       => Element.Fire,
            ZodiacSign.Cancer or ZodiacSign.Scorpio or ZodiacSign.Pisces       => Element.Water,
            ZodiacSign.Taurus or ZodiacSign.Virgo or ZodiacSign.Capricorn      => Element.Earth,
            ZodiacSign.Gemini or ZodiacSign.Libra or ZodiacSign.Aquarius       => Element.Air,
            _                                                                   => Element.None
        };

        public static Planet PlanetFor(ZodiacSign sign) => sign switch
        {
            ZodiacSign.Aries or ZodiacSign.Scorpio      => Planet.Mars,
            ZodiacSign.Taurus or ZodiacSign.Libra       => Planet.Venus,
            ZodiacSign.Gemini or ZodiacSign.Virgo       => Planet.Mercury,
            ZodiacSign.Cancer or ZodiacSign.Leo         => Planet.Moon,
            ZodiacSign.Sagittarius or ZodiacSign.Pisces => Planet.Jupiter,
            ZodiacSign.Capricorn or ZodiacSign.Aquarius => Planet.Saturn,
            _                                           => Planet.None
        };
    }
}
