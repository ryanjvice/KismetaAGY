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
            Action<string>? onInspect = null,
            bool inspectViaButton = false)
        {
            return CreateInternal(SymbolGlyphs.CompactRank(rank), suit, planet, selected, aligned,
                instanceId, onInspect, inspectViaButton);
        }

        public static VisualElement CreateFromDefinition(
            CardDefinition def,
            bool selected = false,
            bool aligned = false,
            string? instanceId = null,
            Action<string>? onInspect = null,
            string? rankLabelOverride = null,
            bool inspectViaButton = false)
        {
            var rankLabel = rankLabelOverride ?? SymbolGlyphs.CompactRank(def.Rank);
            return CreateInternal(rankLabel, def.Suit, def.Planet, selected, aligned,
                instanceId, onInspect, inspectViaButton);
        }

        static VisualElement CreateInternal(
            string rankLabel,
            Suit suit,
            Planet planet,
            bool selected,
            bool aligned,
            string? instanceId,
            Action<string>? onInspect,
            bool inspectViaButton)
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
                dot.pickingMode = PickingMode.Ignore;
                chip.Add(dot);
            }

            if (planet != Planet.None)
            {
                var planetGlyph = SymbolGlyphs.CreatePlanetLabel(
                    SymbolGlyphs.PlanetGlyph(planet), "card-chip__planet");
                planetGlyph.pickingMode = PickingMode.Ignore;
                chip.Add(planetGlyph);
            }

            var rankLbl = new Label(rankLabel);
            rankLbl.AddToClassList("card-chip__rank");
            if (rankLabel.Length >= 2)
                rankLbl.AddToClassList("card-chip__rank--compact");
            rankLbl.pickingMode = PickingMode.Ignore;
            chip.Add(rankLbl);

            if (suit != Suit.None)
            {
                var suitGlyph = SymbolGlyphs.CreateEmojiLabel(SymbolGlyphs.SuitGlyph(suit), "card-chip__suit");
                suitGlyph.pickingMode = PickingMode.Ignore;
                chip.Add(suitGlyph);
            }

            if (onInspect != null && !string.IsNullOrEmpty(instanceId))
            {
                if (inspectViaButton)
                {
                    chip.AddToClassList("card-chip--inspectable");
                    var icon = SymbolGlyphs.CreateInfoIconLabel("card-chip__inspect");
                    chip.Add(icon);
                }

                WireInspect(chip, instanceId, onInspect);
            }

            return chip;
        }

        static void WireInspect(VisualElement chip, string instanceId, Action<string> onInspect)
        {
            chip.pickingMode = PickingMode.Position;
            chip.style.cursor = new StyleCursor(StyleKeyword.Auto);
            foreach (var child in chip.Children())
                child.pickingMode = PickingMode.Ignore;

            var cardId = instanceId;
            chip.AddManipulator(new Clickable(() => onInspect(cardId)));
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
