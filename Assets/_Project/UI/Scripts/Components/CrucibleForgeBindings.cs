using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Applies crucible forge progress art to the Autumn board-stage host.</summary>
    public static class CrucibleForgeBindings
    {
        const string CatalogResourcePath = "CrucibleForgeArt";

        static readonly (Suit suit, string classSuffix)[] ReagentSlots =
        {
            (Suit.Wands, "wands"),
            (Suit.Cups, "cups"),
            (Suit.Pentacles, "pentacles"),
            (Suit.Swords, "swords"),
        };

        static CrucibleForgeArt? _catalog;

        public static void ApplyForge(
            VisualElement? forgeStage,
            Label? statusLabel,
            PlayerState player,
            string statusText)
        {
            if (statusLabel != null)
                statusLabel.text = statusText;

            if (forgeStage == null)
                return;

            var catalog = ResolveCatalog();
            int current = player.StonePosition.Value;

            for (int i = 0; i < 8; i++)
            {
                var marker = forgeStage.Q<VisualElement>(className: $"stone-marker--{i}");
                if (marker == null)
                    continue;

                ApplyMarkerSprite(marker, catalog?.Get(new StonePosition(i)));
                marker.EnableInClassList("stone-marker--current", current == i);
            }

            var altar = forgeStage.Q<VisualElement>("stone-marker-altar");
            if (altar != null)
            {
                ApplyMarkerSprite(altar, catalog?.Get(StonePosition.Altar));
                altar.EnableInClassList("stone-marker--current", current == 8);
            }
        }

        public static void ApplyCauldronReagents(VisualElement? cauldronMini, PlayerState player)
        {
            if (cauldronMini == null)
                return;

            foreach (var (suit, classSuffix) in ReagentSlots)
            {
                var slot = cauldronMini.Q<VisualElement>(className: $"cauldron-mini__reagent--{classSuffix}");
                if (slot == null)
                    continue;

                bool lit = player.IsCauldronLit(suit);
                slot.EnableInClassList("cauldron-mini__reagent--lit", lit);

                if (lit)
                {
                    var sprite = UiArtBindings.Catalog?.CauldronFor(suit);
                    if (sprite != null)
                    {
                        slot.style.backgroundImage = new StyleBackground(sprite);
                        slot.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                    }
                }
                else
                {
                    slot.style.backgroundImage = StyleKeyword.None;
                }
            }
        }

        static void ApplyMarkerSprite(VisualElement marker, Sprite? sprite)
        {
            if (sprite != null)
            {
                marker.style.backgroundImage = new StyleBackground(sprite);
                marker.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                marker.style.display = DisplayStyle.Flex;
            }
            else
            {
                marker.style.backgroundImage = StyleKeyword.None;
                marker.style.display = DisplayStyle.None;
            }
        }

        static CrucibleForgeArt? ResolveCatalog()
        {
            if (_catalog == null)
                _catalog = Resources.Load<CrucibleForgeArt>(CatalogResourcePath);
            return _catalog;
        }
    }
}
