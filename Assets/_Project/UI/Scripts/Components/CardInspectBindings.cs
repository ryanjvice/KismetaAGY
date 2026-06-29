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

        static readonly string[] MajorChipClasses = { "card-chip--fate", "card-chip--adept" };

        static readonly string[] ModalThemeClasses =
        {
            "card-modal--inspect", "card-modal--fate", "card-modal--adept"
        };

        static readonly string[] HeroThemeClasses =
        {
            "card-modal__hero--inspect", "card-modal__hero--fate", "card-modal__hero--adept"
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

            var cosmic = session.Board.CosmicAgeSign;
            int pts = AlignmentService.ScoreCard(def.Suit, def.Planet, cosmic);

            if (def.IsMinorArcana)
            {
                ResetModalTheme(root);
                var nameLbl = root.Q<Label>("inspect-name");
                if (nameLbl != null)
                    nameLbl.text = $"{def.Rank} of {def.Suit}";

                BindHero(root, def);
                BindAspects(root, def);
                BindEffect(root, "inspect-effect-section", "inspect-effect-eyebrow", "inspect-effect", def);
                RebuildGoodForTags(root, def, pts);
                ShowMinorDetail(root, true);
                ShowMajorDetail(root, false);
            }
            else
            {
                BindMajorArcana(root, def);
            }

            BindAlignment(root, def, cosmic, pts);
        }

        static void BindMajorArcana(VisualElement root, CardDefinition def)
        {
            var isFate = def.MajorArcanaType == MajorArcanaType.Fate;
            ResetModalTheme(root);
            ApplyModalTheme(root, isFate);

            var nameLbl = root.Q<Label>("inspect-name");
            if (nameLbl != null)
                nameLbl.text = def.Name;

            var subtitle = root.Q<Label>("inspect-subtitle");
            if (subtitle != null)
            {
                var typeLabel = def.MajorArcanaType.ToString().ToLowerInvariant();
                var numeral = RomanNumerals.ToArcanaLabel(def.ArcanaNumber);
                subtitle.text = $"{typeLabel} · {numeral}";
            }

            BindMajorHero(root, def, isFate);
            BindEffect(root, "inspect-major-effect-section", "inspect-major-effect-eyebrow",
                "inspect-major-effect", def);
            ShowMinorDetail(root, false);
            ShowMajorDetail(root, true);
        }

        static void BindMajorHero(VisualElement root, CardDefinition def, bool isFate)
        {
            var hero = root.Q<VisualElement>("inspect-hero-tarot");
            var heroIcon = root.Q<Label>("inspect-hero-icon");
            if (hero == null) return;

            foreach (var cls in SuitChipClasses)
                hero.RemoveFromClassList(cls);
            foreach (var cls in MajorChipClasses)
                hero.RemoveFromClassList(cls);
            hero.AddToClassList(isFate ? "card-chip--fate" : "card-chip--adept");

            var numeral = RomanNumerals.ToArcanaLabel(def.ArcanaNumber);
            CardArtBindings.Apply(hero, heroIcon, def);
            if (heroIcon != null)
            {
                heroIcon.RemoveFromClassList("ti-icon");
                heroIcon.RemoveFromClassList("inspect-hero-icon");
                heroIcon.AddToClassList("inspect-hero-numeral");
                if (Resources.Load<Sprite>($"CardArt/{def.Id}") == null)
                {
                    heroIcon.style.display = DisplayStyle.Flex;
                    heroIcon.text = numeral;
                    if (numeral.Length >= 2)
                        heroIcon.AddToClassList("inspect-hero-numeral--compact");
                    else
                        heroIcon.RemoveFromClassList("inspect-hero-numeral--compact");
                }
            }
        }

        static void ApplyModalTheme(VisualElement root, bool isFate)
        {
            root.RemoveFromClassList("card-modal--inspect");
            root.AddToClassList(isFate ? "card-modal--fate" : "card-modal--adept");

            var heroSection = root.Q(className: "card-modal__hero");
            if (heroSection == null) return;
            foreach (var cls in HeroThemeClasses)
                heroSection.RemoveFromClassList(cls);
            heroSection.AddToClassList(isFate ? "card-modal__hero--fate" : "card-modal__hero--adept");
        }

        static void ResetModalTheme(VisualElement root)
        {
            foreach (var cls in ModalThemeClasses)
                root.RemoveFromClassList(cls);
            root.AddToClassList("card-modal--inspect");

            var heroSection = root.Q(className: "card-modal__hero");
            if (heroSection == null) return;
            foreach (var cls in HeroThemeClasses)
                heroSection.RemoveFromClassList(cls);
            heroSection.AddToClassList("card-modal__hero--inspect");

            var hero = root.Q<VisualElement>("inspect-hero-tarot");
            if (hero != null)
            {
                foreach (var cls in MajorChipClasses)
                    hero.RemoveFromClassList(cls);
            }

            var heroIcon = root.Q<Label>("inspect-hero-icon");
            if (heroIcon != null)
            {
                heroIcon.RemoveFromClassList("inspect-hero-numeral");
                heroIcon.RemoveFromClassList("inspect-hero-numeral--compact");
                heroIcon.AddToClassList("ti-icon");
                heroIcon.AddToClassList("inspect-hero-icon");
            }
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
                SymbolGlyphs.ApplyTablerIcon(heroIcon, SymbolGlyphs.SuitGlyph(def.Suit),
                    SymbolGlyphs.SuitTablerTint(def.Suit, hero: true));
        }

        static void BindAspects(VisualElement root, CardDefinition def)
        {
            var element = Correspondence.ElementFor(def.Suit);

            var elementLbl = root.Q<Label>("inspect-aspect-element");
            if (elementLbl != null)
                elementLbl.text = element != Element.None ? element.ToString() : "—";

            var elementIcon = root.Q<Label>("inspect-aspect-element-icon");
            if (elementIcon != null)
            {
                if (element != Element.None)
                {
                    SymbolGlyphs.ApplyTablerIcon(elementIcon, SymbolGlyphs.ElementGlyph(element),
                        SymbolGlyphs.ElementTablerTint(element));
                }
                else
                {
                    elementIcon.text = "—";
                    elementIcon.style.display = DisplayStyle.Flex;
                }
            }

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
                if (def.Suit != Suit.None)
                {
                    SymbolGlyphs.ApplyTablerIcon(suitIcon, SymbolGlyphs.SuitGlyph(def.Suit),
                        SymbolGlyphs.SuitTablerTint(def.Suit));
                }
                else
                {
                    suitIcon.text = "—";
                    suitIcon.style.display = DisplayStyle.Flex;
                }
            }
        }

        static void BindEffect(
            VisualElement root,
            string sectionName,
            string eyebrowName,
            string textName,
            CardDefinition def)
        {
            var section = root.Q<VisualElement>(sectionName);
            var eyebrow = root.Q<Label>(eyebrowName);
            var text = root.Q<Label>(textName);
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

        static void ShowMajorDetail(VisualElement root, bool visible)
        {
            var section = root.Q<VisualElement>("inspect-major-detail");
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
