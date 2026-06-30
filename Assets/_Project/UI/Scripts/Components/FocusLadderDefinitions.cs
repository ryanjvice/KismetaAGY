using System.Collections.Generic;
using Kismeta.Core.Domain;

namespace Kismeta.UI.Components
{
    public readonly struct SeasonRecapHero
    {
        public string Name { get; }
        public string Tagline { get; }

        public SeasonRecapHero(string name, string tagline)
        {
            Name = name;
            Tagline = tagline;
        }
    }

    public readonly struct SeasonFocusTip
    {
        public string Title { get; }
        public string Detail { get; }

        public SeasonFocusTip(string title, string detail)
        {
            Title = title;
            Detail = detail;
        }
    }

    public static class FocusLadderDefinitions
    {
        static readonly Dictionary<Season, SeasonRecapHero> HeroBySeason = new()
        {
            [Season.Spring] = new SeasonRecapHero("SPRING", "THE WHEEL TURNS · SET THE AGE"),
            [Season.Summer] = new SeasonRecapHero("SUMMER", "THE HIGH SUN · CONSORT & CONTEST"),
            [Season.Autumn] = new SeasonRecapHero("AUTUMN", "THE FORGE SEASON · CRAFT & FORGE"),
            [Season.Winter] = new SeasonRecapHero("WINTER", "THE LONG NIGHT · CLOSE THE AGE")
        };

        static readonly Dictionary<Season, IReadOnlyList<SeasonFocusTip>> TipsBySeason = new()
        {
            [Season.Spring] = new[]
            {
                new SeasonFocusTip(
                    "Read card effects",
                    "Many Kismeta cards hint at spread vs. hand placement and future tableau synergies."),
                new SeasonFocusTip(
                    "Commune before Card Lock",
                    "Your spread and hand layout is fixed until Winter — arrange cards while you still can."),
                new SeasonFocusTip(
                    "Open Effects",
                    "See how the cosmic age and your sign interact with astral houses and adepts."),
                new SeasonFocusTip(
                    "Check Build Astral House",
                    "Build when you hold a minor matching your sign's planet, the sign is unclaimed, and you have tokens left."),
                new SeasonFocusTip(
                    "Inspect the board",
                    "Use the magnifier to see rival sign claims before building on a sign."),
                new SeasonFocusTip(
                    "Compare harvest and alignment",
                    "Sign match bonuses affect how many cards you keep from harvest.")
            },
            [Season.Summer] = new[]
            {
                new SeasonFocusTip(
                    "Review Your Codex",
                    "Open Codex to see which spread cards you need to activate crucibles and light cauldrons."),
                new SeasonFocusTip(
                    "Survey the Table",
                    "Use Table to review each rival's spread, arcanum, and active crucible cards before you act."),
                new SeasonFocusTip(
                    "Consider Effects",
                    "Open Effects to see cosmic-age bonuses, houses, adepts, and other modifiers in play this round."),
                new SeasonFocusTip(
                    "Consider Trades",
                    "Look for mutually beneficial trades — swap cards or reagents when both sides gain from the deal."),
                new SeasonFocusTip(
                    "Capitalize on Weaknesses",
                    "When a rival is exposed, press with Duels, Gambits, and Opposition while you still have fuel."),
                new SeasonFocusTip(
                    "Protect Your Assets",
                    "Place protective wards to make it harder for rivals to challenge you in Gambits or Opposition.")
            },
            [Season.Autumn] = new[]
            {
                new SeasonFocusTip(
                    "Read Codex alchemical costs",
                    "The alchemical section lists reagent costs to Fire and Temper each crucible slot."),
                new SeasonFocusTip(
                    "Craft into lit cauldrons",
                    "You can only craft into lit cauldrons — activate first if you need a new element."),
                new SeasonFocusTip(
                    "Fire when ready",
                    "Fire an active crucible when you can pay its alchemical cost — your stone moves into the Forge."),
                new SeasonFocusTip(
                    "Temper after a full round",
                    "If your stone sat in the Forge all round, Temper to advance along the mantle ring."),
                new SeasonFocusTip(
                    "Leave Stasis if needed",
                    "Opposition can send your stone to Stasis — spend Salt to move it back to its previous forge spot."),
                new SeasonFocusTip(
                    "Watch rival forge progress",
                    "Board inspect and Effects show where rivals stand — time Opposition accordingly.")
            },
            [Season.Winter] = new[]
            {
                new SeasonFocusTip(
                    "Unlock and reorganize",
                    "Move cards freely between hand and spread before card limits are enforced."),
                new SeasonFocusTip(
                    "Wager wisely",
                    "Only stake cards you can afford to lose — correct age guesses double the payout."),
                new SeasonFocusTip(
                    "Plan for card limits",
                    "Spread: 5 cards | Hand: 5 cards | Arcanum: adepts only — decide what to cut early."),
                new SeasonFocusTip(
                    "Re-open Codex",
                    "Confirm crucible progress before the age turns and cards lock again in Spring."),
                new SeasonFocusTip(
                    "Re-read before you cut",
                    "Check card effects before discarding or wagering — some cards help in the coming Spring.")
            }
        };

        public static bool TryGetHero(Season season, out SeasonRecapHero hero) =>
            HeroBySeason.TryGetValue(season, out hero);

        public static IReadOnlyList<SeasonFocusTip> TipsFor(Season season) =>
            TipsBySeason.TryGetValue(season, out var tips) ? tips : TipsBySeason[Season.Spring];

        static readonly string[] RowNumClasses =
        {
            "intro-row__num--gold",
            "intro-row__num--green",
            "intro-row__num--purple",
            "intro-row__num--violet",
            "intro-row__num--amber",
            "intro-row__num--winter"
        };

        public static string RowNumClassForIndex(int index) =>
            RowNumClasses[index % RowNumClasses.Length];
    }
}
