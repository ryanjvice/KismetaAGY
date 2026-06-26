using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.Core.Views;

namespace Kismeta.UI.Components
{
    /// <summary>Shared crucible formula progress formatting for hub and activate UI.</summary>
    internal static class CrucibleFormulaDisplay
    {
        public static List<(string id, CardDefinition def)> CollectSpreadCards(
            GameSession session,
            PublicPlayerView player)
        {
            var db = session.Rules?.CardDatabase;
            if (db == null) return new List<(string id, CardDefinition def)>();

            var spreadCards = new List<(string id, CardDefinition def)>();
            foreach (var id in player.Spread)
            {
                var inst = session.GetCard(id);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def != null)
                    spreadCards.Add((id, def));
            }

            return spreadCards;
        }

        public static List<(string id, CardDefinition def)> CollectSpreadCards(
            GameSession session,
            PlayerState player)
        {
            var db = session.Rules?.CardDatabase;
            if (db == null) return new List<(string id, CardDefinition def)>();

            var spreadCards = new List<(string id, CardDefinition def)>();
            foreach (var id in player.Spread)
            {
                var inst = session.GetCard(id);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def != null)
                    spreadCards.Add((id, def));
            }

            return spreadCards;
        }

        public static bool TryFormulaForSuit(
            GameSession session,
            PublicPlayerView player,
            Suit suit,
            out CodexFormulaDefinition? formula)
        {
            formula = null;
            if (!CauldronHubBindings.TrySlotIndexForSuit(session, player.PlayerId, suit, out int slotIndex))
                return false;

            var codexDb = session.Rules?.CodexDatabase;
            if (codexDb == null) return false;

            formula = codexDb.GetFormula(player.AssignedCodex, slotIndex);
            return formula != null;
        }

        public static (string countText, string reqText, bool ready) FormatProgress(
            CodexFormulaDefinition formula,
            List<(string id, CardDefinition def)> spreadCards)
        {
            if (formula.FormulaType == CodexFormulaType.AnyThreePlanet)
            {
                int count = CountMatchingPlanet(spreadCards, formula.RequiredPlanet);
                return ($"{count}/3", formula.RequiredPlanet.ToString(), count >= 3);
            }

            int best = ActivationCardSuggester.BestRankSumFromSpread(spreadCards, formula.RequiredSuit);
            return ($"{best}/{formula.MinRankSum}", formula.RequiredSuit.ToString(), best >= formula.MinRankSum);
        }

        public static (string text, bool ready) FormatProgressLine(
            CodexFormulaDefinition formula,
            List<(string id, CardDefinition def)> spreadCards)
        {
            var (countText, reqText, ready) = FormatProgress(formula, spreadCards);
            var parts = countText.Split('/');
            if (parts.Length == 2)
                return ($"{parts[0]} / {parts[1]} {reqText}", ready);

            return ($"{countText} {reqText}", ready);
        }

        static int CountMatchingPlanet(
            List<(string id, CardDefinition def)> spreadCards,
            Planet planet)
        {
            int count = 0;
            foreach (var (_, def) in spreadCards)
            {
                if (def.Planet == planet)
                    count++;
            }

            return count;
        }
    }
}
