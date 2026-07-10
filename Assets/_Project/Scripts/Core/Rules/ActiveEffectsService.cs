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

        public static ActiveEffectsSnapshot Build(GameSession session, int playerId)
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
                BuildSpreadCardsSection(session, player, cosmic)
            };

            return new ActiveEffectsSnapshot(subtitle, cosmicAge, sections);
        }

        /// <summary>Effects that may influence an in-flight duel (both players, staked cards, Justice age).</summary>
        public static ActiveEffectsSnapshot BuildDuelRelevant(
            GameSession session,
            int defenderId,
            int attackerId,
            string? targetCardId,
            string? anteCardId)
        {
            var cosmic = session.Board.CosmicAgeSign;
            var planet = Correspondence.PlanetFor(cosmic);
            var element = Correspondence.ElementFor(cosmic);

            string subtitle = cosmic == ZodiacSign.None
                ? "Cosmic age not yet cast"
                : $"Age of {cosmic} · {planet} · {element}";

            var cosmicAge = session.Board.ContestEffects.DuelBestOfThree
                || session.Board.ContestEffects.GambitBestOfThree
                ? BuildCosmicAgeFeatured(session, cosmic, planet, element)
                : EmptyCosmicAgeItem();

            var sections = new List<ActiveEffectSection>();

            if (defenderId >= 0 && defenderId < session.Players.Count)
            {
                var defender = session.Players[defenderId];
                AddFilteredSection(sections, FilterDuelSection(
                    BuildAstralHousesSection(session, defender, cosmic)));
                AddFilteredSection(sections, FilterDuelSection(
                    BuildAdeptsSection(session, defender)));
                AddFilteredSection(sections, FilterDuelSection(
                    BuildFatesSection(session, defender)));
                AddFilteredSection(sections, BuildDuelSpreadSection(
                    session, defender, cosmic, targetCardId, anteCardId,
                    "Your spread", targetCardId));
            }

            if (attackerId >= 0 && attackerId < session.Players.Count && attackerId != defenderId)
            {
                var attacker = session.Players[attackerId];
                AddFilteredSection(sections, FilterDuelSection(
                    BuildAstralHousesSection(session, attacker, cosmic)));
                AddFilteredSection(sections, FilterDuelSection(
                    BuildAdeptsSection(session, attacker)));
                AddFilteredSection(sections, FilterDuelSection(
                    BuildFatesSection(session, attacker)));
                AddFilteredSection(sections, BuildDuelSpreadSection(
                    session, attacker, cosmic, targetCardId, anteCardId,
                    $"{PlayerLabel(session, attackerId)} spread", anteCardId));
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

        static ActiveEffectSection? FilterDuelSection(ActiveEffectSection section)
        {
            var items = section.Items.Where(item =>
                PertainsToDuel(item.Description) || PertainsToDuel(item.Title)).ToList();
            if (items.Count == 0)
                return null;

            return new ActiveEffectSection(
                section.SectionId,
                section.Title,
                items.Count.ToString(),
                items,
                section.FooterNote);
        }

        static ActiveEffectSection? BuildDuelSpreadSection(
            GameSession session,
            PlayerState player,
            ZodiacSign cosmic,
            string? targetCardId,
            string? anteCardId,
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
                if (!IsDuelRelevantSpreadCard(id, def, targetCardId, anteCardId, highlightCardId))
                    continue;

                int alignPts = AlignmentService.ScoreCard(def.Suit, def.Planet, cosmic);
                crucibleMatches.TryGetValue(id, out var slotLabel);
                var state = SpreadCardEffectEvaluator.Evaluate(player, def, cosmic, alignPts, slotLabel);

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

        static bool IsDuelRelevantSpreadCard(
            string id,
            CardDefinition def,
            string? targetCardId,
            string? anteCardId,
            string? highlightCardId)
        {
            if (id == targetCardId || id == anteCardId || id == highlightCardId)
                return true;

            return def.EffectType.Equals("Duel", StringComparison.OrdinalIgnoreCase)
                || def.EffectType.Equals("Gambit", StringComparison.OrdinalIgnoreCase);
        }

        static bool PertainsToDuel(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return text.Contains("duel", StringComparison.OrdinalIgnoreCase)
                || text.Contains("gambit", StringComparison.OrdinalIgnoreCase)
                || text.Contains("combat", StringComparison.OrdinalIgnoreCase);
        }

        static ActiveEffectItem BuildCosmicAgeFeatured(
            GameSession session,
            ZodiacSign cosmic,
            Planet planet,
            Element element)
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
                    bool arrested = player.ArrestedAdepts.Contains(id);
                    bool used = player.UsedAdeptInstanceIdsThisAge.Contains(id);
                    var badge = AdeptEffectCatalog.BadgeFor(def.ArcanaNumber, arrested, used);

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
            ZodiacSign cosmic)
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

            var spreadCards = new List<(string id, CardDefinition def)>();
            foreach (var id in player.Spread)
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

                var state = SpreadCardEffectEvaluator.Evaluate(player, def, cosmic, alignPts, slotLabel);
                if (state.IsInactive)
                {
                    inactiveCount++;
                    continue;
                }

                string title = def.IsMinorArcana
                    ? $"{def.Rank} of {def.Suit}"
                    : def.Name;

                activeItems.Add(new ActiveEffectItem(
                    id,
                    title,
                    state.Description,
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
