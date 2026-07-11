using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Views;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Shared glyph + S/H/A/R summary row for player inventory and rival strips.</summary>
    public static class PlayerSummaryRowBuilder
    {
        public static void BindExisting(
            VisualElement root,
            PublicPlayerView player,
            int? spreadCountOverride = null,
            int? handCountOverride = null)
        {
            var glyph = root.Q<Label>("summary-sign-glyph");
            if (glyph != null)
            {
                SymbolGlyphs.TagZodiac(glyph);
                glyph.text = player.CurrentSign == ZodiacSign.None
                    ? "?"
                    : SymbolGlyphs.Zodiac(player.CurrentSign);
            }

            SetCountLabel(root, "summary-spread-count", "S", spreadCountOverride ?? player.Spread.Count);
            SetCountLabel(root, "summary-hand-count", "H", handCountOverride ?? player.HandCardCount);
            SetCountLabel(root, "summary-arcanum-count", "A", player.Arcanum.Count);
            SetWagerCountLabel(root, player.FatefulWagerCount);
            SetCountLabel(root, "summary-reagent-total", "R", ReagentTotal(player));
        }

        public static VisualElement Build(PublicPlayerView player)
        {
            var row = new VisualElement();
            row.AddToClassList("inventory-summary-row");
            row.style.paddingTop = row.style.paddingBottom =
                row.style.paddingLeft = row.style.paddingRight = 0;
            row.style.minHeight = StyleKeyword.Null;

            var glyph = new Label(player.CurrentSign == ZodiacSign.None
                ? "?"
                : SymbolGlyphs.Zodiac(player.CurrentSign));
            glyph.AddToClassList("inventory-summary__glyph");
            SymbolGlyphs.TagZodiac(glyph);
            row.Add(glyph);

            var counts = new VisualElement();
            counts.AddToClassList("inventory-summary__counts");
            counts.Add(MakeCountLabel("S", player.Spread.Count, "inventory-summary__count--spread"));
            counts.Add(MakeCountLabel("H", player.HandCardCount, "inventory-summary__count--hand"));
            counts.Add(MakeCountLabel("A", player.Arcanum.Count, "inventory-summary__count--arcanum"));
            if (player.FatefulWagerCount > 0)
                counts.Add(MakeCountLabel("W", player.FatefulWagerCount, "inventory-summary__count--wager"));
            counts.Add(MakeCountLabel("R", ReagentTotal(player), "inventory-summary__count--reagent"));
            row.Add(counts);

            return row;
        }

        static void SetCountLabel(VisualElement root, string name, string prefix, int count)
        {
            var lbl = root.Q<Label>(name);
            if (lbl != null)
                lbl.text = $"{prefix} {count}";
        }

        static void SetWagerCountLabel(VisualElement root, int count)
        {
            var counts = root.Q(className: "inventory-summary__counts");
            var reagent = root.Q<Label>("summary-reagent-total");
            if (counts == null || reagent == null) return;

            var lbl = root.Q<Label>("summary-wager-count");
            if (count > 0)
            {
                if (lbl == null)
                {
                    lbl = MakeCountLabel("W", count, "inventory-summary__count--wager");
                    lbl.name = "summary-wager-count";
                    counts.Insert(counts.IndexOf(reagent), lbl);
                }
                else
                {
                    lbl.text = $"W {count}";
                }
            }
            else
            {
                lbl?.RemoveFromHierarchy();
            }
        }

        static Label MakeCountLabel(string prefix, int count, string modifierClass)
        {
            var lbl = new Label($"{prefix} {count}");
            lbl.AddToClassList("inventory-summary__count");
            lbl.AddToClassList(modifierClass);
            return lbl;
        }

        static int ReagentTotal(PublicPlayerView player)
        {
            int total = 0;
            foreach (var kv in player.Reagents)
                total += kv.Value;
            return total;
        }
    }
}
