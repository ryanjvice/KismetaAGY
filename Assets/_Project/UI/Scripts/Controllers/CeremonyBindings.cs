using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.Core.Views;
using Kismeta.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    internal static class CeremonyBindings
    {
        public static string PlayerName(PlayerColor color) => color switch
        {
            PlayerColor.Red => "Red alchemist",
            PlayerColor.Green => "Green alchemist",
            PlayerColor.Blue => "Blue alchemist",
            PlayerColor.White => "White alchemist",
            _ => "Alchemist"
        };

        public static string PlayerName(GameSession session, int playerId)
        {
            if (playerId < 0 || playerId >= session.Players.Count)
                return "Alchemist";
            return PlayerName(session.Players[playerId].Color);
        }

        public static int NextAgekeeperId(GameSession session)
        {
            int count = session.Players.Count;
            for (int i = 0; i < count; i++)
            {
                if (!session.Players[i].IsAgekeeper) continue;
                return session.Players[(i + 1) % count].PlayerId;
            }
            return 0;
        }

        public static int FindAgekeeperId(GameSession session)
        {
            foreach (var p in session.Players)
            {
                if (p.IsAgekeeper)
                    return p.PlayerId;
            }
            return 0;
        }

        public static void BindAgeOpening(VisualElement? root, GameSession session)
        {
            if (root == null) return;

            var sign = session.Board.CosmicAgeSign;
            var planet = Correspondence.PlanetFor(sign);
            var element = Correspondence.ElementFor(sign);
            var (effectName, effectDesc) = DescribeCosmicEffect(sign);
            int keeperId = FindAgekeeperId(session);

            SetLabel(root, "age-name", sign.ToString().ToUpperInvariant());
            SetLabel(root, "age-planet", planet.ToString());
            SetLabel(root, "age-element", element.ToString());
            SetLabel(root, "effect-name", effectName);
            SetLabel(root, "effect-desc", effectDesc);
            SetLabel(root, "agekeeper-line",
                $"{PlayerName(session, keeperId)} serves as Agekeeper — they cast the age and hold the key this round.");
            BindSigilGlyph(root, sign);
        }

        public static void BindAgeClosing(VisualElement? root, GameSession session)
        {
            if (root == null) return;

            var sign = session.Board.CosmicAgeSign;
            int nextKeeper = NextAgekeeperId(session);
            SetLabel(root, "age-name", sign.ToString().ToUpperInvariant());
            SetLabel(root, "keypass-line",
                $"The key passes to {PlayerName(session, nextKeeper)}. They will cast the next age — its sign is unknown until the dice fall.");
            BindSigilGlyph(root, sign);

            var list = root.Q<VisualElement>("standings-list");
            if (list == null) return;
            list.Clear();

            var view = GamePublicView.From(session);
            foreach (var player in view.Players)
            {
                var row = new VisualElement();
                row.AddToClassList("panel");
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 6;

                var dot = new VisualElement();
                dot.style.width = 13;
                dot.style.height = 13;
                dot.style.borderTopLeftRadius = 7;
                dot.style.borderTopRightRadius = 7;
                dot.style.borderBottomLeftRadius = 7;
                dot.style.borderBottomRightRadius = 7;
                dot.style.marginRight = 9;
                dot.style.backgroundColor = PlayerDotColor(player.Color);

                var summary = new Label($"{PlayerName(player.Color)} · {player.StoneState} (stone {player.StonePosition.Value})");
                summary.style.flexGrow = 1;
                summary.style.fontSize = 11;
                summary.style.color = new StyleColor(new Color(0.95f, 0.91f, 0.82f));

                var detail = new Label($"{player.Spread.Count} spread · {player.HandCardCount} hand");
                detail.style.fontSize = 10;
                detail.style.color = new StyleColor(new Color(0.72f, 0.68f, 0.62f));

                row.Add(dot);
                row.Add(summary);
                row.Add(detail);
                list.Add(row);
            }
        }

        public static void BindRoundOpen(VisualElement? root, GameSession session, int localPlayerId)
        {
            if (root == null) return;

            int keeperId = FindAgekeeperId(session);
            SetLabel(root, "keeper-eyebrow", $"{PlayerName(session, keeperId)} casts the age");

            var player = localPlayerId >= 0 && localPlayerId < session.Players.Count
                ? session.Players[localPlayerId]
                : null;

            bool hasWager = player != null
                && player.FatefulWagerSign != ZodiacSign.None
                && player.FatefulWagerCards.Count > 0;

            var pending = root.Q<VisualElement>("pending-wager");
            if (pending != null)
                pending.style.display = hasWager ? DisplayStyle.Flex : DisplayStyle.None;

            if (hasWager)
            {
                SetLabel(root, "wager-line",
                    $"You staked {player!.FatefulWagerCards.Count} card(s) on {player.FatefulWagerSign} last Winter — it resolves when the die lands.");
            }

            var dieFace = root.Q<Label>("die-face");
            if (dieFace != null)
            {
                SymbolGlyphs.TagZodiac(dieFace);
                if (session.Board.CosmicAgeSign == ZodiacSign.None)
                    dieFace.text = "?";
            }
        }

        public static void ApplySeasonIntroClass(VisualElement? root, Season season)
        {
            if (root == null) return;
            root.RemoveFromClassList("intro--spring");
            root.RemoveFromClassList("intro--summer");
            root.RemoveFromClassList("intro--autumn");
            root.RemoveFromClassList("intro--winter");
            root.AddToClassList($"intro--{season.ToString().ToLowerInvariant()}");
        }

        static (string name, string desc) DescribeCosmicEffect(ZodiacSign sign) => sign switch
        {
            ZodiacSign.Aries => ("+1 Base Harvest", "while Aries reigns, every player draws one extra harvest card"),
            ZodiacSign.Libra => ("+1 Base Harvest", "while Libra reigns, every player draws one extra harvest card"),
            ZodiacSign.Taurus => ("Court Pentacles are a Wild Suit", "every Court Pentacle counts as any suit you need"),
            ZodiacSign.Leo => ("Court Wands are a Wild Suit", "every Court Wand counts as any suit you need"),
            ZodiacSign.Scorpio => ("Court Cups are a Wild Suit", "every Court Cup counts as any suit you need"),
            ZodiacSign.Aquarius => ("Court Swords are a Wild Suit", "every Court Sword counts as any suit you need"),
            ZodiacSign.Cancer => ("Salt costs 2 cards", "craft Salt from any two cards while Cancer reigns"),
            ZodiacSign.Capricorn => ("Salt costs 2 cards", "craft Salt from any two cards while Capricorn reigns"),
            ZodiacSign.Gemini => ("Quicksilver costs 2 Swords", "craft Quicksilver from two Swords with the Swords cauldron lit"),
            ZodiacSign.Virgo => ("Vitriol costs 2 Pentacles", "craft Vitriol from two Pentacles with the Pentacles cauldron lit"),
            ZodiacSign.Sagittarius => ("Sulphur costs 2 Wands", "craft Sulphur from two Wands with the Wands cauldron lit"),
            ZodiacSign.Pisces => ("Aqua Regia costs 2 Cups", "craft Aqua Regia from two Cups with the Cups cauldron lit"),
            _ => ("No cosmic effect", "the heavens are still this round")
        };

        static Color PlayerDotColor(PlayerColor color) => color switch
        {
            PlayerColor.Red => new Color(0.75f, 0.22f, 0.17f),
            PlayerColor.Green => new Color(0.12f, 0.43f, 0.29f),
            PlayerColor.Blue => new Color(0.18f, 0.43f, 0.64f),
            PlayerColor.White => new Color(0.72f, 0.72f, 0.72f),
            _ => new Color(0.5f, 0.5f, 0.5f)
        };

        static void BindSigilGlyph(VisualElement root, ZodiacSign sign)
        {
            var glyph = root.Q<Label>(className: "ceremony-sigil__glyph");
            if (glyph == null) return;
            SymbolGlyphs.TagZodiac(glyph);
            glyph.text = SymbolGlyphs.Zodiac(sign);
        }

        static void SetLabel(VisualElement root, string name, string text)
        {
            var lbl = root.Q<Label>(name);
            if (lbl != null)
                lbl.text = text;
        }
    }
}
