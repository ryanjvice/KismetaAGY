using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Rules;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Maps gameplay events to one-line narrative beats about effect state changes.</summary>
    public static class EffectChangeNotifier
    {
        public static string? TryGetBeat(IGameEvent evt, GameSession? session)
        {
            switch (evt)
            {
                case CosmicAgeSetEvent age:
                {
                    var (name, desc) = CosmicEffectDescriber.DescribeCosmicAge(age.Sign);
                    return $"{name} — {desc}";
                }
                case FateResolvedEvent fate when fate.ArcanaNum == 11:
                    return "Justice binds Duels and Gambits to best-of-three this age.";
                case FateResolvedEvent fate when session != null:
                    return DescribeFateBeat(session, fate);
                case TowerFateResolvedEvent tower when tower.AdeptsArrested > 0:
                    return $"Tower arrested {tower.AdeptsArrested} rival Adept{(tower.AdeptsArrested == 1 ? "" : "s")} this age.";
                default:
                    return null;
            }
        }

        public static void ApplyBeat(VisualElement? root, string beat)
        {
            if (root == null || string.IsNullOrWhiteSpace(beat))
                return;

            var label = root.Q<Label>("narrative-beat");
            if (label != null)
                label.text = beat;
        }

        static string? DescribeFateBeat(GameSession session, FateResolvedEvent fate)
        {
            var inst = session.GetCard(fate.FateCardId);
            var db = session.Rules?.CardDatabase;
            var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;
            if (def == null || string.IsNullOrWhiteSpace(def.Name))
                return null;

            if (!string.IsNullOrWhiteSpace(inst?.FateResolutionNote))
                return $"{def.Name} — {inst.FateResolutionNote}";

            return $"{def.Name} resolved — check Active Effects for details.";
        }
    }
}
