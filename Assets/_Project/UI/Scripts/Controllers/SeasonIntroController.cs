using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.UI;

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
        }
    }

    public sealed class SpringIntroController : SeasonIntroControllerBase
    {
        public override string ScreenId => ScreenIds.SpringIntro;
        protected override Season IntroSeason => Season.Spring;
    }

    public sealed class SummerIntroController : SeasonIntroControllerBase
    {
        public override string ScreenId => ScreenIds.SummerIntro;
        protected override Season IntroSeason => Season.Summer;
    }

    public sealed class AutumnIntroController : SeasonIntroControllerBase
    {
        public override string ScreenId => ScreenIds.AutumnIntro;
        protected override Season IntroSeason => Season.Autumn;
    }

    public sealed class WinterIntroController : SeasonIntroControllerBase
    {
        public override string ScreenId => ScreenIds.WinterIntro;
        protected override Season IntroSeason => Season.Winter;
    }
}
