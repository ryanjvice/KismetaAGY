using System.Collections.Generic;
using Kismeta.Core.Domain;
using UnityEngine;

namespace Kismeta.UI.Narrative
{
    [CreateAssetMenu(fileName = "NarrativeSlotCatalog", menuName = "Kismeta/UI/Narrative Slot Catalog")]
    public sealed class NarrativeSlotCatalog : ScriptableObject
    {
        const string ResourcePath = "NarrativeSlotCatalog";

        [SerializeField] List<NarrativeSlotEntry> _entries = new();

        static NarrativeSlotCatalog? _cached;

        public IReadOnlyList<NarrativeSlotEntry> Entries => _entries;

        public bool TryGet(string id, out NarrativeSlotEntry entry)
        {
            entry = null!;
            if (string.IsNullOrEmpty(id))
                return false;

            foreach (var e in _entries)
            {
                if (e != null && e.Id == id)
                {
                    entry = e;
                    return true;
                }
            }

            foreach (var e in NarrativeSlotCatalogDefaults.AllEntries)
            {
                if (e != null && e.Id == id)
                {
                    entry = e;
                    return true;
                }
            }

            return false;
        }

        public static NarrativeSlotCatalog Load()
        {
            if (_cached != null)
                return _cached;

            _cached = Resources.Load<NarrativeSlotCatalog>(ResourcePath);
            if (_cached == null)
            {
                Debug.LogWarning(
                    $"[Narrative] {ResourcePath} not found in Resources; using built-in catalog defaults.");
                _cached = CreateInstance<NarrativeSlotCatalog>();
                _cached._entries = new List<NarrativeSlotEntry>(NarrativeSlotCatalogDefaults.AllEntries);
            }

            return _cached;
        }

        public void SetEntries(IReadOnlyList<NarrativeSlotEntry> entries)
        {
            _entries = new List<NarrativeSlotEntry>(entries);
        }

        public static void ClearCache() => _cached = null;
    }

    /// <summary>All narrative slot entries mirrored from the narrative framework doc (26 total).</summary>
    public static class NarrativeSlotCatalogDefaults
    {
        public static IReadOnlyList<NarrativeSlotEntry> SpringSlice { get; } = new[]
        {
            new NarrativeSlotEntry(
                "spring.intro",
                Season.Spring,
                NarrativeSlotTier.Transition,
                0,
                "A new age dawns. Set the sign, claim your place on the wheel, and gather your harvest.",
                "Open the round: set the cosmic age, find your sign, and gather your harvest.",
                "This age's sign shapes everything — which cards harvest well, which alignments score, and your own cosmic effect for the round.",
                new[] { "Begin Spring" }),
            new NarrativeSlotEntry(
                "spring.focus",
                Season.Spring,
                NarrativeSlotTier.Transition,
                0,
                string.Empty,
                "Before cards lock, use your tools — read effects, commune, and check whether an Astral House is worth building.",
                string.Empty,
                new[] { "Focus" }),
            new NarrativeSlotEntry(
                "spring.setage",
                Season.Spring,
                NarrativeSlotTier.Action,
                1,
                "Roll the Cosmic Age die, and let the heavens set this round's Sign for all.",
                "Roll the Cosmic Age die — one of twelve signs sets this round's Sign, Planet, and Element.",
                "These three Aspects govern every harvest bonus and alignment all round.",
                new[] { "Cast The Age" }),
            new NarrativeSlotEntry(
                "spring.sign",
                Season.Spring,
                NarrativeSlotTier.Action,
                2,
                "Roll your Zodiac die and take your sign. The cosmos marks you for this age.",
                "Roll your Zodiac die and move your meeple to your sign — your identity for this round.",
                "Your sign is an alignment source AND grants a personal cosmic effect that lasts until Winter.",
                new[] { "Roll Your Zodiac Die" }),
            new NarrativeSlotEntry(
                "spring.harvest",
                Season.Spring,
                NarrativeSlotTier.Action,
                3,
                "Tally your alignment bonuses and take your harvest from the Agekeeper.",
                "Tally your bonus from every source, then take your full harvest from the Agekeeper.",
                "Sign +3, Planet +2, Element +1.",
                new[] { "Deal & Commune" }),
            new NarrativeSlotEntry(
                "spring.hub",
                Season.Spring,
                NarrativeSlotTier.Action,
                4,
                "Study the wheel. Commune, build a House, and prepare for Summer.",
                "Commune with your cards, build an Astral House on your sign, and review the table before Summer.",
                "Spread cards score alignment and activate Crucibles but can be stolen in a Duel. Hand cards are safe but idle.",
                new[] { "Commune With Cards", "Build A House", "Proceed To Summer" }),
            new NarrativeSlotEntry(
                "spring.passed",
                Season.Spring,
                NarrativeSlotTier.Action,
                4,
                "You yielded the wheel, and spring waits on your rivals.",
                "Spring ends when every player passes in a row without acting.",
                "You will be asked to respond if a rival's action requires your input.",
                new[] { "Waiting" }),
            new NarrativeSlotEntry(
                "spring.commune",
                Season.Spring,
                NarrativeSlotTier.Action,
                4,
                "Sort your fortune. Place cards in Spread, Hand, and Arcanum.",
                "Sort your cards into Spread (visible engine) and Hand (hidden reserve). Major Arcana go to your Arcanum.",
                "Spread cards score alignment and activate Crucibles but can be stolen in a Duel. Hand cards are safe but idle.",
                new[] { "Lock The Tableau" }),
            new NarrativeSlotEntry(
                "spring.buildhouse",
                Season.Spring,
                NarrativeSlotTier.Action,
                4,
                "Raise a House on your sign. Pay one planet-matching card and anchor the sky.",
                "Pay 1 card matching your sign's planet to raise a House on your current sign — a permanent harvest source and cosmic effect.",
                "Houses are permanent — the card is spent for good. You may only build on the sign you rolled this round.",
                new[] { "Raise The House" }),
            new NarrativeSlotEntry(
                "spring.lock",
                Season.Spring,
                NarrativeSlotTier.Action,
                5,
                "Lock your tableau. Spread and Hand freeze until Winter releases them.",
                "Confirm your placement — cards freeze between Spread and Hand until Winter.",
                "Over-load your Spread and you expose value to theft; under-load it and you starve your engine. This is binding.",
                new[] { "Lock 🔒" })
        };

        public static IReadOnlyList<NarrativeSlotEntry> SummerSlice { get; } = new[]
        {
            new NarrativeSlotEntry(
                "summer.intro",
                Season.Summer,
                NarrativeSlotTier.Transition,
                0,
                "The sun rides high. Craft, activate, trade, duel, gambit, and oppose as you will.",
                "Take any actions, in any order, as many as you can fuel — no fixed order, no turn limit.",
                string.Empty,
                new[] { "Begin Summer" }),
            new NarrativeSlotEntry(
                "summer.focus",
                Season.Summer,
                NarrativeSlotTier.Transition,
                0,
                string.Empty,
                "This round is for consorting and contesting — review Codex and the Table, then Trade, Duel, Gambit, and Opposition as opportunities arise.",
                string.Empty,
                new[] { "Focus" }),
            new NarrativeSlotEntry(
                "summer.hub",
                Season.Summer,
                NarrativeSlotTier.Action,
                1,
                "Take your turn beneath the high sun. Craft, consort, or activate.",
                "Take any actions, in any order, as many as you can fuel.",
                string.Empty,
                new[] { "Craft", "Consort", "Activate", "Finish Turn" }),
            new NarrativeSlotEntry(
                "summer.passed",
                Season.Summer,
                NarrativeSlotTier.Action,
                1,
                "You yielded the contest, and the high sun waits on your rivals.",
                "Summer ends when every player passes in a row without acting.",
                "You will be asked to respond if a rival's action requires your input.",
                new[] { "Waiting" }),
            new NarrativeSlotEntry(
                "summer.activate",
                Season.Summer,
                NarrativeSlotTier.Action,
                2,
                "Activate a Crucible. Complete its Codex formula and light the cauldron.",
                "Collect a Codex card set in your Spread, discard it, light the matching cauldron, and flip the Crucible card face-up.",
                "The coal can never be moved once placed — light the cauldron your higher-tier cards will demand.",
                new[] { "Activate" }),
            new NarrativeSlotEntry(
                "summer.craft",
                Season.Summer,
                NarrativeSlotTier.Action,
                3,
                "Craft a reagent. Feed three suit-matched cards into a lit cauldron.",
                "Discard 3 suit-matching cards into a lit cauldron to craft 1 elemental reagent. Salt needs no cauldron — any 3 cards.",
                "Reagents only transfer by Trade. Each elemental type needs its own cauldron lit first.",
                new[] { "Forge" }),
            new NarrativeSlotEntry(
                "summer.buildhouse",
                Season.Summer,
                NarrativeSlotTier.Action,
                4,
                "Raise a House on your sign. Pay two planet-matching cards and stake the sky.",
                "Pay 2 cards matching your sign's planet to raise a House — a permanent harvest source, opposition boost, and cosmic effect.",
                "Houses are permanent — the cards are spent for good and cannot be reclaimed. Build deliberately.",
                new[] { "Raise The House" }),
            new NarrativeSlotEntry(
                "summer.wards",
                Season.Summer,
                NarrativeSlotTier.Action,
                5,
                "Set your wards. Place reagents on Active cards before rivals come.",
                "Place reagents on your Active Crucible or Adept cards to set the fee challengers must pay to Gambit them.",
                "Crucible wards are permanent; Adept wards return if the Adept leaves play. An unwarded active card can be gambited for free.",
                new[] { "Seal The Wards" }),
            new NarrativeSlotEntry(
                "summer.trade",
                Season.Summer,
                NarrativeSlotTier.Action,
                6,
                "Strike a bargain. Exchange cards, reagents, or Active Crucibles with a rival.",
                "Exchange cards, reagents, or Active Crucible cards freely with a rival — the only way to move reagents.",
                "Hidden Hand cards cannot be requested. In Magnus mode, misaligned players trade 2:1.",
                new[] { "Complete Trade" }),
            new NarrativeSlotEntry(
                "summer.duel",
                Season.Summer,
                NarrativeSlotTier.Action,
                7,
                "Duel for a Spread card. Ante your own and roll against a rival.",
                "Name a card in a rival's Spread, ante a card of your own, and roll — higher roll takes the prize.",
                "Lose, and your ante returns to the deck. Only Spread cards can be targeted; the Hand is safe.",
                new[] { "Ante & Roll" }),
            new NarrativeSlotEntry(
                "summer.gambit",
                Season.Summer,
                NarrativeSlotTier.Action,
                8,
                "Gambit a rival's Active card. Pay their ward fee and roll for the prize.",
                "Stake one of your Active cards to seize a rival's Crucible or Adept card. Pay their ward fee, then roll.",
                "Lose, and your offered card is Arrested — pay 1 Salt to free it, and you can't re-gambit that rival this round.",
                new[] { "Challenge This Rival" })
        };

        public static IReadOnlyList<NarrativeSlotEntry> WinterSlice { get; } = new[]
        {
            new NarrativeSlotEntry(
                "winter.intro",
                Season.Winter,
                NarrativeSlotTier.Transition,
                0,
                "The long night settles in. Unlock, wager, trim your hand, and turn the wheel.",
                "Close the age: unlock your cards, place an optional wager, enforce limits, and transit to the next age.",
                string.Empty,
                new[] { "Begin Winter" }),
            new NarrativeSlotEntry(
                "winter.focus",
                Season.Winter,
                NarrativeSlotTier.Transition,
                0,
                string.Empty,
                "The age is closing — reorganize, wager wisely, and trim to limits before transit.",
                string.Empty,
                new[] { "Focus" }),
            new NarrativeSlotEntry(
                "winter.unlock",
                Season.Winter,
                NarrativeSlotTier.Action,
                1,
                "Unlock your cards. Move freely between Hand and Spread one last time.",
                "Move cards freely between Hand and Spread one last time before the age closes.",
                string.Empty,
                new[] { "Unlock Cards" }),
            new NarrativeSlotEntry(
                "winter.wager",
                Season.Winter,
                NarrativeSlotTier.Action,
                2,
                "Bet on the sign the coming age will wear.",
                "Predict the next cosmic sign and stake any cards from your Spread or Hand. Guess right and your wager doubles.",
                "Guess wrong and the cards are lost to the Fates. Major Arcana in your Arcanum cannot be wagered.",
                new[] { "Place Wager", "Skip Wager" }),
            new NarrativeSlotEntry(
                "winter.limits",
                Season.Winter,
                NarrativeSlotTier.Action,
                3,
                "Enforce your limits. Discard down to five Spread and five Hand.",
                "Discard down to 5 Spread and 5 Hand. Return all Fate cards to the deck; Adepts remain.",
                string.Empty,
                new[] { "Enforce Limits" }),
            new NarrativeSlotEntry(
                "winter.transit",
                Season.Winter,
                NarrativeSlotTier.Action,
                4,
                "Turn the wheel. Pass the key and begin the next Cosmic Age.",
                string.Empty,
                string.Empty,
                new[] { "Turn The Wheel", "Cast The Next Age" })
        };

        public static IReadOnlyList<NarrativeSlotEntry> AutumnSlice { get; } = new[]
        {
            new NarrativeSlotEntry(
                "autumn.intro",
                Season.Autumn,
                NarrativeSlotTier.Transition,
                0,
                "The crucible runs hot. Craft, activate, fire your stone, and drive it toward gold.",
                "Take any actions, in any order, as many as you can fuel — craft, activate, and advance at the forge.",
                string.Empty,
                new[] { "Begin Autumn" },
                "Forging"),
            new NarrativeSlotEntry(
                "autumn.focus",
                Season.Autumn,
                NarrativeSlotTier.Transition,
                0,
                string.Empty,
                "The forge is live — cross-check Codex costs before you Fire, Temper, or break Stasis.",
                string.Empty,
                new[] { "Focus" }),
            new NarrativeSlotEntry(
                "autumn.survey",
                Season.Autumn,
                NarrativeSlotTier.Action,
                1,
                "Read the forge before you move.",
                "Note every stone's position — Mantle (safe), Forge (vulnerable), Stasis (frozen), or the Altar.",
                string.Empty,
                new[] { "Survey" },
                "Survey"),
            new NarrativeSlotEntry(
                "autumn.passed",
                Season.Autumn,
                NarrativeSlotTier.Action,
                2,
                "You stepped back from the flames. The forge still burns for your rivals.",
                "Autumn ends when every player passes in a row without acting.",
                "You will be asked to respond if a rival's action requires your input.",
                new[] { "Waiting" },
                "Waiting"),
            new NarrativeSlotEntry(
                "summer.opposition",
                Season.Summer,
                NarrativeSlotTier.Action,
                9,
                "Align against a rival and break their forging.",
                "Pay the defender's ward fee, roll, and total your Aspect alignment. Win, and their stone falls to Stasis.",
                "Each defense the rival survives grants them a stacking Besieged Bonus — a dogpile on the leader can backfire.",
                new[] { "Oppose" },
                "Opposing"),
            new NarrativeSlotEntry(
                "autumn.fire",
                Season.Autumn,
                NarrativeSlotTier.Action,
                3,
                "Commit your stone to the flames.",
                "Satisfy an Active Crucible card's formula — alignment + reagents — and advance your stone into the forge.",
                "A stone Fired this Autumn is safe this round, but Forging next round leaves it open to Opposition. Ward as you advance.",
                new[] { "Fire" },
                "Forging"),
            new NarrativeSlotEntry(
                "autumn.temper",
                Season.Autumn,
                NarrativeSlotTier.Action,
                4,
                "Temper your stone. Advance it to the next Mantle and seal this stage.",
                "Advance a stone that has forged a full round to the next Mantle space and discard its Crucible card.",
                "A stone that returned from Stasis this round cannot Temper yet — it must forge a full round first.",
                new[] { "Temper" },
                "Tempering"),
            new NarrativeSlotEntry(
                "autumn.leavestasis",
                Season.Autumn,
                NarrativeSlotTier.Action,
                5,
                "Leave Stasis. Pay Salt and return your stone to the forge.",
                "Pay 2 Salt to return your stone to its old forge position and resume forging.",
                "If your old position is taken, you must wait a round or win a Stasis Opposition to swap into it.",
                new[] { "Leave Stasis" },
                "In Stasis")
        };

        public static IReadOnlyList<NarrativeSlotEntry> GameSlice { get; } = new[]
        {
            new NarrativeSlotEntry(
                "game.overview",
                Season.Spring,
                NarrativeSlotTier.Transition,
                -1,
                "Journey through Cosmic Ages. Align with each sign, forge your stone, and reach the Altar.",
                "Align with each Cosmic Age; work with or defend against Rivals; complete all four Crucible Cards and reach the Altar.",
                string.Empty,
                new[] { "Determine the Agekeeper" })
        };

        public static IReadOnlyList<NarrativeSlotEntry> AllEntries { get; } = Combine(
            GameSlice, SpringSlice, SummerSlice, AutumnSlice, WinterSlice);

        static NarrativeSlotEntry[] Combine(params IReadOnlyList<NarrativeSlotEntry>[] slices)
        {
            var list = new List<NarrativeSlotEntry>();
            foreach (var slice in slices)
            {
                foreach (var entry in slice)
                    list.Add(entry);
            }
            return list.ToArray();
        }
    }
}
