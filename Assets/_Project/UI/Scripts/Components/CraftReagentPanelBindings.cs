using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.UI.Controllers;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Shared reagent picker / forge UI for craft screens.</summary>
    public static class CraftReagentPanelBindings
    {
        public static readonly string[] ReagentKeys = { "salt", "sulphur", "vitriol", "aqua", "quicksilver" };

        public static bool HasAnyCraftableReagent(PlayerState player)
        {
            // Salt is always available; elemental reagents require a lit cauldron.
            if (player == null) return false;
            return true;
        }

        public static void LockReagentPicks(VisualElement root, PlayerState player)
        {
            foreach (var key in ReagentKeys)
            {
                var btn = root.Q<Button>($"pick-{key}");
                if (btn == null) continue;

                var type = KeyToReagent(key);
                bool locked = type != ReagentType.Salt
                    && !player.IsCauldronLit(Correspondence.SuitFor(type));
                btn.EnableInClassList("reagent-pick--locked", locked);
                btn.SetEnabled(!locked);
            }
        }

        public static void SetActiveReagentPick(VisualElement root, ReagentType reagent)
        {
            var activeKey = ReagentKeyFor(reagent);
            foreach (var k in ReagentKeys)
                root.Q<Button>($"pick-{k}")?.EnableInClassList("reagent-pick--active", k == activeKey);
        }

        public static bool IsReagentPickLocked(VisualElement root, string key)
        {
            var btn = root.Q<Button>($"pick-{key}");
            return btn != null && btn.ClassListContains("reagent-pick--locked");
        }

        public static void UpdateCauldronNote(VisualElement root, GameSession session, PlayerState player,
            ReagentType reagent)
        {
            var note = root.Q<Label>("cauldron-note");
            if (note == null) return;

            if (reagent == ReagentType.Salt)
            {
                note.text = "Salt accepts any cards from your spread or hand.";
                return;
            }

            var suit = Correspondence.SuitFor(reagent);
            int need = EffectiveCost(session, player, reagent);
            bool lit = player.IsCauldronLit(suit);
            var color = CauldronNameFor(suit);
            note.text = lit
                ? $"{reagent} needs the lit {color} cauldron — discard {need} {suit}."
                : $"The {color} cauldron must be lit before crafting {reagent}. Activate a crucible card to light it.";
        }

        public static void RefreshForgeButton(VisualElement root, GameSession session, PlayerState player,
            ReagentType reagent, int selectedCount)
        {
            var b = root.Q<Button>("forge-btn");
            var countLbl = root.Q<Label>("pick-count");
            if (b == null) return;

            int need = EffectiveCost(session, player, reagent);
            if (countLbl != null) countLbl.text = $"{selectedCount} / {need}";

            var contract = root.Q<Label>("contract-line");
            if (contract != null)
            {
                if (reagent == ReagentType.Salt)
                    contract.text = $"{need} cards → 1 Salt";
                else
                {
                    var suit = Correspondence.SuitFor(reagent);
                    var color = CauldronNameFor(suit);
                    contract.text = $"{need} {suit} → 1 {reagent} into the {color} cauldron";
                }
            }

            bool ready = selectedCount >= need;
            b.SetEnabled(ready);
            b.EnableInClassList("btn--disabled", !ready);
            b.EnableInClassList("btn--primary", ready);
            b.text = ready
                ? "Forge the reagent"
                : $"Forge — needs {need - selectedCount} more card{(need - selectedCount == 1 ? "" : "s")}";
        }

        public static int EffectiveCost(GameSession session, PlayerState player, ReagentType reagent)
            => SummerActionBindings.CraftEffectiveCost(session, player, reagent);

        public static ReagentType KeyToReagent(string key) => key switch
        {
            "sulphur" => ReagentType.Sulphur,
            "vitriol" => ReagentType.Vitriol,
            "aqua" => ReagentType.AquaRegia,
            "quicksilver" => ReagentType.Quicksilver,
            _ => ReagentType.Salt
        };

        public static string ReagentKeyFor(ReagentType type) => type switch
        {
            ReagentType.Sulphur => "sulphur",
            ReagentType.Vitriol => "vitriol",
            ReagentType.AquaRegia => "aqua",
            ReagentType.Quicksilver => "quicksilver",
            _ => "salt"
        };

        public static string CauldronNameFor(Suit suit) => suit switch
        {
            Suit.Wands => "Red",
            Suit.Cups => "Blue",
            Suit.Pentacles => "Green",
            Suit.Swords => "Yellow",
            _ => ""
        };

        public static string ReagentDisplayName(ReagentType type) => type switch
        {
            ReagentType.AquaRegia => "Aqua Regia",
            _ => type.ToString()
        };

        public static string ReagentDotClass(ReagentType type) => type switch
        {
            ReagentType.Sulphur => "reagent-dot--sulphur",
            ReagentType.Vitriol => "reagent-dot--vitriol",
            ReagentType.AquaRegia => "reagent-dot--aqua",
            ReagentType.Quicksilver => "reagent-dot--quick",
            _ => "reagent-dot--salt"
        };
    }
}
