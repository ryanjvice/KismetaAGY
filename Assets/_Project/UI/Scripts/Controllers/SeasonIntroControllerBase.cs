using System;
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
        Action? _overviewClickHandler;

        public VisualTreeAsset? OverviewAsset { get; set; }
        public GameOverviewRecapHost? OverviewRecapHost { get; set; }

        protected override void Wire()
        {
            SeasonInfoRecapBindings.WireTabs(Root);
            var btn = Btn(SeasonInfoRecapBindings.PrimaryActionBtnName);
            if (btn != null)
                btn.clicked += OnPrimaryAction;

            _overviewClickHandler ??= OpenGreatYearOverview;
            SeasonInfoRecapBindings.WireGreatYearOverview(Root, _overviewClickHandler);
        }

        protected override void Unwire()
        {
            var btn = Btn(SeasonInfoRecapBindings.PrimaryActionBtnName);
            if (btn != null)
                btn.clicked -= OnPrimaryAction;

            if (_overviewClickHandler != null)
                SeasonInfoRecapBindings.UnwireGreatYearOverview(Root);
        }

        public void BindState(GameSession session, CeremonyGate gate)
        {
            _gate = gate;
            if (Root == null)
                return;

            SeasonInfoRecapBindings.PopulateCeremony(Root, IntroSeason, OverviewAsset);
            CeremonyRevealMotion.RevealSeasonInfoRecap(Root);
        }

        void OnPrimaryAction() => _gate?.Complete();

        void OpenGreatYearOverview() => OverviewRecapHost?.Show();
    }
}
