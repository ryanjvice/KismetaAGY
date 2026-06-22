using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.Core.Views;

namespace Kismeta.Core.Players
{
    /// <summary>
    /// Greedy heuristic AI for hot-seat testing without a human player.
    ///
    /// Commune  — puts all available cards into Spread.
    /// Summer   — activates the first Dormant Crucible slot if it has cards, else crafts Salt,
    ///            else passes. Each activation costs any 3 Spread cards.
    /// Autumn   — tempers if eligible, else fires the first Active slot if it can afford the cost,
    ///            else passes.
    /// </summary>
    public sealed class SimpleAIController : IPlayerController
    {
        public PlayerSlot Slot { get; }
        public bool IsLocalHuman => false;

        public SimpleAIController(PlayerSlot slot) => Slot = slot;

        public Task<IGameCommand> RequestActionAsync(GameContext context, CancellationToken ct = default)
        {
            IGameCommand action = context.Hint switch
            {
                ActionHint.RollZodiac        => new RollZodiacCommand(Slot.Index),
                ActionHint.AcknowledgeSign   => new PassActionCommand(Slot.Index),
                ActionHint.ConfirmHarvest    => new HarvestCommand(Slot.Index, 0),
                ActionHint.Commune           => DecideCommune(context),
                ActionHint.SummerAction      => DecideSummer(context),
                ActionHint.AutumnAction      => DecideAutumn(context),
                ActionHint.AdeptDecision     => DecideAdept(context),
                ActionHint.FateReagentChoice    => new FateReagentChoiceCommand(Slot.Index, ReagentType.Salt),
                ActionHint.FateLoversChoice     => new FateLoversChoiceCommand(Slot.Index, false),
                ActionHint.FateLoversTargetPick => DecideFateLoversTarget(context),
                ActionHint.FateMoonDecision  => DecideFateMoon(context),
                ActionHint.WinterAction      => DecideWinter(context),
                ActionHint.DiscardToLimit    => DecideDiscardToLimit(context),
                _                            => new PassActionCommand(Slot.Index)
            };

            return Task.FromResult(action);
        }

        // ─── Spring: Commune ──────────────────────────────────────────────────────

        private IGameCommand DecideCommune(GameContext ctx)
        {
            var pid    = Slot.Index;
            var spread = ctx.PublicView.Players[pid].Spread;
            // Only minor arcana may be assigned to Spread or Hand
            var allCards = new List<string>(spread.Count + ctx.PrivateView.Hand.Count);
            foreach (var id in spread)               if (IsMinorArcana(ctx, id)) allCards.Add(id);
            foreach (var id in ctx.PrivateView.Hand) if (IsMinorArcana(ctx, id)) allCards.Add(id);
            return CommuneCommand.AllToSpread(pid, allCards);
        }

        // ─── Summer: Activate or Craft or Pass ────────────────────────────────────

        private IGameCommand DecideSummer(GameContext ctx)
        {
            var pid     = Slot.Index;
            var player  = ctx.PublicView.Players[pid];
            var spreadIds = new List<string>(player.Spread);

            // Try to activate each Dormant slot by finding a valid card set from Spread.
            for (int i = 0; i < player.CrucibleSlots.Count; i++)
            {
                var slot = player.CrucibleSlots[i];
                if (slot.State != CrucibleCardState.Dormant || !slot.HasCoal) continue;

                var payment = FindActivationCards(ctx, player.AssignedCodex, i, spreadIds);
                if (payment != null)
                    return new ActivateCrucibleCommand(pid, i, payment);
            }

            // Try to build an Astral House on current sign if unplaced houses remain
            if (player.UnplacedAstralHouses > 0
                && player.CurrentSign != ZodiacSign.None
                && !player.AstralHouses.Contains(player.CurrentSign)
                && ctx.CardDatabase != null)
            {
                var sign   = player.CurrentSign;
                bool signFree = true;
                foreach (var p in ctx.PublicView.Players)
                    if (p.PlayerId != pid && p.AstralHouses.Contains(sign))
                    { signFree = false; break; }

                if (signFree)
                {
                    var planet   = Correspondence.PlanetFor(sign);
                    var cardMap  = ctx.PublicView.CardInstanceToDefinition;
                    string? payCard = null;
                    foreach (var id in spreadIds)
                    {
                        if (!cardMap.TryGetValue(id, out var defId)) continue;
                        var def = ctx.CardDatabase.GetById(defId);
                        if (def != null && def.Planet == planet) { payCard = id; break; }
                    }
                    if (payCard != null)
                        return new BuildAstralHouseCommand(pid, sign, new List<string> { payCard });
                }
            }

            // Try to craft Salt (needs any 3 cards from Spread)
            if (spreadIds.Count >= 3)
            {
                var payment = spreadIds.GetRange(0, 3);
                return new CraftReagentCommand(pid, ReagentType.Salt, payment);
            }

            // Duel an opponent if we have at least 2 Spread cards (ante 1, keep 1)
            if (spreadIds.Count >= 2)
            {
                var target = FindDuelTarget(ctx, pid);
                if (target >= 0)
                {
                    var rivalSpread = ctx.PublicView.Players[target].Spread;
                    if (rivalSpread.Count > 0)
                        return new InitiateDuelCommand(pid, target, rivalSpread[0], spreadIds[0]);
                }
            }

            // Gambit if we have an Active Crucible slot — stakes an arrested outcome
            var gambitTarget = FindGambitTarget(ctx, pid);
            if (gambitTarget.TargetId >= 0 && gambitTarget.OfferedCardId != null)
                return new InitiateGambitCommand(pid, gambitTarget.TargetId, gambitTarget.OfferedCardId);

            return new PassCrucibleActionCommand(pid);
        }

        // ─── Autumn: Temper, Fire, or Pass ────────────────────────────────────────

        private IGameCommand DecideAutumn(GameContext ctx)
        {
            var pid       = Slot.Index;
            var player    = ctx.PublicView.Players[pid];
            var spreadIds = new List<string>(player.Spread);

            // Temper: stone must be Forging AND at a Forge position AND not returned from Stasis this round
            if (player.StoneState == StoneState.Forging
                && player.StonePosition.IsForge
                && !player.ReturnedFromStasisThisRound)
                return new TemperCommand(pid);

            // Leave Stasis
            if (player.StoneState == StoneState.Stasis
                && player.Reagents.TryGetValue(ReagentType.Salt, out int salt) && salt >= 2)
                return new LeaveStasisCommand(pid);

            // Fire: stone must be at Mantle and we need reagents + an Active slot.
            if (player.StoneState == StoneState.Tempering && player.StonePosition.IsMantle)
            {
                bool hasFireReagents =
                    player.Reagents.TryGetValue(ReagentType.Sulphur,     out int su) && su > 0 ||
                    player.Reagents.TryGetValue(ReagentType.AquaRegia,   out int ar) && ar > 0 ||
                    player.Reagents.TryGetValue(ReagentType.Vitriol,     out int vi) && vi > 0 ||
                    player.Reagents.TryGetValue(ReagentType.Quicksilver, out int qk) && qk > 0;

                if (hasFireReagents)
                {
                    for (int i = 0; i < player.CrucibleSlots.Count; i++)
                    {
                        if (player.CrucibleSlots[i].State != CrucibleCardState.Active) continue;

                        // Submit only the minimum cards that satisfy the alchemical formula.
                        var alignCards = FindMinimumFireCards(ctx, player.AssignedCodex, i, spreadIds);
                        return new FireStoneCommand(pid, i, alignCards);
                    }
                }
            }

            // Opposition: target the opponent who is Forging and furthest ahead
            // but only if our stone is not in Stasis and not fresh out of Stasis
            if (player.StoneState != StoneState.Stasis && !player.ReturnedFromStasisThisRound)
            {
                var oppTarget = FindOppositionTarget(ctx, pid);
                if (oppTarget >= 0)
                    return new InitiateOppositionCommand(pid, oppTarget);
            }

            return new PassCrucibleActionCommand(pid);
        }

        // ─── Winter: free-action pool ─────────────────────────────────────────────

        private IGameCommand DecideWinter(GameContext ctx)
        {
            var pid    = Slot.Index;
            var player = ctx.PublicView.Players[pid];

            // Move any Hand cards to Spread (AI keeps everything visible in Spread)
            foreach (var id in ctx.PrivateView.Hand)
            {
                if (IsMinorArcana(ctx, id))
                    return new WinterMoveCardCommand(pid, id, toSpread: true);
            }

            // Nothing to move — pass
            return new PassActionCommand(pid);
        }

        private IGameCommand DecideDiscardToLimit(GameContext ctx)
        {
            var pid    = Slot.Index;
            var player = ctx.PublicView.Players[pid];

            // Build lists of cards to discard from each zone (lowest-rank cards go first)
            var spreadIds  = new List<string>(player.Spread);
            var handIds    = new List<string>(ctx.PrivateView.Hand);

            int spreadExcess = spreadIds.Count - WinterRules.SpreadLimit;
            int handExcess   = handIds.Count   - WinterRules.HandLimit;

            var discardSpread = spreadExcess > 0 ? spreadIds.GetRange(0, spreadExcess) : new List<string>();
            var discardHand   = handExcess   > 0 ? handIds.GetRange(0,   handExcess)   : new List<string>();

            return new DiscardToLimitCommand(pid, discardSpread, discardHand);
        }

        // ─── Adept + Fate decisions ───────────────────────────────────────────────

        private IGameCommand DecideAdept(GameContext ctx)
        {
            if (ctx.PendingCardId == null) return new DeclineAdeptCommand(Slot.Index, "");

            var pid        = Slot.Index;
            var allCards   = AllPlayerCards(ctx);
            var arcanumAdepts = ctx.ArcanumAdeptIds ?? System.Array.Empty<string>();

            // Need 3 payment cards to buy
            if (allCards.Count < 3) return new DeclineAdeptCommand(pid, ctx.PendingCardId);

            var payment = allCards.GetRange(0, 3);

            // If Arcanum is full, swap out the first existing Adept
            if (arcanumAdepts.Count >= 2)
            {
                string swapOut = arcanumAdepts[0];
                return new BuyAdeptCommand(pid, ctx.PendingCardId, payment, swapOut);
            }

            return new BuyAdeptCommand(pid, ctx.PendingCardId, payment);
        }

        private IGameCommand DecideFateLoversTarget(GameContext ctx)
        {
            // Pick the first opponent (the one after us in player order)
            int count    = ctx.PublicView.Players.Count;
            int targetId = (Slot.Index + 1) % count;
            return new FateLoversTargetCommand(Slot.Index, targetId);
        }

        private IGameCommand DecideFateMoon(GameContext ctx)
        {
            // Pick the first 2 from the 4 Moon-drawn cards (provided via context)
            var source = ctx.MoonDrawnCardIds ?? ctx.PrivateView.Hand;
            var keep = source.Count >= 2
                ? new List<string> { source[0], source[1] }
                : new List<string>(source);
            return new FateMoonDecisionCommand(Slot.Index, keep);
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        // ─── Combat target finders ────────────────────────────────────────────────

        /// <summary>
        /// Returns the player ID of the best Opposition target, or -1 if none is eligible.
        /// Targets must be Forging (not newly Fired this round) and we must have enough reagents
        /// to cover their Forge Ward cost.
        /// Prefers the target whose stone is furthest ahead on the track.
        /// </summary>
        private static int FindOppositionTarget(GameContext ctx, int ownPid)
        {
            int bestId   = -1;
            int bestPos  = -1;
            var self     = ctx.PublicView.Players[ownPid];

            int totalSelfReagents = 0;
            foreach (var kv in self.Reagents) totalSelfReagents += kv.Value;

            foreach (var opp in ctx.PublicView.Players)
            {
                if (opp.PlayerId == ownPid) continue;
                if (opp.StoneState != StoneState.Forging) continue;
                if (totalSelfReagents < opp.StoneWardCount) continue; // can't afford ward fee

                // Don't target a stone that was Fired this round (immune rule; server enforces,
                // but we can check heuristically by FiredAtRound — not in public view, so just try)
                if (opp.StonePosition.Value > bestPos)
                {
                    bestPos = opp.StonePosition.Value;
                    bestId  = opp.PlayerId;
                }
            }

            return bestId;
        }

        /// <summary>
        /// Returns the ID of a Duel target (first opponent), or -1 if no valid target.
        /// Only initiates if the opponent has a Spread card to win.
        /// </summary>
        private static int FindDuelTarget(GameContext ctx, int ownPid)
        {
            foreach (var opp in ctx.PublicView.Players)
            {
                if (opp.PlayerId == ownPid) continue;
                if (opp.Spread.Count > 0)
                    return opp.PlayerId;
            }
            return -1;
        }

        /// <summary>
        /// Returns the best Gambit target and the offered card ID.
        /// Uses the first Active Crucible slot as the offered card.
        /// Returns (-1, null) if no viable gambit.
        /// </summary>
        private static (int TargetId, string? OfferedCardId) FindGambitTarget(GameContext ctx, int ownPid)
        {
            var self = ctx.PublicView.Players[ownPid];

            // Find an Active slot to offer
            string? offeredId = null;
            foreach (var slot in self.CrucibleSlots)
            {
                if (slot.State == CrucibleCardState.Active)
                { offeredId = slot.CardInstanceId; break; }
            }

            if (offeredId == null) return (-1, null);

            // Pick first opponent who has an Active slot (worth arresting)
            foreach (var opp in ctx.PublicView.Players)
            {
                if (opp.PlayerId == ownPid) continue;
                bool hasActive = false;
                foreach (var slot in opp.CrucibleSlots)
                    if (slot.State == CrucibleCardState.Active) { hasActive = true; break; }
                if (hasActive)
                    return (opp.PlayerId, offeredId);
            }

            return (-1, null);
        }

        /// <summary>All minor-arcana cards in Spread + Hand (Major Arcana excluded — cannot be payment).</summary>
        private static List<string> AllPlayerCards(GameContext ctx)
        {
            int pid    = ctx.ActivePlayerId;
            var spread = ctx.PublicView.Players[pid].Spread;
            var list   = new List<string>(spread.Count + ctx.PrivateView.Hand.Count);
            foreach (var id in spread)               if (IsMinorArcana(ctx, id)) list.Add(id);
            foreach (var id in ctx.PrivateView.Hand) if (IsMinorArcana(ctx, id)) list.Add(id);
            return list;
        }

        /// <summary>Returns true when the card instance is minor arcana (safe for Hand/Spread/payment).</summary>
        private static bool IsMinorArcana(GameContext ctx, string instanceId)
        {
            if (ctx.CardDatabase == null) return true; // assume safe when db not wired
            if (!ctx.PublicView.CardInstanceToDefinition.TryGetValue(instanceId, out var defId))
                return true;
            var def = ctx.CardDatabase.GetById(defId);
            return def != null && !def.IsMajorArcana;
        }

        /// <summary>
        /// Returns the minimum set of Spread cards needed to satisfy the alchemical formula
        /// for the given crucible slot, or an empty list when the formula is not wired/satisfied.
        /// Submitting fewer cards prevents discarding unneeded Spread cards.
        /// </summary>
        private static List<string> FindMinimumFireCards(
            GameContext ctx, CodexVariant codex, int slotIndex, IReadOnlyList<string> spreadIds)
        {
            if (ctx.AlchemicalValidator == null || ctx.CardDatabase == null)
                return new List<string>(); // validator not wired — submit empty, server will validate

            int pid   = ctx.ActivePlayerId;
            var slots = ctx.PublicView.Players[pid].CrucibleSlots;
            if (slotIndex >= slots.Count) return new List<string>();

            var formulaStr = slots[slotIndex].AlchemicalFormula;
            if (formulaStr == null) return new List<string>(); // formula not yet visible (Dormant)

            var cardMap = ctx.PublicView.CardInstanceToDefinition;

            // Resolve spread card definitions
            var defs = new List<(string id, CardDefinition def)>();
            foreach (var id in spreadIds)
            {
                if (!cardMap.TryGetValue(id, out var defId)) continue;
                var def = ctx.CardDatabase.GetById(defId);
                if (def != null) defs.Add((id, def));
            }

            // Try increasing subset sizes until validator says OK
            for (int size = 0; size <= defs.Count; size++)
            {
                var candidate    = new List<CardDefinition>();
                var candidateIds = new List<string>();
                for (int j = 0; j < size && j < defs.Count; j++)
                {
                    candidateIds.Add(defs[j].id);
                    candidate.Add(defs[j].def);
                }
                var (ok, _) = ctx.AlchemicalValidator.Validate(formulaStr, candidate);
                if (ok) return candidateIds;
            }

            return new List<string>(); // can't satisfy formula — submit empty; server will reject
        }

        /// <summary>
        /// Attempts to assemble a valid card set from Spread for the given codex slot.
        /// Returns the card IDs to submit, or null if no valid set was found.
        /// </summary>
        private static List<string>? FindActivationCards(
            GameContext ctx, CodexVariant codex, int slotIndex, IReadOnlyList<string> spreadIds)
        {
            if (ctx.CodexDatabase == null || ctx.CardDatabase == null || spreadIds.Count == 0)
                return null;

            var formula = ctx.CodexDatabase.GetFormula(codex, slotIndex);
            if (formula == null) return null;

            var cardMap = ctx.PublicView.CardInstanceToDefinition;
            var spreadDefs = new List<(string id, CardDefinition def)>();
            foreach (var id in spreadIds)
            {
                if (!cardMap.TryGetValue(id, out var defId)) continue;
                var def = ctx.CardDatabase.GetById(defId);
                if (def != null) spreadDefs.Add((id, def));
            }

            return ActivationCardSuggester.SuggestActivationCards(spreadDefs, formula, ctx.CardDatabase);
        }
    }
}
