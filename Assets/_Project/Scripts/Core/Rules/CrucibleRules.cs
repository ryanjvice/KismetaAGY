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
                // Verify all supplied alignment cards are actually in this player's Spread
                if (alignmentCardIds != null)
                {
                    foreach (var id in alignmentCardIds)
                    {
                        if (!player.Spread.Contains(id))
                            return CommandResult.Invalid(
                                $"Alignment card {id} is not in Player {playerId}'s Spread.");
                    }
                }

                var cards = ResolveCardDefs(session, alignmentCardIds);
                var cardList = new List<CardDefinition>(cards);

                bool worldWildcardActive = player.WorldWildcardFlipped
                    && AdeptAttunement.IsWorldResonant(session, player)
                    && crucibleDef.ArcanaNumber >= 0;

                if (worldWildcardActive)
                    cardList.Add(WildcardLinkService.CreateVirtualWildcard(crucibleDef.ArcanaNumber));

                var (ok, reason) = _alignmentValidator.Validate(
                    crucibleDef.AlchemicalFormula, cardList, _db);
                if (!ok) return CommandResult.Invalid($"Alignment not satisfied: {reason}");

                if (worldWildcardActive)
                    player.WorldWildcardFlipped = false;

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
                var wildTypes = ForgeReagentPaymentService.GetWildReagentTypes(player, session, _db);
                if (!ForgeReagentPaymentService.CanPayFireCost(player, crucibleDef.AlchemicalCost, wildTypes))
                    return CommandResult.Invalid("Insufficient reagents to Fire.");
                ForgeReagentPaymentService.PayFireCost(player, crucibleDef.AlchemicalCost, wildTypes);
            }

            // Move stone from Mantle to the next Forge position and enter Forging state
            player.StonePosition = player.StonePosition.Advance();
            player.StoneState    = StoneState.Forging;
            slot.Fire(session.Board.RoundNumber);

            ForgeEffectService.TryGrantRank7Reagents(session, playerId, _db);

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

            // Burn any remaining Forge Ward Reagents unless World adept retains them
            if (!AdeptEffectService.HasAdept(session, player, AdeptEffectService.WorldArcana))
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

            // Return stone to its preserved Forge position.
            player.StoneState = StoneState.Forging;
            player.ReturnedFromStasisThisRound = true;
            session.Board.StasisOccupancy[playerId] = false;

            // Stasis Opposition: if another stone is already Forging at the same position, tiebreak by dice.
            foreach (var occupier in session.Players)
            {
                if (occupier.PlayerId == playerId) continue;
                if (occupier.StoneState != StoneState.Forging) continue;
                if (occupier.StonePosition.Value != player.StonePosition.Value) continue;

                // Position clash — roll until no tie
                int returnerRoll, occupierRoll;
                do
                {
                    returnerRoll = _rng.Next(1, 13);
                    occupierRoll = _rng.Next(1, 13);
                } while (returnerRoll == occupierRoll);

                bool returnerWins = returnerRoll > occupierRoll;
                int  loserId      = returnerWins ? occupier.PlayerId : playerId;
                var  loser        = session.Players[loserId];

                loser.StoneState = StoneState.Stasis;
                loser.StoneWardCount = 0;
                session.Board.StasisOccupancy[loserId] = true;

                session.EmitEvent(new StasisOppositionEvent(
                    playerId, occupier.PlayerId, returnerRoll, occupierRoll, loserId));
                break; // at most one clash per Leave Stasis
            }

            return CommandResult.Ok($"Left Stasis. Stone returns to {player.StonePosition}.");
        }

        // ─── Opposition ───────────────────────────────────────────────────────────

        public CommandResult TryInitiateOppose(GameSession session, int attackerId, int defenderId)
        {
            var validation = ValidateOppositionSetup(session, attackerId, defenderId);
            if (!validation.IsOk) return validation;

            var saltCost = CombatRules.TrySpendSwordsFiveSalt(session, session.Players[attackerId], ContestKind.Opposition);
            if (!saltCost.IsOk) return saltCost;

            int wardCost = session.Players[defenderId].StoneWardCount;
            session.Board.PendingContest = new PendingContest
            {
                Kind       = ContestKind.Opposition,
                AttackerId = attackerId,
                DefenderId = defenderId
            };

            session.EmitEvent(new OppositionOfferedEvent(attackerId, defenderId, wardCost));
            return CommandResult.Ok($"Opposition offered against P{defenderId}.");
        }

        public CommandResult TryRespondOpposition(GameSession session, int defenderId, bool accept)
        {
            var pending = session.Board.PendingContest;
            if (pending == null || pending.Kind != ContestKind.Opposition)
                return CommandResult.Invalid("No pending opposition to respond to.");
            if (pending.DefenderId != defenderId)
                return CommandResult.Invalid("Only the opposition defender may respond.");

            var attackerId = pending.AttackerId;
            session.Board.PendingContest = null;

            if (!accept)
            {
                session.EmitEvent(new OppositionDeclinedEvent(attackerId, defenderId));
                return CommandResult.Ok("Opposition declined.");
            }

            var validation = ValidateOppositionSetup(session, attackerId, defenderId);
            if (!validation.IsOk) return validation;

            return ResolveOpposition(session, attackerId, defenderId);
        }

        public CommandResult TryOppose(GameSession session, int attackerId, int defenderId)
        {
            var initiate = TryInitiateOppose(session, attackerId, defenderId);
            if (!initiate.IsOk) return initiate;
            return TryRespondOpposition(session, defenderId, accept: true);
        }

        static CommandResult ValidateOppositionSetup(GameSession session, int attackerId, int defenderId)
        {
            if (attackerId == defenderId)
                return CommandResult.Invalid("Cannot oppose yourself.");

            var defender = session.Players[defenderId];
            if (defender.StoneState != StoneState.Forging)
                return CommandResult.Invalid("Target stone must be Forging to initiate Opposition.");

            var defenderFiredSlot = defender.CrucibleSlots.Find(
                s => s.State == CrucibleCardState.Fired && s.FiredAtRound == session.Board.RoundNumber);
            if (defenderFiredSlot != null)
                return CommandResult.Invalid("This stone was Fired this Autumn and cannot be targeted yet.");

            var attacker = session.Players[attackerId];
            if (defender.StoneWardCount > 0
                && ReagentSpendHelper.TotalReagents(attacker) < defender.StoneWardCount)
                return CommandResult.Invalid(
                    $"Attacker must pay {defender.StoneWardCount} reagent(s) to breach Ward — insufficient.");

            return CommandResult.Ok();
        }

        CommandResult ResolveOpposition(GameSession session, int attackerId, int defenderId)
        {
            var defender = session.Players[defenderId];
            var attacker = session.Players[attackerId];

            if (!ReagentSpendHelper.TrySpend(attacker, defender.StoneWardCount, null, out var spendError))
                return CommandResult.Invalid(spendError ?? "Could not pay ward breach fee.");

            int attackScore, defendScore;
            if (_alignmentService != null)
            {
                var cosmicSign = session.Board.CosmicAgeSign;
                var attackerPlayer = session.Players[attackerId];
                ZodiacSign originalSign = attackerPlayer.CurrentSign;
                if (attackerPlayer.HierophantOppositionShift != 0 && originalSign != ZodiacSign.None)
                    attackerPlayer.CurrentSign = AdeptEffectService.ShiftSign(
                        originalSign, attackerPlayer.HierophantOppositionShift);

                attackScore = _alignmentService.CalculateAlignmentPoints(session, attackerId, cosmicSign);
                defendScore = _alignmentService.CalculateAlignmentPoints(session, defenderId, cosmicSign);
                attackScore += PlayerAspectAlignment.MagnusOppositionAlignmentBonus(
                    session, attackerId, defenderId);

                attackerPlayer.CurrentSign = originalSign;
            }
            else
            {
                attackScore = 0;
                defendScore = 0;
            }

            defendScore += defender.BesiegedBonusCount;

            int attackRoll, defendRoll;

            do
            {
                attackRoll = _rng.Next(1, 13);
                defendRoll = _rng.Next(1, 13);
            } while (attackScore + attackRoll == defendScore + defendRoll);

            int attackTotal = attackScore + attackRoll;
            int defendTotal = defendScore + defendRoll;

            bool attackerWins = attackTotal > defendTotal;
            int  loserId      = attackerWins ? defenderId : attackerId;
            int  winnerId     = attackerWins ? attackerId : defenderId;

            var loser  = session.Players[loserId];
            var winner = session.Players[winnerId];

            loser.StoneState       = StoneState.Stasis;
            loser.StoneWardCount   = 0;
            session.Board.StasisOccupancy[loserId] = true;

            if (winnerId == defenderId)
                winner.BesiegedBonusCount++;

            AdeptEffectService.TryApplyStarPostLossDraw(session, loserId);
            SpreadSocialEffectService.TryApplyContestWinDraw(session, winnerId, ContestKind.Opposition, _db);

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

        public CommandResult TryPlaceAdeptWard(GameSession session, int playerId, string adeptCardId,
            ReagentType reagentType)
        {
            var player = session.Players[playerId];

            if (!player.Arcanum.Contains(adeptCardId))
                return CommandResult.Invalid($"Card {adeptCardId} is not in Player {playerId}'s Arcanum.");

            var inst = session.GetCard(adeptCardId);
            var def = inst != null ? _db.GetById(inst.DefinitionId) : null;
            if (def?.MajorArcanaType != MajorArcanaType.Adept)
                return CommandResult.Invalid("Only Adept cards can receive Adept wards.");

            if (!player.SpendReagent(reagentType, 1))
                return CommandResult.Invalid($"Not enough {reagentType} to place a Ward.");

            player.AddAdeptWard(adeptCardId);
            return CommandResult.Ok($"Ward placed on Adept {adeptCardId}.");
        }

        public CommandResult TryRefreshAdept(GameSession session, int playerId, string adeptCardId)
        {
            var player = session.Players[playerId];

            if (!player.Arcanum.Contains(adeptCardId))
                return CommandResult.Invalid($"Card {adeptCardId} is not in Player {playerId}'s Arcanum.");

            if (!player.ArrestedAdepts.Contains(adeptCardId))
                return CommandResult.Invalid($"Card {adeptCardId} is not arrested.");

            if (!player.SpendReagent(ReagentType.Salt, 1))
                return CommandResult.Invalid("Refreshing an arrested Adept costs 1 Salt.");

            player.ArrestedAdepts.Remove(adeptCardId);
            session.EmitEvent(new AdeptRefreshedEvent(playerId, adeptCardId));
            return CommandResult.Ok("Adept refreshed.");
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

    }
}
