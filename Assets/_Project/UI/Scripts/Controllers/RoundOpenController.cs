using System.Collections;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
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
        enum Phase { Cast, Rolling, Reveal }

        const int LocalPlayerId = 0;
        const float RevealPauseSec = 0.3f;

        public override string ScreenId => ScreenIds.RoundOpen;

        CeremonyGate? _gate;
        GameSession? _session;
        Phase _phase = Phase.Cast;

        protected override void Wire()
        {
            Btn("roll-btn")!.clicked += OnRollClicked;
            Btn("enter-btn")!.clicked += () => _gate?.Complete();
        }

        protected override void Unwire()
        {
            _phase = Phase.Cast;
        }

        public void BindState(GameSession session, CeremonyGate gate)
        {
            _session = session;
            _gate = gate;
            if (Root == null) return;
            if (_phase == Phase.Rolling) return;

            ResolveInitialPhase(session);
            SetPhaseVisibility(_phase);

            if (_phase == Phase.Cast)
                CeremonyBindings.BindRoundOpen(Root, session, LocalPlayerId);
            else
                CeremonyBindings.BindAgeOpening(Root, session);
        }

        void ResolveInitialPhase(GameSession session)
        {
            if (_phase == Phase.Reveal) return;

            int keeperId = CeremonyBindings.FindAgekeeperId(session);
            bool humanAgekeeper = keeperId == LocalPlayerId;
            _phase = humanAgekeeper ? Phase.Cast : Phase.Reveal;
        }

        void OnRollClicked()
        {
            if (_phase != Phase.Cast) return;
            StartCoroutine(CastAgeDie());
        }

        void SetPhaseVisibility(Phase phase)
        {
            var cast = El("cast-phase");
            var reveal = El("reveal-phase");
            if (cast != null)
                cast.style.display = phase == Phase.Reveal ? DisplayStyle.None : DisplayStyle.Flex;
            if (reveal != null)
                reveal.style.display = phase == Phase.Reveal ? DisplayStyle.Flex : DisplayStyle.None;
        }

        IEnumerator CastAgeDie()
        {
            if (_session == null || _gate == null || Root == null) yield break;
            _phase = Phase.Rolling;
            Btn("roll-btn")?.SetEnabled(false);

            int keeperId = CeremonyBindings.FindAgekeeperId(_session);
            _session.Apply(new RollCosmicAgeCommand(keeperId));
            var sign = _session.Board.CosmicAgeSign;

            var face = Lbl("die-face");
            yield return DieAnimator.RollZodiacLabel(face, sign);
            yield return new WaitForSeconds(RevealPauseSec);

            _phase = Phase.Reveal;
            SetPhaseVisibility(Phase.Reveal);
            CeremonyBindings.BindAgeOpening(Root, _session);
        }
    }
}
