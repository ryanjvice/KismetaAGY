using Kismeta.Core.Domain;
using Kismeta.UI;

namespace Kismeta.UI.Controllers
{
    public sealed class AutumnIntroController : SeasonIntroControllerBase
    {
        public override string ScreenId => ScreenIds.AutumnIntro;
        protected override Season IntroSeason => Season.Autumn;
    }
}
