using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;

namespace Kismeta.Core.Rules
{
    /// <summary>Spread harvest bonuses for SpringRules and HarvestBreakdownService.</summary>
    public static class HarvestModifierService
    {
        public static int SpreadPassiveBonus(GameSession session, int playerId)
        {
            var db = session.Rules?.CardDatabase;
            if (db == null)
                return 0;

            var player = session.Players[playerId];
            var cosmic = session.Board.CosmicAgeSign;
            int bonus = 0;

            foreach (var cardId in player.Spread)
            {
                var inst = session.GetCard(cardId);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def == null || !SpreadHarvestEffectCatalog.IsAceV2Passive(def))
                    continue;
                if (!SpreadEffectPredicates.IsPassiveCosmicElementActive(def, cosmic))
                    continue;

                bonus += 2;
            }

            return bonus;
        }

        public static int HouseDoublingBonus(GameSession session, int playerId)
        {
            var player = session.Players[playerId];
            var cosmic = session.Board.CosmicAgeSign;
            var activeElements = GetActiveHarvestDoubleElements(session, player);
            if (activeElements.Count == 0)
                return 0;

            int extra = 0;
            foreach (var houseSign in player.AstralHouses)
            {
                var element = Correspondence.ElementFor(houseSign);
                if (!activeElements.Contains(element))
                    continue;

                extra += HarvestBreakdownService.AlignmentBonus(houseSign, cosmic);
            }

            return extra;
        }

        public static void AppendSpreadBonusRows(
            GameSession session,
            int playerId,
            List<HarvestSourceRow> rows)
        {
            var db = session.Rules?.CardDatabase;
            if (db == null)
                return;

            var player = session.Players[playerId];
            var cosmic = session.Board.CosmicAgeSign;

            foreach (var cardId in player.Spread)
            {
                var inst = session.GetCard(cardId);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def == null)
                    continue;

                if (SpreadHarvestEffectCatalog.IsAceV2Passive(def)
                    && SpreadEffectPredicates.IsPassiveCosmicElementActive(def, cosmic))
                {
                    rows.Add(new HarvestSourceRow(
                        $"Spread: {def.Name}",
                        "ace passive · cosmic element",
                        2,
                        HarvestAspectTier.None));
                }
            }

            var activeElements = GetActiveHarvestDoubleElements(session, player);
            if (activeElements.Count == 0)
                return;

            foreach (var houseSign in player.AstralHouses)
            {
                var element = Correspondence.ElementFor(houseSign);
                if (!activeElements.Contains(element))
                    continue;

                int pts = HarvestBreakdownService.AlignmentBonus(houseSign, cosmic);
                if (pts <= 0)
                    continue;

                rows.Add(new HarvestSourceRow(
                    $"Astral House doubled: {houseSign}",
                    $"rank 2 harvest · {element.ToString().ToLowerInvariant()}",
                    pts,
                    HarvestAspectTier.None));
            }
        }

        private static HashSet<Element> GetActiveHarvestDoubleElements(GameSession session, PlayerState player)
        {
            var db = session.Rules?.CardDatabase;
            var active = new HashSet<Element>();
            if (db == null)
                return active;

            foreach (var cardId in player.Spread)
            {
                var inst = session.GetCard(cardId);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def == null || !SpreadHarvestEffectCatalog.IsRank2HarvestDouble(def))
                    continue;
                if (!SpreadEffectPredicates.IsHarvestHouseElementActive(player, def))
                    continue;

                var element = SpreadEffectPredicates.HarvestDoubleElementFor(def);
                if (element != Element.None)
                    active.Add(element);
            }

            return active;
        }
    }
}
