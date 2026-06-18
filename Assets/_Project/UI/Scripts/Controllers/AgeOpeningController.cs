using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.UI;

namespace Kismeta.UI.Controllers
{
    public sealed class AgeOpeningController : ScreenController
    {
        public override string ScreenId => ScreenIds.AgeOpening;

        CeremonyGate? _gate;

        protected override void Wire()
        {
            Btn("enter-btn")!.clicked += () => _gate?.Complete();
        }

        public void BindState(GameSession session, CeremonyGate gate)
        {
            _gate = gate;
            CeremonyBindings.BindAgeOpening(Root, session);
        }
    }
}
