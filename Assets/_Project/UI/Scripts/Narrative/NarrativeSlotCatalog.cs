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
                    $"[Narrative] {ResourcePath} not found in Resources; using built-in Autumn slice defaults.");
                _cached = CreateInstance<NarrativeSlotCatalog>();
                _cached._entries = new List<NarrativeSlotEntry>(NarrativeSlotCatalogDefaults.AutumnSlice);
            }

            return _cached;
        }

        public void SetEntries(IReadOnlyList<NarrativeSlotEntry> entries)
        {
            _entries = new List<NarrativeSlotEntry>(entries);
        }

        public static void ClearCache() => _cached = null;
    }

    /// <summary>Autumn vertical-slice entries mirrored from the narrative framework doc.</summary>
    public static class NarrativeSlotCatalogDefaults
    {
        public static IReadOnlyList<NarrativeSlotEntry> AutumnSlice { get; } = new[]
        {
            new NarrativeSlotEntry(
                "autumn.intro",
                Season.Autumn,
                NarrativeSlotTier.Transition,
                0,
                "The crucible runs hot. Drive your stone toward gold — but a forging stone is a stone exposed.",
                "Advance your stone through the forge — and oppose any rival that dares to forge.",
                "The moment your stone enters the forge it can be Opposed. Weigh advancing now against waiting for a safer age.",
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
                "autumn.oppose",
                Season.Autumn,
                NarrativeSlotTier.Action,
                2,
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
    }
}
