using System.Collections.Generic;

namespace Kismeta.Core.Rules
{
    public enum ActiveEffectPolarity
    {
        Buff,
        Neutral,
        Debuff
    }

    public enum ActiveEffectBadgeTone
    {
        Neutral,
        Permanent,
        Active,
        Available,
        Used,
        Resolved,
        Aligned,
        Pending,
        Arrested,
        Buff,
        Debuff
    }

    public readonly struct ActiveEffectBadge
    {
        public readonly string Text;
        public readonly ActiveEffectBadgeTone Tone;

        public ActiveEffectBadge(string text, ActiveEffectBadgeTone tone)
        {
            Text = text;
            Tone = tone;
        }
    }

    public readonly struct ActiveEffectItem
    {
        public readonly string Id;
        public readonly string Title;
        public readonly string Description;
        public readonly string? Footer;
        public readonly ActiveEffectBadge Badge;
        public readonly string? IconKey;
        public readonly ActiveEffectPolarity Polarity;

        public ActiveEffectItem(
            string id,
            string title,
            string description,
            ActiveEffectBadge badge,
            string? iconKey = null,
            string? footer = null,
            ActiveEffectPolarity polarity = ActiveEffectPolarity.Neutral)
        {
            Id = id;
            Title = title;
            Description = description;
            Footer = footer;
            Badge = badge;
            IconKey = iconKey;
            Polarity = polarity;
        }
    }

    public readonly struct ActiveEffectSection
    {
        public readonly string SectionId;
        public readonly string Title;
        public readonly string BadgeText;
        public readonly IReadOnlyList<ActiveEffectItem> Items;
        public readonly string? FooterNote;

        public ActiveEffectSection(
            string sectionId,
            string title,
            string badgeText,
            IReadOnlyList<ActiveEffectItem> items,
            string? footerNote = null)
        {
            SectionId = sectionId;
            Title = title;
            BadgeText = badgeText;
            Items = items;
            FooterNote = footerNote;
        }
    }

    public readonly struct ActiveEffectsSnapshot
    {
        public readonly string Subtitle;
        public readonly ActiveEffectItem CosmicAge;
        public readonly IReadOnlyList<ActiveEffectSection> Sections;

        public ActiveEffectsSnapshot(
            string subtitle,
            ActiveEffectItem cosmicAge,
            IReadOnlyList<ActiveEffectSection> sections)
        {
            Subtitle = subtitle;
            CosmicAge = cosmicAge;
            Sections = sections;
        }
    }
}
