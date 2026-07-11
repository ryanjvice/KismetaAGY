using System;
using System.Collections.Generic;
using System.Linq;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>Builds a read-only snapshot of the local player's active effects for the Active Effects panel.</summary>
    public static class ActiveEffectsService
    {
        public static void MarkAdeptUsed(GameSession session, int playerId, string adeptInstanceId)
        {
            if (playerId < 0 || playerId >= session.Players.Count)
                return;
            session.Players[playerId].UsedAdeptInstanceIdsThisAge.Add(adeptInstanceId);
        }

        public static ActiveEffectsSnapshot Build(
            GameSession session,
            int playerId,
            IReadOnlyList<string>? spreadOverride = null)
        {
            var player = session.Players[playerId];
            var cosmic = session.Board.CosmicAgeSign;
            var planet = Correspondence.PlanetFor(cosmic);
            var element = Correspondence.ElementFor(cosmic);

            string subtitle = cosmic == ZodiacSign.None
                ? "Cosmic age not yet cast"
                : $"Age of {cosmic} · {planet} · {element}";

            var cosmicAge = BuildCosmicAgeFeatured(session, cosmic, planet, element);
            var sections = new List<ActiveEffectSection>
            {
                BuildAstralHousesSection(session, player, cosmic),
                BuildAdeptsSection(session, player),
                BuildFatesSection(session, player),
                BuildSpreadCardsSection(session, player, cosmic, spreadOverride)
            };

            return new ActiveEffectsSnapshot(subtitle, cosmicAge, sections);
        }

        /// <summary>Effects that may influence an in-flight duel (both players, staked cards, Justice age).</summary>
        public static ActiveEffectsSnapshot BuildDuelRelevant(
            GameSession session,
            int perspectivePlayerId,
            int attackerId,
            int defenderId,
            string? targetCardId,
            string? anteCardId) =>
            BuildContestRelevant(
                session, ContestKind.Duel, perspectivePlayerId, defenderId, attackerId,
                targetCardId, anteCardId, null);

        /// <summary>Effects that may influence an in-flight gambit (both players, offered card, Justice age).</summary>
        public static ActiveEffectsSnapshot BuildGambitRelevant(
            GameSession session,
            int perspectivePlayerId,
            int attackerId,
            int defenderId,
            string? offeredCardId) =>
            BuildContestRelevant(
                session, ContestKind.Gambit, perspectivePlayerId, defenderId, attackerId,
                null, null, offeredCardId);

        /// <summary>Effects that may influence an opposition (both players, alignment sources, besieged).</summary>
        public static ActiveEffectsSnapshot BuildOppositionRelevant(
            GameSession session,
            int challengerId,
            int defenderId)
        {
            var cosmic = session.Board.CosmicAgeSign;
            var planet = Correspondence.PlanetFor(cosmic);
            var element = Correspondence.ElementFor(cosmic);

            string subtitle = cosmic == ZodiacSign.None
                ? "Cosmic age not yet cast"
                : $"Age of {cosmic} · {planet} · {element}";

            var modifierParts = new List<string>();
            if (PlayerAspectAlignment.IsMagnusMisalignedChallenger(session, challengerId, defenderId))
                modifierParts.Add("Magnus: +1 alignment (misaligned challenger)");

            int besieged = defenderId >= 0 && defenderId < session.Players.Count
                ? session.Players[defenderId].BesiegedBonusCount
                : 0;
            if (besieged > 0)
                modifierParts.Add($"Besieged: +{besieged} defender alignment");

            if (modifierParts.Count > 0)
                subtitle += $" · {string.Join(" · ", modifierParts)}";

            var sections = new List<ActiveEffectSection>();

            if (challengerId >= 0 && challengerId < session.Players.Count)
            {
                var challenger = session.Players[challengerId];
                AddFilteredSection(sections, FilterOppositionSection(
                    BuildAdeptsSection(session, challenger)));
                AddFilteredSection(sections, BuildOppositionSpreadSection(
                    session, challenger, cosmic, "Your spread"));
            }

            if (defenderId >= 0 && defenderId < session.Players.Count && defenderId != challengerId)
            {
                var defender = session.Players[defenderId];
                AddFilteredSection(sections, FilterOppositionSection(
                    BuildAdeptsSection(session, defender)));
                AddFilteredSection(sections, BuildOppositionSpreadSection(
                    session, defender, cosmic, $"{PlayerLabel(session, defenderId)} spread"));
            }

            return new ActiveEffectsSnapshot(subtitle, EmptyCosmicAgeItem(), sections);
        }

        static ActiveEffectSection? FilterOppositionSection(ActiveEffectSection section)
        {
            var items = section.Items.Where(item =>
                PertainsToOpposition(item.Description) || PertainsToOpposition(item.Title)).ToList();
            if (items.Count == 0)
                return null;

            return new ActiveEffectSection(
                section.SectionId,
                section.Title,
                items.Count.ToString(),
                items,
                section.FooterNote);
        }

        static bool PertainsToOpposition(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return text.Contains("opposition", StringComparison.OrdinalIgnoreCase)
                || text.Contains("alignment", StringComparison.OrdinalIgnoreCase)
                || text.Contains("wild suit", StringComparison.OrdinalIgnoreCase)
                || text.Contains("besieged", StringComparison.OrdinalIgnoreCase)
                || text.Contains("hermit", StringComparison.OrdinalIgnoreCase)
                || text.Contains("hierophant", StringComparison.OrdinalIgnoreCase);
        }

        static ActiveEffectSection? BuildOppositionSpreadSection(
            GameSession session,
            PlayerState player,
            ZodiacSign cosmic,
            string title)
        {
            var db = session.Rules?.CardDatabase;
            if (db == null)
                return null;

            var items = new List<ActiveEffectItem>();
            foreach (var id in player.Spread)
            {
                var inst = session.GetCard(id);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def == null) continue;

                if (!def.EffectType.Equals("Opposition", StringComparison.OrdinalIgnoreCase)
                    && !def.EffectType.Equals("Harvest", StringComparison.OrdinalIgnoreCase))
                    continue;

                int alignPts = AlignmentService.ScoreCardForOpposition(def, cosmic);
                var state = SpreadCardEffectEvaluator.Evaluate(
                    session, player, id, def, cosmic, alignPts, null);
                if (state.IsInactive)
                    continue;

                string cardTitle = def.IsMinorArcana
                    ? $"{def.Rank} of {def.Suit}"
                    : def.Name;

                items.Add(new ActiveEffectItem(
                    id,
                    cardTitle,
                    state.Description,
                    state.Badge,
                    iconKey: SuitIconKey(def.Suit),
                    polarity: state.Polarity));
            }

            if (items.Count == 0)
                return null;

            items.Sort(CompareSpreadItems);
            return new ActiveEffectSection(
                $"opposition-spread-{player.PlayerId}",
                title,
                $"{items.Count} active",
                items);
        }

        static ActiveEffectsSnapshot BuildContestRelevant(
            GameSession session,
            ContestKind kind,
            int perspectivePlayerId,
            int defenderId,
            int attackerId,
            string? targetCardId,
            string? anteCardId,
            string? offeredCardId)
        {
            var cosmic = session.Board.CosmicAgeSign;
            var planet = Correspondence.PlanetFor(cosmic);
            var element = Correspondence.ElementFor(cosmic);

            string subtitle = cosmic == ZodiacSign.None
                ? "Cosmic age not yet cast"
                : $"Age of {cosmic} · {planet} · {element}";

            string modifierSummary = ContestModifierService.DescribeForPlayer(
                session, kind, perspectivePlayerId, attackerId, defenderId);
            if (!string.IsNullOrWhiteSpace(modifierSummary))
                subtitle += $" · {modifierSummary}";

            var modifiers = ContestModifierService.Build(session, kind, attackerId, defenderId);
            bool boardBestOfThree = session.Board.ContestEffects.IsBestOfThree(kind);
            bool scopedBestOfThree = kind == ContestKind.Duel && modifiers.AttackerForcesBestOfThree;
            var cosmicAge = boardBestOfThree || scopedBestOfThree
                ? BuildCosmicAgeFeatured(session, cosmic, planet, element, scopedBestOfThree)
                : EmptyCosmicAgeItem();

            var sections = new List<ActiveEffectSection>();

            if (defenderId >= 0 && defenderId < session.Players.Count)
            {
                var defender = session.Players[defenderId];
                AddFilteredSection(sections, FilterContestSection(kind,
                    BuildAstralHousesSection(session, defender, cosmic)));
                AddFilteredSection(sections, FilterContestSection(kind,
                    BuildAdeptsSection(session, defender)));
                AddFilteredSection(sections, FilterContestSection(kind,
                    BuildFatesSection(session, defender)));
                AddFilteredSection(sections, BuildContestSpreadSection(
                    session, kind, defender, cosmic, targetCardId, anteCardId, offeredCardId,
                    "Your spread", targetCardId ?? offeredCardId));
            }

            if (attackerId >= 0 && attackerId < session.Players.Count && attackerId != defenderId)
            {
                var attacker = session.Players[attackerId];
                AddFilteredSection(sections, FilterContestSection(kind,
                    BuildAstralHousesSection(session, attacker, cosmic)));
                AddFilteredSection(sections, FilterContestSection(kind,
                    BuildAdeptsSection(session, attacker)));
                AddFilteredSection(sections, FilterContestSection(kind,
                    BuildFatesSection(session, attacker)));
                AddFilteredSection(sections, BuildContestSpreadSection(
                    session, kind, attacker, cosmic, targetCardId, anteCardId, offeredCardId,
                    $"{PlayerLabel(session, attackerId)} spread", offeredCardId));
            }

            return new ActiveEffectsSnapshot(subtitle, cosmicAge, sections);
        }

        static ActiveEffectItem EmptyCosmicAgeItem() => new(
            "cosmic-age",
            string.Empty,
            string.Empty,
            new ActiveEffectBadge(string.Empty, ActiveEffectBadgeTone.Neutral));

        static void AddFilteredSection(List<ActiveEffectSection> sections, ActiveEffectSection? section)
        {
            if (section == null || section.Value.Items.Count == 0)
                return;
            sections.Add(section.Value);
        }

        static ActiveEffectSection? FilterContestSection(ContestKind kind, ActiveEffectSection section)
        {
            var items = section.Items.Where(item =>
                PertainsToContest(kind, item.Description) || PertainsToContest(kind, item.Title)).ToList();
            if (items.Count == 0)
                return null;

            return new ActiveEffectSection(
                section.SectionId,
                section.Title,
                items.Count.ToString(),
                items,
                section.FooterNote);
        }

        static ActiveEffectSection? FilterDuelSection(ActiveEffectSection section) =>
            FilterContestSection(ContestKind.Duel, section);

        static ActiveEffectSection? BuildContestSpreadSection(
            GameSession session,
            ContestKind kind,
            PlayerState player,
            ZodiacSign cosmic,
            string? targetCardId,
            string? anteCardId,
            string? offeredCardId,
            string title,
            string? highlightCardId)
        {
            var db = session.Rules?.CardDatabase;
            if (db == null)
                return null;

            var codexDb = session.Rules?.CodexDatabase;
            var spreadCards = new List<(string id, CardDefinition def)>();
            foreach (var id in player.Spread)
            {
                var inst = session.GetCard(id);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def != null)
                    spreadCards.Add((id, def));
            }

            var crucibleMatches = BuildCrucibleContributions(session, player, spreadCards, codexDb);
            var items = new List<ActiveEffectItem>();

            foreach (var (id, def) in spreadCards)
            {
                if (!IsContestRelevantSpreadCard(session, player, kind, id, def, targetCardId, anteCardId, offeredCardId, highlightCardId))
                    continue;

                int alignPts = AlignmentService.ScoreCard(def.Suit, def.Planet, cosmic);
                crucibleMatches.TryGetValue(id, out var slotLabel);
                var state = SpreadCardEffectEvaluator.Evaluate(
                    session, player, id, def, cosmic, alignPts, slotLabel);

                string cardTitle = def.IsMinorArcana
                    ? $"{def.Rank} of {def.Suit}"
                    : def.Name;

                if (state.IsInactive && id != targetCardId && id != anteCardId && id != highlightCardId)
                    continue;

                string description = state.IsInactive
                    ? FirstLine(def.EffectText)
                    : state.Description;

                var badge = state.IsInactive
                    ? new ActiveEffectBadge("staked card", ActiveEffectBadgeTone.Pending)
                    : state.Badge;

                items.Add(new ActiveEffectItem(
                    id,
                    cardTitle,
                    description,
                    badge,
                    iconKey: SuitIconKey(def.Suit),
                    polarity: state.IsInactive ? ActiveEffectPolarity.Neutral : state.Polarity));
            }

            items.Sort(CompareSpreadItems);
            if (items.Count == 0)
                return null;

            return new ActiveEffectSection(
                $"spread-cards-{player.PlayerId}",
                title,
                $"{items.Count} active",
                items);
        }

        static bool IsContestRelevantSpreadCard(
            GameSession session,
            PlayerState player,
            ContestKind kind,
            string id,
            CardDefinition def,
            string? targetCardId,
            string? anteCardId,
            string? offeredCardId,
            string? highlightCardId)
        {
            if (id == targetCardId || id == anteCardId || id == offeredCardId || id == highlightCardId)
                return true;

            if (def.EffectType.Equals("Reversed", StringComparison.OrdinalIgnoreCase))
            {
                if (kind != ContestKind.Duel && kind != ContestKind.Gambit)
                    return false;
                return ReversedCurseService.IsCurseActive(session, player, id, def);
            }

            if (kind == ContestKind.Duel
                && def.EffectType.Equals("Duel", StringComparison.OrdinalIgnoreCase))
                return true;

            if (kind == ContestKind.Gambit
                && def.EffectType.Equals("Gambit", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        static bool PertainsToContest(ContestKind kind, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            if (text.Contains("duel", StringComparison.OrdinalIgnoreCase)
                || text.Contains("gambit", StringComparison.OrdinalIgnoreCase)
                || text.Contains("combat", StringComparison.OrdinalIgnoreCase)
                || text.Contains("dice", StringComparison.OrdinalIgnoreCase)
                || text.Contains("reroll", StringComparison.OrdinalIgnoreCase)
                || text.Contains("best-of", StringComparison.OrdinalIgnoreCase))
                return true;

            return kind == ContestKind.Gambit
                && text.Contains("opposition", StringComparison.OrdinalIgnoreCase);
        }

        static bool PertainsToDuel(string text) => PertainsToContest(ContestKind.Duel, text);

        static ActiveEffectItem BuildCosmicAgeFeatured(
            GameSession session,
            ZodiacSign cosmic,
            Planet planet,
            Element element,
            bool scopedDuelBestOfThree = false)
        {
            var (name, desc) = CosmicEffectDescriber.DescribeCosmicAge(cosmic);
            int keeperId = FindAgekeeperId(session);
            string keeperName = PlayerLabel(session, keeperId);

            string body = cosmic == ZodiacSign.None
                ? "The age has not been cast yet."
                : $"{name} — {desc} while {cosmic} reigns.";

            if (session.Board.ContestEffects.DuelBestOfThree
                || session.Board.ContestEffects.GambitBestOfThree)
                body += " Duels and Gambits resolve as best-of-three this age (Justice).";
            else if (scopedDuelBestOfThree)
                body += " This duel resolves as best-of-three (6 of Swords).";

            string footer = cosmic == ZodiacSign.None
                ? string.Empty
                : $"{planet} · {element} · rolled by {keeperName} (Agekeeper)";

            return new ActiveEffectItem(
                "cosmic-age",
                name,
                body,
                new ActiveEffectBadge("expires at age end", ActiveEffectBadgeTone.Active),
                iconKey: "cosmic-age",
                footer: footer);
        }

        static ActiveEffectSection BuildAstralHousesSection(
            GameSession session,
            PlayerState player,
            ZodiacSign cosmic)
        {
            var items = new List<ActiveEffectItem>();
            foreach (var houseSign in player.AstralHouses.OrderBy(s => (int)s))
            {
                string alignment = CosmicEffectDescriber.DescribeAlignmentContribution(houseSign, cosmic);
                string personal = cosmic != houseSign
                    ? CosmicEffectDescriber.DescribePersonalEffect(houseSign)
                    : string.Empty;

                string description = alignment;
                if (!string.IsNullOrEmpty(personal))
                    description += " " + personal;

                items.Add(new ActiveEffectItem(
                    $"house-{houseSign}",
                    $"House on {houseSign}",
                    description.Trim(),
                    new ActiveEffectBadge("permanent", ActiveEffectBadgeTone.Permanent),
                    iconKey: "house"));
            }

            return new ActiveEffectSection(
                "astral-houses",
                "Astral houses",
                items.Count.ToString(),
                items);
        }

        static ActiveEffectSection BuildAdeptsSection(GameSession session, PlayerState player)
        {
            var db = session.Rules?.CardDatabase;
            var items = new List<ActiveEffectItem>();
            int limit = ArcanumLimitFor(session, player);

            if (db != null)
            {
                foreach (var id in player.Arcanum)
                {
                    var inst = session.GetCard(id);
                    var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                    if (def?.MajorArcanaType != MajorArcanaType.Adept)
                        continue;

                    string description = FirstLine(def.EffectText);
                    bool attuned = AdeptAttunement.IsResonant(player, def);
                    if (attuned && !string.IsNullOrWhiteSpace(def.EffectTextResonant))
                        description += " Resonant: " + FirstLine(def.EffectTextResonant);

                    if (def.ArcanaNumber == 2 && attuned)
                    {
                        int handLimit = PlayerLimitService.GetHandLimit(session, player);
                        description += $" Hand limit: {handLimit}.";
                    }
                    if (def.ArcanaNumber == 3 && player.EmpressMarkedReagents.Count > 0)
                    {
                        var marked = string.Join(", ", player.EmpressMarkedReagents);
                        description += $" Marked this age: {marked}.";
                    }

                    if (def.ArcanaNumber == 14 && player.TemperanceSaltWildReagent.HasValue)
                    {
                        description += $" Salt wild for {player.TemperanceSaltWildReagent.Value}.";
                    }

                    if (def.ArcanaNumber == 4 && player.EmperorProtectedCardIds.Count > 0)
                    {
                        description += $" Protected: {string.Join(", ", player.EmperorProtectedCardIds)}.";
                    }

                    if (def.ArcanaNumber == 2 && session.Board.PendingPriestessReturns.Contains(player.PlayerId))
                    {
                        description += " Priestess harvest: return 2 cards.";
                    }

                    if (def.ArcanaNumber == 5 && player.HierophantOppositionShift != 0)
                    {
                        description += $" Opposition shift: {player.HierophantOppositionShift:+0;-0}.";
                    }

                    if (def.ArcanaNumber == 1 && attuned)
                        description += " Reversed immunity active in your Spread.";

                    if (def.ArcanaNumber == 21 && attuned)
                    {
                        if (player.WorldWildcardFlipped)
                            description += " Crucible wildcard flipped for next Fire.";
                        else if (player.UsedAdeptInstanceIdsThisAge.Contains(id))
                            description += " Refresh with 1 Salt to flip again.";
                    }

                    if (CardEffectSuppressionService.IsSuppressed(session, id))
                        description += " Effects nullified by Star.";

                    bool arrested = player.ArrestedAdepts.Contains(id);
                    bool used = player.UsedAdeptInstanceIdsThisAge.Contains(id);
                    var badge = AdeptEffectCatalog.BadgeFor(def.ArcanaNumber, arrested, used, attuned);

                    items.Add(new ActiveEffectItem(
                        id,
                        def.Name,
                        description,
                        badge,
                        iconKey: $"adept-{def.ArcanaNumber}"));
                }
            }

            return new ActiveEffectSection(
                "adepts",
                "Adepts",
                $"{items.Count} / {limit}",
                items);
        }

        static ActiveEffectSection BuildFatesSection(GameSession session, PlayerState player)
        {
            var db = session.Rules?.CardDatabase;
            var items = new List<ActiveEffectItem>();
            var pendingFateIds = new HashSet<string>(
                session.Board.PendingFateDecisions
                    .Where(p => p.PlayerId == player.PlayerId)
                    .Select(p => p.FateCardId));

            if (db != null)
            {
                foreach (var id in player.Arcanum)
                {
                    var inst = session.GetCard(id);
                    var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                    if (def?.MajorArcanaType != MajorArcanaType.Fate)
                        continue;

                    bool pending = pendingFateIds.Contains(id);
                    string description = !string.IsNullOrWhiteSpace(inst?.FateResolutionNote)
                        ? inst.FateResolutionNote!
                        : FirstLine(def.EffectText);

                    ActiveEffectBadge badge = pending
                        ? new ActiveEffectBadge("pending decision", ActiveEffectBadgeTone.Pending)
                        : new ActiveEffectBadge("resolved · face-up", ActiveEffectBadgeTone.Resolved);

                    items.Add(new ActiveEffectItem(
                        id,
                        def.Name,
                        description,
                        badge,
                        iconKey: $"fate-{def.ArcanaNumber}"));
                }
            }

            return new ActiveEffectSection(
                "fates",
                "Fates",
                items.Count.ToString(),
                items);
        }

        static ActiveEffectSection BuildSpreadCardsSection(
            GameSession session,
            PlayerState player,
            ZodiacSign cosmic,
            IReadOnlyList<string>? spreadOverride = null)
        {
            var db = session.Rules?.CardDatabase;
            var codexDb = session.Rules?.CodexDatabase;
            var activeItems = new List<ActiveEffectItem>();
            int inactiveCount = 0;

            if (db == null)
            {
                return new ActiveEffectSection(
                    "spread-cards",
                    "Spread cards",
                    "0 active",
                    activeItems,
                    null);
            }

            var spreadIds = spreadOverride ?? player.Spread;
            var spreadCards = new List<(string id, CardDefinition def)>();
            foreach (var id in spreadIds)
            {
                var inst = session.GetCard(id);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def != null)
                    spreadCards.Add((id, def));
            }

            var crucibleMatches = BuildCrucibleContributions(session, player, spreadCards, codexDb);

            foreach (var (id, def) in spreadCards)
            {
                int alignPts = AlignmentService.ScoreCard(def.Suit, def.Planet, cosmic);
                crucibleMatches.TryGetValue(id, out var slotLabel);

                var state = SpreadCardEffectEvaluator.Evaluate(
                    session, player, id, def, cosmic, alignPts, slotLabel);
                if (state.IsInactive)
                {
                    inactiveCount++;
                    continue;
                }

                string title = def.IsMinorArcana
                    ? $"{def.Rank} of {def.Suit}"
                    : def.Name;

                string description = state.Description;
                if (SpreadLimitEffectCatalog.IsQueenV2Passive(def))
                {
                    if (SpreadLimitEffectCatalog.GrantsHandBonus(def))
                        description += $" Hand limit: {PlayerLimitService.GetHandLimit(session, player)}.";
                    else if (SpreadLimitEffectCatalog.GrantsSpreadBonus(def))
                        description += $" Spread limit: {PlayerLimitService.GetSpreadLimit(session, player)}.";
                }
                if (def.EffectType.Equals("Craft", StringComparison.OrdinalIgnoreCase)
                    && SpreadCraftEffectCatalog.IsRank8CraftDiscount(def, Correspondence.ReagentFor(def.Suit)))
                {
                    int minCost = CraftModifierService.GetMinimumCost(session, player.PlayerId,
                        Correspondence.ReagentFor(def.Suit));
                    description += $" Crafts {Correspondence.ReagentFor(def.Suit)} at {minCost} cards.";
                }

                activeItems.Add(new ActiveEffectItem(
                    id,
                    title,
                    description,
                    state.Badge,
                    iconKey: SuitIconKey(def.Suit),
                    polarity: state.Polarity));
            }

            activeItems.Sort(CompareSpreadItems);

            string? footer = inactiveCount > 0
                ? $"{inactiveCount} other spread card{(inactiveCount == 1 ? "" : "s")} have no active effect this age."
                : null;

            return new ActiveEffectSection(
                "spread-cards",
                "Spread cards",
                $"{activeItems.Count} active",
                activeItems,
                footer);
        }

        static int CompareSpreadItems(ActiveEffectItem a, ActiveEffectItem b)
        {
            int polarity = PolaritySortKey(a.Polarity).CompareTo(PolaritySortKey(b.Polarity));
            if (polarity != 0)
                return polarity;

            return string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase);
        }

        static int PolaritySortKey(ActiveEffectPolarity polarity) => polarity switch
        {
            ActiveEffectPolarity.Buff => 0,
            ActiveEffectPolarity.Neutral => 1,
            _ => 2
        };

        static Dictionary<string, string> BuildCrucibleContributions(
            GameSession session,
            PlayerState player,
            IReadOnlyList<(string id, CardDefinition def)> spreadCards,
            ICrucibleCodexDatabase? codexDb)
        {
            var result = new Dictionary<string, string>();
            if (codexDb == null || player.AssignedCodex == CodexVariant.None)
                return result;

            for (int i = 0; i < player.CrucibleSlots.Count; i++)
            {
                var slot = player.CrucibleSlots[i];
                if (slot.State != CrucibleCardState.Dormant)
                    continue;

                var formula = codexDb.GetFormula(player.AssignedCodex, i);
                if (formula == null)
                    continue;

                var suggested = ActivationCardSuggester.SuggestActivationCards(spreadCards, formula, session.Rules!.CardDatabase);
                if (suggested == null)
                    continue;

                string slotLabel = $"{ReagentLabel(formula)} formula (slot {SlotLetter(i)})";
                foreach (var cardId in suggested)
                {
                    if (!result.ContainsKey(cardId))
                        result[cardId] = slotLabel;
                }
            }

            return result;
        }

        static string ReagentLabel(CodexFormulaDefinition formula)
        {
            return formula.CauldronSuit switch
            {
                Suit.Wands => "Sulphur",
                Suit.Cups => "Aqua Regia",
                Suit.Pentacles => "Vitriol",
                Suit.Swords => "Quicksilver",
                _ => formula.DisplayName
            };
        }

        static string SlotLetter(int slotIndex) => slotIndex switch
        {
            0 => "A",
            1 => "B",
            2 => "C",
            3 => "D",
            _ => (slotIndex + 1).ToString()
        };

        static int ArcanumLimitFor(GameSession session, PlayerState player)
        {
            var db = session.Rules?.CardDatabase;
            if (db == null) return 2;

            foreach (var id in player.Arcanum)
            {
                var inst = session.GetCard(id);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def?.ArcanaNumber == 9)
                    return 3;
            }

            return 2;
        }

        static int FindAgekeeperId(GameSession session)
        {
            foreach (var p in session.Players)
            {
                if (p.IsAgekeeper)
                    return p.PlayerId;
            }

            return 0;
        }

        static string PlayerLabel(GameSession session, int playerId)
        {
            if (playerId < 0 || playerId >= session.Players.Count)
                return "Agekeeper";
            return $"{session.Players[playerId].Color} alchemist";
        }

        static string FirstLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
            var idx = text.IndexOf('\n');
            return idx >= 0 ? text[..idx].Trim() : text.Trim();
        }

        static string SuitIconKey(Suit suit) => suit switch
        {
            Suit.Wands => "wands",
            Suit.Cups => "cups",
            Suit.Pentacles => "pentacles",
            Suit.Swords => "swords",
            _ => "minor"
        };
    }
}
