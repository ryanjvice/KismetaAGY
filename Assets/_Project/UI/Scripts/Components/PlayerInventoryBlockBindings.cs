using System;
using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.Core.Views;
using Kismeta.UI.Controllers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Shared player inventory block UI for Card Table and Summer roster.</summary>
    public static class PlayerInventoryBlockBindings
    {
        public static VisualElement BuildExpandableHeader(
            PublicPlayerView player,
            bool expanded,
            bool inStasis,
            int threat,
            bool showSelfSuffix,
            bool showSignGlyph,
            Action<int>? onToggleDetail)
        {
            var header = new VisualElement();
            header.AddToClassList("player-block__header");
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;

            var dot = new VisualElement();
            dot.style.width = 20;
            dot.style.height = 20;
            dot.style.borderTopLeftRadius = dot.style.borderTopRightRadius =
                dot.style.borderBottomLeftRadius = dot.style.borderBottomRightRadius = 10;
            dot.style.backgroundColor = new StyleColor(PlayerUiNames.PlayerColor(player.PlayerId));
            dot.style.marginRight = 8;
            header.Add(dot);

            var name = new Label(PlayerUiNames.ShortName(player.PlayerId) + (showSelfSuffix ? " (you)" : ""));
            name.style.fontSize = 13;
            name.style.color = new StyleColor(UiTheme.TextBody);
            name.style.marginRight = showSignGlyph ? 4 : 6;
            header.Add(name);

            if (showSignGlyph)
            {
                var signGlyph = SymbolGlyphs.CreateZodiacLabel(
                    player.CurrentSign == ZodiacSign.None ? "?" : SymbolGlyphs.Zodiac(player.CurrentSign),
                    "player-block__sign-glyph");
                header.Add(signGlyph);
            }

            var meta = new Label(inStasis
                ? "stasis"
                : $"{StoneShort(player.StonePosition)} · {threat}");
            meta.style.fontSize = 9;
            meta.style.color = new StyleColor(UiTheme.TextSub);
            header.Add(meta);

            if (!inStasis && threat >= 8)
            {
                var badge = new Label("threat");
                badge.AddToClassList("threat-badge");
                badge.style.marginLeft = 6;
                header.Add(badge);
            }

            header.Add(new VisualElement { style = { flexGrow = 1 } });

            var hand = new Label($"hand {player.HandCardCount}");
            hand.style.fontSize = 9;
            hand.style.color = new StyleColor(new Color(0.48f, 0.54f, 0.6f));
            hand.style.marginRight = 6;
            header.Add(hand);

            header.Add(SymbolGlyphs.CreateInfoIconLabel("player-block__expand-hint"));

            var chevron = new Label(expanded ? SymbolGlyphs.ChevronUpGlyph : SymbolGlyphs.ChevronDownGlyph);
            chevron.AddToClassList("player-block__chevron");
            chevron.AddToClassList(SymbolGlyphs.TablerIconClass);
            header.Add(chevron);

            int playerId = player.PlayerId;
            header.pickingMode = PickingMode.Position;
            header.focusable = true;
            foreach (var child in header.Children())
                child.pickingMode = PickingMode.Ignore;
            if (onToggleDetail != null)
                header.AddManipulator(new Clickable(() => onToggleDetail.Invoke(playerId)));

            return header;
        }

        public static void BuildDetailSection(
            VisualElement detail,
            PublicPlayerView player,
            GameSession session,
            ICardDatabase? db,
            Action<string>? onInspect,
            bool inspectViaButton = false)
        {
            detail.Add(MakeDetailSection("current sign", BuildZodiacRow(player)));

            var housesRow = new VisualElement();
            housesRow.AddToClassList("player-block__detail-row");
            if (player.AstralHouses.Count == 0 && player.UnplacedAstralHouses <= 0)
            {
                housesRow.Add(new Label("none") { name = "detail-empty" });
            }
            else
            {
                var chips = new VisualElement();
                chips.AddToClassList("house-chip-row");
                foreach (var sign in player.AstralHouses)
                    chips.Add(MakeHouseChip(sign));
                housesRow.Add(chips);
                if (player.UnplacedAstralHouses > 0)
                {
                    var unplaced = new Label($"unplaced {player.UnplacedAstralHouses}");
                    unplaced.AddToClassList("detail-meta");
                    housesRow.Add(unplaced);
                }
            }
            detail.Add(MakeDetailSection("astral houses", housesRow));

            var reagentRow = new VisualElement();
            reagentRow.AddToClassList("reagent-row");
            foreach (ReagentType rt in Enum.GetValues(typeof(ReagentType)))
            {
                player.Reagents.TryGetValue(rt, out int count);
                reagentRow.Add(MakeReagentChip(rt, count));
            }
            detail.Add(MakeDetailSection("reagents", reagentRow));

            var crucibleHost = new VisualElement();
            crucibleHost.AddToClassList("crucible-slot-list");
            if (player.CrucibleSlots.Count == 0)
            {
                crucibleHost.Add(new Label("none") { name = "detail-empty" });
            }
            else
            {
                foreach (var slot in player.CrucibleSlots)
                    crucibleHost.Add(MakeCrucibleSlotRow(slot, session, db, onInspect, inspectViaButton));
            }
            detail.Add(MakeDetailSection("crucible", crucibleHost));

            detail.Add(MakeDetailSection("stone", BuildStoneRow(player)));
        }

        public static VisualElement MakeCardZone()
        {
            var row = new VisualElement();
            row.AddToClassList("player-block__zone");
            return row;
        }

        public static VisualElement MakeChip(
            CardDefinition def,
            string cardId,
            bool aligned,
            Action<string>? onInspect,
            string? rankLabel = null,
            bool inspectViaButton = false)
        {
            return CardChipFactory.CreateFromDefinition(
                def,
                aligned: aligned,
                instanceId: cardId,
                onInspect: onInspect,
                rankLabelOverride: rankLabel,
                inspectViaButton: inspectViaButton);
        }

        public static VisualElement MakeHiddenChip()
        {
            var chip = new VisualElement();
            chip.AddToClassList("card-chip");
            chip.AddToClassList("card-chip--hidden");
            var rank = new Label("?");
            rank.AddToClassList("card-chip__rank");
            rank.style.color = new StyleColor(new Color(0.55f, 0.5f, 0.58f));
            chip.Add(rank);
            return chip;
        }

        public static Label MakeEyebrow(string text, string? ussClass = null)
        {
            var lbl = new Label(text);
            if (ussClass != null)
                lbl.AddToClassList(ussClass);
            lbl.style.fontSize = 8;
            lbl.style.color = new StyleColor(new Color(0.54f, 0.42f, 0.48f));
            if (ussClass == null)
                lbl.style.marginBottom = 4;
            return lbl;
        }

        public static Button MakeAction(string name, string text, Color bg, Action onClick)
        {
            var btn = new Button(onClick) { text = text, name = name };
            btn.AddToClassList("table-action");
            btn.style.backgroundColor = new StyleColor(bg);
            return btn;
        }

        public static string StoneShort(StonePosition pos) => pos.Value switch
        {
            8 => "Altar",
            >= 6 => "Gold",
            >= 4 => "Silver",
            >= 2 => "Bronze",
            _ => "Lead"
        };

        static VisualElement MakeDetailSection(string eyebrow, VisualElement content)
        {
            var section = new VisualElement();
            section.AddToClassList("player-block__detail-section");
            section.Add(MakeEyebrow(eyebrow));
            section.Add(content);
            return section;
        }

        static VisualElement BuildZodiacRow(PublicPlayerView player)
        {
            var row = new VisualElement();
            row.AddToClassList("player-block__detail-row");
            if (player.CurrentSign == ZodiacSign.None)
            {
                var empty = new Label("—");
                empty.AddToClassList("detail-zodiac__name");
                row.Add(empty);
                return row;
            }

            row.Add(SymbolGlyphs.CreateZodiacLabel(SymbolGlyphs.Zodiac(player.CurrentSign), "detail-zodiac__glyph"));
            var name = new Label(player.CurrentSign.ToString());
            name.AddToClassList("detail-zodiac__name");
            row.Add(name);
            return row;
        }

        static VisualElement BuildStoneRow(PublicPlayerView player)
        {
            var row = new VisualElement();
            row.AddToClassList("stone-status");

            var status = new Label($"{StoneShort(player.StonePosition)} ({player.StonePosition.Value}) · {player.StoneState}");
            status.AddToClassList("stone-status__label");
            row.Add(status);

            if (player.StoneWardCount > 0)
                row.Add(MakeWardBadge(player.StoneWardCount));

            return row;
        }

        static VisualElement MakeHouseChip(ZodiacSign sign)
        {
            var chip = new VisualElement();
            chip.AddToClassList("house-chip");
            chip.Add(SymbolGlyphs.CreateZodiacLabel(SymbolGlyphs.Zodiac(sign), "house-chip__glyph"));
            return chip;
        }

        static VisualElement MakeReagentChip(ReagentType type, int count)
        {
            var chip = new VisualElement();
            chip.AddToClassList("reagent-chip");
            chip.AddToClassList(ReagentChipClass(type));
            var lbl = new Label(count.ToString());
            lbl.AddToClassList("reagent-chip__count");
            chip.Add(lbl);
            return chip;
        }

        static string ReagentChipClass(ReagentType type) => type switch
        {
            ReagentType.Sulphur => "reagent-chip--sulphur",
            ReagentType.AquaRegia => "reagent-chip--aqua",
            ReagentType.Vitriol => "reagent-chip--vitriol",
            ReagentType.Quicksilver => "reagent-chip--quick",
            _ => "reagent-chip--salt"
        };

        static VisualElement MakeCrucibleSlotRow(
            CrucibleSlotView slot,
            GameSession session,
            ICardDatabase? db,
            Action<string>? onInspect,
            bool inspectViaButton)
        {
            var row = new VisualElement();
            row.AddToClassList("crucible-slot-row");

            if (slot.State == CrucibleCardState.Dormant)
            {
                row.Add(MakeHiddenChip());
            }
            else
            {
                var inst = session.GetCard(slot.CardInstanceId);
                var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;
                if (def != null)
                    row.Add(MakeChip(def, slot.CardInstanceId, aligned: false, onInspect, inspectViaButton: inspectViaButton));
                else
                    row.Add(MakeHiddenChip());
            }

            var metaParts = new List<string> { slot.State.ToString().ToLowerInvariant() };
            if (slot.HasCoal)
                metaParts.Add("coal");
            var meta = new Label(string.Join(" · ", metaParts));
            meta.AddToClassList("crucible-slot-row__meta");
            row.Add(meta);

            if (slot.WardCount > 0)
                row.Add(MakeWardBadge(slot.WardCount));

            return row;
        }

        static Label MakeWardBadge(int count)
        {
            var badge = new Label($"ward ×{count}");
            badge.AddToClassList("ward-badge");
            return badge;
        }
    }
}
