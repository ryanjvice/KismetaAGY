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
            var allCards = AllPlayerCards(ctx);

            // Try to activate the first Dormant slot (needs 3 cards)
            for (int i = 0; i < player.CrucibleSlots.Count; i++)
            {
                var slot = player.CrucibleSlots[i];
                if (slot.State == CrucibleCardState.Dormant && allCards.Count >= 3)
                {
                    var payment = allCards.GetRange(0, 3);
                    return new ActivateCrucibleCommand(pid, i, payment);
                }
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

            // Try to craft Salt (needs any 3 cards)
            if (allCards.Count >= 3)
            {
                var payment = allCards.GetRange(0, 3);
                return new CraftReagentCommand(pid, ReagentType.Salt, payment);
            }

            return new PassCrucibleActionCommand(pid);
        }

        // ─── Autumn: Temper, Fire, or Pass ────────────────────────────────────────

        private IGameCommand DecideAutumn(GameContext ctx)
        {
            var pid    = Slot.Index;
            var player = ctx.PublicView.Players[pid];

            // Temper if stone is currently Forging (completed a full round in the Forge)
            if (player.StoneState == StoneState.Forging)
                return new TemperCommand(pid);

            // Fire the first Active slot — only if we have at least one non-Salt reagent
            // to cover the Alchemical Formula cost (exact cost unknown without the DB,
            // but zero non-Salt reagents guarantees failure, so skip it).
            if (player.StoneState == StoneState.Tempering)
            {
                bool hasFireReagents =
                    player.Reagents.TryGetValue(ReagentType.Sulphur,    out int su) && su > 0 ||
                    player.Reagents.TryGetValue(ReagentType.AquaRegia,  out int ar) && ar > 0 ||
                    player.Reagents.TryGetValue(ReagentType.Vitriol,    out int vi) && vi > 0 ||
                    player.Reagents.TryGetValue(ReagentType.Quicksilver,out int qk) && qk > 0;

                if (hasFireReagents)
                {
                    for (int i = 0; i < player.CrucibleSlots.Count; i++)
                        if (player.CrucibleSlots[i].State == CrucibleCardState.Active)
                            return new FireStoneCommand(pid, i);
                }
            }

            return new PassCrucibleActionCommand(pid);
        }

        // ─── Adept + Fate decisions ───────────────────────────────────────────────

        private IGameCommand DecideAdept(GameContext ctx)
        {
            if (ctx.PendingCardId == null) return new DeclineAdeptCommand(Slot.Index, "");

            var pid      = Slot.Index;
            var allCards = AllPlayerCards(ctx);
            var player   = ctx.PublicView.Players[pid];
            int adeptCount = player.Arcanum.Count; // Arcanum already contains any Fate cards too (transient)

            // Buy if we have space and can afford 3 cards for payment
            if (adeptCount < 2 && allCards.Count >= 3)
            {
                var payment = allCards.GetRange(0, 3);
                return new BuyAdeptCommand(pid, ctx.PendingCardId, payment);
            }

            return new DeclineAdeptCommand(pid, ctx.PendingCardId);
        }

        private IGameCommand DecideFateMoon(GameContext ctx)
        {
            // Keep the first 2 offered cards (already in context as a subset of Hand)
            // FateMoonDecisionCommand carries the list of card IDs to keep
            var keep = ctx.PrivateView.Hand.Count >= 2
                ? new List<string> { ctx.PrivateView.Hand[0], ctx.PrivateView.Hand[1] }
                : new List<string>(ctx.PrivateView.Hand);
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
    }
}
