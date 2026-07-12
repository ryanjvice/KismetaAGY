using System;
using System.Collections.Generic;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    public static class EmperorProtectionBindings
    {
        public const int RequiredCount = 2;

        public static void PopulateCards(
            VisualElement host,
            GameSession session,
            int playerId,
            HashSet<string> selected,
            Action onChanged)
        {
            host.Clear();
            var db = session.Rules?.CardDatabase;
            if (db == null) return;

            var player = session.Players[playerId];
            bool resonant = AdeptAttunement.IsEmperorResonant(session, player);

            void AddZoneCards(IReadOnlyList<string> ids, string zoneLabel)
            {
                foreach (var id in ids)
                {
                    var inst = session.GetCard(id);
                    var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                    if (def == null || def.IsMajorArcana) continue;

                    bool sel = selected.Contains(id);
                    var chip = CardChipFactory.CreateFromDefinition(def, selected: sel);
                    chip.style.width = 36;
                    chip.style.height = 50;
                    chip.style.marginRight = 6;
                    chip.style.marginBottom = 6;
                    chip.tooltip = zoneLabel;
                    string captured = id;
                    CardChipFactory.WireTap(chip, () =>
                    {
                        if (selected.Contains(captured))
                            selected.Remove(captured);
                        else if (selected.Count < RequiredCount)
                            selected.Add(captured);
                        onChanged();
                    });
                    host.Add(chip);
                }
            }

            AddZoneCards(player.Spread, "Spread");
            if (resonant)
                AddZoneCards(player.Hand, "Hand");
        }
    }
}
