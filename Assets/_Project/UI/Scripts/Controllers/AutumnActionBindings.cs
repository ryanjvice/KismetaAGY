using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.UI.Components;

namespace Kismeta.UI.Controllers
{
    public static class AutumnActionBindings
    {
        public const int StasisSaltCost = 2;

        public static bool CanFire(GameSession session, PlayerState player) =>
            GetFireableSlotIndices(session, player).Count > 0;

        public static List<int> GetFireableSlotIndices(GameSession session, PlayerState player)
        {
            var list = new List<int>();
            if (!player.StonePosition.IsMantle) return list;

            var db = session.Rules?.CardDatabase;
            if (db == null) return list;

            for (int i = 0; i < player.CrucibleSlots.Count; i++)
            {
                var slot = player.CrucibleSlots[i];
                if (slot.State != CrucibleCardState.Active) continue;
                var inst = session.GetCard(slot.CardInstanceId);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def != null && CanPayCost(player, def.AlchemicalCost))
                    list.Add(i);
            }
            return list;
        }

        public static bool CanTemper(GameSession session, PlayerState player) =>
            player.StoneState == StoneState.Forging
            && player.StonePosition.IsForge
            && !player.ReturnedFromStasisThisRound
            && FindEligibleTemperSlotIndex(session, player) >= 0;

        public static int FindEligibleTemperSlotIndex(GameSession session, PlayerState player)
        {
            for (int i = 0; i < player.CrucibleSlots.Count; i++)
            {
                var s = player.CrucibleSlots[i];
                if (s.State == CrucibleCardState.Fired
                    && s.FiredAtRound >= 0
                    && s.FiredAtRound < session.Board.RoundNumber)
                    return i;
            }
            return -1;
        }

        public static List<int> GetEligibleTemperSlotIndices(GameSession session, PlayerState player)
        {
            var list = new List<int>();
            for (int i = 0; i < player.CrucibleSlots.Count; i++)
            {
                var s = player.CrucibleSlots[i];
                if (s.State == CrucibleCardState.Fired
                    && s.FiredAtRound >= 0
                    && s.FiredAtRound < session.Board.RoundNumber)
                    list.Add(i);
            }
            return list;
        }

        public static bool IsWinningTemper(PlayerState player) =>
            player.StonePosition.Advance().IsAltar;

        public static bool CanLeaveStasis(PlayerState player) =>
            player.StoneState == StoneState.Stasis;

        public static bool HasSaltForStasis(PlayerState player) =>
            player.GetReagent(ReagentType.Salt) >= StasisSaltCost;

        public static bool ForgeSpotOccupied(GameSession session, int playerId)
        {
            var player = session.Players[playerId];
            foreach (var p in session.Players)
            {
                if (p.PlayerId == playerId) continue;
                if (p.StoneState == StoneState.Forging
                    && p.StonePosition.Value == player.StonePosition.Value)
                    return true;
            }
            return false;
        }

        public static bool HasOpposeTargets(GameSession session, int playerId)
        {
            for (int i = 0; i < session.Players.Count; i++)
                if (ContestBindings.CanTargetForOpposition(session, playerId, i))
                    return true;
            return false;
        }

        public static bool CanPayCost(PlayerState player, ReagentCost cost) =>
            player.GetReagent(ReagentType.Sulphur) >= cost.Sulphur
            && player.GetReagent(ReagentType.AquaRegia) >= cost.AquaRegia
            && player.GetReagent(ReagentType.Vitriol) >= cost.Vitriol
            && player.GetReagent(ReagentType.Quicksilver) >= cost.Quicksilver
            && player.GetReagent(ReagentType.Salt) >= cost.Salt;

        public static string FormatReagentCost(ReagentCost cost)
        {
            if (cost.Total == 0) return "no reagent cost";
            var parts = new List<string>();
            if (cost.Salt > 0) parts.Add($"{cost.Salt} Salt");
            if (cost.Sulphur > 0) parts.Add($"{cost.Sulphur} Sulphur");
            if (cost.Vitriol > 0) parts.Add($"{cost.Vitriol} Vitriol");
            if (cost.Quicksilver > 0) parts.Add($"{cost.Quicksilver} Quicksilver");
            if (cost.AquaRegia > 0) parts.Add($"{cost.AquaRegia} Aqua Regia");
            return string.Join(", ", parts);
        }

        public static string StoneStatusLabel(PlayerState player) =>
            $"stone · {player.StoneState.ToString().ToLowerInvariant()} · {player.StonePosition}";
    }
}
