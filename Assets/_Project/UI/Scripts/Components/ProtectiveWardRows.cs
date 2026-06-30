using System;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    public static class ProtectiveWardRows
    {
        public enum TargetKind { Crucible, Adept, Forge }

        public readonly struct RowSpec
        {
            public RowSpec(string key, TargetKind kind, string title, string stateText, int existingWards,
                bool canAdjust, string? slotLabel = null)
            {
                Key = key;
                Kind = kind;
                Title = title;
                StateText = stateText;
                ExistingWards = existingWards;
                CanAdjust = canAdjust;
                SlotLabel = slotLabel;
            }

            public string Key { get; }
            public TargetKind Kind { get; }
            public string Title { get; }
            public string StateText { get; }
            public int ExistingWards { get; }
            public bool CanAdjust { get; }
            public string? SlotLabel { get; }
        }

        public static void Populate(
            VisualElement? root,
            GameSession session,
            int playerId,
            bool canPlaceCrucibleOrAdept,
            bool canPlaceForge,
            Action<string, int>? onAdjust)
        {
            if (root == null || playerId < 0 || playerId >= session.Players.Count)
                return;

            var player = session.Players[playerId];
            var db = session.Rules?.CardDatabase;

            ClearHost(root, "crucible-ward-rows");
            ClearHost(root, "adept-ward-rows");
            ClearHost(root, "forge-ward-row");

            var crucibleHost = root.Q<VisualElement>("crucible-ward-rows");
            var adeptHost = root.Q<VisualElement>("adept-ward-rows");
            var forgeHost = root.Q<VisualElement>("forge-ward-row");

            bool anyCrucible = false;
            for (int i = 0; i < player.CrucibleSlots.Count; i++)
            {
                var slot = player.CrucibleSlots[i];
                if (slot.State != CrucibleCardState.Active && slot.State != CrucibleCardState.Fired)
                    continue;

                anyCrucible = true;
                string title = $"Crucible slot {i}";
                if (db != null)
                {
                    var inst = session.GetCard(slot.CardInstanceId);
                    var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                    if (def != null)
                        title = def.Name;
                }

                string state = slot.WardCount > 0
                    ? $"{slot.WardCount} ward(s) placed"
                    : canPlaceCrucibleOrAdept
                        ? "unwarded — free to gambit"
                        : "unwarded — place during your Summer turn";

                crucibleHost?.Add(BuildRow(new RowSpec(
                    $"crucible-{i}",
                    TargetKind.Crucible,
                    title,
                    state,
                    slot.WardCount,
                    canPlaceCrucibleOrAdept,
                    i.ToString()), onAdjust));
            }
            if (!anyCrucible)
                crucibleHost?.Add(EmptyNote("No active crucible cards to ward."));

            bool anyAdept = false;
            foreach (var cardId in player.Arcanum)
            {
                var inst = session.GetCard(cardId);
                var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;
                if (def?.MajorArcanaType != MajorArcanaType.Adept)
                    continue;

                anyAdept = true;
                int wards = player.GetAdeptWardCount(cardId);
                string state = wards > 0
                    ? $"{wards} ward(s) placed"
                    : canPlaceCrucibleOrAdept
                        ? "unwarded — free to gambit"
                        : "unwarded — place during your Summer turn";

                adeptHost?.Add(BuildRow(new RowSpec(
                    $"adept-{cardId}",
                    TargetKind.Adept,
                    def.Name,
                    state,
                    wards,
                    canPlaceCrucibleOrAdept), onAdjust));
            }
            if (!anyAdept)
                adeptHost?.Add(EmptyNote("No adepts in Arcanum."));

            if (player.StoneState == StoneState.Forging)
            {
                string forgeTitle = $"Forge · {player.StonePosition}";
                string forgeState = player.StoneWardCount > 0
                    ? $"{player.StoneWardCount} ward(s) — Opposition entry fee"
                    : canPlaceForge
                        ? "unwarded — free to oppose"
                        : "unwarded — place during your Autumn turn while forging";

                forgeHost?.Add(BuildRow(new RowSpec(
                    "stone",
                    TargetKind.Forge,
                    forgeTitle,
                    forgeState,
                    player.StoneWardCount,
                    canPlaceForge,
                    "\u26cf"), onAdjust));
            }
            else
            {
                forgeHost?.Add(EmptyNote("Stone is not at a forge position."));
            }
        }

        static void ClearHost(VisualElement root, string name)
        {
            var host = root.Q<VisualElement>(name);
            host?.Clear();
        }

        static VisualElement EmptyNote(string text)
        {
            var note = new Label(text);
            note.style.fontSize = 10;
            note.style.color = new StyleColor(new Color(0.72f, 0.6f, 0.43f));
            note.style.marginTop = 4;
            note.style.marginBottom = 6;
            note.style.whiteSpace = WhiteSpace.Normal;
            return note;
        }

        static VisualElement BuildRow(RowSpec spec, Action<string, int>? onAdjust)
        {
            var row = new VisualElement();
            row.AddToClassList("stepper-row");

            var icon = new VisualElement();
            icon.style.width = 30;
            icon.style.height = 42;
            icon.style.borderTopLeftRadius = 6;
            icon.style.borderTopRightRadius = 6;
            icon.style.borderBottomLeftRadius = 6;
            icon.style.borderBottomRightRadius = 6;
            icon.style.alignItems = Align.Center;
            icon.style.justifyContent = Justify.Center;
            icon.style.marginRight = 10;

            if (spec.Kind == TargetKind.Adept)
            {
                icon.style.backgroundColor = new StyleColor(new Color(36f / 255f, 16f / 255f, 40f / 255f));
                icon.style.borderTopWidth = icon.style.borderBottomWidth = icon.style.borderLeftWidth = icon.style.borderRightWidth = 1;
                icon.style.borderTopColor = icon.style.borderBottomColor = icon.style.borderLeftColor = icon.style.borderRightColor =
                    new StyleColor(new Color(106f / 255f, 74f / 255f, 138f / 255f));
                var iconLbl = new Label("\uea8a");
                iconLbl.AddToClassList("ti-icon");
                iconLbl.style.color = new StyleColor(new Color(205f / 255f, 176f / 255f, 224f / 255f));
                icon.Add(iconLbl);
            }
            else if (spec.Kind == TargetKind.Forge)
            {
                icon.style.backgroundColor = new StyleColor(new Color(42f / 255f, 28f / 255f, 18f / 255f));
                icon.style.borderTopWidth = icon.style.borderBottomWidth = icon.style.borderLeftWidth = icon.style.borderRightWidth = 1;
                icon.style.borderTopColor = icon.style.borderBottomColor = icon.style.borderLeftColor = icon.style.borderRightColor =
                    new StyleColor(new Color(201f / 255f, 150f / 255f, 47f / 255f));
                var iconLbl = new Label(spec.SlotLabel ?? "\u26cf");
                iconLbl.style.fontSize = 14;
                iconLbl.style.color = new StyleColor(new Color(232f / 255f, 185f / 255f, 74f / 255f));
                icon.Add(iconLbl);
            }
            else
            {
                icon.style.backgroundColor = new StyleColor(new Color(58f / 255f, 26f / 255f, 14f / 255f));
                icon.style.borderTopWidth = icon.style.borderBottomWidth = icon.style.borderLeftWidth = icon.style.borderRightWidth = 1;
                icon.style.borderTopColor = icon.style.borderBottomColor = icon.style.borderLeftColor = icon.style.borderRightColor =
                    new StyleColor(new Color(201f / 255f, 150f / 255f, 47f / 255f));
                var iconLbl = new Label(spec.SlotLabel ?? "?");
                iconLbl.style.fontSize = 12;
                iconLbl.style.color = new StyleColor(new Color(232f / 255f, 185f / 255f, 74f / 255f));
                iconLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                icon.Add(iconLbl);
            }
            row.Add(icon);

            var info = new VisualElement { style = { flexGrow = 1 } };
            info.Add(new Label(spec.Title) { style = { fontSize = 12, color = new Color(243f / 255f, 233f / 255f, 210f / 255f) } });
            var stateColor = spec.ExistingWards > 0
                ? new Color(127f / 255f, 196f / 255f, 168f / 255f)
                : spec.CanAdjust
                    ? new Color(240f / 255f, 144f / 255f, 138f / 255f)
                    : new Color(0.72f, 0.6f, 0.43f);
            info.Add(new Label(spec.StateText) { style = { fontSize = 9, color = stateColor } });
            row.Add(info);

            var minus = new Button { name = $"ward-{spec.Key}-minus" };
            minus.AddToClassList("stepper-btn");
            minus.AddToClassList("stepper-btn--minus");
            minus.Add(new Label("−"));
            minus.SetEnabled(spec.CanAdjust);
            string key = spec.Key;
            minus.clicked += () => onAdjust?.Invoke(key, -1);

            var val = new Label("0") { name = $"ward-{spec.Key}-val" };
            val.AddToClassList("stepper-value");
            val.style.color = spec.ExistingWards > 0
                ? new StyleColor(new Color(127f / 255f, 196f / 255f, 168f / 255f))
                : new StyleColor(new Color(184f / 255f, 154f / 255f, 110f / 255f));

            var plus = new Button { name = $"ward-{spec.Key}-plus" };
            plus.AddToClassList("stepper-btn");
            plus.AddToClassList("stepper-btn--plus");
            plus.Add(new Label("+"));
            plus.SetEnabled(spec.CanAdjust);
            plus.clicked += () => onAdjust?.Invoke(key, +1);

            row.Add(minus);
            row.Add(val);
            row.Add(plus);

            return row;
        }
    }
}
