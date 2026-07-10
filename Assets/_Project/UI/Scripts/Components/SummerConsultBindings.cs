using System;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    public enum SummerConsultView
    {
        Table,
        Zodiac,
        Crucible
    }

    /// <summary>Toggles Summer main-scene consult panels (Table / Zodiac / Crucible).</summary>
    public static class SummerConsultBindings
    {
        const string TableStageName = "summer-consult-table";
        const string ZodiacStageName = "summer-consult-zodiac";
        const string CrucibleStageName = "summer-consult-crucible";
        const string CrucibleSectionName = "summer-consult-crucible-section";

        const string TableBtnName = "consult-table-btn";
        const string ZodiacBtnName = "consult-zodiac-btn";
        const string CrucibleBtnName = "consult-crucible-btn";

        const string StageHiddenClass = "summer-consult-stage--hidden";
        const string CrucibleSectionHiddenClass = "summer-consult-crucible-section--hidden";
        const string ActiveBtnClass = "consult-btn--active";

        public static void ApplyView(
            VisualElement? root,
            SummerConsultView view,
            Action? onDismissInspect = null)
        {
            if (root == null) return;

            var nextHasInspect = IsInspectView(view);

            SetStageVisible(root, TableStageName, view == SummerConsultView.Table);
            SetStageVisible(root, ZodiacStageName, view == SummerConsultView.Zodiac);
            SetStageVisible(root, CrucibleStageName, view == SummerConsultView.Crucible);

            var crucibleSection = root.Q(CrucibleSectionName);
            crucibleSection?.EnableInClassList(
                CrucibleSectionHiddenClass,
                view != SummerConsultView.Crucible);

            CentralPanelInspectBindings.SetFabVisible(
                root,
                nextHasInspect,
                !nextHasInspect ? onDismissInspect : null);
        }

        public static void BindButtonStates(VisualElement? root, SummerConsultView view)
        {
            if (root == null) return;

            SetConsultButtonActive(root.Q<Button>(TableBtnName), view == SummerConsultView.Table);
            SetConsultButtonActive(root.Q<Button>(ZodiacBtnName), view == SummerConsultView.Zodiac);
            SetConsultButtonActive(root.Q<Button>(CrucibleBtnName), view == SummerConsultView.Crucible);
        }

        static bool IsInspectView(SummerConsultView view) =>
            view is SummerConsultView.Zodiac or SummerConsultView.Crucible;

        static void SetStageVisible(VisualElement root, string stageName, bool visible)
        {
            root.Q(stageName)?.EnableInClassList(StageHiddenClass, !visible);
        }

        static void SetConsultButtonActive(Button? button, bool active)
        {
            button?.EnableInClassList(ActiveBtnClass, active);
        }
    }
}
