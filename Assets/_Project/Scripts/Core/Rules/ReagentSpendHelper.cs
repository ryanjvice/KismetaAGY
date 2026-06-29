using System;
using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>Shared reagent spending for contest ward fees and similar costs.</summary>
    public static class ReagentSpendHelper
    {
        public static readonly ReagentType[] PriorityOrder =
        {
            ReagentType.Salt, ReagentType.Sulphur, ReagentType.AquaRegia,
            ReagentType.Vitriol, ReagentType.Quicksilver
        };

        public static int TotalReagents(PlayerState player)
        {
            int total = 0;
            foreach (ReagentType rt in Enum.GetValues(typeof(ReagentType)))
                total += player.GetReagent(rt);
            return total;
        }

        public static List<(ReagentType Type, int Count)> BuildPriorityPayments(PlayerState player, int amount)
        {
            var payments = new List<(ReagentType, int)>();
            if (amount <= 0) return payments;

            int remaining = amount;
            foreach (var rt in PriorityOrder)
            {
                if (remaining <= 0) break;
                int have = player.GetReagent(rt);
                if (have <= 0) continue;
                int spend = Math.Min(have, remaining);
                payments.Add((rt, spend));
                remaining -= spend;
            }

            return payments;
        }

        public static bool TrySpend(PlayerState player, int amount,
            IReadOnlyList<(ReagentType Type, int Count)>? chosenPayments,
            out string? error)
        {
            if (amount <= 0)
            {
                error = null;
                return true;
            }

            if (TotalReagents(player) < amount)
            {
                error = $"Need {amount} reagent(s); only {TotalReagents(player)} available.";
                return false;
            }

            var payments = chosenPayments != null && chosenPayments.Count > 0
                ? new List<(ReagentType, int)>(chosenPayments)
                : BuildPriorityPayments(player, amount);

            int sum = 0;
            foreach (var (_, count) in payments) sum += count;
            if (sum != amount)
            {
                error = $"Must pay exactly {amount} reagent(s).";
                return false;
            }

            foreach (var (type, count) in payments)
            {
                if (player.GetReagent(type) < count)
                {
                    error = $"Insufficient {type}.";
                    return false;
                }
            }

            foreach (var (type, count) in payments)
                player.SpendReagent(type, count);

            error = null;
            return true;
        }
    }
}
