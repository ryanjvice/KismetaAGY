using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
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
                ActionHint.Commune        => DecideCommune(context),
                ActionHint.SummerAction   => DecideSummer(context),
                ActionHint.AutumnAction   => DecideAutumn(context),
                ActionHint.AdeptDecision  => DecideAdept(context),
                ActionHint.FateReagentChoice => new FateReagentChoiceCommand(Slot.Index, ReagentType.Salt),
                ActionHint.FateLoversChoice  => new FateLoversChoiceCommand(Slot.Index, false), // choose Reagent
                ActionHint.FateMoonDecision  => DecideFateMoon(context),
                _                            => new PassActionCommand(Slot.Index)
            };

            return Task.FromResult(action);
        }

        // ─── Spring: Commune ──────────────────────────────────────────────────────

        private IGameCommand DecideCommune(GameContext ctx)
        {
            var pid = Slot.Index;
            var spread  = ctx.PublicView.Players[pid].Spread;
            var allCards = new List<string>(spread.Count + ctx.PrivateView.Hand.Count);
            foreach (var id in spread)                allCards.Add(id);
            foreach (var id in ctx.PrivateView.Hand)  allCards.Add(id);
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
            if (player.UnplacedAstralHouses > 0 && player.CurrentSign != ZodiacSign.None)
            {
                var sign = player.CurrentSign;
                bool signFree = true;
                foreach (var p in ctx.PublicView.Players)
                    if (p.PlayerId != pid && p.AstralHouses.Contains(sign))
                    { signFree = false; break; }

                if (signFree && !player.AstralHouses.Contains(sign))
                {
                    // Find 2 cards matching the sign's planet in Spread
                    var planet = Correspondence.PlanetFor(sign);
                    // AI can't look up card definitions here without db — pass based on Spread labels
                    // Simple fallback: skip if we can't easily validate
                    // (Full validation happens in AstralHouseService)
                }
            }

            // Try to craft Salt (needs any 3 cards from Spread)
            if (spreadIds.Count >= 3)
            {
                var payment = spreadIds.GetRange(0, 3);
                return new CraftReagentCommand(pid, ReagentType.Salt, payment);
            }

            return new PassCrucibleActionCommand(pid);
        }

        // ─── Autumn: Temper, Fire, or Pass ────────────────────────────────────────

        private IGameCommand DecideAutumn(GameContext ctx)
        {
            var pid    = Slot.Index;
            var player = ctx.PublicView.Players[pid];

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
            // AI passes empty alignment cards — the validator allows it when no validator is wired in tests;
            // in full game the Fire will fail gracefully if cards are insufficient, and AI will Pass.
            if (player.StoneState == StoneState.Tempering && player.StonePosition.IsMantle)
            {
                bool hasFireReagents =
                    player.Reagents.TryGetValue(ReagentType.Sulphur,     out int su) && su > 0 ||
                    player.Reagents.TryGetValue(ReagentType.AquaRegia,   out int ar) && ar > 0 ||
                    player.Reagents.TryGetValue(ReagentType.Vitriol,     out int vi) && vi > 0 ||
                    player.Reagents.TryGetValue(ReagentType.Quicksilver, out int qk) && qk > 0;

                if (hasFireReagents)
                {
                    // Build a best-effort alignment card list from Spread
                    var spreadCards = new System.Collections.Generic.List<string>(player.Spread);
                    for (int i = 0; i < player.CrucibleSlots.Count; i++)
                        if (player.CrucibleSlots[i].State == CrucibleCardState.Active)
                            return new FireStoneCommand(pid, i, spreadCards);
                }
            }

            return new PassCrucibleActionCommand(pid);
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

        private static List<string> AllPlayerCards(GameContext ctx)
        {
            int pid  = ctx.ActivePlayerId;
            var spread = ctx.PublicView.Players[pid].Spread;
            var list = new List<string>(spread.Count + ctx.PrivateView.Hand.Count);
            foreach (var id in spread)                list.Add(id);
            foreach (var id in ctx.PrivateView.Hand)  list.Add(id);
            return list;
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

            // Resolve all spread card definitions.
            var cardMap = ctx.PublicView.CardInstanceToDefinition;
            var spreadDefs = new List<(string id, CardDefinition def)>();
            foreach (var id in spreadIds)
            {
                if (!cardMap.TryGetValue(id, out var defId)) continue;
                var def = ctx.CardDatabase.GetById(defId);
                if (def != null) spreadDefs.Add((id, def));
            }

            if (formula.FormulaType == CodexFormulaType.AnyThreePlanet)
            {
                // Find exactly 3 cards matching the required planet.
                var matching = new List<string>();
                foreach (var (id, def) in spreadDefs)
                    if (def.Planet == formula.RequiredPlanet)
                        matching.Add(id);

                return matching.Count >= 3 ? matching.GetRange(0, 3) : null;
            }
            else if (formula.FormulaType == CodexFormulaType.RankSum)
            {
                // Find cards of the required suit with combined rank sum >= minRankSum.
                var matching = new List<(string id, CardDefinition def)>();
                foreach (var (id, def) in spreadDefs)
                    if (def.Suit == formula.RequiredSuit)
                        matching.Add((id, def));

                if (matching.Count == 0) return null;

                // Greedy: sort by rank descending (Aces as 15), pick until sum >= threshold.
                matching.Sort((a, b) =>
                {
                    int ra = a.def.Rank == Rank.Ace ? 15 : (int)a.def.Rank;
                    int rb = b.def.Rank == Rank.Ace ? 15 : (int)b.def.Rank;
                    return rb.CompareTo(ra);
                });

                var chosen  = new List<string>();
                int running = 0;
                foreach (var (id, def) in matching)
                {
                    chosen.Add(id);
                    running += def.Rank == Rank.Ace ? 15 : (int)def.Rank;
                    if (running >= formula.MinRankSum)
                        return chosen;
                }

                return null; // Couldn't reach the threshold.
            }

            return null;
        }
    }
}
