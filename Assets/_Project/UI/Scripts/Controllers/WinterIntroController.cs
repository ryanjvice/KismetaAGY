using Kismeta.Core.Domain;
using Kismeta.UI;

namespace Kismeta.UI.Controllers
{
    public sealed class WinterIntroController : SeasonIntroControllerBase
    {
        public override string ScreenId => ScreenIds.WinterIntro;
        protected override Season IntroSeason => Season.Winter;
    }
}
