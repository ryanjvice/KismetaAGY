using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
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
                ActionHint.Commune      => DecideCommune(context),
                ActionHint.SummerAction => DecideSummer(context),
                ActionHint.AutumnAction => DecideAutumn(context),
                _                       => new PassActionCommand(Slot.Index)
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

            // Temper if we have a Fired slot (from a prior round)
            if (player.StoneState == StoneState.Forging)
                return new TemperCommand(pid);

            // Fire the first Active slot if stone is on Mantle
            if (player.StoneState == StoneState.Tempering || player.StoneState == StoneState.Stasis)
            {
                for (int i = 0; i < player.CrucibleSlots.Count; i++)
                    if (player.CrucibleSlots[i].State == CrucibleCardState.Active)
                        return new FireStoneCommand(pid, i);
            }

            return new PassCrucibleActionCommand(pid);
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
