using System;
using System.Collections.Generic;
using Kismeta.Core.Entities;
using Kismeta.UI.Controllers;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Shared tap-swap zone binding for Winter unlock (hub-embedded and standalone screen).</summary>
    public static class WinterUnlockZoneBindings
    {
        public struct ZoneBindState
        {
            public bool ZonesBuilt;
            public VisualElement? BuiltForRoot;
            public string LastLayoutKey;
        }

        public static void Reset(ref ZoneBindState state)
        {
            state.ZonesBuilt = false;
            state.BuiltForRoot = null;
            state.LastLayoutKey = "";
        }

        public static void BindZones(
            ref ZoneBindState state,
            VisualElement root,
            GameSession session,
            int playerId,
            Action<string, bool> onTap)
        {
            var player = session.Players[playerId];
            var spread = CollectMinor(session, player.Spread);
            var hand = CollectMinor(session, player.Hand);
            RenderZonesIfNeeded(ref state, root, session, spread, hand, onTap);
        }

        static List<string> CollectMinor(GameSession session, IEnumerable<string> ids)
        {
            var list = new List<string>();
            foreach (var id in ids)
            {
                if (TapSwapBindings.IsMinorArcana(session, id))
                    list.Add(id);
            }
            return list;
        }

        static void RenderZonesIfNeeded(
            ref ZoneBindState state,
            VisualElement root,
            GameSession session,
            IReadOnlyList<string> spread,
            IReadOnlyList<string> hand,
            Action<string, bool> onTap)
        {
            var layoutKey = LayoutKey(spread, hand);
            if (state.ZonesBuilt
                && ReferenceEquals(root, state.BuiltForRoot)
                && layoutKey == state.LastLayoutKey)
                return;

            TapSwapBindings.RebuildZones(
                root, session, spread, hand, session.Board.CosmicAgeSign, onTap);
            state.ZonesBuilt = true;
            state.BuiltForRoot = root;
            state.LastLayoutKey = layoutKey;
        }

        static string LayoutKey(IReadOnlyList<string> spread, IReadOnlyList<string> hand)
            => string.Join(",", spread) + "|" + string.Join(",", hand);
    }
}
