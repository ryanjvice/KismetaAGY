using System;
using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    public static class FateDecisionBindings
    {
        static readonly ReagentType[] Reagents =
        {
            ReagentType.Salt, ReagentType.Sulphur, ReagentType.AquaRegia,
            ReagentType.Vitriol, ReagentType.Quicksilver
        };

        public static void PopulateMoonCards(
            VisualElement host,
            GameSession session,
            HashSet<string> selected,
            Action onChanged)
        {
            host.Clear();
            var db = session.Rules?.CardDatabase;
            foreach (var id in session.Board.FateMoonDrawnCardIds)
            {
                var inst = session.GetCard(id);
                var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;
                if (def == null) continue;

                bool sel = selected.Contains(id);
                var chip = CardChipFactory.CreateFromDefinition(def, selected: sel);
                chip.style.width = 36;
                chip.style.height = 50;
                chip.style.marginRight = 6;
                chip.style.marginBottom = 6;
                string captured = id;
                chip.RegisterCallback<ClickEvent>(_ =>
                {
                    if (selected.Contains(captured))
                        selected.Remove(captured);
                    else if (selected.Count < 2)
                        selected.Add(captured);
                    onChanged();
                });
                host.Add(chip);
            }
        }

        public static void PopulateReagentButtons(
            VisualElement host,
            Action<ReagentType> onPick)
        {
            host.Clear();
            foreach (var r in Reagents)
            {
                var type = r;
                var btn = new Button(() => onPick(type)) { text = $"Take 1 {r}" };
                btn.AddToClassList("fate-reagent-btn");
                host.Add(btn);
            }
        }

        public static void PopulateLoversTargetButtons(
            VisualElement host,
            GameSession session,
            int drawerId,
            Action<int> onPick)
        {
            host.Clear();
            foreach (var p in session.Players)
            {
                if (p.PlayerId == drawerId) continue;
                int targetId = p.PlayerId;
                var btn = new Button(() => onPick(targetId))
                {
                    text = $"{PlayerUiNames.ShortName(targetId)} chooses my reward"
                };
                btn.AddToClassList("btn");
                btn.AddToClassList("btn--secondary");
                btn.style.marginBottom = 6;
                host.Add(btn);
            }
        }
    }
}
