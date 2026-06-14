using System;
using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Implements the full Crucible Card lifecycle:
    ///   Activate (Summer)  — validate Codex formula, discard cards, remove coal, light cauldron, reveal card.
    ///   Fire    (Autumn)   — pay Alchemical Cost in reagents; stone moves to Forging.
    ///   Temper  (Autumn)   — advance stone one step; check for Altar win.
    ///   LeaveStasis        — spend 2 Salt to exit Stasis.
    ///   Opposition (Autumn)— dice-only: higher roll wins; loser's stone → Stasis.
    /// </summary>
    public sealed class CrucibleRules : ICrucibleService
    {
        private const int StasisSaltCost = 2;

        private readonly ICardDatabase                _db;
        private readonly ICrucibleCodexDatabase       _codexDb;
        private readonly CodexFormulaValidator        _validator;
        private readonly AlchemicalAlignmentValidator? _alignmentValidator;
        private readonly IAlignmentService?           _alignmentService;
        private readonly Random                       _rng;

        public CrucibleRules(ICardDatabase db, ICrucibleCodexDatabase codexDb,
            AlchemicalAlignmentValidator? alignmentValidator = null,
            IAlignmentService?            alignmentService   = null,
            int? seed = null)
        {
            _db                 = db;
            _codexDb            = codexDb;
            _validator          = new CodexFormulaValidator(db);
            _alignmentValidator = alignmentValidator;
            _alignmentService   = alignmentService;
            _rng                = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        // ─── Activate ─────────────────────────────────────────────────────────────

        public CommandResult TryActivate(GameSession session, int playerId, int slotIndex,
            IReadOnlyList<string> cardInstanceIds)
        {
            var player = session.Players[playerId];

            if (slotIndex < 0 || slotIndex >= player.CrucibleSlots.Count)
                return CommandResult.Invalid($"No Crucible slot at index {slotIndex}.");

            var slot = player.CrucibleSlots[slotIndex];
            if (slot.State != CrucibleCardState.Dormant)
                return CommandResult.Invalid("Slot must be Dormant to Activate.");

            if (!slot.HasCoal)
                return CommandResult.Invalid("Slot has no Coal; cannot Activate.");

            if (cardInstanceIds == null || cardInstanceIds.Count == 0)
                return CommandResult.Invalid("No cards submitted for activation.");

            // Look up the Codex formula for this player's codex + slot.
            var formula = _codexDb.GetFormula(player.AssignedCodex, slotIndex);
            if (formula == null)
                return CommandResult.Invalid(
                    $"No Codex formula found for Codex {player.AssignedCodex}, slot {slotIndex}.");

            // All submitted cards must come from Spread only (Hand not allowed for activation).
            var spreadSet = new HashSet<string>(player.Spread);
            foreach (var id in cardInstanceIds)
                if (!spreadSet.Contains(id))
                    return CommandResult.Invalid($"Card {id} is not in your Spread. Activation requires Spread cards only.");

            // Resolve definitions.
            var defs = new List<CardDefinition>(cardInstanceIds.Count);
            foreach (var id in cardInstanceIds)
            {
                var inst = session.GetCard(id);
                if (inst == null)
                    return CommandResult.Invalid($"Card instance {id} not found.");
                var def = _db.GetById(inst.DefinitionId);
                if (def == null)
                    return CommandResult.Invalid($"Card definition for {id} not found.");
                defs.Add(def);
            }

            // Validate against the Codex formula.
            var (ok, reason) = _validator.ValidateDefs(defs, formula);
            if (!ok)
                return CommandResult.Invalid($"Formula not satisfied: {reason}");

            // Discard submitted cards from Spread.
            foreach (var id in cardInstanceIds)
            {
                player.Spread.Remove(id);
                session.Board.CommonDiscard.Add(id);
                session.GetCard(id)?.MoveTo(CardZone.Discard, -1);
            }

            // Activate slot (removes coal, transitions Dormant → Active).
            slot.Activate();

            // Light the cauldron determined by the formula (not the crucible card's suit).
            player.LightCauldron(formula.CauldronSuit);

            session.EmitEvent(new CrucibleActivatedEvent(playerId, slotIndex, slot.CardInstanceId, formula.CauldronSuit));
            return CommandResult.Ok();
        }

        // ─── Fire ─────────────────────────────────────────────────────────────────

        public CommandResult TryFire(GameSession session, int playerId, int slotIndex,
            IReadOnlyList<string>? alignmentCardIds = null)
        {
            var player = session.Players[playerId];

            if (slotIndex < 0 || slotIndex >= player.CrucibleSlots.Count)
                return CommandResult.Invalid($"No Crucible slot at index {slotIndex}.");

            var slot = player.CrucibleSlots[slotIndex];
            if (slot.State != CrucibleCardState.Active)
                return CommandResult.Invalid("Slot must be Active to Fire.");

            // Fire requires stone at a Mantle position (0, 2, 4, 6)
            if (!player.StonePosition.IsMantle)
                return CommandResult.Invalid(
                    $"Stone must be at a Mantle position to Fire (currently {player.StonePosition}).");

            // Look up the Crucible card for reagent cost
            var crucibleInst = session.GetCard(slot.CardInstanceId);
            var crucibleDef  = crucibleInst != null ? _db.GetById(crucibleInst.DefinitionId) : null;

            // Validate and pay alchemical alignment cards (Stage 2 — handled here when validator present)
            if (_alignmentValidator != null && crucibleDef != null)
            {
                var cards = ResolveCardDefs(session, alignmentCardIds);
                var (ok, reason) = _alignmentValidator.Validate(crucibleDef.AlchemicalFormula, cards);
                if (!ok) return CommandResult.Invalid($"Alignment not satisfied: {reason}");

                // Discard the alignment cards from Spread
                if (alignmentCardIds != null)
                {
                    foreach (var id in alignmentCardIds)
                    {
                        player.Spread.Remove(id);
                        session.Board.CommonDiscard.Add(id);
                        session.GetCard(id)?.MoveTo(CardZone.Discard, -1);
                    }
                }
            }

            // Pay alchemical reagent cost
            if (crucibleDef != null)
            {
                if (!CanPayCost(player, crucibleDef.AlchemicalCost))
                    return CommandResult.Invalid("Insufficient reagents to Fire.");
                PayCost(player, crucibleDef.AlchemicalCost);
            }

            // Move stone from Mantle to the next Forge position and enter Forging state
            player.StonePosition = player.StonePosition.Advance();
            player.StoneState    = StoneState.Forging;
            slot.Fire(session.Board.RoundNumber);

            session.EmitEvent(new StoneFiredEvent(playerId, slotIndex, player.StonePosition));
            return CommandResult.Ok();
        }

        // ─── Temper ───────────────────────────────────────────────────────────────

        public CommandResult TryTemper(GameSession session, int playerId)
        {
            var player = session.Players[playerId];

            if (player.StoneState != StoneState.Forging)
                return CommandResult.Invalid("Stone must be Forging to Temper.");

            // Temper requires stone at a Forge position (1, 3, 5, 7)
            if (!player.StonePosition.IsForge)
                return CommandResult.Invalid(
                    $"Stone must be at a Forge position to Temper (currently {player.StonePosition}).");

            // A stone that returned from Stasis this round must forge a full round first
            if (player.ReturnedFromStasisThisRound)
                return CommandResult.Invalid(
                    "Stone returned from Stasis this round; it must complete a full round of Forging before Tempering.");

            // The Fired slot that was fired in a prior round is eligible
            PlayerCrucibleSlot? eligibleSlot = null;
            for (int i = 0; i < player.CrucibleSlots.Count; i++)
            {
                var s = player.CrucibleSlots[i];
                if (s.State == CrucibleCardState.Fired &&
                    s.FiredAtRound >= 0 &&
                    s.FiredAtRound < session.Board.RoundNumber)
                {
                    eligibleSlot = s;
                    break;
                }
            }

            if (eligibleSlot == null)
                return CommandResult.Invalid("No Fired slot eligible for Temper (must have been Fired a previous round).");

            // Burn any remaining Forge Ward Reagents
            player.StoneWardCount = 0;

            // Advance stone from Forge to next Mantle (or Altar)
            var newPos = player.StonePosition.Advance();
            player.StonePosition = newPos;
            player.StoneState    = StoneState.Tempering;

            // Discard the Fired crucible card
            eligibleSlot.Discard();
            session.Board.CrucibleDiscard.Add(eligibleSlot.CardInstanceId);
            session.GetCard(eligibleSlot.CardInstanceId)?.MoveTo(CardZone.Discard, -1);

            session.EmitEvent(new StoneTemperedEvent(playerId, newPos));

            // Win: stone reaches the Altar
            if (newPos.IsAltar)
                session.SetWinner(playerId);

            return CommandResult.Ok();
        }

        // ─── Leave Stasis ─────────────────────────────────────────────────────────

        public CommandResult TryLeaveStasis(GameSession session, int playerId)
        {
            var player = session.Players[playerId];
            if (player.StoneState != StoneState.Stasis)
                return CommandResult.Invalid("Stone is not in Stasis.");

            if (!player.SpendReagent(ReagentType.Salt, StasisSaltCost))
                return CommandResult.Invalid($"Leaving Stasis requires {StasisSaltCost} Salt.");

            // StonePosition was preserved when the stone entered Stasis; return to that Forge.
            // StoneState returns to Forging (card is still Fired; must complete a full round before Tempering).
            player.StoneState = StoneState.Forging;
            player.ReturnedFromStasisThisRound = true;
            session.Board.StasisOccupancy[playerId] = false;
            return CommandResult.Ok($"Left Stasis. Stone returns to {player.StonePosition}.");
        }

        // ─── Opposition ───────────────────────────────────────────────────────────

        public CommandResult TryOppose(GameSession session, int attackerId, int defenderId)
        {
            if (attackerId == defenderId)
                return CommandResult.Invalid("Cannot oppose yourself.");

            var defender = session.Players[defenderId];
            if (defender.StoneState != StoneState.Forging)
                return CommandResult.Invalid("Target stone must be Forging to initiate Opposition.");

            // Stones Fired this Autumn are immune to Opposition
            var defenderFiredSlot = defender.CrucibleSlots.Find(
                s => s.State == CrucibleCardState.Fired && s.FiredAtRound == session.Board.RoundNumber);
            if (defenderFiredSlot != null)
                return CommandResult.Invalid("This stone was Fired this Autumn and cannot be targeted yet.");

            // Score each side using alignment points (if the service is wired) + a dice roll for tiebreaking
            int attackScore, defendScore;
            if (_alignmentService != null)
            {
                var cosmicSign = session.Board.CosmicAgeSign;
                attackScore = _alignmentService.CalculateAlignmentPoints(session, attackerId, cosmicSign);
                defendScore = _alignmentService.CalculateAlignmentPoints(session, defenderId, cosmicSign);
            }
            else
            {
                attackScore = 0;
                defendScore = 0;
            }

            int attackRoll = _rng.Next(1, 13);
            int defendRoll = _rng.Next(1, 13);

            // Best-of-3 if Justice fate is active
            if (session.Board.BestOfThreeDuels)
            {
                int aWins = 0, dWins = 0;
                while (aWins < 2 && dWins < 2)
                {
                    int a = _rng.Next(1, 13);
                    int d = _rng.Next(1, 13);
                    if (a >= d) aWins++; else dWins++;
                }
                attackRoll = aWins >= 2 ? 12 : 1;
                defendRoll = dWins >= 2 ? 12 : 1;
            }

            // Final score = alignment score + dice roll; attacker wins ties
            int attackTotal = attackScore + attackRoll;
            int defendTotal = defendScore + defendRoll;

            // Attacker wins ties; loser's Forge Wards discarded, stone enters Stasis
            bool attackerWins = attackTotal >= defendTotal;
            int  loserId      = attackerWins ? defenderId : attackerId;
            int  winnerId     = attackerWins ? attackerId : defenderId;

            var loser  = session.Players[loserId];
            var winner = session.Players[winnerId];

            loser.StoneState       = StoneState.Stasis;
            loser.StoneWardCount   = 0; // Wards discarded on loss
            session.Board.StasisOccupancy[loserId] = true;
            // winner.StoneWardCount remains (wards stay on successful defence)

            session.EmitEvent(new OppositionResolvedEvent(
                attackerId, defenderId, attackRoll, defendRoll, loserId,
                attackScore, defendScore));
            return CommandResult.Ok(
                $"Opposition: Att {attackScore}+{attackRoll}={attackTotal} vs Def {defendScore}+{defendRoll}={defendTotal}. P{loserId} → Stasis.");
        }

        // ─── Place Ward (Card) ────────────────────────────────────────────────────

        public CommandResult TryPlaceCardWard(GameSession session, int playerId, int slotIndex,
            ReagentType reagentType)
        {
            var player = session.Players[playerId];

            if (slotIndex < 0 || slotIndex >= player.CrucibleSlots.Count)
                return CommandResult.Invalid($"No Crucible slot at index {slotIndex}.");

            var slot = player.CrucibleSlots[slotIndex];
            if (slot.State != CrucibleCardState.Active && slot.State != CrucibleCardState.Fired)
                return CommandResult.Invalid("Slot must be Active or Fired to place a Ward.");

            if (!player.SpendReagent(reagentType, 1))
                return CommandResult.Invalid($"Not enough {reagentType} to place a Ward.");

            slot.AddWard();
            return CommandResult.Ok($"Ward placed on Crucible slot {slotIndex}.");
        }

        // ─── Place Ward (Stone/Forge) ──────────────────────────────────────────────

        public CommandResult TryPlaceStoneWard(GameSession session, int playerId, ReagentType reagentType)
        {
            var player = session.Players[playerId];

            if (player.StoneState != StoneState.Forging)
                return CommandResult.Invalid("Stone must be Forging to place a Forge Ward.");

            if (!player.SpendReagent(reagentType, 1))
                return CommandResult.Invalid($"Not enough {reagentType} to place a Forge Ward.");

            player.StoneWardCount++;
            return CommandResult.Ok($"Forge Ward placed (total: {player.StoneWardCount}).");
        }

        // ─── Private helpers ──────────────────────────────────────────────────────

        private List<CardDefinition> ResolveCardDefs(GameSession session, IReadOnlyList<string>? ids)
        {
            var defs = new List<CardDefinition>();
            if (ids == null) return defs;
            foreach (var id in ids)
            {
                var inst = session.GetCard(id);
                if (inst == null) continue;
                var def = _db.GetById(inst.DefinitionId);
                if (def != null) defs.Add(def);
            }
            return defs;
        }

        private static bool CanPayCost(PlayerState player, ReagentCost cost) =>
            player.GetReagent(ReagentType.Sulphur)     >= cost.Sulphur     &&
            player.GetReagent(ReagentType.AquaRegia)   >= cost.AquaRegia   &&
            player.GetReagent(ReagentType.Vitriol)     >= cost.Vitriol     &&
            player.GetReagent(ReagentType.Quicksilver) >= cost.Quicksilver &&
            player.GetReagent(ReagentType.Salt)        >= cost.Salt;

        private static void PayCost(PlayerState player, ReagentCost cost)
        {
            player.SpendReagent(ReagentType.Sulphur,     cost.Sulphur);
            player.SpendReagent(ReagentType.AquaRegia,   cost.AquaRegia);
            player.SpendReagent(ReagentType.Vitriol,     cost.Vitriol);
            player.SpendReagent(ReagentType.Quicksilver, cost.Quicksilver);
            player.SpendReagent(ReagentType.Salt,        cost.Salt);
        }
    }
}
