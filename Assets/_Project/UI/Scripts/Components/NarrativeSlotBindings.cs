using System;
using Kismeta.UI.Narrative;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    [Flags]
    public enum NarrativeSlotMask
    {
        Beat = 1,
        Charge = 2,
        Stakes = 4,
        All = Beat | Charge | Stakes
    }

    public static class NarrativeSlotBindings
    {
        public static bool BindById(
            VisualElement? root,
            string stepId,
            NarrativeVerbosity? verbosity = null,
            NarrativeSlotMask mask = NarrativeSlotMask.All)
        {
            if (root == null || string.IsNullOrEmpty(stepId))
                return false;

            var catalog = NarrativeSlotCatalog.Load();
            if (!catalog.TryGet(stepId, out var entry))
                return false;

            Bind(root, entry, verbosity ?? NarrativeVerbositySettings.Default, mask);
            return true;
        }

        public static void Bind(
            VisualElement? root,
            NarrativeSlotEntry entry,
            NarrativeVerbosity verbosity,
            NarrativeSlotMask mask = NarrativeSlotMask.All)
        {
            if (root == null || entry == null)
                return;

            bool showBeat = verbosity != NarrativeVerbosity.Terse;
            bool showStakes = verbosity != NarrativeVerbosity.Terse && entry.HasStakes;

            if (mask.HasFlag(NarrativeSlotMask.Beat))
                SetLabel(root, "narrative-beat", entry.Beat, showBeat);
            else
                SetLabel(root, "narrative-beat", string.Empty, false);

            if (mask.HasFlag(NarrativeSlotMask.Charge))
                SetLabel(root, "narrative-charge", entry.Charge, true);
            else
                SetLabel(root, "narrative-charge", string.Empty, false);

            if (mask.HasFlag(NarrativeSlotMask.Stakes))
            {
                var stakesWrap = root.Q<VisualElement>("narrative-stakes-wrap")
                    ?? root.Q(className: "narrative-stakes-wrap");
                if (stakesWrap != null)
                {
                    stakesWrap.style.display = showStakes ? DisplayStyle.Flex : DisplayStyle.None;
                    if (showStakes)
                    {
                        var stakesLbl = stakesWrap.Q<Label>("narrative-stakes")
                            ?? stakesWrap.Q<Label>(className: "narrative-stakes");
                        if (stakesLbl != null)
                            stakesLbl.text = entry.Stakes;
                    }
                }
                else
                {
                    SetLabel(root, "narrative-stakes", entry.Stakes, showStakes);
                }
            }
            else
            {
                var stakesWrap = root.Q<VisualElement>("narrative-stakes-wrap")
                    ?? root.Q(className: "narrative-stakes-wrap");
                if (stakesWrap != null)
                    stakesWrap.style.display = DisplayStyle.None;
                else
                    SetLabel(root, "narrative-stakes", string.Empty, false);
            }

            if (mask == NarrativeSlotMask.All)
                BindPrimaryVerb(root, entry);
        }

        static void SetLabel(VisualElement root, string name, string text, bool visible)
        {
            var lbl = root.Q<Label>(name);
            if (lbl == null)
                return;

            lbl.text = text ?? string.Empty;
            lbl.style.display = visible && !string.IsNullOrWhiteSpace(text)
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        static void BindPrimaryVerb(VisualElement root, NarrativeSlotEntry entry)
        {
            if (entry.Verbs == null || entry.Verbs.Length == 0)
                return;

            var begin = root.Q<Button>("begin-btn");
            if (begin != null)
                begin.text = entry.Verbs[0];
        }
    }
}
