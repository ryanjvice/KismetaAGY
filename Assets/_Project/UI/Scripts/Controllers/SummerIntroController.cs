using Kismeta.Core.Domain;
using Kismeta.UI;

namespace Kismeta.UI.Controllers
{
    public sealed class SummerIntroController : SeasonIntroControllerBase
    {
        public override string ScreenId => ScreenIds.SummerIntro;
        protected override Season IntroSeason => Season.Summer;
    }
}
