using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.UI;
using Kismeta.UI.Components;
using Kismeta.UI.Narrative;

namespace Kismeta.UI.Controllers
{
    public abstract class SeasonIntroControllerBase : ScreenController
    {
        protected abstract Season IntroSeason { get; }

        CeremonyGate? _gate;

        protected override void Wire()
        {
            Btn("begin-btn")!.clicked += () => _gate?.Complete();
        }

        public void BindState(GameSession session, CeremonyGate gate)
        {
            _gate = gate;
            CeremonyBindings.ApplySeasonIntroClass(Root, IntroSeason);
            UiArtBindings.ApplyIntroSigil(Root);
            NarrativeSlotBindings.BindById(Root, NarrativeStepResolver.ResolveSeasonIntro(IntroSeason));
        }
    }
}
