using Kismeta.Core.Commands;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.UI;

namespace Kismeta.UI.Controllers
{
    public sealed class AgeClosingController : ScreenController
    {
        public override string ScreenId => ScreenIds.AgeClosing;

        CeremonyGate? _gate;

        protected override void Wire()
        {
            Btn("next-age-btn")!.clicked += () =>
                _gate?.CompleteWithCommand(new TransitAgeCommand());
        }

        public void BindState(GameSession session, CeremonyGate gate)
        {
            _gate = gate;
            CeremonyBindings.BindAgeClosing(Root, session);
        }
    }
}
