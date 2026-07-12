using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;

namespace Kismeta.Core.Rules
{
    public enum ActiveEffectPolarity
    {
        Buff,
        Neutral,
        Debuff
    }

    public enum ActiveEffectActionKind
    {
        None,
        ActivateEmperor
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
        Attuned,
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
        public readonly ActiveEffectActionKind Action;

        public ActiveEffectItem(
            string id,
            string title,
            string description,
            ActiveEffectBadge badge,
            string? iconKey = null,
            string? footer = null,
            ActiveEffectPolarity polarity = ActiveEffectPolarity.Neutral,
            ActiveEffectActionKind action = ActiveEffectActionKind.None)
        {
            Id = id;
            Title = title;
            Description = description;
            Footer = footer;
            Badge = badge;
            IconKey = iconKey;
            Polarity = polarity;
            Action = action;
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

    public enum EffectGlanceScope
    {
        General,
        Harvest,
        Commune,
        Contest,
        Opposition,
        Forge,
        Craft,
        Winter
    }

    public readonly struct EffectGlanceContext
    {
        public readonly Season Season;
        public readonly ActionHint Hint;
        public readonly EffectGlanceScope Scope;
        public readonly IReadOnlyList<string>? SpreadOverride;

        public EffectGlanceContext(
            Season season,
            ActionHint hint,
            EffectGlanceScope scope,
            IReadOnlyList<string>? spreadOverride = null)
        {
            Season = season;
            Hint = hint;
            Scope = scope;
            SpreadOverride = spreadOverride;
        }

        public static EffectGlanceContext From(
            GameSession session,
            ActionHint hint = ActionHint.None,
            IReadOnlyList<string>? spreadOverride = null)
        {
            var season = session.Phase.CurrentSeason;
            return new EffectGlanceContext(season, hint, ResolveScope(season, hint), spreadOverride);
        }

        public static EffectGlanceScope ResolveScope(Season season, ActionHint hint) => hint switch
        {
            ActionHint.Commune => EffectGlanceScope.Commune,
            ActionHint.ConfirmHarvest => EffectGlanceScope.Harvest,
            ActionHint.DuelResponse or ActionHint.GambitResponse
                or ActionHint.SummerContestResponse or ActionHint.TradeResponse => EffectGlanceScope.Contest,
            ActionHint.OppositionResponse => EffectGlanceScope.Opposition,
            ActionHint.AutumnForgeResponse => EffectGlanceScope.Forge,
            ActionHint.WinterAction => EffectGlanceScope.Winter,
            _ => season switch
            {
                Season.Spring => EffectGlanceScope.Harvest,
                Season.Summer => EffectGlanceScope.Contest,
                Season.Autumn => EffectGlanceScope.Opposition,
                Season.Winter => EffectGlanceScope.Winter,
                _ => EffectGlanceScope.General
            }
        };
    }

    public readonly struct EffectGlanceSnapshot
    {
        public readonly int TotalCount;
        public readonly IReadOnlyList<ActiveEffectItem> Chips;
        public readonly int OverflowCount;

        public EffectGlanceSnapshot(
            int totalCount,
            IReadOnlyList<ActiveEffectItem> chips,
            int overflowCount)
        {
            TotalCount = totalCount;
            Chips = chips;
            OverflowCount = overflowCount;
        }

        public static EffectGlanceSnapshot Empty { get; } =
            new(0, System.Array.Empty<ActiveEffectItem>(), 0);
    }
}
