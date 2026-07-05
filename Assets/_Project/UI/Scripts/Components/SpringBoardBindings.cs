using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using UnityEngine;
using UnityEngine.UIElements;
using static Kismeta.UI.Components.ZodiacWheelLayout;

namespace Kismeta.UI.Components
{
    /// <summary>
    /// Renders the Spring hub zodiac wheel with cosmic-age pawn (middle ring), player meeples (outer ring),
    /// and astral house tokens (outermost ring).
    /// Wheel art is set in USS (see spring-board__wheel); tokens use USS sprites + runtime placement.
    /// </summary>
    public static class SpringBoardBindings
    {
        const float OuterRingRadiusPct = 43f;
        const float MiddleRingRadiusPct = 26f;
        const float HouseRingRadiusPct = 52f;
        const float MeepleSizePx = 40f;
        const float PawnSizePx = 34f;
        const float HouseSizePx = 32f;
        const float SameSignOffsetDeg = 4f;

        public static void BindBoard(VisualElement? boardRoot, GameSession session)
        {
            if (boardRoot == null) return;

            var tokensHost = boardRoot.Q("spring-board-tokens");
            if (tokensHost == null) return;

            tokensHost.Clear();

            var cosmicSign = session.Board.CosmicAgeSign;
            if (cosmicSign != ZodiacSign.None)
            {
                PlaceToken(
                    tokensHost,
                    "cosmic-age-pawn",
                    cosmicSign,
                    MiddleRingRadiusPct,
                    PawnSizePx,
                    isPawn: true);
            }

            var signCounts = new Dictionary<ZodiacSign, int>();
            foreach (var player in session.Players)
            {
                if (player.CurrentSign == ZodiacSign.None) continue;

                int index = signCounts.TryGetValue(player.CurrentSign, out int count) ? count : 0;
                signCounts[player.CurrentSign] = index + 1;

                float offsetDeg = index == 0 ? 0f : SameSignOffsetDeg * (index % 2 == 1 ? 1f : -1f) * ((index + 1) / 2);
                PlaceToken(
                    tokensHost,
                    $"meeple-{player.PlayerId}",
                    player.CurrentSign,
                    OuterRingRadiusPct,
                    MeepleSizePx,
                    offsetDeg,
                    player.Color);
            }

            foreach (var player in session.Players)
            {
                foreach (var sign in player.AstralHouses)
                {
                    PlaceToken(
                        tokensHost,
                        $"house-p{player.PlayerId}-{sign}",
                        sign,
                        HouseRingRadiusPct,
                        HouseSizePx,
                        color: player.Color,
                        isHouse: true);
                }
            }
        }

        static void PlaceToken(
            VisualElement host,
            string name,
            ZodiacSign sign,
            float radiusPct,
            float sizePx,
            float angleOffsetDeg = 0f,
            PlayerColor color = PlayerColor.Red,
            bool isPawn = false,
            bool isHouse = false)
        {
            var token = new VisualElement { name = name };
            token.AddToClassList("spring-board__token");
            if (isPawn)
                token.AddToClassList("spring-board__token--pawn");
            else if (isHouse)
                token.AddToClassList(HouseClassFor(color));
            else
                token.AddToClassList(MeepleClassFor(color));
            host.Add(token);

            float angleRad = SegmentCenterAngleRad(sign, angleOffsetDeg);
            float half = sizePx * 0.5f;
            float cx = 50f + radiusPct * Mathf.Sin(angleRad);
            float cy = 50f - radiusPct * Mathf.Cos(angleRad);

            token.style.position = Position.Absolute;
            token.style.width = sizePx;
            token.style.height = sizePx;
            token.style.left = Length.Percent(cx);
            token.style.top = Length.Percent(cy);
            token.style.translate = new Translate(
                new Length(-half, LengthUnit.Pixel),
                new Length(-half, LengthUnit.Pixel));
        }

        static string MeepleClassFor(PlayerColor color) => color switch
        {
            PlayerColor.Red => "spring-board__token--meeple-red",
            PlayerColor.Green => "spring-board__token--meeple-green",
            PlayerColor.Blue => "spring-board__token--meeple-blue",
            PlayerColor.White => "spring-board__token--meeple-white",
            _ => "spring-board__token--meeple-red"
        };

        static string HouseClassFor(PlayerColor color) => color switch
        {
            PlayerColor.Red => "spring-board__token--house-red",
            PlayerColor.Green => "spring-board__token--house-green",
            PlayerColor.Blue => "spring-board__token--house-blue",
            PlayerColor.White => "spring-board__token--house-white",
            _ => "spring-board__token--house-red"
        };

    }
}
