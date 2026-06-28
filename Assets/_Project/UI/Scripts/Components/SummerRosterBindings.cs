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
    /// <summary>Simplified player roster for the Summer main-scene central panel.</summary>
    public static class SummerRosterBindings
    {
        public static void Populate(
            VisualElement root,
            GameSession session,
            int localPlayerId,
            HashSet<int> expandedBlocks,
            Action<string>? onInspect,
            Action<int>? onToggleDetail)
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
                bool expanded = expandedBlocks.Contains(p.PlayerId);
                int threat = alignment?.CalculateAlignmentPoints(session, p.PlayerId, cosmic) ?? 0;

                var block = new VisualElement();
                block.name = $"player-block-{p.PlayerId}";
                block.AddToClassList("player-block");
                if (expanded)
                    block.AddToClassList("player-block--expanded");
                block.style.borderLeftColor = new StyleColor(PlayerUiNames.PlayerColor(p.PlayerId));

                block.Add(BuildExpandableHeader(p, expanded, inStasis, threat, showSelfSuffix: false,
                    showSignGlyph: true, onToggleDetail));

                block.Add(MakeEyebrow("spread", "player-block__eyebrow"));
                var spreadRow = MakeCardZone();
                foreach (var cardId in p.Spread)
                {
                    var inst = session.GetCard(cardId);
                    var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;
                    if (def == null) continue;
                    int align = AlignmentService.ScoreCard(def.Suit, def.Planet, cosmic);
                    spreadRow.Add(MakeChip(def, cardId, align > 0, onInspect, inspectViaButton: true));
                }
                block.Add(spreadRow);

                if (expanded)
                {
                    var inlineZones = new VisualElement();
                    inlineZones.AddToClassList("player-block__inline-zones");
                    inlineZones.Add(BuildArcanumColumn(p, session, db, onInspect));
                    inlineZones.Add(BuildCrucibleColumn(p, session, db, onInspect));
                    block.Add(inlineZones);

                    var detail = new VisualElement();
                    detail.AddToClassList("player-block__detail");
                    BuildDetailSection(detail, p, session, db, onInspect, inspectViaButton: true);
                    block.Add(detail);
                }

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

        static VisualElement BuildArcanumColumn(
            PublicPlayerView p,
            GameSession session,
            ICardDatabase? db,
            Action<string>? onInspect)
        {
            var col = new VisualElement();
            col.AddToClassList("player-block__zone-col");
            col.AddToClassList("player-block__zone-col--arcanum");
            col.Add(MakeEyebrow("arcanum", "player-block__eyebrow"));

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
                        def.IsMajorArcana ? "★" : null,
                        inspectViaButton: true));
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
            col.Add(MakeEyebrow("crucible", "player-block__eyebrow"));

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
                                RomanNumerals.ToArcanaLabel(def.ArcanaNumber),
                                inspectViaButton: true));
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
    }
}
