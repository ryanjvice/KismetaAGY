using System;
using Kismeta.UI.Components;
using Kismeta.UI.Narrative;

namespace Kismeta.UI.Controllers
{
    public sealed class GameOverviewIntroController : ScreenController
    {
        public override string ScreenId => ScreenIds.GameOverviewIntro;

        public Action? OnContinue;

        protected override void Wire()
        {
            Btn("continue-btn")!.clicked += OnContinueClicked;
        }

        protected override void Unwire()
        {
            Btn("continue-btn")!.clicked -= OnContinueClicked;
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
                Btn("continue-btn")!.text = entry.Verbs[0];
        }

        void OnContinueClicked() => OnContinue?.Invoke();
    }
}
