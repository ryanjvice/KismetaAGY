using System;
using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.Core.Views;
using Kismeta.UI.Controllers;
using UnityEngine;
using UnityEngine.UIElements;
using static Kismeta.UI.Components.PlayerInventoryBlockBindings;

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

                block.Add(BuildExpandableHeader(p, expanded, inStasis, threat, showSelfSuffix: isSelf,
                    showSignGlyph: false, onToggleDetail));

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
    }
}
