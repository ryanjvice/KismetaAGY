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
    public static class CardTableBindings
    {
        public enum SortMode { Threat, Turn, Arcanum }

        public static void Populate(
            VisualElement root,
            GameSession session,
            int localPlayerId,
            Season season,
            SortMode sort,
            HashSet<int> expandedBlocks,
            int focusPlayerId,
            Action<string>? onInspect,
            Action<int>? onDuel,
            Action<int>? onGambit,
            Action<int>? onTrade,
            Action<int>? onToggleDetail,
            Action<int>? onOppose = null)
        {
            var scroll = root.Q<ScrollView>("players");
            if (scroll == null) return;
            scroll.Clear();

            var view = GamePublicView.From(session);
            var players = new List<PublicPlayerView>(view.Players);
            players.Sort((a, b) => ComparePlayers(session, a, b, sort));

            var db = session.Rules?.CardDatabase;
            var alignment = session.Rules?.Alignment;
            var cosmic = session.Board.CosmicAgeSign;
            bool summerContests = season == Season.Summer;

            VisualElement? focusBlock = null;

            foreach (var p in players)
            {
                bool isSelf = p.PlayerId == localPlayerId;
                bool inStasis = p.StoneState == StoneState.Stasis;
                bool expanded = expandedBlocks.Contains(p.PlayerId);
                int threat = alignment?.CalculateAlignmentPoints(session, p.PlayerId, cosmic) ?? 0;

                var block = new VisualElement();
                block.name = $"player-block-{p.PlayerId}";
                block.AddToClassList("player-block");
                if (expanded)
                    block.AddToClassList("player-block--expanded");
                block.style.borderLeftColor = new StyleColor(PlayerUiNames.PlayerColor(p.PlayerId));
                if (isSelf)
                {
                    var border = new StyleColor(new Color(0.35f, 0.27f, 0.13f));
                    block.style.borderTopColor = block.style.borderRightColor =
                        block.style.borderBottomColor = block.style.borderLeftColor = border;
                }

                var header = new VisualElement();
                header.AddToClassList("player-block__header");
                header.style.flexDirection = FlexDirection.Row;
                header.style.alignItems = Align.Center;
                header.style.marginBottom = 8;

                var dot = new VisualElement();
                dot.style.width = 20;
                dot.style.height = 20;
                dot.style.borderTopLeftRadius = dot.style.borderTopRightRadius =
                    dot.style.borderBottomLeftRadius = dot.style.borderBottomRightRadius = 10;
                dot.style.backgroundColor = new StyleColor(PlayerUiNames.PlayerColor(p.PlayerId));
                dot.style.marginRight = 8;
                header.Add(dot);

                var name = new Label(PlayerUiNames.ShortName(p.PlayerId) + (isSelf ? " (you)" : ""));
                name.style.fontSize = 13;
                name.style.color = new StyleColor(UiTheme.TextBody);
                name.style.marginRight = 6;
                header.Add(name);

                var meta = new Label(inStasis
                    ? "stasis"
                    : $"{StoneShort(p.StonePosition)} · {threat}");
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

                var hand = new Label($"hand {p.HandCardCount}");
                hand.style.fontSize = 9;
                hand.style.color = new StyleColor(new Color(0.48f, 0.54f, 0.6f));
                hand.style.marginRight = 6;
                header.Add(hand);

                header.Add(SymbolGlyphs.CreateInfoIconLabel("player-block__expand-hint"));

                var chevron = new Label(expanded ? SymbolGlyphs.ChevronUpGlyph : SymbolGlyphs.ChevronDownGlyph);
                chevron.AddToClassList("player-block__chevron");
                chevron.AddToClassList(SymbolGlyphs.TablerIconClass);
                header.Add(chevron);

                int playerId = p.PlayerId;
                header.pickingMode = PickingMode.Position;
                header.focusable = true;
                foreach (var child in header.Children())
                    child.pickingMode = PickingMode.Ignore;
                if (onToggleDetail != null)
                    header.AddManipulator(new Clickable(() => onToggleDetail.Invoke(playerId)));
                block.Add(header);

                block.Add(MakeEyebrow("spread"));
                var spreadRow = MakeCardZone();
                foreach (var cardId in p.Spread)
                {
                    var inst = session.GetCard(cardId);
                    var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;
                    if (def == null) continue;
                    int align = AlignmentService.ScoreCard(def.Suit, def.Planet, cosmic);
                    spreadRow.Add(MakeChip(def, cardId, align > 0, onInspect));
                }
                block.Add(spreadRow);

                block.Add(MakeEyebrow(isSelf ? "hand" : $"hand · {p.HandCardCount} hidden"));
                var handRow = MakeCardZone();
                if (isSelf)
                {
                    foreach (var cardId in session.Players[p.PlayerId].Hand)
                    {
                        var inst = session.GetCard(cardId);
                        var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;
                        if (def == null) continue;
                        int align = AlignmentService.ScoreCard(def.Suit, def.Planet, cosmic);
                        handRow.Add(MakeChip(def, cardId, align > 0, onInspect));
                    }
                }
                else
                {
                    for (int i = 0; i < p.HandCardCount; i++)
                        handRow.Add(MakeHiddenChip());
                }
                block.Add(handRow);

                if (p.Arcanum.Count > 0)
                {
                    block.Add(MakeEyebrow("arcanum"));
                    var arcRow = MakeCardZone();
                    foreach (var cardId in p.Arcanum)
                    {
                        var inst = session.GetCard(cardId);
                        var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;
                        if (def == null) continue;
                        arcRow.Add(MakeChip(
                            def,
                            cardId,
                            aligned: false,
                            onInspect,
                            def.IsMajorArcana ? "★" : null));
                    }
                    block.Add(arcRow);
                }

                if (expanded)
                {
                    var detail = new VisualElement();
                    detail.AddToClassList("player-block__detail");
                    BuildDetailSection(detail, p, session, db, onInspect);
                    block.Add(detail);
                }

                if (inStasis)
                {
                    var note = new Label("in stasis — cannot be opposed this age");
                    note.style.fontSize = 9;
                    note.style.color = new StyleColor(new Color(0.48f, 0.54f, 0.6f));
                    block.Add(note);
                }
                else if (!isSelf && summerContests)
                {
                    var actions = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                    actions.Add(MakeAction($"duel-{p.PlayerId}", "duel",
                        new Color(0.16f, 0.07f, 0.07f), () => onDuel?.Invoke(p.PlayerId)));
                    actions.Add(MakeAction($"gambit-{p.PlayerId}", "gambit",
                        new Color(0.16f, 0.14f, 0.05f), () => onGambit?.Invoke(p.PlayerId)));
                    actions.Add(MakeAction($"trade-{p.PlayerId}", "trade",
                        new Color(0.09f, 0.13f, 0.18f), () => onTrade?.Invoke(p.PlayerId)));
                    if (onOppose != null && ContestBindings.CanTargetForOpposition(session, localPlayerId, p.PlayerId))
                        actions.Add(MakeAction($"oppose-{p.PlayerId}", "oppose",
                            new Color(0.18f, 0.06f, 0.16f), () => onOppose.Invoke(p.PlayerId)));
                    block.Add(actions);
                }

                if (focusPlayerId >= 0 && p.PlayerId == focusPlayerId)
                    focusBlock = block;

                scroll.Add(block);
            }

            if (focusBlock != null)
                scroll.schedule.Execute(() => scroll.ScrollTo(focusBlock)).ExecuteLater(0);
        }

        static void BuildDetailSection(
            VisualElement detail,
            PublicPlayerView p,
            GameSession session,
            ICardDatabase? db,
            Action<string>? onInspect)
        {
            detail.Add(MakeDetailSection("current sign", BuildZodiacRow(p)));

            var housesRow = new VisualElement();
            housesRow.AddToClassList("player-block__detail-row");
            if (p.AstralHouses.Count == 0 && p.UnplacedAstralHouses <= 0)
            {
                housesRow.Add(new Label("none") { name = "detail-empty" });
            }
            else
            {
                var chips = new VisualElement();
                chips.AddToClassList("house-chip-row");
                foreach (var sign in p.AstralHouses)
                    chips.Add(MakeHouseChip(sign));
                housesRow.Add(chips);
                if (p.UnplacedAstralHouses > 0)
                {
                    var unplaced = new Label($"unplaced {p.UnplacedAstralHouses}");
                    unplaced.AddToClassList("detail-meta");
                    housesRow.Add(unplaced);
                }
            }
            detail.Add(MakeDetailSection("astral houses", housesRow));

            var reagentRow = new VisualElement();
            reagentRow.AddToClassList("reagent-row");
            foreach (ReagentType rt in Enum.GetValues(typeof(ReagentType)))
            {
                p.Reagents.TryGetValue(rt, out int count);
                reagentRow.Add(MakeReagentChip(rt, count));
            }
            detail.Add(MakeDetailSection("reagents", reagentRow));

            var crucibleHost = new VisualElement();
            crucibleHost.AddToClassList("crucible-slot-list");
            if (p.CrucibleSlots.Count == 0)
            {
                crucibleHost.Add(new Label("none") { name = "detail-empty" });
            }
            else
            {
                foreach (var slot in p.CrucibleSlots)
                    crucibleHost.Add(MakeCrucibleSlotRow(slot, session, db, onInspect));
            }
            detail.Add(MakeDetailSection("crucible", crucibleHost));

            detail.Add(MakeDetailSection("stone", BuildStoneRow(p)));
        }

        static VisualElement MakeDetailSection(string eyebrow, VisualElement content)
        {
            var section = new VisualElement();
            section.AddToClassList("player-block__detail-section");
            section.Add(MakeEyebrow(eyebrow));
            section.Add(content);
            return section;
        }

        static VisualElement BuildZodiacRow(PublicPlayerView p)
        {
            var row = new VisualElement();
            row.AddToClassList("player-block__detail-row");
            if (p.CurrentSign == ZodiacSign.None)
            {
                var empty = new Label("—");
                empty.AddToClassList("detail-zodiac__name");
                row.Add(empty);
                return row;
            }

            row.Add(SymbolGlyphs.CreateZodiacLabel(SymbolGlyphs.Zodiac(p.CurrentSign), "detail-zodiac__glyph"));
            var name = new Label(p.CurrentSign.ToString());
            name.AddToClassList("detail-zodiac__name");
            row.Add(name);
            return row;
        }

        static VisualElement BuildStoneRow(PublicPlayerView p)
        {
            var row = new VisualElement();
            row.AddToClassList("stone-status");

            var status = new Label($"{StoneShort(p.StonePosition)} ({p.StonePosition.Value}) · {p.StoneState}");
            status.AddToClassList("stone-status__label");
            row.Add(status);

            if (p.StoneWardCount > 0)
                row.Add(MakeWardBadge(p.StoneWardCount));

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
            Action<string>? onInspect)
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
                    row.Add(MakeChip(def, slot.CardInstanceId, aligned: false, onInspect));
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

        static int ComparePlayers(GameSession session, PublicPlayerView a, PublicPlayerView b, SortMode sort)
        {
            return sort switch
            {
                SortMode.Turn => a.PlayerId.CompareTo(b.PlayerId),
                SortMode.Arcanum => b.Arcanum.Count.CompareTo(a.Arcanum.Count),
                _ => (session.Rules?.Alignment?.CalculateAlignmentPoints(session, b.PlayerId,
                        session.Board.CosmicAgeSign) ?? 0)
                    .CompareTo(session.Rules?.Alignment?.CalculateAlignmentPoints(session, a.PlayerId,
                        session.Board.CosmicAgeSign) ?? 0)
            };
        }

        static VisualElement MakeCardZone()
        {
            var row = new VisualElement();
            row.AddToClassList("player-block__zone");
            return row;
        }

        static VisualElement MakeChip(CardDefinition def, string cardId, bool aligned, Action<string>? onInspect,
            string? rankLabel = null)
        {
            return CardChipFactory.CreateFromDefinition(
                def,
                aligned: aligned,
                instanceId: cardId,
                onInspect: onInspect,
                rankLabelOverride: rankLabel);
        }

        static VisualElement MakeHiddenChip()
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

        static Label MakeEyebrow(string text)
        {
            var lbl = new Label(text);
            lbl.style.fontSize = 8;
            lbl.style.color = new StyleColor(new Color(0.54f, 0.42f, 0.48f));
            lbl.style.marginBottom = 4;
            return lbl;
        }

        static Button MakeAction(string name, string text, Color bg, Action onClick)
        {
            var btn = new Button(onClick) { text = text, name = name };
            btn.AddToClassList("table-action");
            btn.style.backgroundColor = new StyleColor(bg);
            return btn;
        }

        static string StoneShort(StonePosition pos) => pos.Value switch
        {
            8 => "Altar",
            >= 6 => "Gold",
            >= 4 => "Silver",
            >= 2 => "Bronze",
            _ => "Lead"
        };
    }
}
