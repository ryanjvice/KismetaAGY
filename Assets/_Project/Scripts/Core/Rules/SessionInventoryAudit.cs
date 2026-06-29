using System;
using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    public enum InventoryLocationKind
    {
        CommonDeck,
        CommonDiscard,
        CrucibleDeck,
        CrucibleDiscard,
        Spread,
        Hand,
        Arcanum,
        FatefulWager,
        CrucibleSlot,
        PendingAdept,
    }

    public sealed class InventoryViolation
    {
        public string Code { get; }
        public string Message { get; }
        public string? CardId { get; }

        public InventoryViolation(string code, string message, string? cardId = null)
        {
            Code = code;
            Message = message;
            CardId = cardId;
        }

        public override string ToString() =>
            CardId == null ? $"[{Code}] {Message}" : $"[{Code}] {Message} (card={CardId})";
    }

    public sealed class SessionInventoryAuditResult
    {
        public IReadOnlyList<InventoryViolation> Violations { get; }
        public string? Context { get; }
        public bool IsConsistent => Violations.Count == 0;

        public SessionInventoryAuditResult(IReadOnlyList<InventoryViolation> violations, string? context)
        {
            Violations = violations;
            Context = context;
        }
    }

    /// <summary>
    /// Validates that card instances, zone lists, reagents, and limbo buffers stay in sync.
    /// </summary>
    public static class SessionInventoryAudit
    {
        public static bool Enabled { get; set; } =
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            true;
#else
            false;
#endif

        public static event Action<SessionInventoryAuditResult>? OnViolation;

        public static SessionInventoryAuditResult Audit(GameSession session, string? context = null)
        {
            var violations = new List<InventoryViolation>();
            AuditCards(session, violations);
            AuditReagents(session, violations);
            AuditLimbo(session, violations);
            var result = new SessionInventoryAuditResult(violations, context);
            if (!result.IsConsistent)
                OnViolation?.Invoke(result);
            return result;
        }

        public static void RunIfEnabled(GameSession session, string? context = null)
        {
            if (!Enabled) return;
            Audit(session, context);
        }

        static void AuditCards(GameSession session, List<InventoryViolation> violations)
        {
            var locations = new Dictionary<string, List<(InventoryLocationKind Kind, int OwnerId)>>();

            void Record(string cardId, InventoryLocationKind kind, int ownerId = -1)
            {
                if (!locations.TryGetValue(cardId, out var list))
                {
                    list = new List<(InventoryLocationKind, int)>();
                    locations[cardId] = list;
                }
                list.Add((kind, ownerId));
            }

            foreach (var id in session.Board.CommonDeck)
                Record(id, InventoryLocationKind.CommonDeck);

            foreach (var id in session.Board.CommonDiscard)
                Record(id, InventoryLocationKind.CommonDiscard);

            foreach (var id in session.Board.CrucibleDeck)
                Record(id, InventoryLocationKind.CrucibleDeck);

            var crucibleSlotIds = new HashSet<string>();
            foreach (var player in session.Players)
            {
                foreach (var slot in player.CrucibleSlots)
                    crucibleSlotIds.Add(slot.CardInstanceId);
            }

            foreach (var id in session.Board.CrucibleDiscard)
            {
                if (crucibleSlotIds.Contains(id))
                    continue;
                Record(id, InventoryLocationKind.CrucibleDiscard);
            }

            foreach (var (_, adeptId) in session.Board.PendingAdeptDecisions)
                Record(adeptId, InventoryLocationKind.PendingAdept);

            foreach (var player in session.Players)
            {
                foreach (var id in player.Spread)
                    Record(id, InventoryLocationKind.Spread, player.PlayerId);
                foreach (var id in player.Hand)
                    Record(id, InventoryLocationKind.Hand, player.PlayerId);
                foreach (var id in player.Arcanum)
                    Record(id, InventoryLocationKind.Arcanum, player.PlayerId);
                foreach (var id in player.FatefulWagerCards)
                    Record(id, InventoryLocationKind.FatefulWager, player.PlayerId);
                foreach (var slot in player.CrucibleSlots)
                    Record(slot.CardInstanceId, InventoryLocationKind.CrucibleSlot, player.PlayerId);
            }

            foreach (var (cardId, entries) in locations)
            {
                if (entries.Count > 1)
                {
                    violations.Add(new InventoryViolation(
                        "duplicate_location",
                        $"Card appears in {entries.Count} locations: {FormatLocations(entries)}.",
                        cardId));
                }

                var inst = session.GetCard(cardId);
                if (inst == null)
                {
                    violations.Add(new InventoryViolation(
                        "missing_registry",
                        "Card id is tracked in a zone list but missing from the session registry.",
                        cardId));
                    continue;
                }

                if (entries.Count == 0)
                    continue;

                var (kind, ownerId) = entries[0];
                if (!ZoneMatches(kind, ownerId, inst.Zone, inst.OwnerId))
                {
                    violations.Add(new InventoryViolation(
                        "zone_mismatch",
                        $"Registry has Zone={inst.Zone}, OwnerId={inst.OwnerId} but is stored as {kind} (owner {ownerId}).",
                        cardId));
                }
            }

            foreach (var (cardId, inst) in session.Cards)
            {
                if (!locations.ContainsKey(cardId))
                {
                    violations.Add(new InventoryViolation(
                        "orphan_registry",
                        $"Registry entry Zone={inst.Zone}, OwnerId={inst.OwnerId} is not in any canonical location.",
                        cardId));
                }
            }

            foreach (var player in session.Players)
            {
                var crucibleIds = new HashSet<string>();
                foreach (var slot in player.CrucibleSlots)
                    crucibleIds.Add(slot.CardInstanceId);

                foreach (var id in player.Spread)
                {
                    if (crucibleIds.Contains(id))
                        violations.Add(new InventoryViolation("crucible_overlap", "Crucible slot card also listed in Spread.", id));
                    if (player.FatefulWagerCards.Contains(id))
                        violations.Add(new InventoryViolation("wager_overlap", "Wager card also listed in Spread.", id));
                }

                foreach (var id in player.Hand)
                {
                    if (crucibleIds.Contains(id))
                        violations.Add(new InventoryViolation("crucible_overlap", "Crucible slot card also listed in Hand.", id));
                    if (player.FatefulWagerCards.Contains(id))
                        violations.Add(new InventoryViolation("wager_overlap", "Wager card also listed in Hand.", id));
                }

                foreach (var id in player.Arcanum)
                {
                    if (crucibleIds.Contains(id))
                        violations.Add(new InventoryViolation("crucible_overlap", "Crucible slot card also listed in Arcanum.", id));
                }
            }
        }

        static void AuditReagents(GameSession session, List<InventoryViolation> violations)
        {
            foreach (var player in session.Players)
            {
                foreach (ReagentType type in Enum.GetValues(typeof(ReagentType)))
                {
                    if (player.GetReagent(type) < 0)
                    {
                        violations.Add(new InventoryViolation(
                            "negative_reagent",
                            $"Player {player.PlayerId} has negative {type} count."));
                    }
                }
            }
        }

        static void AuditLimbo(GameSession session, List<InventoryViolation> violations)
        {
            foreach (var player in session.Players)
            {
                if (player.FatefulWagerCards.Count > 0 && player.FatefulWagerSign == ZodiacSign.None)
                {
                    violations.Add(new InventoryViolation(
                        "wager_sign_missing",
                        $"Player {player.PlayerId} has {player.FatefulWagerCards.Count} wager card(s) but no wager sign."));
                }

                if (player.FatefulWagerSign != ZodiacSign.None && player.FatefulWagerCards.Count == 0)
                {
                    violations.Add(new InventoryViolation(
                        "wager_cards_missing",
                        $"Player {player.PlayerId} has wager sign {player.FatefulWagerSign} but no wager cards."));
                }

                foreach (var id in player.FatefulWagerCards)
                {
                    if (player.Spread.Contains(id) || player.Hand.Contains(id))
                    {
                        violations.Add(new InventoryViolation(
                            "wager_visible",
                            "Wager card is still listed in Spread or Hand.",
                            id));
                    }
                }
            }

            foreach (var (playerId, adeptId) in session.Board.PendingAdeptDecisions)
            {
                if (playerId < 0 || playerId >= session.Players.Count)
                {
                    violations.Add(new InventoryViolation(
                        "pending_adept_player",
                        $"Pending adept references invalid player {playerId}.",
                        adeptId));
                    continue;
                }

                var player = session.Players[playerId];
                if (player.Spread.Contains(adeptId) || player.Hand.Contains(adeptId) || player.Arcanum.Contains(adeptId))
                {
                    violations.Add(new InventoryViolation(
                        "pending_adept_visible",
                        "Pending adept is still listed in a player zone.",
                        adeptId));
                }

                if (session.Board.CommonDiscard.Contains(adeptId))
                {
                    violations.Add(new InventoryViolation(
                        "pending_adept_discarded",
                        "Pending adept is already in Common Discard.",
                        adeptId));
                }
            }

            if (session.Board.FateMoonDrawnCardIds.Count > 0)
            {
                bool moonPending = false;
                foreach (var (_, _, arcanaNum) in session.Board.PendingFateDecisions)
                {
                    if (arcanaNum == 18)
                    {
                        moonPending = true;
                        break;
                    }
                }

                if (!moonPending)
                {
                    violations.Add(new InventoryViolation(
                        "moon_pool_stale",
                        $"FateMoonDrawnCardIds has {session.Board.FateMoonDrawnCardIds.Count} card(s) but no pending Moon fate."));
                }

                foreach (var id in session.Board.FateMoonDrawnCardIds)
                {
                    bool inHand = false;
                    foreach (var player in session.Players)
                    {
                        if (player.Hand.Contains(id))
                        {
                            inHand = true;
                            break;
                        }
                    }

                    if (!inHand)
                    {
                        violations.Add(new InventoryViolation(
                            "moon_pool_hand",
                            "Moon-drawn card is tracked in FateMoonDrawnCardIds but not in any Hand.",
                            id));
                    }
                }
            }
        }

        static bool ZoneMatches(InventoryLocationKind kind, int ownerId, CardZone zone, int registryOwnerId)
        {
            return kind switch
            {
                InventoryLocationKind.CommonDeck => zone == CardZone.Deck && registryOwnerId == -1,
                InventoryLocationKind.CrucibleDeck => zone == CardZone.Deck && registryOwnerId == -1,
                InventoryLocationKind.CommonDiscard => zone == CardZone.Discard && registryOwnerId == -1,
                InventoryLocationKind.CrucibleDiscard => zone == CardZone.Discard,
                InventoryLocationKind.Spread => zone == CardZone.Spread && registryOwnerId == ownerId,
                InventoryLocationKind.Hand => zone == CardZone.Hand && registryOwnerId == ownerId,
                InventoryLocationKind.Arcanum => zone == CardZone.Arcanum && registryOwnerId == ownerId,
                InventoryLocationKind.FatefulWager => zone == CardZone.Deck && registryOwnerId == -1,
                InventoryLocationKind.CrucibleSlot => zone == CardZone.Deck && registryOwnerId == ownerId,
                InventoryLocationKind.PendingAdept => zone == CardZone.Deck && registryOwnerId == -1,
                _ => false,
            };
        }

        static string FormatLocations(IReadOnlyList<(InventoryLocationKind Kind, int OwnerId)> entries)
        {
            var parts = new List<string>(entries.Count);
            foreach (var (kind, ownerId) in entries)
                parts.Add(ownerId >= 0 ? $"{kind}(P{ownerId})" : kind.ToString());
            return string.Join(", ", parts);
        }
    }
}
