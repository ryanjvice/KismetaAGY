using System;
using Kismeta.UI.Settings;
using Kismeta.UI.Narrative;
using UnityEngine;
using UnityEngine.UIElements;
using Kismeta.UI;

namespace Kismeta.UI.Controllers
{
    public sealed class SettingsScreenController : ScreenController
    {
        public override string ScreenId => ScreenIds.Settings;

        public Action? OnBack;

        protected override void Wire()
        {
            Btn("back-btn")!.clicked += () => OnBack?.Invoke();
            Btn("rules-link-btn")!.clicked += () => Application.OpenURL(GameSettings.RulesUrl);

            Btn("verbosity-full")!.clicked += () => SetVerbosity(NarrativeVerbosity.Full);
            Btn("verbosity-light")!.clicked += () => SetVerbosity(NarrativeVerbosity.Light);
            Btn("verbosity-terse")!.clicked += () => SetVerbosity(NarrativeVerbosity.Terse);

            var reducedMotion = Root?.Q<Toggle>("reduced-motion-toggle");
            if (reducedMotion != null)
                reducedMotion.RegisterValueChangedCallback(evt =>
                    GameSettings.SetReducedMotion(evt.newValue));

            WireSlider("master-volume", GameSettings.SetMasterVolume);
            WireSlider("music-volume", GameSettings.SetMusicVolume);
            WireSlider("sfx-volume", GameSettings.SetSfxVolume);
        }

        protected override void Bind() => RefreshUi();

        void WireSlider(string name, Action<float> onChanged)
        {
            var slider = Root?.Q<Slider>(name);
            if (slider == null) return;
            slider.RegisterValueChangedCallback(evt => onChanged(evt.newValue));
        }

        void SetVerbosity(NarrativeVerbosity verbosity)
        {
            GameSettings.SetNarrativeVerbosity(verbosity);
            RefreshVerbosityPips();
        }

        void RefreshUi()
        {
            var reducedMotion = Root?.Q<Toggle>("reduced-motion-toggle");
            if (reducedMotion != null)
                reducedMotion.SetValueWithoutNotify(GameSettings.ReducedMotion);

            SetSliderValue("master-volume", GameSettings.MasterVolume);
            SetSliderValue("music-volume", GameSettings.MusicVolume);
            SetSliderValue("sfx-volume", GameSettings.SfxVolume);
            RefreshVerbosityPips();
        }

        void SetSliderValue(string name, float value)
        {
            var slider = Root?.Q<Slider>(name);
            if (slider != null)
                slider.SetValueWithoutNotify(value);
        }

        void RefreshVerbosityPips()
        {
            if (Root == null) return;
            var verbosity = GameSettings.NarrativeVerbosity;
            Btn("verbosity-full")!.EnableInClassList("player-pip--active", verbosity == NarrativeVerbosity.Full);
            Btn("verbosity-light")!.EnableInClassList("player-pip--active", verbosity == NarrativeVerbosity.Light);
            Btn("verbosity-terse")!.EnableInClassList("player-pip--active", verbosity == NarrativeVerbosity.Terse);
        }
    }
}
