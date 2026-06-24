using System;
using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.UI.Controllers;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Tap-to-select card pool for Summer craft / build actions.</summary>
    public static class SummerCardPickBindings
    {
        public static void RebuildPool(VisualElement root, GameSession session,
            IReadOnlyList<string> cardIds, HashSet<string> selected,
            Suit? filterSuit, Action<string> onToggle)
        {
            var pool = root.Q<VisualElement>("card-pool")
                ?? root.Q<VisualElement>("cost-cards");
            if (pool == null) return;

            pool.Clear();
            var db = session.Rules?.CardDatabase;
            if (db == null) return;

            var seen = new HashSet<string>();
            foreach (var cardId in cardIds)
            {
                if (!seen.Add(cardId)) continue;
                if (!TapSwapBindings.IsMinorArcana(session, cardId)) continue;

                var inst = session.GetCard(cardId);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def == null) continue;

                bool isSelected = selected.Contains(cardId);
                bool matches = filterSuit == null || def.Suit == filterSuit.Value
                    || (filterSuit == Suit.None);

                var chip = CardChipFactory.CreateFromDefinition(def, selected: isSelected);
                chip.userData = cardId;

                if (filterSuit != null && filterSuit != Suit.None && def.Suit != filterSuit.Value)
                    chip.style.opacity = 0.22f;

                if (matches || filterSuit == null)
                    WireChip(chip, () => onToggle(cardId));

                pool.Add(chip);
            }
        }

        static void WireChip(VisualElement chip, Action onTap)
        {
            chip.pickingMode = PickingMode.Position;
            chip.style.cursor = new StyleCursor(StyleKeyword.Auto);
            foreach (var child in chip.Children())
                child.pickingMode = PickingMode.Ignore;
            chip.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                UiMotion.PulseChip(chip);
                onTap();
            });
        }

        public static List<string> CollectMinorCards(GameSession session, PlayerState player)
        {
            var list = new List<string>();
            var seen = new HashSet<string>();
            foreach (var id in player.Spread)
                if (seen.Add(id) && TapSwapBindings.IsMinorArcana(session, id)) list.Add(id);
            foreach (var id in player.Hand)
                if (seen.Add(id) && TapSwapBindings.IsMinorArcana(session, id)) list.Add(id);
            return list;
        }
    }
}
