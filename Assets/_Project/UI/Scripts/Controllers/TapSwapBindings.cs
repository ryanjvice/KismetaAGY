using System;
using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.UI.Components;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    /// <summary>Shared tap-to-swap card zone UI for Commune and Winter Unlock screens.</summary>
    public static class TapSwapBindings
    {
        public static bool IsMinorArcana(GameSession session, string instanceId)
        {
            var inst = session.GetCard(instanceId);
            var def = inst != null ? session.Rules?.CardDatabase?.GetById(inst.DefinitionId) : null;
            return def != null && !def.IsMajorArcana;
        }

        public static int CardAlignPoints(GameSession session, string instanceId, ZodiacSign referenceSign)
        {
            if (referenceSign == ZodiacSign.None) return 0;
            var inst = session.GetCard(instanceId);
            var def = inst != null ? session.Rules?.CardDatabase?.GetById(inst.DefinitionId) : null;
            if (def == null) return 0;

            var refPlanet = Correspondence.PlanetFor(referenceSign);
            if (def.Planet != Planet.None && def.Planet == refPlanet) return 2;

            if (def.Suit != Suit.None)
            {
                var cardElement = Correspondence.ElementFor(def.Suit);
                var refElement = Correspondence.ElementFor(referenceSign);
                if (cardElement != Element.None && cardElement == refElement) return 1;
            }

            return 0;
        }

        public static VisualElement BuildChip(GameSession session, string cardId, ZodiacSign referenceSign,
            Action<string, bool> onTap, bool fromSpread)
        {
            var inst = session.GetCard(cardId);
            var db = session.Rules?.CardDatabase;
            var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;
            if (def == null) return new VisualElement();

            int align = CardAlignPoints(session, cardId, referenceSign);
            var chip = CardChipFactory.Create(def.Rank.ToString(), def.Suit, aligned: align > 0);
            chip.userData = cardId;
            chip.pickingMode = PickingMode.Position;
            chip.style.cursor = new StyleCursor(StyleKeyword.Auto);

            if (align > 0)
            {
                var tag = new Label("+" + align);
                tag.AddToClassList("card-chip__tag");
                tag.pickingMode = PickingMode.Ignore;
                chip.Add(tag);
            }

            foreach (var child in chip.Children())
                child.pickingMode = PickingMode.Ignore;

            chip.RegisterCallback<ClickEvent>(_ =>
            {
                UiMotion.PulseChip(chip);
                onTap(cardId, fromSpread);
            });
            return chip;
        }

        public static void RebuildZones(VisualElement root, GameSession session,
            IReadOnlyList<string> spreadIds, IReadOnlyList<string> handIds,
            ZodiacSign referenceSign, Action<string, bool> onTap)
        {
            RebuildZone(root, "spread-cards", spreadIds, session, referenceSign, onTap, fromSpread: true);
            RebuildZone(root, "hand-cards", handIds, session, referenceSign, onTap, fromSpread: false);

            SetLabel(root, "spread-count", spreadIds.Count.ToString());
            SetLabel(root, "hand-count", handIds.Count.ToString());
            UpdateReadout(root, session, spreadIds, handIds, referenceSign);
        }

        static void RebuildZone(VisualElement root, string containerName, IReadOnlyList<string> cardIds,
            GameSession session, ZodiacSign referenceSign, Action<string, bool> onTap, bool fromSpread)
        {
            var zone = root.Q<VisualElement>(containerName);
            if (zone == null) return;
            zone.Clear();
            foreach (var id in cardIds)
                zone.Add(BuildChip(session, id, referenceSign, onTap, fromSpread));
        }

        public static void UpdateReadout(VisualElement root, GameSession session,
            IReadOnlyList<string> spreadIds, IReadOnlyList<string> handIds, ZodiacSign referenceSign)
        {
            int align = 0;
            foreach (var id in spreadIds)
                align += CardAlignPoints(session, id, referenceSign);

            SetLabel(root, "spread-align", "+" + align);
            SetLabel(root, "hand-safe", handIds.Count.ToString());
            SetLabel(root, "verdict", align >= 4 ? "strong" : align >= 2 ? "balanced" : "guarded");
        }

        static void SetLabel(VisualElement root, string name, string text)
        {
            var lbl = root.Q<Label>(name);
            if (lbl != null) lbl.text = text;
        }
    }
}
