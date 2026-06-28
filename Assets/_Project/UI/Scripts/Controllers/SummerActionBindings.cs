using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.UI;
using Kismeta.UI.Components;

namespace Kismeta.UI.Controllers
{
    public static class SummerActionBindings
    {
        public static int ResolvePlayerId(GameSession session, CommandBridge bridge)
        {
            int pid = bridge.ActivePlayerId;
            if (pid >= 0 && pid < session.Players.Count)
                return pid;

            var hs = bridge.PendingController;
            if (hs != null && hs.Slot.Index >= 0 && hs.Slot.Index < session.Players.Count)
                return hs.Slot.Index;

            return -1;
        }

        public static ReagentType SuitToReagent(Suit suit) => Correspondence.ReagentFor(suit);

        public static int CraftEffectiveCost(GameSession session, PlayerState player, ReagentType reagent)
        {
            const int defaultCost = 3;
            var cosEffect = session.Board.CosmicEffect;
            var personal = player.PersonalCosmicEffects;

            bool saltCheap = cosEffect.SaltCostsTwo || personal.SaltCostsTwo;
            bool elemCheap = (cosEffect.CheapCraftReagent == reagent && cosEffect.CheapCraftSuit != Suit.None)
                          || (personal.CheapCraftReagent == reagent && personal.CheapCraftSuit != Suit.None);

            if (reagent == ReagentType.Salt && saltCheap) return 2;
            if (reagent != ReagentType.Salt && elemCheap) return 2;
            return defaultCost;
        }

        public static bool CanStillCraftReagent(GameSession session, PlayerState player)
        {
            var cards = SummerCardPickBindings.CollectMinorCards(session, player);
            int need = CraftEffectiveCost(session, player, ReagentType.Salt);
            return cards.Count >= need;
        }

        public static bool HasActivatableCrucible(PlayerState player)
        {
            foreach (var slot in player.CrucibleSlots)
                if (slot.State == CrucibleCardState.Dormant && slot.HasCoal)
                    return true;
            return false;
        }
    }
}
