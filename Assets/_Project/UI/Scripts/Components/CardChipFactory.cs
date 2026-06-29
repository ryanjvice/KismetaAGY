using System;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using UnityEngine;
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

            var topRow = new VisualElement();
            topRow.AddToClassList("card-chip__top");
            topRow.pickingMode = PickingMode.Ignore;

            var suitCorner = new VisualElement();
            suitCorner.AddToClassList("card-chip__corner");
            suitCorner.AddToClassList("card-chip__corner--left");
            suitCorner.pickingMode = PickingMode.Ignore;
            if (suit != Suit.None)
            {
                var suitGlyph = SymbolGlyphs.CreateChipSuitLabel(
                    SymbolGlyphs.SuitGlyph(suit), Color.white);
                suitGlyph.pickingMode = PickingMode.Ignore;
                suitCorner.Add(suitGlyph);
            }
            topRow.Add(suitCorner);

            var planetCorner = new VisualElement();
            planetCorner.AddToClassList("card-chip__corner");
            planetCorner.AddToClassList("card-chip__corner--right");
            planetCorner.pickingMode = PickingMode.Ignore;
            if (planet != Planet.None)
            {
                var planetGlyph = SymbolGlyphs.CreatePlanetLabel(
                    SymbolGlyphs.PlanetGlyph(planet), "card-chip__planet");
                planetGlyph.pickingMode = PickingMode.Ignore;
                planetCorner.Add(planetGlyph);
            }
            topRow.Add(planetCorner);
            chip.Add(topRow);

            var rankLbl = new Label(rankLabel);
            rankLbl.AddToClassList("card-chip__rank");
            if (rankLabel.Length >= 2)
                rankLbl.AddToClassList("card-chip__rank--compact");
            rankLbl.pickingMode = PickingMode.Ignore;
            chip.Add(rankLbl);

            var bottomRow = new VisualElement();
            bottomRow.AddToClassList("card-chip__bottom");
            bottomRow.pickingMode = PickingMode.Ignore;

            if (inspectViaButton && !string.IsNullOrEmpty(instanceId))
            {
                chip.AddToClassList("card-chip--inspectable");
                var icon = SymbolGlyphs.CreateInfoIconLabel("card-chip__inspect");
                icon.pickingMode = PickingMode.Ignore;
                bottomRow.Add(icon);
            }

            chip.Add(bottomRow);

            if (aligned)
            {
                var dot = new VisualElement();
                dot.AddToClassList("card-chip__align-dot");
                dot.pickingMode = PickingMode.Ignore;
                chip.Add(dot);
            }

            if (onInspect != null && !string.IsNullOrEmpty(instanceId))
                WireInspect(chip, instanceId, onInspect);

            return chip;
        }

        public static void WireTap(VisualElement chip, Action onTap, bool pulse = true)
        {
            chip.pickingMode = PickingMode.Position;
            chip.style.cursor = new StyleCursor(StyleKeyword.Auto);
            chip.AddToClassList("card-chip--tappable");

            chip.Q(className: "card-chip__hit")?.RemoveFromHierarchy();
            SetIgnorePicking(chip);

            var hit = new VisualElement();
            hit.AddToClassList("card-chip__hit");
            hit.pickingMode = PickingMode.Position;
            chip.Add(hit);

            hit.AddManipulator(new Clickable(() =>
            {
                if (pulse) UiMotion.PulseChip(chip);
                onTap();
            }));
        }

        static void WireInspect(VisualElement chip, string instanceId, Action<string> onInspect)
        {
            var cardId = instanceId;
            WireTap(chip, () => onInspect(cardId), pulse: false);
        }

        static void SetIgnorePicking(VisualElement root)
        {
            foreach (var child in root.Children())
            {
                if (child.ClassListContains("card-chip__hit"))
                    continue;
                child.pickingMode = PickingMode.Ignore;
                SetIgnorePicking(child);
            }
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
