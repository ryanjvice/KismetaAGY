using System;
using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.UI.Components;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    /// <summary>Tap-to-select card pool for Summer craft / build actions.</summary>
    public static class SummerCardPickBindings
    {
        public static void RebuildPool(
            VisualElement pool,
            GameSession session,
            IReadOnlyList<string> cardIds,
            HashSet<string> selected,
            Suit? filterSuit,
            Action<string> onToggle)
        {
            if (pool == null) return;
            pool.Clear();
            var db = session.Rules?.CardDatabase;
            if (db == null) return;

            foreach (var id in cardIds)
            {
                var inst = session.GetCard(id);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def == null) continue;

                bool sel = selected.Contains(id);
                bool matches = filterSuit == null || def.Suit == filterSuit.Value;
                var chip = CardChipFactory.Create(def.Rank.ToString(), def.Suit, selected: sel);
                chip.userData = id;

                if (!matches)
                    chip.style.opacity = 0.22f;

                chip.RegisterCallback<ClickEvent>(_ =>
                {
                    if (!matches) return;
                    onToggle(id);
                });
                pool.Add(chip);
            }
        }
    }
}
