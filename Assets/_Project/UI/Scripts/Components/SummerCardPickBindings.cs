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

            foreach (var cardId in cardIds)
            {
                if (!TapSwapBindings.IsMinorArcana(session, cardId)) continue;

                var inst = session.GetCard(cardId);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def == null) continue;

                bool isSelected = selected.Contains(cardId);
                bool matches = filterSuit == null || def.Suit == filterSuit.Value
                    || (filterSuit == Suit.None);

                var chip = CardChipFactory.Create(def.Rank.ToString(), def.Suit, selected: isSelected);
                chip.userData = cardId;

                if (filterSuit != null && filterSuit != Suit.None && def.Suit != filterSuit.Value)
                    chip.style.opacity = 0.22f;

                if (matches || filterSuit == null)
                    chip.RegisterCallback<ClickEvent>(_ => onToggle(cardId));

                pool.Add(chip);
            }
        }

        public static List<string> CollectMinorCards(GameSession session, PlayerState player)
        {
            var list = new List<string>();
            foreach (var id in player.Spread)
                if (TapSwapBindings.IsMinorArcana(session, id)) list.Add(id);
            foreach (var id in player.Hand)
                if (TapSwapBindings.IsMinorArcana(session, id)) list.Add(id);
            return list;
        }
    }
}
