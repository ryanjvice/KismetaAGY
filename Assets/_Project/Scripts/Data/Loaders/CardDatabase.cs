using System;
using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using UnityEngine;

namespace Kismeta.Data.Loaders
{
    /// <summary>
    /// Loads all 156 CardDefinitions from the generated cards.json at runtime.
    /// Provides a single lookup point for the game layer to resolve definitions by ID.
    ///
    /// Usage: Call CardDatabase.Load() once at startup (e.g. from GameBootstrap).
    /// cards.json must live in Assets/_Project/Data/Resources/ so Resources.Load can find it.
    /// </summary>
    public sealed class CardDatabase
    {
        public const int ExpectedCardCount = 156;
        public const string ResourcePath   = "cards";

        private readonly Dictionary<string, CardDefinition> _byId = new();

        public IReadOnlyDictionary<string, CardDefinition> All => _byId;
        public int Count => _byId.Count;

        /// <summary>Loads cards.json from Resources and returns a populated CardDatabase.</summary>
        public static CardDatabase Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
                throw new InvalidOperationException(
                    $"CardDatabase: could not find '{ResourcePath}' in Resources. " +
                    $"Run 'npm run data:sync' and ensure cards.json is in Assets/_Project/Data/Resources/.");

            return LoadFromJson(asset.text);
        }

        /// <summary>Parses JSON text directly. Used by tests without Unity Resources.</summary>
        public static CardDatabase LoadFromJson(string json)
        {
            // Unity's JsonUtility cannot deserialize top-level arrays, so we wrap.
            var wrapped = "{ \"cards\": " + json + " }";
            var dto     = JsonUtility.FromJson<CardListWrapper>(wrapped);

            var db = new CardDatabase();
            foreach (var d in dto.cards)
            {
                var def = MapDto(d);
                if (db._byId.ContainsKey(def.Id))
                    Debug.LogWarning($"[CardDatabase] Duplicate card id: {def.Id}");
                else
                    db._byId[def.Id] = def;
            }

            if (db.Count != ExpectedCardCount)
                Debug.LogWarning(
                    $"[CardDatabase] Expected {ExpectedCardCount} cards, loaded {db.Count}. " +
                    "Run 'npm run data:sync' if the count is wrong.");
            else
                Debug.Log($"[CardDatabase] Loaded {db.Count} cards successfully.");

            return db;
        }

        public CardDefinition? GetById(string id) =>
            _byId.TryGetValue(id, out var def) ? def : null;

        public IEnumerable<CardDefinition> GetBySuit(Suit suit)
        {
            foreach (var d in _byId.Values)
                if (d.Suit == suit) yield return d;
        }

        public IEnumerable<CardDefinition> GetCrucibleByGroup(CrucibleGroup group)
        {
            foreach (var d in _byId.Values)
                if (d.IsCrucible && d.CrucibleGroup == group) yield return d;
        }

        private static CardDefinition MapDto(CardJsonDto d)
        {
            var deck            = ParseEnum<Deck>(d.deck, Deck.Kismeta);
            var suit            = ParseEnum<Suit>(d.suit, Suit.None);
            var rank            = ParseEnum<Rank>(d.rank, Rank.None);
            var variant         = (CardVariant)d.cardVariant;
            var majorArcana     = ParseEnum<MajorArcanaType>(d.majorArcanaType, MajorArcanaType.None);
            var sign            = ParseEnum<ZodiacSign>(d.sign, ZodiacSign.None);
            var planet          = ParsePlanet(d.planet);
            var crucibleGroup   = ParseEnum<CrucibleGroup>(d.crucibleGroup, CrucibleGroup.None);

            ReagentCost cost = ReagentCost.Zero;
            if (d.alchemicalCost != null)
            {
                cost = new ReagentCost(
                    d.alchemicalCost.sulphur,
                    d.alchemicalCost.aquaRegia,
                    d.alchemicalCost.vitriol,
                    d.alchemicalCost.quicksilver,
                    d.alchemicalCost.salt);
            }

            return new CardDefinition(
                id:                  d.id,
                deck:                deck,
                suit:                suit,
                rank:                rank,
                variant:             variant,
                majorArcanaType:     majorArcana,
                arcanaNumber:        d.arcanaNumber,
                sign:                sign,
                planet:              planet,
                effectType:          d.effectType ?? "",
                effectText:          d.effectText ?? "",
                wildcardArcanaNumber:d.wildcardArcanaNumber,
                crucibleGroup:       crucibleGroup,
                activationFormula:   d.activationFormula ?? "",
                alchemicalFormula:   d.alchemicalFormula ?? "",
                alchemicalCost:      cost);
        }

        private static T ParseEnum<T>(string? value, T fallback) where T : struct, Enum
        {
            if (string.IsNullOrEmpty(value)) return fallback;
            return Enum.TryParse<T>(value, ignoreCase: true, out var result) ? result : fallback;
        }

        private static Planet ParsePlanet(string? value)
        {
            if (string.IsNullOrEmpty(value)) return Planet.None;
            // "Sun/Moon" entries map to Moon (the primary night luminary in Kismeta correspondence).
            if (value == "Sun/Moon") return Planet.Moon;
            return Enum.TryParse<Planet>(value, ignoreCase: true, out var p) ? p : Planet.None;
        }
    }
}
