using System.Collections.Generic;
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
        const int MaxStasisSlots = 4;

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

            for (int i = 0; i < 8; i++)
            {
                var marker = FindTrackMarker(forgeStage, i);
                if (marker == null)
                    continue;

                ApplyMarkerSprite(marker, catalog?.Get(new StonePosition(i)));
            }

            var altar = forgeStage.Q<VisualElement>("stone-marker-altar");
            if (altar != null)
                ApplyMarkerSprite(altar, catalog?.Get(StonePosition.Altar));
        }

        public static void ApplyAllPlayerStones(
            VisualElement? forgeStage,
            VisualElement? stasisRow,
            GameSession session,
            int localPlayerId)
        {
            if (forgeStage == null || stasisRow == null)
                return;

            var art = UiArtBindings.Catalog;
            var stonesHost = forgeStage.Q<VisualElement>("player-stones");
            if (stonesHost == null)
                return;

            stonesHost.Clear();
            ResetStasisSlots(stasisRow);

            var trackPlayers = new List<(int playerId, PlayerState player)>();
            var countByPosition = new Dictionary<int, int>();

            for (int i = 0; i < session.Players.Count; i++)
            {
                var player = session.Players[i];

                if (player.StoneState == StoneState.Stasis)
                {
                    var sprite = art?.PlayerStoneFor(player.Color);
                    if (sprite != null)
                        ApplyStasisStone(stasisRow, i, sprite, i == localPlayerId);
                    continue;
                }

                int pos = player.StonePosition.Value;
                trackPlayers.Add((i, player));
                countByPosition[pos] = countByPosition.GetValueOrDefault(pos) + 1;
            }

            var indexByPosition = new Dictionary<int, int>();

            foreach (var (playerId, player) in trackPlayers)
            {
                var sprite = art?.PlayerStoneFor(player.Color);
                if (sprite == null)
                    continue;

                int pos = player.StonePosition.Value;
                int index = indexByPosition.GetValueOrDefault(pos);
                indexByPosition[pos] = index + 1;
                int count = countByPosition[pos];
                bool isLocal = playerId == localPlayerId;

                var stone = new VisualElement();
                stone.AddToClassList("player-stone");
                stone.AddToClassList("forge-pos");
                stone.AddToClassList(pos >= 8 ? "forge-pos--altar" : $"forge-pos--{pos}");
                if (isLocal)
                    stone.AddToClassList("player-stone--local");

                if (IsMantlePosition(pos) && count > 1)
                    ApplyMantleRadialOffset(stone, pos, index, count);
                else if (index > 0)
                    stone.AddToClassList($"player-stone--stack-{index}");

                ApplyMarkerSprite(stone, sprite);
                stonesHost.Add(stone);
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

        static bool IsMantlePosition(int position) =>
            position >= 0 && position < 8 && position % 2 == 0;

        static float MantleRadialCenterAngle(int position) => position switch
        {
            0 => 225f,
            2 => 135f,
            4 => 45f,
            6 => 315f,
            _ => 0f
        };

        const float MantleRadialRadiusPx = 18f;
        const float PlayerStoneHalfSizePx = 14f;

        static void ApplyMantleRadialOffset(VisualElement stone, int position, int index, int count)
        {
            float centerRad = MantleRadialCenterAngle(position) * Mathf.Deg2Rad;
            float angle = centerRad + (2f * Mathf.PI * index / count);
            float dx = MantleRadialRadiusPx * Mathf.Cos(angle);
            float dy = MantleRadialRadiusPx * Mathf.Sin(angle);

            stone.style.translate = new Translate(
                new Length(-PlayerStoneHalfSizePx + dx, LengthUnit.Pixel),
                new Length(-PlayerStoneHalfSizePx + dy, LengthUnit.Pixel));
        }

        static VisualElement? FindTrackMarker(VisualElement forgeStage, int position)
        {
            foreach (var child in forgeStage.Children())
            {
                if (!child.ClassListContains("stone-marker"))
                    continue;
                if (child.ClassListContains($"forge-pos--{position}"))
                    return child;
            }

            return null;
        }

        static void ResetStasisSlots(VisualElement stasisRow)
        {
            for (int i = 0; i < MaxStasisSlots; i++)
            {
                var slot = stasisRow.Q<VisualElement>($"stasis-slot-{i}");
                if (slot == null)
                    continue;

                slot.EnableInClassList("stasis-slot--occupied", false);
                slot.EnableInClassList("stasis-slot--local", false);

                var gem = slot.Q<VisualElement>(className: "stasis-slot__stone");
                if (gem != null)
                {
                    gem.style.backgroundImage = StyleKeyword.None;
                    gem.style.display = DisplayStyle.None;
                }
            }
        }

        static void ApplyStasisStone(VisualElement stasisRow, int playerId, Sprite sprite, bool isLocal)
        {
            if (playerId < 0 || playerId >= MaxStasisSlots)
                return;

            var slot = stasisRow.Q<VisualElement>($"stasis-slot-{playerId}");
            if (slot == null)
                return;

            slot.EnableInClassList("stasis-slot--occupied", true);
            slot.EnableInClassList("stasis-slot--local", isLocal);

            var gem = slot.Q<VisualElement>(className: "stasis-slot__stone");
            if (gem == null)
                return;

            ApplyMarkerSprite(gem, sprite);
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
