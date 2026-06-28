using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>
    /// Renders the Spring hub zodiac wheel with cosmic-age pawn (middle ring) and player meeples (outer ring).
    /// </summary>
    public static class SpringBoardBindings
    {
        const float OuterRingRadiusPct = 42f;
        const float MiddleRingRadiusPct = 28f;
        const float TokenSizePct = 11f;

        static readonly Dictionary<ZodiacSign, int> SignSlotFromTop = BuildSignSlotMap();

        public static void BindBoard(VisualElement? boardRoot, GameSession session)
        {
            if (boardRoot == null) return;

            var catalog = UiArtBindings.Catalog;
            UiArtBindings.ApplyBackground(boardRoot.Q("spring-board-wheel"), catalog?.SpringHubWheel);

            var tokensHost = boardRoot.Q("spring-board-tokens");
            if (tokensHost == null) return;

            tokensHost.Clear();

            var cosmicSign = session.Board.CosmicAgeSign;
            if (cosmicSign != ZodiacSign.None)
                PlaceToken(tokensHost, "cosmic-age-pawn", catalog?.CosmicAgePawn, cosmicSign, MiddleRingRadiusPct);

            foreach (var player in session.Players)
            {
                if (player.CurrentSign == ZodiacSign.None) continue;
                var sprite = catalog?.MeepleFor(player.Color);
                PlaceToken(tokensHost, $"meeple-{player.PlayerId}", sprite, player.CurrentSign, OuterRingRadiusPct);
            }
        }

        static void PlaceToken(
            VisualElement host,
            string name,
            Sprite? sprite,
            ZodiacSign sign,
            float radiusPct)
        {
            if (sprite == null) return;

            var token = host.Q(name);
            if (token == null)
            {
                token = new VisualElement { name = name };
                token.AddToClassList("spring-board__token");
                host.Add(token);
            }

            UiArtBindings.ApplyBackground(token, sprite, BackgroundSizeType.Contain);

            float angleRad = SlotAngleRad(sign);
            float sizePct = TokenSizePct;
            float half = sizePct * 0.5f;
            float cx = 50f + radiusPct * Mathf.Sin(angleRad);
            float cy = 50f - radiusPct * Mathf.Cos(angleRad);

            token.style.position = Position.Absolute;
            token.style.width = Length.Percent(sizePct);
            token.style.height = Length.Percent(sizePct);
            token.style.left = Length.Percent(cx - half);
            token.style.top = Length.Percent(cy - half);
        }

        static float SlotAngleRad(ZodiacSign sign)
        {
            if (!SignSlotFromTop.TryGetValue(sign, out int slot))
                return 0f;
            return slot * (Mathf.PI / 6f) - Mathf.PI * 0.5f;
        }

        /// <summary>
        /// Wheel art places Gemini at 12 o'clock; slots advance clockwise.
        /// </summary>
        static Dictionary<ZodiacSign, int> BuildSignSlotMap()
        {
            var order = new[]
            {
                ZodiacSign.Gemini, ZodiacSign.Cancer, ZodiacSign.Leo, ZodiacSign.Virgo,
                ZodiacSign.Libra, ZodiacSign.Scorpio, ZodiacSign.Sagittarius, ZodiacSign.Capricorn,
                ZodiacSign.Aquarius, ZodiacSign.Pisces, ZodiacSign.Aries, ZodiacSign.Taurus
            };

            var map = new Dictionary<ZodiacSign, int>(12);
            for (int i = 0; i < order.Length; i++)
                map[order[i]] = i;
            return map;
        }
    }
}
