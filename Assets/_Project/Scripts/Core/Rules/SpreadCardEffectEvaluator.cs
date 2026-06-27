using System;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    public readonly struct SpreadCardEffectState
    {
        public bool IsListed { get; }
        public bool IsInactive { get; }
        public ActiveEffectPolarity Polarity { get; }
        public ActiveEffectBadge Badge { get; }
        public string Description { get; }

        public SpreadCardEffectState(
            bool isListed,
            ActiveEffectPolarity polarity,
            ActiveEffectBadge badge,
            string description)
        {
            IsListed = isListed;
            IsInactive = !isListed;
            Polarity = polarity;
            Badge = badge;
            Description = description;
        }
    }

    /// <summary>Determines whether a spread card should appear in Active Effects and how to describe it.</summary>
    public static class SpreadCardEffectEvaluator
    {
        public static SpreadCardEffectState Evaluate(
            PlayerState player,
            CardDefinition def,
            ZodiacSign cosmic,
            int alignPts,
            string? crucibleSlotLabel)
        {
            bool hasAlignment = alignPts > 0;
            bool hasCrucible = crucibleSlotLabel != null;
            var effectKind = ClassifyEffect(def);
            bool cardEffectActive = IsCardEffectActive(player, def, cosmic, effectKind);
            bool isActionCard = effectKind == SpreadCardEffectKind.Action;

            bool isListed = hasAlignment || hasCrucible || cardEffectActive || isActionCard;
            if (!isListed)
            {
                return new SpreadCardEffectState(
                    false,
                    ActiveEffectPolarity.Neutral,
                    new ActiveEffectBadge(string.Empty, ActiveEffectBadgeTone.Neutral),
                    string.Empty);
            }

            var polarity = ResolvePolarity(def, effectKind, cardEffectActive, hasAlignment, hasCrucible);
            var badge = ResolveBadge(def, effectKind, cardEffectActive, hasAlignment, hasCrucible, polarity);
            var description = BuildDescription(
                def,
                cosmic,
                alignPts,
                crucibleSlotLabel,
                effectKind,
                cardEffectActive,
                isActionCard);

            return new SpreadCardEffectState(true, polarity, badge, description);
        }

        enum SpreadCardEffectKind
        {
            None,
            Reversed,
            Combat,
            Passive,
            SpreadPassive,
            Forge,
            Wildcard,
            Action
        }

        static SpreadCardEffectKind ClassifyEffect(CardDefinition def)
        {
            if (string.IsNullOrWhiteSpace(def.EffectText))
                return SpreadCardEffectKind.None;

            return def.EffectType switch
            {
                "Reversed" => SpreadCardEffectKind.Reversed,
                "Gambit" or "Duel" => SpreadCardEffectKind.Combat,
                "Passive" => SpreadCardEffectKind.Passive,
                "Harvest" or "Opposition" or "Social" => SpreadCardEffectKind.SpreadPassive,
                "Forge" => SpreadCardEffectKind.Forge,
                "WildcardLink" => SpreadCardEffectKind.Wildcard,
                "Craft" or "Build" or "Entry Fee" => SpreadCardEffectKind.Action,
                _ => SpreadCardEffectKind.None
            };
        }

        static bool IsCardEffectActive(
            PlayerState player,
            CardDefinition def,
            ZodiacSign cosmic,
            SpreadCardEffectKind kind)
        {
            return kind switch
            {
                SpreadCardEffectKind.Reversed => true,
                SpreadCardEffectKind.Combat => true,
                SpreadCardEffectKind.Passive => IsPassiveActive(player, def, cosmic),
                SpreadCardEffectKind.SpreadPassive => IsSpreadPassiveActive(player, def),
                SpreadCardEffectKind.Forge => true,
                SpreadCardEffectKind.Wildcard => true,
                SpreadCardEffectKind.Action => true,
                _ => false
            };
        }

        static bool IsPassiveActive(PlayerState player, CardDefinition def, ZodiacSign cosmic)
        {
            if (TryParseCosmicElementCondition(def.EffectText, out var requiredElement))
                return requiredElement == Correspondence.ElementFor(cosmic);

            return true;
        }

        static bool IsSpreadPassiveActive(PlayerState player, CardDefinition def)
        {
            if (def.EffectType != "Harvest")
                return true;

            if (!TryParseHarvestElementCondition(def.EffectText, out var requiredElement))
                return true;

            foreach (var houseSign in player.AstralHouses)
            {
                if (Correspondence.ElementFor(houseSign) == requiredElement)
                    return true;
            }

            return false;
        }

        static ActiveEffectPolarity ResolvePolarity(
            CardDefinition def,
            SpreadCardEffectKind kind,
            bool cardEffectActive,
            bool hasAlignment,
            bool hasCrucible)
        {
            if (kind == SpreadCardEffectKind.Reversed && cardEffectActive)
                return ActiveEffectPolarity.Debuff;

            if (kind == SpreadCardEffectKind.Combat && cardEffectActive)
                return ClassifyTextPolarity(def.EffectText);

            if (hasAlignment)
                return ActiveEffectPolarity.Buff;

            if (kind == SpreadCardEffectKind.Action)
                return ActiveEffectPolarity.Neutral;

            if (kind == SpreadCardEffectKind.Wildcard && cardEffectActive)
                return ActiveEffectPolarity.Neutral;

            if (hasCrucible && !cardEffectActive)
                return ActiveEffectPolarity.Neutral;

            if (cardEffectActive && kind is SpreadCardEffectKind.Passive
                or SpreadCardEffectKind.SpreadPassive
                or SpreadCardEffectKind.Forge)
            {
                return ClassifyTextPolarity(def.EffectText) == ActiveEffectPolarity.Debuff
                    ? ActiveEffectPolarity.Debuff
                    : ActiveEffectPolarity.Buff;
            }

            if (hasCrucible)
                return ActiveEffectPolarity.Neutral;

            return ActiveEffectPolarity.Neutral;
        }

        static ActiveEffectBadge ResolveBadge(
            CardDefinition def,
            SpreadCardEffectKind kind,
            bool cardEffectActive,
            bool hasAlignment,
            bool hasCrucible,
            ActiveEffectPolarity polarity)
        {
            if (kind == SpreadCardEffectKind.Reversed && cardEffectActive)
                return new ActiveEffectBadge("reversed · active", ActiveEffectBadgeTone.Debuff);

            if (kind == SpreadCardEffectKind.Combat && cardEffectActive)
            {
                string label = def.EffectType.Equals("Duel", StringComparison.OrdinalIgnoreCase)
                    ? "duel · active"
                    : "gambit · active";
                return polarity == ActiveEffectPolarity.Debuff
                    ? new ActiveEffectBadge(label, ActiveEffectBadgeTone.Debuff)
                    : new ActiveEffectBadge(label, ActiveEffectBadgeTone.Buff);
            }

            if (kind == SpreadCardEffectKind.Passive && cardEffectActive)
                return new ActiveEffectBadge("passive · active", ActiveEffectBadgeTone.Buff);

            if (kind is SpreadCardEffectKind.SpreadPassive or SpreadCardEffectKind.Forge && cardEffectActive)
            {
                string label = $"{def.EffectType.ToLowerInvariant()} · active";
                return new ActiveEffectBadge(label, ActiveEffectBadgeTone.Buff);
            }

            if (hasAlignment)
                return new ActiveEffectBadge("aligned this age", ActiveEffectBadgeTone.Aligned);

            if (kind == SpreadCardEffectKind.Wildcard && cardEffectActive)
                return new ActiveEffectBadge("wildcard link", ActiveEffectBadgeTone.Neutral);

            if (kind == SpreadCardEffectKind.Action)
                return new ActiveEffectBadge("action · available", ActiveEffectBadgeTone.Neutral);

            if (hasCrucible)
                return new ActiveEffectBadge("in activation set", ActiveEffectBadgeTone.Neutral);

            return new ActiveEffectBadge("active", ActiveEffectBadgeTone.Neutral);
        }

        static string BuildDescription(
            CardDefinition def,
            ZodiacSign cosmic,
            int alignPts,
            string? crucibleSlotLabel,
            SpreadCardEffectKind kind,
            bool cardEffectActive,
            bool isActionCard)
        {
            var parts = new System.Collections.Generic.List<string> { "In spread" };

            if (alignPts > 0)
                parts.Add($"{DescribeSpreadAlignment(alignPts, def, cosmic)} this age");

            if (cardEffectActive || isActionCard)
            {
                string effectLine = FirstLine(def.EffectText);
                if (!string.IsNullOrEmpty(effectLine))
                    parts.Add(effectLine);
            }

            string description = string.Join(" · ", parts);
            if (!description.EndsWith('.'))
                description += ".";

            if (crucibleSlotLabel != null)
                description += $" Contributes to {crucibleSlotLabel}.";

            return description;
        }

        static ActiveEffectPolarity ClassifyTextPolarity(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return ActiveEffectPolarity.Neutral;

            string lower = text.ToLowerInvariant();
            if (lower.Contains("must offer")
                || lower.Contains("reduced by")
                || lower.Contains("cost an additional")
                || lower.Contains("opponent gains")
                || lower.Contains("opponent chooses")
                || lower.Contains("pay an extra")
                || lower.Contains("best-of-3")
                || lower.StartsWith("-"))
            {
                return ActiveEffectPolarity.Debuff;
            }

            if (text.TrimStart().StartsWith('+'))
                return ActiveEffectPolarity.Buff;

            return ActiveEffectPolarity.Buff;
        }

        static bool TryParseCosmicElementCondition(string text, out Element element)
        {
            element = Element.None;
            const string marker = "Cosmic Age's Element is ";
            int idx = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return false;

            string tail = text[(idx + marker.Length)..].Trim().TrimEnd('.');
            return Enum.TryParse(tail, ignoreCase: true, out element);
        }

        static bool TryParseHarvestElementCondition(string text, out Element element)
        {
            element = Element.None;
            const string marker = "Astral Houses on ";
            int idx = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return false;

            string tail = text[(idx + marker.Length)..];
            int end = tail.IndexOf(' ');
            if (end < 0)
                return false;

            return Enum.TryParse(tail[..end], ignoreCase: true, out element);
        }

        static string DescribeSpreadAlignment(int points, CardDefinition def, ZodiacSign cosmic)
        {
            if (points >= 3)
                return $"{cosmic} alignment +3";
            if (points >= 2)
                return $"{def.Planet} alignment +2";
            if (points >= 1)
                return $"{Correspondence.ElementFor(def.Suit)} alignment +1";
            return "no alignment";
        }

        static string FirstLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
            var idx = text.IndexOf('\n');
            return idx >= 0 ? text[..idx].Trim() : text.Trim();
        }
    }
}
