using System;
using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using UnityEngine;

namespace Kismeta.Data.Loaders
{
    /// <summary>
    /// Loads all 16 Crucible Codex formula entries from crucible-codex.json at runtime.
    /// Implements <see cref="ICrucibleCodexDatabase"/> for injection into rule services.
    ///
    /// crucible-codex.json must live in Assets/_Project/Data/Resources/.
    /// </summary>
    public sealed class CrucibleCodexDatabase : ICrucibleCodexDatabase
    {
        public const string ResourcePath = "crucible-codex";

        // Keyed by (codex, slotIndex).
        private readonly Dictionary<(CodexVariant, int), CodexFormulaDefinition> _byKey = new();
        private readonly Dictionary<CodexVariant, List<CodexFormulaDefinition>>  _byCodex = new();

        public static CrucibleCodexDatabase Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
                throw new InvalidOperationException(
                    $"CrucibleCodexDatabase: could not find '{ResourcePath}' in Resources.");
            return LoadFromJson(asset.text);
        }

        public static CrucibleCodexDatabase LoadFromJson(string json)
        {
            var dto = JsonUtility.FromJson<CodexWrapper>(json);
            var db  = new CrucibleCodexDatabase();

            foreach (var e in dto.entries)
            {
                var codex       = ParseEnum<CodexVariant>(e.codex,       CodexVariant.None);
                var formulaType = ParseEnum<CodexFormulaType>(e.formulaType, CodexFormulaType.None);
                var planet      = ParseEnum<Planet>(e.planet,            Planet.None);
                var requiredSuit= ParseEnum<Suit>(e.suit,                Suit.None);
                var cauldronSuit= CauldronSuitFromName(e.cauldron);

                var def = new CodexFormulaDefinition(
                    codex:          codex,
                    slotIndex:      e.slotIndex,
                    cauldron:       e.cauldron ?? "",
                    cauldronSuit:   cauldronSuit,
                    formulaType:    formulaType,
                    requiredPlanet: planet,
                    requiredSuit:   requiredSuit,
                    minRankSum:     e.minRankSum,
                    displayName:    e.displayName ?? "");

                var key = (codex, e.slotIndex);
                if (!db._byKey.ContainsKey(key))
                    db._byKey[key] = def;

                if (!db._byCodex.ContainsKey(codex))
                    db._byCodex[codex] = new List<CodexFormulaDefinition>();
                db._byCodex[codex].Add(def);
            }

            // Sort each codex group by slot index for consistent ordering.
            foreach (var list in db._byCodex.Values)
                list.Sort((a, b) => a.SlotIndex.CompareTo(b.SlotIndex));

            return db;
        }

        public CodexFormulaDefinition? GetFormula(CodexVariant codex, int slotIndex) =>
            _byKey.TryGetValue((codex, slotIndex), out var def) ? def : null;

        public IReadOnlyList<CodexFormulaDefinition> GetEntriesForCodex(CodexVariant codex) =>
            _byCodex.TryGetValue(codex, out var list) ? list : Array.Empty<CodexFormulaDefinition>();

        // Cauldron name → suit (matches Correspondence table).
        private static Suit CauldronSuitFromName(string? name) => name switch
        {
            "Red"    => Suit.Wands,
            "Blue"   => Suit.Cups,
            "Green"  => Suit.Pentacles,
            "Yellow" => Suit.Swords,
            _        => Suit.None
        };

        private static T ParseEnum<T>(string? value, T fallback) where T : struct, Enum
        {
            if (string.IsNullOrEmpty(value)) return fallback;
            return Enum.TryParse<T>(value, ignoreCase: true, out var result) ? result : fallback;
        }

        // ─── JSON DTOs ────────────────────────────────────────────────────────────

        [Serializable]
        private class CodexWrapper
        {
            public CodexEntryDto[] entries = Array.Empty<CodexEntryDto>();
        }

        [Serializable]
        private class CodexEntryDto
        {
            public string? codex       = null;
            public int     slotIndex   = 0;
            public string? cauldron    = null;
            public string? formulaType = null;
            public string? planet      = null;
            public string? suit        = null;
            public int     minRankSum  = 0;
            public string? displayName = null;
        }
    }
}
