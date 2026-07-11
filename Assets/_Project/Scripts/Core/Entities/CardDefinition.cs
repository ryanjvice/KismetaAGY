using Kismeta.Core.Domain;

namespace Kismeta.Core.Entities
{
    /// <summary>
    /// Immutable template for a single card. Loaded from JSON at startup.
    /// Two CardDefinitions share the same Suit+Rank but differ by Variant (Minor Arcana pairs).
    /// </summary>
    public sealed class CardDefinition
    {
        public string Id { get; }
        public Deck Deck { get; }

        // Minor Arcana fields (null for Major Arcana / Crucible)
        public Suit Suit { get; }
        public Rank Rank { get; }
        public CardVariant Variant { get; }

        // Major Arcana fields (null / None for Minor Arcana)
        public MajorArcanaType MajorArcanaType { get; }
        /// <summary>Tarot number (0–21). -1 when not a Major Arcana card.</summary>
        public int ArcanaNumber { get; }

        // Adept-specific aspects (None for Fates and Minor Arcana)
        public ZodiacSign Sign { get; }

        // Shared
        public string Name { get; }
        public Planet Planet { get; }
        public string EffectType { get; }
        public string EffectText { get; }
        /// <summary>Resonant-layer copy for UI when the adept is attuned. Not parsed for rules.</summary>
        public string EffectTextResonant { get; }
        /// <summary>True for rank 4–6 V1 curse cards.</summary>
        public bool IsCurse { get; }
        /// <summary>Element-wheel pairing label for UI (e.g. "5 of Wands"). Not used for cross-player rules.</summary>
        public string NullifiesCard { get; }

        // Minor Arcana Variant 2 — link to Major Arcana by arcana number (-1 = none)
        public int WildcardArcanaNumber { get; }
        /// <summary>Display label for linked major (e.g. "The Sun"). UI/catalog only.</summary>
        public string WildcardArcanaMajorName { get; }

        // Crucible-specific
        public CrucibleGroup CrucibleGroup { get; }
        public string AlchemicalFormula { get; }
        public ReagentCost AlchemicalCost { get; }

        public bool IsMinorArcana => Deck == Deck.Kismeta && Suit != Suit.None;
        public bool IsMajorArcana => Deck == Deck.Kismeta && Suit == Suit.None;
        public bool IsCrucible => Deck == Deck.Crucible;

        public CardDefinition(
            string id,
            Deck deck,
            Suit suit,
            Rank rank,
            CardVariant variant,
            MajorArcanaType majorArcanaType,
            int arcanaNumber,
            ZodiacSign sign,
            Planet planet,
            string name,
            string effectType,
            string effectText,
            string effectTextResonant,
            bool isCurse,
            string nullifiesCard,
            int wildcardArcanaNumber,
            string wildcardArcanaMajorName,
            CrucibleGroup crucibleGroup,
            string alchemicalFormula,
            ReagentCost alchemicalCost)
        {
            Id = id;
            Deck = deck;
            Suit = suit;
            Rank = rank;
            Variant = variant;
            MajorArcanaType = majorArcanaType;
            ArcanaNumber = arcanaNumber;
            Sign = sign;
            Planet = planet;
            Name = name;
            EffectType = effectType;
            EffectText = effectText;
            EffectTextResonant = effectTextResonant;
            IsCurse = isCurse;
            NullifiesCard = nullifiesCard;
            WildcardArcanaNumber = wildcardArcanaNumber;
            WildcardArcanaMajorName = wildcardArcanaMajorName;
            CrucibleGroup = crucibleGroup;
            AlchemicalFormula = alchemicalFormula;
            AlchemicalCost = alchemicalCost;
        }
    }

    /// <summary>Reagent cost breakdown from the Crucible card tables (SP, AR, V, Q, S columns).</summary>
    public readonly struct ReagentCost
    {
        public readonly int Sulphur;
        public readonly int AquaRegia;
        public readonly int Vitriol;
        public readonly int Quicksilver;
        public readonly int Salt;
        public int Total => Sulphur + AquaRegia + Vitriol + Quicksilver + Salt;

        public ReagentCost(int sulphur, int aquaRegia, int vitriol, int quicksilver, int salt)
        {
            Sulphur = sulphur;
            AquaRegia = aquaRegia;
            Vitriol = vitriol;
            Quicksilver = quicksilver;
            Salt = salt;
        }

        public static readonly ReagentCost Zero = new(0, 0, 0, 0, 0);
    }
}
