using System;
using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using Kismeta.Core.Views;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Binds the Summer main-scene cauldron hub from public player state.</summary>
    internal static class CauldronHubBindings
    {
        static readonly (string id, Suit suit, string abbrev, ReagentType reagent)[] Slots =
        {
            ("cauldron-n", Suit.Wands,     "RED",   ReagentType.Sulphur),
            ("cauldron-e", Suit.Cups,       "BLUE",  ReagentType.AquaRegia),
            ("cauldron-s", Suit.Pentacles,  "GREEN", ReagentType.Vitriol),
            ("cauldron-w", Suit.Swords,     "YEL",   ReagentType.Quicksilver),
        };

        static readonly Dictionary<VisualElement, List<(VisualElement el, EventCallback<ClickEvent> cb)>> Wired = new();

        public static void Bind(VisualElement? root, PublicPlayerView? player)
        {
            if (root == null) return;

            var hub = root.Q<VisualElement>("cauldron-hub") ?? root;
            int activeCrucibles = 0;

            foreach (var (id, suit, abbrev, reagent) in Slots)
            {
                var el = hub.Q<VisualElement>(id);
                if (el == null) continue;

                bool lit = player != null && player.IsCauldronLit(suit);

                el.EnableInClassList("cauldron--lit", lit);
                el.EnableInClassList("cauldron--dormant", !lit);

                var colorLbl = hub.Q<Label>($"{id}-color");
                var reagentLbl = hub.Q<Label>($"{id}-reagent");
                var stateLbl = hub.Q<Label>($"{id}-state");

                if (colorLbl != null)
                    colorLbl.text = abbrev;

                if (reagentLbl != null)
                    reagentLbl.text = ReagentLabel(reagent);

                if (stateLbl != null)
                    stateLbl.text = lit ? "\u2022 lit" : "dormant";
            }

            if (player != null)
            {
                foreach (var slot in player.CrucibleSlots)
                {
                    if (slot.State >= CrucibleCardState.Active)
                        activeCrucibles++;
                }
            }

            var codexCount = hub.Q<Label>("codex-hub-count");
            if (codexCount != null)
                codexCount.text = $"{activeCrucibles}/4";

            var codexStatus = hub.Q<Label>("codex-hub-status");
            if (codexStatus != null)
                codexStatus.text = activeCrucibles > 0 ? "activated" : "dormant";
        }

        public static bool TrySlotIndexForSuit(GameSession session, int playerId, Suit suit, out int slotIndex)
        {
            slotIndex = -1;
            if (playerId < 0 || playerId >= session.Players.Count) return false;

            var codexDb = session.Rules?.CodexDatabase;
            if (codexDb == null) return false;

            var player = session.Players[playerId];
            var cauldronName = CraftReagentPanelBindings.CauldronNameFor(suit);
            if (string.IsNullOrEmpty(cauldronName)) return false;

            for (int i = 0; i < 4; i++)
            {
                var formula = codexDb.GetFormula(player.AssignedCodex, i);
                if (formula == null) continue;
                if (string.Equals(formula.Cauldron, cauldronName, StringComparison.OrdinalIgnoreCase))
                {
                    slotIndex = i;
                    return true;
                }
            }

            return false;
        }

        public static void WireCauldrons(VisualElement? root, Action<Suit>? onTap, Action? onCodexTap = null)
        {
            if (root == null) return;

            var hub = root.Q<VisualElement>("cauldron-hub") ?? root;
            UnwireCauldrons(root);

            var entries = new List<(VisualElement el, EventCallback<ClickEvent> cb)>();

            if (onTap != null)
            {
                foreach (var (id, suit, _, _) in Slots)
                {
                    var el = hub.Q<VisualElement>(id);
                    if (el == null) continue;

                    Suit captured = suit;
                    EventCallback<ClickEvent> cb = _ => onTap(captured);
                    el.RegisterCallback(cb);
                    entries.Add((el, cb));
                }
            }

            if (onCodexTap != null)
            {
                var codex = hub.Q<VisualElement>("codex-hub");
                if (codex != null)
                {
                    EventCallback<ClickEvent> cb = _ => onCodexTap();
                    codex.RegisterCallback(cb);
                    entries.Add((codex, cb));
                }
            }

            if (entries.Count > 0)
                Wired[hub] = entries;
        }

        public static void UnwireCauldrons(VisualElement? root)
        {
            if (root == null) return;

            var hub = root.Q<VisualElement>("cauldron-hub") ?? root;
            if (!Wired.TryGetValue(hub, out var entries)) return;

            foreach (var (el, cb) in entries)
                el.UnregisterCallback(cb);

            Wired.Remove(hub);
        }

        static string ReagentLabel(ReagentType reagent) => reagent switch
        {
            ReagentType.Sulphur => "sulphur",
            ReagentType.AquaRegia => "aqua regia",
            ReagentType.Vitriol => "vitriol",
            ReagentType.Quicksilver => "quicksilver",
            _ => reagent.ToString().ToLowerInvariant()
        };
    }
}
