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
    /// <summary>Simplified player roster for the Summer main-scene central panel.</summary>
    public static class SummerRosterBindings
    {
        public static void Populate(
            VisualElement root,
            GameSession session,
            int localPlayerId,
            Action<string>? onInspect)
        {
            var scroll = root.Q<ScrollView>("players");
            if (scroll == null) return;
            scroll.Clear();

            var view = GamePublicView.From(session);
            var players = new List<PublicPlayerView>(view.Players);
            players.Sort((a, b) => CompareByThreat(session, a, b));

            var db = session.Rules?.CardDatabase;
            var alignment = session.Rules?.Alignment;
            var cosmic = session.Board.CosmicAgeSign;

            foreach (var p in players)
            {
                if (p.PlayerId == localPlayerId)
                    continue;

                bool inStasis = p.StoneState == StoneState.Stasis;
                int threat = alignment?.CalculateAlignmentPoints(session, p.PlayerId, cosmic) ?? 0;

                var block = new VisualElement();
                block.name = $"player-block-{p.PlayerId}";
                block.AddToClassList("player-block");
                block.style.borderLeftColor = new StyleColor(PlayerUiNames.PlayerColor(p.PlayerId));

                block.Add(BuildHeader(p, inStasis, threat));

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

                var inlineZones = new VisualElement();
                inlineZones.AddToClassList("player-block__inline-zones");
                inlineZones.Add(BuildArcanumColumn(p, session, db, onInspect));
                inlineZones.Add(BuildCrucibleColumn(p, session, db, onInspect));
                block.Add(inlineZones);

                if (inStasis)
                {
                    var note = new Label("in stasis — cannot be opposed this age");
                    note.style.fontSize = 9;
                    note.style.color = new StyleColor(new Color(0.48f, 0.54f, 0.6f));
                    block.Add(note);
                }

                scroll.Add(block);
            }
        }

        static VisualElement BuildHeader(PublicPlayerView p, bool inStasis, int threat)
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
            dot.style.backgroundColor = new StyleColor(PlayerUiNames.PlayerColor(p.PlayerId));
            dot.style.marginRight = 8;
            header.Add(dot);

            var name = new Label(PlayerUiNames.ShortName(p.PlayerId));
            name.style.fontSize = 13;
            name.style.color = new StyleColor(UiTheme.TextBody);
            name.style.marginRight = 4;
            header.Add(name);

            var signGlyph = SymbolGlyphs.CreateZodiacLabel(
                p.CurrentSign == ZodiacSign.None ? "?" : SymbolGlyphs.Zodiac(p.CurrentSign),
                "player-block__sign-glyph");
            header.Add(signGlyph);

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
            header.Add(hand);

            return header;
        }

        static VisualElement BuildArcanumColumn(
            PublicPlayerView p,
            GameSession session,
            ICardDatabase? db,
            Action<string>? onInspect)
        {
            var col = new VisualElement();
            col.AddToClassList("player-block__zone-col");
            col.AddToClassList("player-block__zone-col--arcanum");
            col.Add(MakeEyebrow("arcanum"));

            var zone = MakeCardZone();
            zone.AddToClassList("player-block__zone--nowrap");
            if (p.Arcanum.Count == 0)
            {
                var empty = new Label("none");
                empty.AddToClassList("detail-empty");
                zone.Add(empty);
            }
            else
            {
                foreach (var cardId in p.Arcanum)
                {
                    var inst = session.GetCard(cardId);
                    var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;
                    if (def == null) continue;
                    zone.Add(MakeChip(
                        def,
                        cardId,
                        aligned: false,
                        onInspect,
                        def.IsMajorArcana ? "★" : null));
                }
            }

            col.Add(zone);
            return col;
        }

        static VisualElement BuildCrucibleColumn(
            PublicPlayerView p,
            GameSession session,
            ICardDatabase? db,
            Action<string>? onInspect)
        {
            var col = new VisualElement();
            col.AddToClassList("player-block__zone-col");
            col.AddToClassList("player-block__zone-col--crucible");
            col.Add(MakeEyebrow("crucible"));

            var zone = MakeCardZone();
            zone.AddToClassList("player-block__zone--nowrap");
            if (p.CrucibleSlots.Count == 0)
            {
                var empty = new Label("none");
                empty.AddToClassList("detail-empty");
                zone.Add(empty);
            }
            else
            {
                foreach (var slot in p.CrucibleSlots)
                {
                    if (slot.State >= CrucibleCardState.Active)
                    {
                        var inst = session.GetCard(slot.CardInstanceId);
                        var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;
                        if (def != null)
                        {
                            zone.Add(MakeChip(
                                def,
                                slot.CardInstanceId,
                                aligned: false,
                                onInspect,
                                RomanNumerals.ToArcanaLabel(def.ArcanaNumber)));
                            continue;
                        }
                    }

                    zone.Add(MakeHiddenChip());
                }
            }

            col.Add(zone);
            return col;
        }

        static int CompareByThreat(GameSession session, PublicPlayerView a, PublicPlayerView b)
        {
            var cosmic = session.Board.CosmicAgeSign;
            int threatA = session.Rules?.Alignment?.CalculateAlignmentPoints(session, a.PlayerId, cosmic) ?? 0;
            int threatB = session.Rules?.Alignment?.CalculateAlignmentPoints(session, b.PlayerId, cosmic) ?? 0;
            return threatB.CompareTo(threatA);
        }

        static VisualElement MakeCardZone()
        {
            var row = new VisualElement();
            row.AddToClassList("player-block__zone");
            return row;
        }

        static VisualElement MakeChip(
            CardDefinition def,
            string cardId,
            bool aligned,
            Action<string>? onInspect,
            string? rankLabel = null)
        {
            var chip = CardChipFactory.CreateFromDefinition(
                def,
                aligned: aligned,
                instanceId: cardId,
                onInspect: null,
                rankLabelOverride: rankLabel,
                inspectViaButton: true);

            if (onInspect != null && !string.IsNullOrEmpty(cardId))
                WireChipInspect(chip, cardId, onInspect);

            return chip;
        }

        static void WireChipInspect(VisualElement chip, string cardId, Action<string> onInspect)
        {
            chip.pickingMode = PickingMode.Position;
            chip.style.cursor = new StyleCursor(StyleKeyword.Auto);
            SetIgnorePicking(chip);

            chip.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                onInspect(cardId);
            });
        }

        static void SetIgnorePicking(VisualElement root)
        {
            foreach (var child in root.Children())
            {
                child.pickingMode = PickingMode.Ignore;
                SetIgnorePicking(child);
            }
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
            lbl.AddToClassList("player-block__eyebrow");
            lbl.style.fontSize = 8;
            lbl.style.color = new StyleColor(new Color(0.54f, 0.42f, 0.48f));
            return lbl;
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
