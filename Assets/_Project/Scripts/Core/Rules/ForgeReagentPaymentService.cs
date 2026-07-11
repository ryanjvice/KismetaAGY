using System;
using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>
    /// Resolves alchemical reagent payment at Fire, including Queen V1 wild reagent pools.
    /// Wild inventory (e.g. Quicksilver when Queen of Swords is in Spread) may cover any cost shortfall.
    /// </summary>
    public static class ForgeReagentPaymentService
    {
        static readonly ReagentType[] CostTypes =
        {
            ReagentType.Sulphur, ReagentType.AquaRegia, ReagentType.Vitriol,
            ReagentType.Quicksilver, ReagentType.Salt
        };

        public static HashSet<ReagentType> GetWildReagentTypes(PlayerState player, GameSession session, ICardDatabase db)
        {
            var wild = new HashSet<ReagentType>();
            foreach (var cardId in player.Spread)
            {
                var inst = session.GetCard(cardId);
                if (inst == null) continue;
                var def = db.GetById(inst.DefinitionId);
                if (def == null || !SpreadForgeEffectCatalog.IsQueenV1WildReagent(def))
                    continue;
                wild.Add(SpreadForgeEffectCatalog.WildReagentForQueen(def));
            }
            return wild;
        }

        public static bool CanPayFireCost(PlayerState player, ReagentCost cost, IReadOnlyCollection<ReagentType> wildTypes)
        {
            int deficit = ComputeDeficit(player, cost);
            if (deficit <= 0) return true;
            return TotalWildAvailable(player, wildTypes) >= deficit;
        }

        public static void PayFireCost(PlayerState player, ReagentCost cost, IReadOnlyCollection<ReagentType> wildTypes)
        {
            int remainingWild = TotalWildAvailable(player, wildTypes);

            foreach (var type in CostTypes)
            {
                int need = GetCostAmount(cost, type);
                if (need <= 0) continue;

                int exact = Math.Min(need, player.GetReagent(type));
                if (exact > 0)
                    player.SpendReagent(type, exact);

                int shortfall = need - exact;
                if (shortfall <= 0) continue;

                remainingWild = SpendWildPool(player, wildTypes, shortfall, remainingWild);
            }
        }

        static int ComputeDeficit(PlayerState player, ReagentCost cost)
        {
            int deficit = 0;
            foreach (var type in CostTypes)
            {
                int need = GetCostAmount(cost, type);
                if (need <= 0) continue;
                deficit += Math.Max(0, need - player.GetReagent(type));
            }
            return deficit;
        }

        static int TotalWildAvailable(PlayerState player, IReadOnlyCollection<ReagentType> wildTypes)
        {
            int total = 0;
            foreach (var type in wildTypes)
                total += player.GetReagent(type);
            return total;
        }

        static int SpendWildPool(PlayerState player, IReadOnlyCollection<ReagentType> wildTypes,
            int amount, int remainingWildBudget)
        {
            int remaining = amount;
            foreach (var type in ReagentSpendHelper.PriorityOrder)
            {
                if (remaining <= 0) break;
                if (!ContainsWildType(wildTypes, type)) continue;

                int have = player.GetReagent(type);
                if (have <= 0) continue;

                int spend = Math.Min(have, remaining);
                player.SpendReagent(type, spend);
                remaining -= spend;
                remainingWildBudget -= spend;
            }

            if (remaining > 0)
                throw new InvalidOperationException("PayFireCost called without sufficient wild reagents.");

            return remainingWildBudget;
        }

        static bool ContainsWildType(IReadOnlyCollection<ReagentType> wildTypes, ReagentType type)
        {
            foreach (var wild in wildTypes)
                if (wild == type) return true;
            return false;
        }

        static int GetCostAmount(ReagentCost cost, ReagentType type) => type switch
        {
            ReagentType.Sulphur     => cost.Sulphur,
            ReagentType.AquaRegia   => cost.AquaRegia,
            ReagentType.Vitriol     => cost.Vitriol,
            ReagentType.Quicksilver => cost.Quicksilver,
            ReagentType.Salt        => cost.Salt,
            _                       => 0
        };
    }
}
