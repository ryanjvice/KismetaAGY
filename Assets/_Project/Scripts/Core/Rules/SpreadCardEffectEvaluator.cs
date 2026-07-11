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
            GameSession session,
            PlayerState player,
            string cardInstanceId,
            CardDefinition def,
            ZodiacSign cosmic,
            int alignPts,
            string? crucibleSlotLabel)
        {
            return EvaluateInternal(
                player, def, cosmic, alignPts, crucibleSlotLabel,
                curseActive: IsReversedCurseActive(session, player, cardInstanceId, def));
        }

        public static SpreadCardEffectState Evaluate(
            PlayerState player,
            CardDefinition def,
            ZodiacSign cosmic,
            int alignPts,
            string? crucibleSlotLabel)
        {
            return EvaluateInternal(player, def, cosmic, alignPts, crucibleSlotLabel, curseActive: null);
        }

        static SpreadCardEffectState EvaluateInternal(
            PlayerState player,
            CardDefinition def,
            ZodiacSign cosmic,
            int alignPts,
            string? crucibleSlotLabel,
            bool? curseActive)
        {
            bool hasAlignment = alignPts > 0;
            bool hasCrucible = crucibleSlotLabel != null;
            var effectKind = ClassifyEffect(def);
            bool isReversed = effectKind == SpreadCardEffectKind.Reversed;
            bool cardEffectActive = IsCardEffectActive(player, def, cosmic, effectKind, curseActive);
            bool isActionCard = effectKind == SpreadCardEffectKind.Action;

            bool isListed = hasAlignment || hasCrucible || cardEffectActive || isActionCard || isReversed;
            if (!isListed)
            {
                return new SpreadCardEffectState(
                    false,
                    ActiveEffectPolarity.Neutral,
                    new ActiveEffectBadge(string.Empty, ActiveEffectBadgeTone.Neutral),
                    string.Empty);
            }

            var polarity = ResolvePolarity(def, effectKind, cardEffectActive, hasAlignment, hasCrucible);
            var badge = ResolveBadge(def, effectKind, cardEffectActive, hasAlignment, hasCrucible, polarity, isReversed);
            var description = BuildDescription(
                def,
                cosmic,
                alignPts,
                crucibleSlotLabel,
                effectKind,
                cardEffectActive,
                isActionCard,
                isReversed);

            return new SpreadCardEffectState(true, polarity, badge, description);
        }

        static bool IsReversedCurseActive(
            GameSession session,
            PlayerState player,
            string cardInstanceId,
            CardDefinition def)
            => ReversedCurseService.IsCurseActive(session, player, cardInstanceId, def);

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
            SpreadCardEffectKind kind,
            bool? curseActive)
        {
            return kind switch
            {
                SpreadCardEffectKind.Reversed => curseActive ?? true,
                SpreadCardEffectKind.Combat => true,
                SpreadCardEffectKind.Passive => IsPassiveActive(player, def, cosmic),
                SpreadCardEffectKind.SpreadPassive => IsSpreadPassiveActive(player, def),
                SpreadCardEffectKind.Forge => true,
                SpreadCardEffectKind.Wildcard => true,
                SpreadCardEffectKind.Action => IsActionActive(player, def),
                _ => false
            };
        }

        static bool IsPassiveActive(PlayerState player, CardDefinition def, ZodiacSign cosmic)
            => SpreadEffectPredicates.IsPassiveCosmicElementActive(def, cosmic)
               || (!SpreadEffectPredicates.TryParseCosmicElementCondition(def.EffectText, out _)
                   && def.EffectType.Equals("Passive", StringComparison.OrdinalIgnoreCase));

        static bool IsSpreadPassiveActive(PlayerState player, CardDefinition def)
            => SpreadEffectPredicates.IsSocialSpreadActive(def)
               || SpreadEffectPredicates.IsHarvestHouseElementActive(player, def);

        static bool IsActionActive(PlayerState player, CardDefinition def)
        {
            if (SpreadHouseEffectCatalog.IsEntryFeeAce(def))
                return SpreadEffectPredicates.IsEntryFeeActive(player, def);
            return true;
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

            if (kind == SpreadCardEffectKind.Reversed && !cardEffectActive)
                return ActiveEffectPolarity.Neutral;

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
            ActiveEffectPolarity polarity,
            bool isReversed)
        {
            if (kind == SpreadCardEffectKind.Reversed && cardEffectActive)
                return new ActiveEffectBadge("reversed · active", ActiveEffectBadgeTone.Debuff);

            if (kind == SpreadCardEffectKind.Reversed && isReversed && !cardEffectActive)
                return new ActiveEffectBadge("reversed · negated", ActiveEffectBadgeTone.Neutral);

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
            {
                if (SpreadHouseEffectCatalog.IsEntryFeeAce(def))
                {
                    return cardEffectActive
                        ? new ActiveEffectBadge("action · ready", ActiveEffectBadgeTone.Buff)
                        : new ActiveEffectBadge("action · unavailable", ActiveEffectBadgeTone.Neutral);
                }

                return new ActiveEffectBadge("action · available", ActiveEffectBadgeTone.Neutral);
            }

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
            bool isActionCard,
            bool isReversed)
        {
            var parts = new System.Collections.Generic.List<string> { "In spread" };

            if (alignPts > 0)
                parts.Add($"{DescribeSpreadAlignment(alignPts, def, cosmic)} this age");

            if (cardEffectActive || isActionCard || (isReversed && !cardEffectActive))
            {
                string effectLine = FirstLine(def.EffectText);
                if (!string.IsNullOrEmpty(effectLine))
                    parts.Add(effectLine);
            }

            string description = string.Join(" · ", parts);
            if (!description.EndsWith('.'))
                description += ".";

            if (isReversed && !cardEffectActive)
                description += " Aligned — curse suppressed.";

            if (crucibleSlotLabel != null)
                description += $" Contributes to {crucibleSlotLabel}.";

            if (kind == SpreadCardEffectKind.Wildcard
                && !string.IsNullOrWhiteSpace(def.WildcardArcanaMajorName))
            {
                description += $" Links to {def.WildcardArcanaMajorName}.";
            }

            if (kind == SpreadCardEffectKind.Passive && DuelProtectionService.IsKingV2Passive(def))
                description += $" Protecting {def.Suit} in spread (this card vulnerable).";

            if (SpreadForgeEffectCatalog.IsQueenV1WildReagent(def))
                description += $" {SpreadForgeEffectCatalog.WildReagentForQueen(def)} wild for Fire reagent cost.";

            if (SpreadForgeEffectCatalog.IsRank7ForgeReagent(def))
                description += $" +1 {SpreadForgeEffectCatalog.ReagentForRank7(def)} when you Fire.";

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
                || lower.Contains("costs +1")
                || lower.Contains("opponent gains")
                || lower.Contains("opponent chooses")
                || lower.Contains("pay an extra")
                || lower.Contains("best-of-3")
                || lower.Contains("draws 2")
                || lower.StartsWith("-"))
            {
                return ActiveEffectPolarity.Debuff;
            }

            if (text.TrimStart().StartsWith('+'))
                return ActiveEffectPolarity.Buff;

            return ActiveEffectPolarity.Buff;
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
