using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>Human-readable cosmic age and personal effect copy shared by ceremony UI and Active Effects.</summary>
    public static class CosmicEffectDescriber
    {
        public static (string Name, string Desc) DescribeCosmicAge(ZodiacSign sign) => sign switch
        {
            ZodiacSign.Aries => ("+1 Base Harvest", "while Aries reigns, every player draws one extra harvest card"),
            ZodiacSign.Libra => ("+1 Base Harvest", "while Libra reigns, every player draws one extra harvest card"),
            ZodiacSign.Taurus => ("Court Pentacles are a Wild Suit", "every Court Pentacle counts as any suit you need"),
            ZodiacSign.Leo => ("Court Wands are a Wild Suit", "every Court Wand counts as any suit you need"),
            ZodiacSign.Scorpio => ("Court Cups are a Wild Suit", "every Court Cup counts as any suit you need"),
            ZodiacSign.Aquarius => ("Court Swords are a Wild Suit", "every Court Sword counts as any suit you need"),
            ZodiacSign.Cancer => ("Salt costs 2 cards", "craft Salt from any two cards while Cancer reigns"),
            ZodiacSign.Capricorn => ("Salt costs 2 cards", "craft Salt from any two cards while Capricorn reigns"),
            ZodiacSign.Gemini => ("Quicksilver costs 2 Swords", "craft Quicksilver from two Swords with the Swords cauldron lit"),
            ZodiacSign.Virgo => ("Vitriol costs 2 Pentacles", "craft Vitriol from two Pentacles with the Pentacles cauldron lit"),
            ZodiacSign.Sagittarius => ("Sulphur costs 2 Wands", "craft Sulphur from two Wands with the Wands cauldron lit"),
            ZodiacSign.Pisces => ("Aqua Regia costs 2 Cups", "craft Aqua Regia from two Cups with the Cups cauldron lit"),
            _ => ("No cosmic effect", "the heavens are still this round")
        };

        public static string DescribePersonalEffect(ZodiacSign sign)
        {
            var flags = CosmicEffectService.EffectFor(sign);
            if (flags.HarvestBaseBonus > 0)
                return "+1 base harvest each round from this sign.";
            if (flags.WildCourtSuit != Suit.None)
                return $"Court {flags.WildCourtSuit} are a wild suit while this house stands.";
            if (flags.SaltCostsTwo)
                return "Craft Salt from any two cards while this house stands.";
            if (flags.CheapCraftSuit != Suit.None)
                return $"Craft {flags.CheapCraftReagent} from two {flags.CheapCraftSuit} with that cauldron lit.";
            return string.Empty;
        }

        public static string DescribeAlignmentContribution(ZodiacSign source, ZodiacSign cosmic)
        {
            int pts = HarvestBreakdownService.AlignmentBonus(source, cosmic);
            if (pts <= 0)
                return "No alignment bonus toward the Cosmic Age this round.";

            if (source == cosmic)
                return "Counts as Sign alignment (+3) toward the Cosmic Age — adds to harvest and every Opposition while active.";

            if (pts >= 2)
            {
                var planet = Correspondence.PlanetFor(source);
                return $"Counts as Planet alignment (+2, {planet}) toward the Cosmic Age — adds to harvest and every Opposition while active.";
            }

            var element = Correspondence.ElementFor(source);
            return $"Counts as Element alignment (+1, {element}) toward the Cosmic Age — adds to harvest and every Opposition while active.";
        }
    }
}
