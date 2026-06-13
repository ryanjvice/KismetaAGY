using System;

namespace Kismeta.Data.Loaders
{
    /// <summary>
    /// Data Transfer Objects matching the cards.json schema produced by export-cards.mjs.
    /// Used only for deserialization; converted to CardDefinition by CardDatabase.
    /// </summary>
    [Serializable]
    public sealed class CardJsonDto
    {
        public string id            = "";
        public string deck          = "";
        public string? suit         = null;
        public string? rank         = null;
        public int    cardVariant   = 0;
        public string? majorArcanaType = null;
        public int    arcanaNumber  = -1;
        public string? name         = null;
        public string? sign         = null;
        public string? planet       = null;
        public string? effectType   = null;
        public string? effectText   = null;
        public string? effectTextResonant = null;
        public bool   isCurse       = false;
        public string? nullifiesCard = null;
        public int    wildcardArcanaNumber = -1;
        public string? wildcardArcanaMajorName = null;
        public string? crucibleGroup = null;
        public string? activationFormula  = null;
        public string? alchemicalFormula  = null;
        public AlchemicalCostDto? alchemicalCost = null;
    }

    [Serializable]
    public sealed class AlchemicalCostDto
    {
        public int sulphur     = 0;
        public int aquaRegia   = 0;
        public int vitriol     = 0;
        public int quicksilver = 0;
        public int salt        = 0;
    }

    /// <summary>Unity JsonUtility cannot deserialize top-level arrays; wrap in an object.</summary>
    [Serializable]
    internal sealed class CardListWrapper
    {
        public CardJsonDto[] cards = Array.Empty<CardJsonDto>();
    }
}
