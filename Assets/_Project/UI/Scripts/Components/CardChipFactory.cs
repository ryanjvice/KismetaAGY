using System;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Factory for compact card chips used in spreads, docks, and trays.</summary>
    public static class CardChipFactory
    {
        public static VisualElement Create(
            Rank rank,
            Suit suit,
            Planet planet,
            bool selected = false,
            bool aligned = false,
            string? instanceId = null,
            Action<string>? onInspect = null)
        {
            return CreateInternal(SymbolGlyphs.CompactRank(rank), suit, planet, selected, aligned,
                instanceId, onInspect);
        }

        public static VisualElement CreateFromDefinition(
            CardDefinition def,
            bool selected = false,
            bool aligned = false,
            string? instanceId = null,
            Action<string>? onInspect = null,
            string? rankLabelOverride = null)
        {
            var rankLabel = rankLabelOverride ?? SymbolGlyphs.CompactRank(def.Rank);
            return CreateInternal(rankLabel, def.Suit, def.Planet, selected, aligned,
                instanceId, onInspect);
        }

        static VisualElement CreateInternal(
            string rankLabel,
            Suit suit,
            Planet planet,
            bool selected,
            bool aligned,
            string? instanceId,
            Action<string>? onInspect)
        {
            var chip = new VisualElement();
            chip.AddToClassList("card-chip");
            chip.AddToClassList(SuitClass(suit));
            if (selected || aligned)
                chip.AddToClassList("card-chip--selected");

            if (aligned)
            {
                var dot = new VisualElement();
                dot.AddToClassList("card-chip__align-dot");
                chip.Add(dot);
            }

            if (planet != Planet.None)
            {
                var planetGlyph = SymbolGlyphs.CreatePlanetLabel(
                    SymbolGlyphs.PlanetGlyph(planet), "card-chip__planet");
                chip.Add(planetGlyph);
            }

            var rankLbl = new Label(rankLabel);
            rankLbl.AddToClassList("card-chip__rank");
            if (rankLabel.Length >= 2)
                rankLbl.AddToClassList("card-chip__rank--compact");
            chip.Add(rankLbl);

            if (suit != Suit.None)
            {
                var suitGlyph = SymbolGlyphs.CreateEmojiLabel(SymbolGlyphs.SuitGlyph(suit), "card-chip__suit");
                chip.Add(suitGlyph);
            }

            if (onInspect != null && !string.IsNullOrEmpty(instanceId))
            {
                chip.RegisterCallback<ClickEvent>(_ => onInspect(instanceId));
                chip.style.cursor = StyleKeyword.Auto;
            }

            return chip;
        }

        static string SuitClass(Suit suit) => suit switch
        {
            Suit.Cups => "card-chip--cups",
            Suit.Pentacles => "card-chip--pentacles",
            Suit.Swords => "card-chip--swords",
            _ => "card-chip--wands"
        };
    }
}
