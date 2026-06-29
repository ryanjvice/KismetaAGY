using System.Collections.Generic;
using Kismeta.Core.Rules;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    public static class ActiveEffectsRows
    {
        public static void Populate(VisualElement? root, ActiveEffectsSnapshot snapshot, string? subtitleOverride = null)
        {
            if (root == null) return;

            var subtitle = root.Q<Label>("active-effects-subtitle");
            if (subtitle != null)
                subtitle.text = subtitleOverride ?? snapshot.Subtitle;

            PopulateFeatured(root.Q<VisualElement>("cosmic-age-featured"), snapshot.CosmicAge);
            ActiveEffectsAccordion.Populate(root.Q<VisualElement>("sections-list"), snapshot.Sections);
        }

        static void PopulateFeatured(VisualElement? host, ActiveEffectItem item)
        {
            if (host == null) return;
            host.Clear();

            var topRow = new VisualElement();
            topRow.AddToClassList("active-effects-featured__row");

            var iconWrap = new VisualElement();
            iconWrap.AddToClassList("active-effects-featured__icon");
            var iconGlyph = new Label("\u223F");
            iconGlyph.AddToClassList("active-effects-featured__icon-glyph");
            iconWrap.Add(iconGlyph);

            var pill = new VisualElement();
            pill.AddToClassList("active-effects-featured__pill");
            var pillLabel = new Label("Cosmic Age");
            pillLabel.AddToClassList("active-effects-featured__pill-label");
            pill.Add(pillLabel);

            var expires = new Label(item.Badge.Text);
            expires.AddToClassList("active-effects-featured__expires");

            topRow.Add(iconWrap);
            topRow.Add(pill);
            topRow.Add(expires);

            var body = new Label(item.Description);
            body.AddToClassList("active-effects-featured__body");

            host.Add(topRow);
            host.Add(body);

            if (!string.IsNullOrEmpty(item.Footer))
            {
                var footer = new Label(item.Footer);
                footer.AddToClassList("active-effects-featured__footer");
                host.Add(footer);
            }
        }
    }
}
