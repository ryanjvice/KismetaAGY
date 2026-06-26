using Kismeta.Core.Domain;
using UnityEngine;

namespace Kismeta.UI.Components
{
    /// <summary>Shared UI Toolkit sprite catalog for board-game art (wheel, cauldrons, titles).</summary>
    [CreateAssetMenu(fileName = "UiArtCatalog", menuName = "Kismeta/UI/UI Art Catalog")]
    public sealed class UiArtCatalog : ScriptableObject
    {
        [Header("Table & backdrop")]
        [SerializeField] Sprite? _tableFeltVignette;
        [SerializeField] Sprite? _heroBurgundy;

        [Header("Wheel stack")]
        [SerializeField] Sprite? _starChart;
        [SerializeField] Sprite? _zodiacWheel;
        [SerializeField] Sprite? _mantleRing;
        [SerializeField] Sprite? _cauldronBackground;
        [SerializeField] Sprite? _crucibleForge;

        [Header("Title wordmarks")]
        [SerializeField] Sprite? _kismetaMetallic;
        [SerializeField] Sprite? _alchemistsMetallic;

        [Header("Cauldrons (by suit)")]
        [SerializeField] Sprite? _cauldronWands;
        [SerializeField] Sprite? _cauldronCups;
        [SerializeField] Sprite? _cauldronSwords;
        [SerializeField] Sprite? _cauldronPentacles;

        public Sprite? TableFeltVignette => _tableFeltVignette;
        public Sprite? HeroBurgundy => _heroBurgundy;
        public Sprite? StarChart => _starChart;
        public Sprite? ZodiacWheel => _zodiacWheel;
        public Sprite? MantleRing => _mantleRing;
        public Sprite? CauldronBackground => _cauldronBackground;
        public Sprite? CrucibleForge => _crucibleForge;
        public Sprite? KismetaMetallic => _kismetaMetallic;
        public Sprite? AlchemistsMetallic => _alchemistsMetallic;

        public Sprite? CauldronFor(Suit suit) => suit switch
        {
            Suit.Wands => _cauldronWands,
            Suit.Cups => _cauldronCups,
            Suit.Swords => _cauldronSwords,
            Suit.Pentacles => _cauldronPentacles,
            _ => null
        };
    }
}
