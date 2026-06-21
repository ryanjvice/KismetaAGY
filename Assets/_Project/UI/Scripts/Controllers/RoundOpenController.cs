using System.Collections;
using Kismeta.Core.Commands;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.UI;
using Kismeta.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class RoundOpenController : ScreenController
    {
        public override string ScreenId => ScreenIds.RoundOpen;

        CeremonyGate? _gate;
        GameSession? _session;
        bool _rolling;

        protected override void Wire()
        {
            Btn("roll-btn")!.clicked += () => { if (!_rolling) StartCoroutine(CastAgeDie()); };
        }

        public void BindState(GameSession session, CeremonyGate gate)
        {
            _session = session;
            _gate = gate;
            if (Root == null) return;
            CeremonyBindings.BindRoundOpen(Root, session, 0);
        }

        IEnumerator CastAgeDie()
        {
            if (_session == null || _gate == null) yield break;
            _rolling = true;
            Btn("roll-btn")?.SetEnabled(false);

            var face = Lbl("die-face");
            yield return DieAnimator.RollLabel(face, Random.Range(1, 13));

            int keeperId = CeremonyBindings.FindAgekeeperId(_session);
            _gate.CompleteWithCommand(new RollCosmicAgeCommand(keeperId));
            _rolling = false;
        }
    }
}
