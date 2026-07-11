using System;
using UnityEngine.UIElements;

namespace Kismeta.UI.Components
{
    /// <summary>Toggles Spring hub consult panels (Table / Zodiac / Crucible).</summary>
    public static class SpringConsultBindings
    {
        const string TableStageName = "spring-consult-table";
        const string ZodiacStageName = "spring-consult-zodiac";
        const string CrucibleStageName = "spring-consult-crucible";
        const string CrucibleSectionName = "spring-consult-crucible-section";

        const string TableBtnName = "consult-table-btn";
        const string ZodiacBtnName = "consult-zodiac-btn";
        const string CrucibleBtnName = "consult-crucible-btn";

        const string StageHiddenClass = "spring-consult-stage--hidden";
        const string CrucibleSectionHiddenClass = "spring-consult-crucible-section--hidden";
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
