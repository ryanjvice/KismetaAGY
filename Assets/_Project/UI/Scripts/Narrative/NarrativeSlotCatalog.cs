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
                "A new age dawns over Kismeta. The stars take their stations, and the alchemists gather their fortune.",
                "Open the round: set the cosmic age, find your sign, and gather your harvest.",
                "This age's sign shapes everything — which cards harvest well, which alignments score, and your own cosmic effect for the round.",
                new[] { "Begin Spring" }),
            new NarrativeSlotEntry(
                "spring.setage",
                Season.Spring,
                NarrativeSlotTier.Action,
                1,
                "The Agekeeper rolls, and the heavens choose the age for all.",
                "Roll the Cosmic Age die — one of twelve signs sets this round's Sign, Planet, and Element.",
                "These three Aspects govern every harvest bonus and alignment all round.",
                new[] { "Cast the age" }),
            new NarrativeSlotEntry(
                "spring.sign",
                Season.Spring,
                NarrativeSlotTier.Action,
                2,
                "Your own die falls, and the cosmos names you for the age.",
                "Roll your Zodiac die and move your meeple to your sign — your identity for this round.",
                "Your sign is an alignment source AND grants a personal cosmic effect that lasts until Winter.",
                new[] { "Roll your zodiac die" }),
            new NarrativeSlotEntry(
                "spring.harvest",
                Season.Spring,
                NarrativeSlotTier.Action,
                3,
                "The age is generous to those who align with it.",
                "Tally your bonus from every source, then take your full harvest from the Agekeeper.",
                "Sign +3, Planet +2, Element +1.",
                new[] { "Deal & commune" }),
            new NarrativeSlotEntry(
                "spring.hub",
                Season.Spring,
                NarrativeSlotTier.Action,
                4,
                "The wheel shows where every soul stands — study it before you choose.",
                "Commune with your cards, build an Astral House on your sign, and review the table before Summer.",
                "Spread cards score alignment and activate Crucibles but can be stolen in a Duel. Hand cards are safe but idle.",
                new[] { "Commune with Cards", "Build a House", "Proceed to Summer" }),
            new NarrativeSlotEntry(
                "spring.commune",
                Season.Spring,
                NarrativeSlotTier.Action,
                4,
                "Lay your fortune out — what you show, and what you keep.",
                "Sort your cards into Spread (visible engine) and Hand (hidden reserve). Major Arcana go to your Arcanum.",
                "Spread cards score alignment and activate Crucibles but can be stolen in a Duel. Hand cards are safe but idle.",
                new[] { "Lock the tableau" }),
            new NarrativeSlotEntry(
                "spring.buildhouse",
                Season.Spring,
                NarrativeSlotTier.Action,
                4,
                "A permanent anchor rises in an ever-turning sky.",
                "Pay 1 card matching your sign's planet to raise a House on your current sign — a permanent harvest source and cosmic effect.",
                "Houses are permanent — the card is spent for good. You may only build on the sign you rolled this round.",
                new[] { "Raise the House" }),
            new NarrativeSlotEntry(
                "spring.lock",
                Season.Spring,
                NarrativeSlotTier.Action,
                5,
                "The stars fix what you have wrought.",
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
                "The sun rides high and the work is long. Trade, duel, gambit, and oppose your rivals beneath the high sun.",
                "Take any actions, in any order, as many as you can fuel — no fixed order, no turn limit.",
                string.Empty,
                new[] { "Begin Summer" }),
            new NarrativeSlotEntry(
                "summer.hub",
                Season.Summer,
                NarrativeSlotTier.Action,
                1,
                "The sun rides high and the work is long.",
                "Take any actions, in any order, as many as you can fuel.",
                string.Empty,
                new[] { "Craft", "Consort", "Activate", "Pass" }),
            new NarrativeSlotEntry(
                "summer.activate",
                Season.Summer,
                NarrativeSlotTier.Action,
                2,
                "The formula handed down by the Fates comes due.",
                "Collect a Codex card set in your Spread, discard it, light the matching cauldron, and flip the Crucible card face-up.",
                "The coal can never be moved once placed — light the cauldron your higher-tier cards will demand.",
                new[] { "Activate" }),
            new NarrativeSlotEntry(
                "summer.craft",
                Season.Summer,
                NarrativeSlotTier.Action,
                3,
                "Raw cards become the fuel of transmutation.",
                "Discard 3 suit-matching cards into a lit cauldron to craft 1 elemental reagent. Salt needs no cauldron — any 3 cards.",
                "Reagents only transfer by Trade. Each elemental type needs its own cauldron lit first.",
                new[] { "Forge" }),
            new NarrativeSlotEntry(
                "summer.buildhouse",
                Season.Summer,
                NarrativeSlotTier.Action,
                4,
                "A permanent anchor rises in an ever-turning sky.",
                "Pay 2 cards matching your sign's planet to raise a House — a permanent harvest source, opposition boost, and cosmic effect.",
                "Houses are permanent — the cards are spent for good and cannot be reclaimed. Build deliberately.",
                new[] { "Raise the House" }),
            new NarrativeSlotEntry(
                "summer.wards",
                Season.Summer,
                NarrativeSlotTier.Action,
                5,
                "Set your tolls before the rivals come.",
                "Place reagents on your Active Crucible or Adept cards to set the fee challengers must pay to Gambit them.",
                "Crucible wards are permanent; Adept wards return if the Adept leaves play. An unwarded active card can be gambited for free.",
                new[] { "Seal the wards" }),
            new NarrativeSlotEntry(
                "summer.trade",
                Season.Summer,
                NarrativeSlotTier.Action,
                6,
                "Bargains struck beneath the high sun.",
                "Exchange cards, reagents, or Active Crucible cards freely with a rival — the only way to move reagents.",
                "Hidden Hand cards cannot be requested. In Magnus mode, misaligned players trade 2:1.",
                new[] { "Complete trade" }),
            new NarrativeSlotEntry(
                "summer.duel",
                Season.Summer,
                NarrativeSlotTier.Action,
                7,
                "Steel meets steel over a coveted card.",
                "Name a card in a rival's Spread, ante a card of your own, and roll — higher roll takes the prize.",
                "Lose, and your ante returns to the deck. Only Spread cards can be targeted; the Hand is safe.",
                new[] { "Ante & roll" }),
            new NarrativeSlotEntry(
                "summer.gambit",
                Season.Summer,
                NarrativeSlotTier.Action,
                8,
                "A wager against the Fates themselves.",
                "Stake one of your Active cards to seize a rival's Crucible or Adept card. Pay their ward fee, then roll.",
                "Lose, and your offered card is Arrested — pay 1 Salt to free it, and you can't re-gambit that rival this round.",
                new[] { "Challenge this rival" })
        };

        public static IReadOnlyList<NarrativeSlotEntry> WinterSlice { get; } = new[]
        {
            new NarrativeSlotEntry(
                "winter.intro",
                Season.Winter,
                NarrativeSlotTier.Transition,
                0,
                "The forge fires bank low. What was gained is reckoned, what was held is loosed, and the wheel turns toward a new age.",
                "Close the age: unlock your cards, place an optional wager, enforce limits, and transit to the next age.",
                "Only your cards reset. Lit cauldrons, astral houses, active Crucible cards, and your stone's forge position all carry forward.",
                new[] { "Begin Winter" }),
            new NarrativeSlotEntry(
                "winter.unlock",
                Season.Winter,
                NarrativeSlotTier.Action,
                1,
                "The stars release their hold.",
                "Move cards freely between Hand and Spread one last time before the age closes.",
                string.Empty,
                new[] { "Unlock cards" }),
            new NarrativeSlotEntry(
                "winter.wager",
                Season.Winter,
                NarrativeSlotTier.Action,
                2,
                "Bet on the sign the coming age will wear.",
                "Predict the next cosmic sign and stake any cards from your Spread or Hand. Guess right and your wager doubles.",
                "Guess wrong and the cards are lost to the Fates. Major Arcana in your Arcanum cannot be wagered.",
                new[] { "Place wager", "Skip wager" }),
            new NarrativeSlotEntry(
                "winter.limits",
                Season.Winter,
                NarrativeSlotTier.Action,
                3,
                "Pare back to what you can carry into the dark.",
                "Discard down to 5 Spread and 5 Hand. Return all Fate cards to the deck; Adepts remain.",
                string.Empty,
                new[] { "Enforce limits" }),
            new NarrativeSlotEntry(
                "winter.transit",
                Season.Winter,
                NarrativeSlotTier.Action,
                4,
                "The key passes, and the wheel turns toward a sign unknown until the dice fall.",
                "The Agekeeper shuffles the deck and passes the key clockwise — a new age begins, unless the Great Work is done.",
                string.Empty,
                new[] { "Turn the wheel", "cast the next age" })
        };

        public static IReadOnlyList<NarrativeSlotEntry> AutumnSlice { get; } = new[]
        {
            new NarrativeSlotEntry(
                "autumn.intro",
                Season.Autumn,
                NarrativeSlotTier.Transition,
                0,
                "The crucible runs hot. Craft your reagents, activate your formulas, and drive your stone toward gold.",
                "Take any actions, in any order, as many as you can fuel — craft, activate, and advance at the forge.",
                string.Empty,
                new[] { "Begin Autumn" },
                "Forging"),
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
                "A full round in the fire, and the stage is sealed forever.",
                "Advance a stone that has forged a full round to the next Mantle space and discard its Crucible card.",
                "A stone that returned from Stasis this round cannot Temper yet — it must forge a full round first.",
                new[] { "Temper" },
                "Tempering"),
            new NarrativeSlotEntry(
                "autumn.leavestasis",
                Season.Autumn,
                NarrativeSlotTier.Action,
                5,
                "Rekindle a stalled stone.",
                "Pay 2 Salt to return your stone to its old forge position and resume forging.",
                "If your old position is taken, you must wait a round or win a Stasis Opposition to swap into it.",
                new[] { "Leave Stasis" },
                "In Stasis")
        };

        public static IReadOnlyList<NarrativeSlotEntry> AllEntries { get; } = Combine(
            SpringSlice, SummerSlice, AutumnSlice, WinterSlice);

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
