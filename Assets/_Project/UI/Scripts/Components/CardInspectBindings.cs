using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    public static class CardInspectBindings
    {
        static readonly string[] SuitChipClasses =
        {
            "card-chip--wands", "card-chip--cups", "card-chip--pentacles", "card-chip--swords"
        };

        public static void BindInspectModal(
            VisualElement root,
            GameSession session,
            string cardInstanceId)
        {
            if (root == null) return;
            var db = session.Rules?.CardDatabase;
            var inst = session.GetCard(cardInstanceId);
            var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;
            if (def == null) return;

            var nameLbl = root.Q<Label>("inspect-name");
            if (nameLbl != null)
                nameLbl.text = def.IsMinorArcana
                    ? $"{def.Rank} of {def.Suit}"
                    : def.EffectText.Length > 0 ? def.EffectText.Split('\n')[0] : def.Id;

            var cosmic = session.Board.CosmicAgeSign;
            int pts = AlignmentService.ScoreCard(def.Suit, def.Planet, cosmic);

            if (def.IsMinorArcana)
            {
                BindHero(root, def);
                BindAspects(root, def);
                BindEffect(root, def);
                RebuildGoodForTags(root, def, pts);
                ShowMinorDetail(root, true);
            }
            else
            {
                ShowMinorDetail(root, false);
            }

            BindAlignment(root, def, cosmic, pts);
        }

        static void BindHero(VisualElement root, CardDefinition def)
        {
            var subtitle = root.Q<Label>("inspect-subtitle");
            if (subtitle != null)
                subtitle.text = $"minor arcana · rank {(int)def.Rank}";

            var hero = root.Q<VisualElement>("inspect-hero-tarot");
            var heroIcon = root.Q<Label>("inspect-hero-icon");
            if (hero != null)
            {
                foreach (var cls in SuitChipClasses)
                    hero.RemoveFromClassList(cls);
                hero.AddToClassList(SuitChipClass(def.Suit));
            }

            CardArtBindings.Apply(hero, heroIcon, def);
            if (heroIcon != null && Resources.Load<Sprite>($"CardArt/{def.Id}") == null)
            {
                heroIcon.text = SymbolGlyphs.SuitGlyph(def.Suit);
                SymbolGlyphs.TagEmoji(heroIcon);
                heroIcon.style.display = DisplayStyle.Flex;
            }
        }

        static void BindAspects(VisualElement root, CardDefinition def)
        {
            var element = Correspondence.ElementFor(def.Suit);

            var elementLbl = root.Q<Label>("inspect-aspect-element");
            if (elementLbl != null)
                elementLbl.text = element != Element.None ? element.ToString() : "—";

            var planetLbl = root.Q<Label>("inspect-aspect-planet");
            if (planetLbl != null)
                planetLbl.text = def.Planet != Planet.None ? def.Planet.ToString() : "—";

            var planetIcon = root.Q<Label>("inspect-aspect-planet-icon");
            if (planetIcon != null)
            {
                planetIcon.text = def.Planet != Planet.None
                    ? SymbolGlyphs.PlanetGlyph(def.Planet)
                    : "—";
                SymbolGlyphs.TagZodiac(planetIcon);
            }

            var suitLbl = root.Q<Label>("inspect-aspect-suit");
            if (suitLbl != null)
                suitLbl.text = def.Suit != Suit.None ? def.Suit.ToString() : "—";

            var suitIcon = root.Q<Label>("inspect-aspect-suit-icon");
            if (suitIcon != null)
            {
                suitIcon.text = def.Suit != Suit.None ? SymbolGlyphs.SuitGlyph(def.Suit) : "—";
                SymbolGlyphs.TagEmoji(suitIcon);
            }
        }

        static void BindEffect(VisualElement root, CardDefinition def)
        {
            var section = root.Q<VisualElement>("inspect-effect-section");
            var eyebrow = root.Q<Label>("inspect-effect-eyebrow");
            var text = root.Q<Label>("inspect-effect");
            if (section == null) return;

            if (string.IsNullOrWhiteSpace(def.EffectText))
            {
                SetDisplay(section, DisplayStyle.None);
                return;
            }

            SetDisplay(section, DisplayStyle.Flex);
            section.style.flexDirection = FlexDirection.Column;
            if (eyebrow != null)
                eyebrow.text = string.IsNullOrWhiteSpace(def.EffectType) ? "effect" : def.EffectType;
            if (text != null)
                text.text = def.EffectText;
        }

        static void BindAlignment(VisualElement root, CardDefinition def, ZodiacSign cosmic, int pts)
        {
            var ageLbl = root.Q<Label>("inspect-align-age");
            var whyLbl = root.Q<Label>("inspect-align-why");
            var ptsLbl = root.Q<Label>("inspect-align-pts");

            if (ageLbl != null)
                ageLbl.text = $"Age of {cosmic} · {Correspondence.PlanetFor(cosmic)} · {Correspondence.ElementFor(cosmic)}";

            if (whyLbl != null)
            {
                var cardElement = Correspondence.ElementFor(def.Suit);
                whyLbl.text = pts switch
                {
                    3 => $"its {def.Sign} matches the cosmic age",
                    2 => $"its {def.Planet} matches the age's planet",
                    1 => $"its {cardElement} matches the age's element",
                    _ => "no aspect matches this age"
                };
            }

            if (ptsLbl != null)
            {
                ptsLbl.text = pts > 0 ? $"+{pts}" : "0";
                ptsLbl.style.color = pts > 0
                    ? new StyleColor(new Color(0.94f, 0.6f, 0.48f))
                    : new StyleColor(new Color(0.72f, 0.6f, 0.43f));
            }
        }

        static void RebuildGoodForTags(VisualElement root, CardDefinition def, int alignPts)
        {
            var container = root.Q<VisualElement>("inspect-good-for");
            if (container == null) return;

            container.Clear();

            if (def.Suit != Suit.None)
            {
                var reagent = Correspondence.ReagentFor(def.Suit);
                container.Add(MakeTag(
                    $"crafting {CraftReagentPanelBindings.ReagentDisplayName(reagent)}",
                    "inspect-tag", "inspect-tag--craft"));
            }

            if (def.Planet != Planet.None)
            {
                container.Add(MakeTag(
                    $"{def.Planet} sets",
                    "inspect-tag", "inspect-tag--set"));
            }

            if (alignPts > 0)
            {
                container.Add(MakeTag(
                    $"alignment +{alignPts}",
                    "inspect-tag", "inspect-tag--align"));
            }
        }

        static Label MakeTag(string text, params string[] ussClasses)
        {
            var tag = new Label(text);
            foreach (var cls in ussClasses)
                tag.AddToClassList(cls);
            return tag;
        }

        static void ShowMinorDetail(VisualElement root, bool visible)
        {
            var section = root.Q<VisualElement>("inspect-minor-detail");
            if (section == null) return;

            section.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (visible)
                section.style.flexDirection = FlexDirection.Column;
        }

        static void SetDisplay(VisualElement el, DisplayStyle display)
        {
            if (el != null)
                el.style.display = display;
        }

        static string SuitChipClass(Suit suit) => suit switch
        {
            Suit.Cups => "card-chip--cups",
            Suit.Pentacles => "card-chip--pentacles",
            Suit.Swords => "card-chip--swords",
            _ => "card-chip--wands"
        };
    }
}
