using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    public static class CrucibleCodexRows
    {
        public static void Populate(VisualElement? root, GameSession session, int playerId)
        {
            if (root == null || playerId < 0 || playerId >= session.Players.Count)
                return;

            var player = session.Players[playerId];
            var codexDb = session.Rules?.CodexDatabase;
            var cardDb = session.Rules?.CardDatabase;

            var subtitle = root.Q<Label>("crucible-codex-subtitle");
            if (subtitle != null)
            {
                subtitle.text = player.AssignedCodex != CodexVariant.None
                    ? $"Codex {player.AssignedCodex} · activation & fire reference"
                    : "Activation & fire reference";
            }

            var activationHost = root.Q<VisualElement>("activation-section");
            var alchemicalHost = root.Q<VisualElement>("alchemical-section");
            if (activationHost == null || alchemicalHost == null)
                return;

            activationHost.Clear();
            alchemicalHost.Clear();

            if (player.AssignedCodex == CodexVariant.None || codexDb == null)
            {
                activationHost.Add(EmptyLabel("Codex not assigned yet."));
                alchemicalHost.Add(EmptyLabel("Activate a crucible card to reveal its alchemical formula."));
                return;
            }

            var spreadCards = CrucibleFormulaDisplay.CollectSpreadCards(session, player);
            var formulas = codexDb.GetEntriesForCodex(player.AssignedCodex);

            foreach (var formula in formulas)
            {
                if (formula.SlotIndex < 0 || formula.SlotIndex >= player.CrucibleSlots.Count)
                    continue;

                var slot = player.CrucibleSlots[formula.SlotIndex];
                activationHost.Add(BuildActivationRow(formula, slot.State, spreadCards));
            }

            bool anyAlchemical = false;
            for (int i = 0; i < player.CrucibleSlots.Count; i++)
            {
                var slot = player.CrucibleSlots[i];
                if (slot.State < CrucibleCardState.Active)
                    continue;

                if (cardDb == null)
                    continue;

                var inst = session.GetCard(slot.CardInstanceId);
                var def = inst != null ? cardDb.GetById(inst.DefinitionId) : null;
                if (def == null)
                    continue;

                alchemicalHost.Add(BuildAlchemicalRow(def, i, slot.State));
                anyAlchemical = true;
            }

            if (!anyAlchemical)
                alchemicalHost.Add(EmptyLabel("Activate a crucible card to reveal its alchemical formula."));
        }

        static VisualElement BuildActivationRow(
            CodexFormulaDefinition formula,
            CrucibleCardState state,
            List<(string id, CardDefinition def)> spreadCards)
        {
            var row = new VisualElement();
            row.AddToClassList("crucible-codex__row");

            var header = new VisualElement();
            header.AddToClassList("crucible-codex__row-header");

            var colorKey = ColorKey(formula.Cauldron);
            var pill = new VisualElement();
            pill.AddToClassList("crucible-codex__slot-pill");
            pill.AddToClassList($"crucible-codex__slot-pill--{colorKey}");
            var pillLabel = new Label(((char)('A' + formula.SlotIndex)).ToString());
            pillLabel.AddToClassList("crucible-codex__slot-pill-label");
            pill.Add(pillLabel);

            var title = new Label(formula.DisplayName);
            title.AddToClassList("crucible-codex__row-title");

            var badge = new VisualElement();
            badge.AddToClassList("crucible-codex__state-badge");
            var badgeLabel = new Label(StateLabel(state));
            badgeLabel.AddToClassList("crucible-codex__state-badge-label");
            badge.Add(badgeLabel);

            header.Add(pill);
            header.Add(title);
            header.Add(badge);

            var req = new Label(FormatRequirementDetail(formula));
            req.AddToClassList("crucible-codex__req");

            row.Add(header);
            row.Add(req);

            if (state == CrucibleCardState.Dormant)
            {
                var (progressText, ready) = CrucibleFormulaDisplay.FormatProgressLine(formula, spreadCards);
                var progress = new Label(progressText);
                progress.AddToClassList("crucible-codex__progress");
                if (ready)
                    progress.AddToClassList("crucible-codex__progress--ready");
                row.Add(progress);
            }

            return row;
        }

        static VisualElement BuildAlchemicalRow(CardDefinition def, int slotIndex, CrucibleCardState state)
        {
            var row = new VisualElement();
            row.AddToClassList("crucible-codex__row");

            var header = new VisualElement();
            header.AddToClassList("crucible-codex__row-header");

            var title = new Label($"Slot {(char)('A' + slotIndex)} · {RomanNumerals.ToArcanaLabel(def.ArcanaNumber)} · {def.Name}");
            title.AddToClassList("crucible-codex__row-title");

            var badge = new VisualElement();
            badge.AddToClassList("crucible-codex__state-badge");
            var badgeLabel = new Label(StateLabel(state));
            badgeLabel.AddToClassList("crucible-codex__state-badge-label");
            badge.Add(badgeLabel);

            header.Add(title);
            header.Add(badge);

            var formula = new Label(string.IsNullOrWhiteSpace(def.AlchemicalFormula) ? "—" : def.AlchemicalFormula);
            formula.AddToClassList("crucible-codex__formula");

            var costHost = new VisualElement();
            costHost.AddToClassList("crucible-codex__cost");
            AutumnBoardBindings.PopulateReagentCost(costHost, def.AlchemicalCost);

            row.Add(header);
            row.Add(formula);
            row.Add(costHost);
            return row;
        }

        static Label EmptyLabel(string text)
        {
            var lbl = new Label(text);
            lbl.AddToClassList("crucible-codex__empty");
            return lbl;
        }

        static string FormatRequirementDetail(CodexFormulaDefinition formula)
        {
            var reagent = Correspondence.ReagentFor(formula.CauldronSuit);
            var reagentName = CraftReagentPanelBindings.ReagentDisplayName(reagent).ToUpperInvariant();
            return $"{formula.Cauldron} cauldron · {reagentName}";
        }

        static string StateLabel(CrucibleCardState state) => state switch
        {
            CrucibleCardState.Dormant => "dormant",
            CrucibleCardState.Active => "active",
            CrucibleCardState.Fired => "fired",
            CrucibleCardState.Arrested => "arrested",
            CrucibleCardState.Discarded => "discarded",
            _ => state.ToString().ToLowerInvariant()
        };

        static string ColorKey(string cauldron) =>
            string.IsNullOrEmpty(cauldron) ? "red" : cauldron.ToLowerInvariant();
    }
}
