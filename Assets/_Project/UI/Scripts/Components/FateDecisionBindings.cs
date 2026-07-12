using System;
using System.Collections.Generic;
using UnityEngine;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
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

        public static void PopulateMoonGiftCards(
            VisualElement host,
            GameSession session,
            int playerId,
            HashSet<string> selected,
            Action onChanged)
        {
            host.Clear();
            var db = session.Rules?.CardDatabase;
            var player = session.Players[playerId];

            void AddZoneCards(IReadOnlyList<string> ids, string zoneLabel)
            {
                foreach (var id in ids)
                {
                    var inst = session.GetCard(id);
                    var def  = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;
                    if (def == null || def.IsMajorArcana) continue;

                    bool sel = selected.Contains(id);
                    var chip = CardChipFactory.CreateFromDefinition(def, selected: sel);
                    chip.style.width = 36;
                    chip.style.height = 50;
                    chip.style.marginRight = 6;
                    chip.style.marginBottom = 6;
                    chip.tooltip = zoneLabel;
                    string captured = id;
                    chip.RegisterCallback<ClickEvent>(_ =>
                    {
                        if (selected.Contains(captured))
                            selected.Remove(captured);
                        else
                            selected.Add(captured);
                        onChanged();
                    });
                    host.Add(chip);
                }
            }

            AddZoneCards(player.Hand, "Hand");
            AddZoneCards(player.Spread, "Spread");
        }

        public static void PopulateMoonGiftReagents(
            VisualElement host,
            Dictionary<ReagentType, int> amounts,
            int playerId,
            GameSession session,
            Action onChanged)
        {
            host.Clear();
            var player = session.Players[playerId];

            foreach (var r in Reagents)
            {
                amounts.TryGetValue(r, out int count);
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 4;

                var label = new Label($"{r}") { style = { width = 90, fontSize = 10 } };
                row.Add(label);

                var minus = new Button(() =>
                {
                    if (count <= 0) return;
                    amounts[r] = count - 1;
                    if (amounts[r] == 0) amounts.Remove(r);
                    onChanged();
                }) { text = "−" };
                minus.style.width = 28;
                row.Add(minus);

                row.Add(new Label(count.ToString()) { style = { width = 24, unityTextAlign = TextAnchor.MiddleCenter } });

                var plus = new Button(() =>
                {
                    int owned = player.GetReagent(r);
                    int next = count + 1;
                    if (next > owned) return;
                    amounts[r] = next;
                    onChanged();
                }) { text = "+" };
                plus.style.width = 28;
                row.Add(plus);

                host.Add(row);
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

        public static void PopulateAlignmentCards(
            VisualElement host,
            GameSession session,
            int playerId,
            HashSet<string> selected,
            Action onChanged)
        {
            host.Clear();
            var db = session.Rules?.CardDatabase;
            foreach (var id in session.Players[playerId].Spread)
            {
                var inst = session.GetCard(id);
                var def  = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;
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
                    else
                        selected.Add(captured);
                    onChanged();
                });
                host.Add(chip);
            }
        }
    }
}
