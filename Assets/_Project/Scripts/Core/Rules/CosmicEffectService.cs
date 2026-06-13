using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Translates the current Cosmic Age Sign into concrete rule flags stored on
    /// <see cref="BoardState.CosmicEffect"/>. Called by SpringRules.RollCosmicAge
    /// after the sign is set; flags are cleared by WinterRules.Transit each round.
    ///
    /// Effect categories (from cosmic-ages.json):
    ///   Aries / Libra          → +1 base Harvest card
    ///   Taurus / Leo / Scorpio / Aquarius  → Court Cards of the matching Suit are Wild
    ///   Cancer / Capricorn     → Craft Salt for any 2 cards (instead of 3)
    ///   Gemini / Virgo / Sagittarius / Pisces → Craft the matching Elemental Reagent for 2 cards
    /// </summary>
    public sealed class CosmicEffectService
    {
        public void Apply(GameSession session, ZodiacSign sign)
        {
            var flags = CosmicEffectFlags.Default;

            switch (sign)
            {
                // +1 Base Harvest
                case ZodiacSign.Aries:
                case ZodiacSign.Libra:
                    flags.HarvestBaseBonus = 1;
                    break;

                // Court Cards Wild Suit
                case ZodiacSign.Taurus:
                    flags.WildCourtSuit = Suit.Pentacles;
                    break;
                case ZodiacSign.Leo:
                    flags.WildCourtSuit = Suit.Wands;
                    break;
                case ZodiacSign.Scorpio:
                    flags.WildCourtSuit = Suit.Cups;
                    break;
                case ZodiacSign.Aquarius:
                    flags.WildCourtSuit = Suit.Swords;
                    break;

                // Salt costs 2 cards
                case ZodiacSign.Cancer:
                case ZodiacSign.Capricorn:
                    flags.SaltCostsTwo = true;
                    break;

                // Cheap elemental craft (2 instead of 3, Cauldron must still be lit)
                case ZodiacSign.Gemini:
                    flags.CheapCraftSuit    = Suit.Swords;
                    flags.CheapCraftReagent = ReagentType.Quicksilver;
                    break;
                case ZodiacSign.Virgo:
                    flags.CheapCraftSuit    = Suit.Pentacles;
                    flags.CheapCraftReagent = ReagentType.Vitriol;
                    break;
                case ZodiacSign.Sagittarius:
                    flags.CheapCraftSuit    = Suit.Wands;
                    flags.CheapCraftReagent = ReagentType.Sulphur;
                    break;
                case ZodiacSign.Pisces:
                    flags.CheapCraftSuit    = Suit.Cups;
                    flags.CheapCraftReagent = ReagentType.AquaRegia;
                    break;
            }

            session.Board.CosmicEffect = flags;
            session.EmitEvent(new CosmicEffectAppliedEvent(sign, DescribeEffect(sign)));
        }

        /// <summary>Clears all flags at the start of each Winter Transit.</summary>
        public void Reset(GameSession session)
        {
            session.Board.CosmicEffect    = CosmicEffectFlags.Default;
            session.Board.BestOfThreeDuels = false;
        }

        private static string DescribeEffect(ZodiacSign sign) => sign switch
        {
            ZodiacSign.Aries        => "+1 Base Harvest",
            ZodiacSign.Libra        => "+1 Base Harvest",
            ZodiacSign.Taurus       => "Court Pentacles Wild",
            ZodiacSign.Leo          => "Court Wands Wild",
            ZodiacSign.Scorpio      => "Court Cups Wild",
            ZodiacSign.Aquarius     => "Court Swords Wild",
            ZodiacSign.Cancer       => "Salt costs 2 cards",
            ZodiacSign.Capricorn    => "Salt costs 2 cards",
            ZodiacSign.Gemini       => "Quicksilver costs 2 Swords",
            ZodiacSign.Virgo        => "Vitriol costs 2 Pentacles",
            ZodiacSign.Sagittarius  => "Sulphur costs 2 Wands",
            ZodiacSign.Pisces       => "Aqua Regia costs 2 Cups",
            _                       => "No effect"
        };
    }
}
