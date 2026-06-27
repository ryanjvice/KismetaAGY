using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Binds side-by-side cosmic age vs. player sign comparison on Spring Harvest.</summary>
    public static class HarvestCompareBindings
    {
        const string SignMatchClass = "harvest-compare__sign--match";
        const string ChipMatchSignClass = "harvest-compare__chip--match-sign";
        const string ChipMatchPlanetClass = "harvest-compare__chip--match-planet";
        const string ChipMatchElementClass = "harvest-compare__chip--match-element";

        static readonly string[] PanelElementClasses =
        {
            "harvest-compare__panel--fire",
            "harvest-compare__panel--water",
            "harvest-compare__panel--earth",
            "harvest-compare__panel--air"
        };

        static readonly string[] ChipElementClasses =
        {
            "harvest-compare__chip--fire",
            "harvest-compare__chip--water",
            "harvest-compare__chip--earth",
            "harvest-compare__chip--air"
        };

        public static void Bind(VisualElement? root, GameSession session, int playerId)
        {
            if (root == null || playerId < 0 || playerId >= session.Players.Count)
                return;

            var cosmic = session.Board.CosmicAgeSign;
            var playerSign = session.Players[playerId].CurrentSign;

            BindSigil(root.Q("compare-age-sigil"), cosmic);
            BindSigil(root.Q("compare-player-sigil"), playerSign);

            SetSignText(root.Q<Label>("compare-age-sign"), cosmic);
            SetSignText(root.Q<Label>("compare-player-sign"), playerSign);

            var cosmicPlanet = Correspondence.PlanetFor(cosmic);
            var playerPlanet = Correspondence.PlanetFor(playerSign);
            var cosmicElement = Correspondence.ElementFor(cosmic);
            var playerElement = Correspondence.ElementFor(playerSign);

            SetAspectText(root.Q<Label>("compare-age-planet"), cosmicPlanet);
            SetAspectText(root.Q<Label>("compare-age-element"), cosmicElement);
            SetAspectText(root.Q<Label>("compare-player-planet"), playerPlanet);
            SetAspectText(root.Q<Label>("compare-player-element"), playerElement);

            SetPanelElement(root.Q("compare-age-panel"), cosmicElement);
            SetPanelElement(root.Q("compare-player-panel"), playerElement);
            SetChipElement(root.Q<Label>("compare-age-element"), cosmicElement);
            SetChipElement(root.Q<Label>("compare-player-element"), playerElement);

            bool signMatch = cosmic != ZodiacSign.None && playerSign != ZodiacSign.None && cosmic == playerSign;
            bool planetMatch = cosmicPlanet != Planet.None && playerPlanet != Planet.None && cosmicPlanet == playerPlanet;
            bool elementMatch = cosmicElement != Element.None && playerElement != Element.None && cosmicElement == playerElement;

            SetSignMatch(root.Q<Label>("compare-age-sign"), signMatch);
            SetSignMatch(root.Q<Label>("compare-player-sign"), signMatch);
            SetChipMatch(root.Q<Label>("compare-age-planet"), signMatch, planetMatch, elementMatch, isPlanet: true);
            SetChipMatch(root.Q<Label>("compare-player-planet"), signMatch, planetMatch, elementMatch, isPlanet: true);
            SetChipMatch(root.Q<Label>("compare-age-element"), signMatch, planetMatch, elementMatch, isPlanet: false);
            SetChipMatch(root.Q<Label>("compare-player-element"), signMatch, planetMatch, elementMatch, isPlanet: false);

            var alignmentLbl = root.Q<Label>("compare-alignment");
            if (alignmentLbl != null)
                alignmentLbl.text = DescribeAlignment(playerSign, cosmic);
        }

        static void BindSigil(VisualElement? sigil, ZodiacSign sign)
        {
            if (sigil == null) return;

            var glyph = sigil.Q<Label>("compare-glyph");
            if (glyph == null)
            {
                glyph = SymbolGlyphs.CreateZodiacLabel(
                    sign == ZodiacSign.None ? "?" : SymbolGlyphs.Zodiac(sign),
                    "harvest-compare__glyph");
                glyph.name = "compare-glyph";
                sigil.Clear();
                sigil.Add(glyph);
                return;
            }

            SymbolGlyphs.TagZodiac(glyph);
            glyph.text = sign == ZodiacSign.None ? "?" : SymbolGlyphs.Zodiac(sign);
        }

        static void SetSignText(Label? lbl, ZodiacSign sign)
        {
            if (lbl == null) return;
            lbl.text = sign == ZodiacSign.None ? "—" : sign.ToString();
        }

        static void SetAspectText(Label? lbl, Planet planet)
        {
            if (lbl == null) return;
            lbl.text = planet == Planet.None ? "—" : planet.ToString();
        }

        static void SetAspectText(Label? lbl, Element element)
        {
            if (lbl == null) return;
            lbl.text = element == Element.None ? "—" : element.ToString();
        }

        static void SetSignMatch(Label? lbl, bool match)
        {
            if (lbl == null) return;
            lbl.EnableInClassList(SignMatchClass, match);
        }

        static void SetChipMatch(Label? lbl, bool signMatch, bool planetMatch, bool elementMatch, bool isPlanet)
        {
            if (lbl == null) return;

            lbl.RemoveFromClassList(ChipMatchSignClass);
            lbl.RemoveFromClassList(ChipMatchPlanetClass);
            lbl.RemoveFromClassList(ChipMatchElementClass);

            if (signMatch)
                lbl.AddToClassList(ChipMatchSignClass);
            else if (isPlanet && planetMatch)
                lbl.AddToClassList(ChipMatchPlanetClass);
            else if (!isPlanet && elementMatch)
                lbl.AddToClassList(ChipMatchElementClass);
        }

        static void SetPanelElement(VisualElement? panel, Element element)
        {
            if (panel == null) return;

            foreach (var cls in PanelElementClasses)
                panel.RemoveFromClassList(cls);

            var clsToAdd = ElementClassFor(element, PanelElementClasses);
            if (clsToAdd != null)
                panel.AddToClassList(clsToAdd);
        }

        static void SetChipElement(Label? lbl, Element element)
        {
            if (lbl == null) return;

            foreach (var cls in ChipElementClasses)
                lbl.RemoveFromClassList(cls);

            var clsToAdd = ElementClassFor(element, ChipElementClasses);
            if (clsToAdd != null)
                lbl.AddToClassList(clsToAdd);
        }

        static string? ElementClassFor(Element element, string[] classes) => element switch
        {
            Element.Fire => classes[0],
            Element.Water => classes[1],
            Element.Earth => classes[2],
            Element.Air => classes[3],
            _ => null
        };

        static string DescribeAlignment(ZodiacSign playerSign, ZodiacSign cosmicSign)
        {
            int bonus = HarvestBreakdownService.AlignmentBonus(playerSign, cosmicSign);
            return bonus switch
            {
                3 => "+3 sign match",
                2 => "+2 planet match",
                1 => "+1 element match",
                _ => "no aspect match"
            };
        }
    }
}
