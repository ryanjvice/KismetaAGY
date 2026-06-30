using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public abstract class SeasonIntroControllerBase : ScreenController
    {
        protected abstract Season IntroSeason { get; }

        CeremonyGate? _gate;

        public VisualTreeAsset? OverviewAsset { get; set; }

        protected override void Wire()
        {
            SeasonInfoRecapBindings.WireTabs(Root);
            var btn = Btn(SeasonInfoRecapBindings.PrimaryActionBtnName);
            if (btn != null)
                btn.clicked += OnPrimaryAction;
        }

        protected override void Unwire()
        {
            var btn = Btn(SeasonInfoRecapBindings.PrimaryActionBtnName);
            if (btn != null)
                btn.clicked -= OnPrimaryAction;
        }

        public void BindState(GameSession session, CeremonyGate gate)
        {
            _gate = gate;
            if (Root == null)
                return;

            SeasonInfoRecapBindings.PopulateCeremony(Root, IntroSeason, OverviewAsset);
        }

        void OnPrimaryAction() => _gate?.Complete();
    }
}
