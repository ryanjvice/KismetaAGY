using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Rules;
using Kismeta.UI;

namespace Kismeta.UI.Controllers
{
    internal static class SummerActionHelpers
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

        public static Suit? GetUniformSuit(GameSession session, IReadOnlyList<string> ids)
        {
            if (ids.Count == 0) return null;
            Suit? suit = null;
            var db = session.Rules?.CardDatabase;
            if (db == null) return null;

            foreach (var id in ids)
            {
                var inst = session.GetCard(id);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def == null || def.Suit == Suit.None) return null;
                if (suit == null) suit = def.Suit;
                else if (suit != def.Suit) return null;
            }
            return suit;
        }

        public static ReagentType SuitToReagent(Suit suit) => suit switch
        {
            Suit.Wands => ReagentType.Sulphur,
            Suit.Cups => ReagentType.AquaRegia,
            Suit.Swords => ReagentType.Quicksilver,
            Suit.Pentacles => ReagentType.Vitriol,
            _ => ReagentType.Salt
        };

        public static string ReagentKey(ReagentType type) => type switch
        {
            ReagentType.Sulphur => "sulphur",
            ReagentType.Vitriol => "vitriol",
            ReagentType.AquaRegia => "aqua",
            ReagentType.Salt => "salt",
            ReagentType.Quicksilver => "quicksilver",
            _ => "salt"
        };

        public static ReagentType KeyToReagent(string key) => key switch
        {
            "sulphur" => ReagentType.Sulphur,
            "vitriol" => ReagentType.Vitriol,
            "aqua" => ReagentType.AquaRegia,
            "quicksilver" => ReagentType.Quicksilver,
            _ => ReagentType.Salt
        };

        public static List<string> CollectMinorCards(GameSession session, PlayerState player)
        {
            var ids = new List<string>();
            foreach (var id in player.Spread)
                if (TapSwapBindings.IsMinorArcana(session, id)) ids.Add(id);
            foreach (var id in player.Hand)
                if (TapSwapBindings.IsMinorArcana(session, id)) ids.Add(id);
            return ids;
        }

        public static List<string>? FindActivationCards(
            GameSession session, PlayerState player, int slotIndex)
        {
            var codexDb = session.Rules?.CodexDatabase;
            var db = session.Rules?.CardDatabase;
            if (codexDb == null || db == null || player.Spread.Count == 0)
                return null;

            var formula = codexDb.GetFormula(player.AssignedCodex, slotIndex);
            if (formula == null) return null;

            var spreadDefs = new List<(string id, CardDefinition def)>();
            foreach (var id in player.Spread)
            {
                var inst = session.GetCard(id);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def != null) spreadDefs.Add((id, def));
            }

            if (formula.FormulaType == CodexFormulaType.AnyThreePlanet)
            {
                var matching = new List<string>();
                foreach (var (id, def) in spreadDefs)
                    if (def.Planet == formula.RequiredPlanet)
                        matching.Add(id);
                return matching.Count >= 3 ? matching.GetRange(0, 3) : null;
            }

            if (formula.FormulaType == CodexFormulaType.RankSum)
            {
                var matching = new List<(string id, CardDefinition def)>();
                foreach (var (id, def) in spreadDefs)
                    if (def.Suit == formula.RequiredSuit)
                        matching.Add((id, def));
                if (matching.Count == 0) return null;

                matching.Sort((a, b) =>
                {
                    int ra = a.def.Rank == Rank.Ace ? 15 : (int)a.def.Rank;
                    int rb = b.def.Rank == Rank.Ace ? 15 : (int)b.def.Rank;
                    return rb.CompareTo(ra);
                });

                var chosen = new List<string>();
                int running = 0;
                foreach (var (id, def) in matching)
                {
                    chosen.Add(id);
                    running += def.Rank == Rank.Ace ? 15 : (int)def.Rank;
                    if (running >= formula.MinRankSum)
                        return chosen;
                }
            }

            return null;
        }
    }
}
