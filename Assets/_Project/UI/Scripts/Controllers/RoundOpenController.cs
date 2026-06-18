using Kismeta.Core.Commands;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class RoundOpenController : ScreenController
    {
        public override string ScreenId => ScreenIds.RoundOpen;

        CeremonyGate? _gate;
        GameSession? _session;

        protected override void Wire()
        {
            Btn("roll-btn")!.clicked += OnRollClicked;
        }

        public void BindState(GameSession session, CeremonyGate gate)
        {
            _session = session;
            _gate = gate;
            if (Root == null) return;
            CeremonyBindings.BindRoundOpen(Root, session, 0);
        }

        void OnRollClicked()
        {
            if (_session == null || _gate == null) return;
            int keeperId = CeremonyBindings.FindAgekeeperId(_session);
            _gate.CompleteWithCommand(new RollCosmicAgeCommand(keeperId));
        }
    }
}
