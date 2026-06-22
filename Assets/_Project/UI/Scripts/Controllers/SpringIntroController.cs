using Kismeta.Core.Domain;
using Kismeta.UI;

namespace Kismeta.UI.Controllers
{
    public sealed class SpringIntroController : SeasonIntroControllerBase
    {
        public override string ScreenId => ScreenIds.SpringIntro;
        protected override Season IntroSeason => Season.Spring;
    }
}
