using System.Collections.Generic;
using Kismeta.Core.Rules;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    public static class ActiveEffectsAccordion
    {
        static string? s_expandedSectionId;

        public static void Populate(VisualElement? host, IReadOnlyList<ActiveEffectSection> sections)
        {
            if (host == null) return;
            host.Clear();

            if (s_expandedSectionId == null && sections.Count > 0)
                s_expandedSectionId = sections[0].SectionId;

            bool anyExpanded = false;
            foreach (var section in sections)
            {
                if (section.SectionId == s_expandedSectionId)
                {
                    anyExpanded = true;
                    break;
                }
            }

            if (!anyExpanded && sections.Count > 0)
                s_expandedSectionId = sections[0].SectionId;

            foreach (var section in sections)
                host.Add(BuildSection(section));
        }

        static VisualElement BuildSection(ActiveEffectSection section)
        {
            var container = new VisualElement();
            container.AddToClassList("active-effects-section");
            container.userData = section.SectionId;

            bool expanded = section.SectionId == s_expandedSectionId;
            container.EnableInClassList("active-effects-section--expanded", expanded);

            var header = new VisualElement();
            header.AddToClassList("active-effects-section__header");
            header.RegisterCallback<ClickEvent>(_ => ToggleSection(container, section.SectionId));

            var icon = new VisualElement();
            icon.AddToClassList("active-effects-section__icon");
            var iconGlyph = new Label(SectionIcon(section.SectionId));
            iconGlyph.AddToClassList("active-effects-section__icon-glyph");
            icon.Add(iconGlyph);

            var title = new Label(section.Title);
            title.AddToClassList("active-effects-section__title");

            var badge = new VisualElement();
            badge.AddToClassList("active-effects-section__badge");
            var badgeLabel = new Label(section.BadgeText);
            badgeLabel.AddToClassList("active-effects-section__badge-label");
            badge.Add(badgeLabel);

            var chevron = new Label(expanded ? "\u2303" : "\u2304");
            chevron.AddToClassList("active-effects-section__chevron");

            header.Add(icon);
            header.Add(title);
            header.Add(badge);
            header.Add(chevron);

            var body = new VisualElement();
            body.AddToClassList("active-effects-section__body");

            if (section.SectionId == "spread-cards")
                PopulateSpreadBody(body, section.Items);
            else
            {
                foreach (var item in section.Items)
                    body.Add(BuildItem(item));
            }

            container.Add(header);
            container.Add(body);

            if (!string.IsNullOrEmpty(section.FooterNote))
            {
                var footer = new Label(section.FooterNote);
                footer.AddToClassList("active-effects-section__footer");
                container.Add(footer);
            }

            return container;
        }

        static VisualElement BuildItem(ActiveEffectItem item)
        {
            var row = new VisualElement();
            row.AddToClassList("active-effects-item");

            var icon = new VisualElement();
            icon.AddToClassList("active-effects-item__icon");
            icon.AddToClassList(IconClass(item.IconKey));
            var glyph = new Label(ItemIconGlyph(item.IconKey));
            glyph.AddToClassList("active-effects-item__icon-glyph");
            icon.Add(glyph);

            var content = new VisualElement();
            content.AddToClassList("active-effects-item__content");

            var title = new Label(item.Title);
            title.AddToClassList("active-effects-item__title");

            var desc = new Label(item.Description);
            desc.AddToClassList("active-effects-item__desc");

            content.Add(title);
            content.Add(desc);

            if (!string.IsNullOrEmpty(item.Badge.Text))
            {
                var badge = new VisualElement();
                badge.AddToClassList("active-effects-item__badge");
                badge.AddToClassList(BadgeClass(item.Badge.Tone));
                var badgeLabel = new Label(item.Badge.Text);
                badgeLabel.AddToClassList("active-effects-item__badge-label");
                badge.Add(badgeLabel);
                content.Add(badge);
            }

            row.Add(icon);
            row.Add(content);
            return row;
        }

        static void PopulateSpreadBody(VisualElement body, IReadOnlyList<ActiveEffectItem> items)
        {
            ActiveEffectPolarity? currentGroup = null;
            foreach (var item in items)
            {
                if (currentGroup != item.Polarity)
                {
                    currentGroup = item.Polarity;
                    body.Add(BuildSpreadGroupEyebrow(currentGroup.Value));
                }

                body.Add(BuildItem(item));
            }
        }

        static VisualElement BuildSpreadGroupEyebrow(ActiveEffectPolarity polarity)
        {
            string text = polarity switch
            {
                ActiveEffectPolarity.Buff => "buffs",
                ActiveEffectPolarity.Debuff => "debuffs",
                _ => "neutral"
            };

            var eyebrow = new Label(text);
            eyebrow.AddToClassList("active-effects-spread-group__eyebrow");
            return eyebrow;
        }

        static void ToggleSection(VisualElement section, string sectionId)
        {
            if (s_expandedSectionId == sectionId)
            {
                s_expandedSectionId = null;
                section.EnableInClassList("active-effects-section--expanded", false);
                section.Q<Label>(className: "active-effects-section__chevron")!.text = "\u2304";
                return;
            }

            var parent = section.parent;
            if (parent != null)
            {
                foreach (var child in parent.Children())
                {
                    if (!child.ClassListContains("active-effects-section")) continue;
                    child.EnableInClassList("active-effects-section--expanded", false);
                    var chev = child.Q<Label>(className: "active-effects-section__chevron");
                    if (chev != null) chev.text = "\u2304";
                }
            }

            s_expandedSectionId = sectionId;
            section.EnableInClassList("active-effects-section--expanded", true);
            section.Q<Label>(className: "active-effects-section__chevron")!.text = "\u2303";
        }

        static string SectionIcon(string sectionId) => sectionId switch
        {
            "astral-houses" => "\u2302",
            "adepts" => "\u2728",
            "fates" => "\u2637",
            "spread-cards" => "\u2758\u2758",
            _ => "\u2022"
        };

        static string ItemIconGlyph(string? iconKey)
        {
            if (string.IsNullOrEmpty(iconKey)) return "\u2022";
            if (iconKey.StartsWith("fate")) return "\u21BB";
            if (iconKey.StartsWith("adept")) return "\u2726";
            if (iconKey == "house") return "\u2605";
            if (iconKey == "cosmic-age") return "\u223F";
            return iconKey switch
            {
                "wands" => "\u2668",
                "cups" => "\u2665",
                "pentacles" => "\u25C6",
                "swords" => "\u2694",
                _ => "\u2022"
            };
        }

        static string IconClass(string? iconKey)
        {
            if (string.IsNullOrEmpty(iconKey)) return "active-effects-item__icon--major";
            if (iconKey.StartsWith("fate")) return "active-effects-item__icon--fate";
            if (iconKey.StartsWith("adept")) return "active-effects-item__icon--major";
            if (iconKey == "house") return "active-effects-item__icon--house";
            return iconKey switch
            {
                "wands" => "active-effects-item__icon--wands",
                "cups" => "active-effects-item__icon--cups",
                "pentacles" => "active-effects-item__icon--pentacles",
                "swords" => "active-effects-item__icon--swords",
                _ => "active-effects-item__icon--major"
            };
        }

        static string BadgeClass(ActiveEffectBadgeTone tone) => tone switch
        {
            ActiveEffectBadgeTone.Permanent => "active-effects-badge--permanent",
            ActiveEffectBadgeTone.Active => "active-effects-badge--active",
            ActiveEffectBadgeTone.Available => "active-effects-badge--available",
            ActiveEffectBadgeTone.Used => "active-effects-badge--used",
            ActiveEffectBadgeTone.Resolved => "active-effects-badge--resolved",
            ActiveEffectBadgeTone.Aligned => "active-effects-badge--aligned",
            ActiveEffectBadgeTone.Pending => "active-effects-badge--pending",
            ActiveEffectBadgeTone.Arrested => "active-effects-badge--arrested",
            ActiveEffectBadgeTone.Buff => "active-effects-badge--buff",
            ActiveEffectBadgeTone.Debuff => "active-effects-badge--debuff",
            _ => "active-effects-badge--neutral"
        };
    }
}
