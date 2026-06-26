using System;
using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.Core.Views;
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
            Action<string>? onInspect,
            Action<int>? onDuel,
            Action<int>? onGambit,
            Action<int>? onTrade)
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

            foreach (var p in players)
            {
                bool isSelf = p.PlayerId == localPlayerId;
                bool inStasis = p.StoneState == StoneState.Stasis;
                int threat = alignment?.CalculateAlignmentPoints(session, p.PlayerId, cosmic) ?? 0;

                var block = new VisualElement();
                block.AddToClassList("player-block");
                block.style.borderLeftColor = new StyleColor(PlayerUiNames.PlayerColor(p.PlayerId));
                if (isSelf)
                {
                    var border = new StyleColor(new Color(0.35f, 0.27f, 0.13f));
                    block.style.borderTopColor = block.style.borderRightColor =
                        block.style.borderBottomColor = block.style.borderLeftColor = border;
                }

                var header = new VisualElement();
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
                header.Add(hand);
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
                    block.Add(actions);
                }

                scroll.Add(block);
            }
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
