using System;
using Kismeta.UI.Components;
using Kismeta.UI.Narrative;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class GameOverviewIntroController : ScreenController
    {
        enum Step { GreatWork = 0, Seasons = 1 }

        static readonly string[] StepPanels = { "step-great-work", "step-seasons" };

        public override string ScreenId => ScreenIds.GameOverviewIntro;

        public Action? OnContinue;
        public Action? OnDismiss;
        public bool ReviewMode { get; set; }

        Step _step = Step.GreatWork;
        string _finalVerb = "Determine the Agekeeper";

        protected override void Wire()
        {
            Btn("continue-btn")!.clicked += OnContinueClicked;
        }

        protected override void Unwire()
        {
            Btn("continue-btn")!.clicked -= OnContinueClicked;
            _step = Step.GreatWork;
            ReviewMode = false;
            OnDismiss = null;
        }

        protected override void Bind()
        {
            if (!IsAttached)
                return;

            UiArtBindings.ApplyIntroSigil(Root);
            var stepId = NarrativeStepResolver.ResolveGameOverview();
            NarrativeSlotBindings.BindById(Root, stepId, mask: NarrativeSlotMask.Beat);

            var catalog = NarrativeSlotCatalog.Load();
            if (catalog.TryGet(stepId, out var entry) && entry.Verbs.Length > 0)
                _finalVerb = entry.Verbs[0];

            _step = Step.GreatWork;
            ShowStep((int)_step);
        }

        void OnContinueClicked()
        {
            if (_step == Step.GreatWork)
            {
                _step = Step.Seasons;
                ShowStep((int)_step);
                return;
            }

            if (ReviewMode)
                OnDismiss?.Invoke();
            else
                OnContinue?.Invoke();
        }

        void ShowStep(int index)
        {
            foreach (var id in StepPanels)
            {
                var el = El(id);
                if (el != null)
                    el.style.display = id == StepPanels[index] ? DisplayStyle.Flex : DisplayStyle.None;
            }

            SetWizard("overview-dot", 2, index + 1);
            Btn("continue-btn")!.text = index == 0
                ? "Continue"
                : ReviewMode ? "Close" : _finalVerb;
        }

        void SetWizard(string prefix, int total, int activeIndex)
        {
            for (int i = 1; i <= total; i++)
            {
                var dot = El($"{prefix}-{i}");
                if (dot == null) continue;
                dot.RemoveFromClassList("wizard-dot--active");
                dot.RemoveFromClassList("wizard-dot--done");
                if (i < activeIndex) dot.AddToClassList("wizard-dot--done");
                else if (i == activeIndex) dot.AddToClassList("wizard-dot--active");
            }
        }
    }
}
