using System;
using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.UI.Components;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    /// <summary>Shared tableau zone UI for harvest dealing and commune arrangement.</summary>
    public static class TableauBindings
    {
        public static void RebuildDealingZones(VisualElement? scope, GameSession session, int playerId,
            ZodiacSign referenceSign, Action<string>? onInspect = null)
        {
            if (scope == null) return;
            var player = session.Players[playerId];

            var spreadIds = new List<string>(player.Spread.Count);
            foreach (var id in player.Spread)
                if (TapSwapBindings.IsMinorArcana(session, id)) spreadIds.Add(id);

            var handIds = new List<string>(player.Hand.Count);
            foreach (var id in player.Hand)
                handIds.Add(id);

            var arcanumIds = new List<string>(player.Arcanum);

            RebuildZone(scope, "spread-cards", spreadIds, session, referenceSign, null, fromSpread: true, onInspect);
            RebuildZone(scope, "hand-cards", handIds, session, referenceSign, null, fromSpread: false, onInspect);
            RebuildArcanumZone(scope, arcanumIds, session, onInspect, session, playerId);

            SetLabel(scope, "commune-spread-count", spreadIds.Count.ToString());
            SetLabel(scope, "commune-hand-count", handIds.Count.ToString());
            SetLabel(scope, "commune-arcanum-count", arcanumIds.Count.ToString());
        }

        public static void RebuildCommuneZones(VisualElement? scope, GameSession session, int playerId,
            IReadOnlyList<string> spreadIds, IReadOnlyList<string> handIds,
            ZodiacSign referenceSign, Action<string, bool> onTap,
            Action<string>? onInspect = null)
        {
            if (scope == null) return;

            RebuildZone(scope, "spread-cards", spreadIds, session, referenceSign, onTap, fromSpread: true, onInspect);
            RebuildZone(scope, "hand-cards", handIds, session, referenceSign, onTap, fromSpread: false, onInspect);

            IReadOnlyList<string> arcanumIds = playerId >= 0 && playerId < session.Players.Count
                ? session.Players[playerId].Arcanum
                : Array.Empty<string>();
            RebuildArcanumZone(scope, arcanumIds, session, onInspect, session, playerId);

            SetLabel(scope, "commune-spread-count", spreadIds.Count.ToString());
            SetLabel(scope, "commune-hand-count", handIds.Count.ToString());
            SetLabel(scope, "commune-arcanum-count", arcanumIds.Count.ToString());
        }

        public static VisualElement? AppendRoutedCard(VisualElement? scope, GameSession session,
            HarvestCardRoutedEvent routed, ZodiacSign referenceSign, Action<string>? onInspect = null)
        {
            if (scope == null) return null;

            string containerName = routed.Target switch
            {
                HarvestRouteTarget.Hand => "hand-cards",
                HarvestRouteTarget.Arcanum => "arcanum-cards",
                HarvestRouteTarget.AdeptLimbo => "arcanum-cards",
                _ => null!
            };
            if (containerName == null) return null;

            var zone = scope.Q<VisualElement>(containerName);
            if (zone == null) return null;

            VisualElement chip;
            if (routed.Target is HarvestRouteTarget.Arcanum or HarvestRouteTarget.AdeptLimbo)
            {
                var inst = session.GetCard(routed.CardId);
                var db = session.Rules?.CardDatabase;
                var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;
                chip = def != null
                    ? CardChipFactory.CreateForArcanum(def, instanceId: routed.CardId, onInspect: onInspect, inspectViaButton: true)
                    : new VisualElement();
                if (routed.Target == HarvestRouteTarget.AdeptLimbo)
                    chip.AddToClassList("card-chip--pending-adept");
            }
            else
            {
                chip = TapSwapBindings.BuildChip(session, routed.CardId, referenceSign, (_, _) => { }, fromSpread: false);
            }

            chip.userData = routed.CardId;
            zone.Add(chip);
            UiMotion.FadeIn(chip);

            UpdateCountsFromSession(scope, session, routed.PlayerId);
            return chip;
        }

        public static void ClearHandZone(VisualElement? scope, GameSession session, int playerId, ZodiacSign referenceSign)
        {
            if (scope == null) return;
            var zone = scope.Q<VisualElement>("hand-cards");
            zone?.Clear();
            UpdateCountsFromSession(scope, session, playerId);
        }

        static void UpdateCountsFromSession(VisualElement scope, GameSession session, int playerId)
        {
            if (playerId < 0 || playerId >= session.Players.Count) return;
            var player = session.Players[playerId];

            int spreadCount = 0;
            foreach (var id in player.Spread)
                if (TapSwapBindings.IsMinorArcana(session, id)) spreadCount++;

            SetLabel(scope, "commune-spread-count", spreadCount.ToString());
            SetLabel(scope, "commune-hand-count", player.Hand.Count.ToString());
            SetLabel(scope, "commune-arcanum-count", player.Arcanum.Count.ToString());
        }

        static int ResolvePlayerIdFromCards(GameSession session, IReadOnlyList<string> spreadIds, IReadOnlyList<string> handIds)
        {
            foreach (var id in handIds)
            {
                var inst = session.GetCard(id);
                if (inst?.OwnerId >= 0) return inst.OwnerId;
            }
            foreach (var id in spreadIds)
            {
                var inst = session.GetCard(id);
                if (inst?.OwnerId >= 0) return inst.OwnerId;
            }
            return 0;
        }

        static void RebuildZone(VisualElement root, string containerName, IReadOnlyList<string> cardIds,
            GameSession session, ZodiacSign referenceSign, Action<string, bool>? onTap, bool fromSpread,
            Action<string>? onInspect)
        {
            var zone = root.Q<VisualElement>(containerName);
            if (zone == null) return;
            zone.Clear();
            foreach (var id in cardIds)
            {
                VisualElement chip;
                if (onTap != null)
                    chip = TapSwapBindings.BuildChip(session, id, referenceSign, onTap, fromSpread);
                else
                {
                    var inst = session.GetCard(id);
                    var db = session.Rules?.CardDatabase;
                    var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;
                    chip = def != null
                        ? CardChipFactory.CreateFromDefinition(def, instanceId: id, onInspect: onInspect, inspectViaButton: true)
                        : new VisualElement();
                }
                zone.Add(chip);
            }
        }

        static void RebuildArcanumZone(VisualElement root, IReadOnlyList<string> cardIds,
            GameSession session, Action<string>? onInspect,
            GameSession? pendingSession = null, int playerId = -1)
        {
            var zone = root.Q<VisualElement>("arcanum-cards");
            if (zone == null) return;
            zone.Clear();
            var db = session.Rules?.CardDatabase;
            foreach (var id in cardIds)
            {
                var inst = session.GetCard(id);
                var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;
                if (def == null) continue;
                zone.Add(CardChipFactory.CreateForArcanum(def, instanceId: id, onInspect: onInspect, inspectViaButton: true));
            }

            if (pendingSession != null && playerId >= 0)
            {
                foreach (var (pid, adeptId) in pendingSession.Board.PendingAdeptDecisions)
                {
                    if (pid != playerId) continue;
                    if (ContainsCard(cardIds, adeptId)) continue;
                    var inst = pendingSession.GetCard(adeptId);
                    var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;
                    if (def == null) continue;
                    var chip = CardChipFactory.CreateForArcanum(def, instanceId: adeptId, onInspect: onInspect, inspectViaButton: true);
                    chip.AddToClassList("card-chip--pending-adept");
                    zone.Add(chip);
                }
            }
        }

        static bool ContainsCard(IReadOnlyList<string> cardIds, string cardId)
        {
            for (int i = 0; i < cardIds.Count; i++)
                if (cardIds[i] == cardId) return true;
            return false;
        }

        static void SetLabel(VisualElement root, string name, string text)
        {
            var lbl = root.Q<Label>(name);
            if (lbl != null) lbl.text = text;
        }
    }
}
