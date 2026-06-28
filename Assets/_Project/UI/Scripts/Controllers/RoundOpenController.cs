using System.Collections;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.UI;
using Kismeta.UI.Components;
using Kismeta.UI.Narrative;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class RoundOpenController : ScreenController
    {
        enum Phase { Cast, Rolling, Reveal }

        const int LocalPlayerId = 0;
        const float RevealPauseSec = 0.3f;
        const string RevealCharge =
            "Read the Sign, Planet, Element, and cosmic effect aloud.";

        public override string ScreenId => ScreenIds.RoundOpen;

        CeremonyGate? _gate;
        GameSession? _session;
        Phase _phase = Phase.Cast;
        ZodiacSign _signAtCeremonyStart = ZodiacSign.None;
        bool _capturedCeremonyStartSign;

        protected override void Wire()
        {
            Btn("roll-btn")!.clicked += OnRollClicked;
            Btn("enter-btn")!.clicked += () => _gate?.Complete();
        }

        protected override void Unwire()
        {
            _phase = Phase.Cast;
            _capturedCeremonyStartSign = false;
            _signAtCeremonyStart = ZodiacSign.None;
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
            {
                CeremonyBindings.BindRoundOpen(Root, session, LocalPlayerId);
                NarrativeSlotBindings.BindById(
                    Root,
                    "spring.setage",
                    mask: NarrativeSlotMask.Charge | NarrativeSlotMask.Stakes);
            }
            else
            {
                CeremonyBindings.BindAgeOpening(Root, session);
                BindRevealCharge();
            }
        }

        void BindRevealCharge()
        {
            var lbl = Lbl("reveal-charge");
            if (lbl != null)
                lbl.text = RevealCharge;
        }

        void ResolveInitialPhase(GameSession session)
        {
            if (_phase == Phase.Reveal) return;

            if (!_capturedCeremonyStartSign)
            {
                _signAtCeremonyStart = session.Board.CosmicAgeSign;
                _capturedCeremonyStartSign = true;
            }

            int keeperId = CeremonyBindings.FindAgekeeperId(session);
            bool humanAgekeeper = keeperId == LocalPlayerId;

            if (humanAgekeeper
                && session.Board.CosmicAgeSign != ZodiacSign.None
                && session.Board.CosmicAgeSign != _signAtCeremonyStart)
            {
                _phase = Phase.Reveal;
                return;
            }

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
            BindRevealCharge();
        }
    }
}
