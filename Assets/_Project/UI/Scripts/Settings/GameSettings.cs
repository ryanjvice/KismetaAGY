using Kismeta.UI.Narrative;
using UnityEngine;

namespace Kismeta.UI.Settings
{
    /// <summary>Persisted player preferences loaded at bootstrap.</summary>
    public static class GameSettings
    {
        private const string KeyMasterVolume = "kismeta.masterVolume";
        private const string KeyMusicVolume = "kismeta.musicVolume";
        private const string KeySfxVolume = "kismeta.sfxVolume";
        private const string KeyReducedMotion = "kismeta.reducedMotion";
        private const string KeyNarrativeVerbosity = "kismeta.narrativeVerbosity";

        public const string RulesUrl = "https://www.kismeta.fun";

        public static void OpenRulesWiki() => Application.OpenURL(RulesUrl);

        public static float MasterVolume { get; private set; } = 1f;
        public static float MusicVolume { get; private set; } = 1f;
        public static float SfxVolume { get; private set; } = 1f;
        public static bool ReducedMotion { get; private set; }
        public static NarrativeVerbosity NarrativeVerbosity { get; private set; } = NarrativeVerbosity.Light;

        public static void Load()
        {
            MasterVolume = PlayerPrefs.GetFloat(KeyMasterVolume, 1f);
            MusicVolume = PlayerPrefs.GetFloat(KeyMusicVolume, 1f);
            SfxVolume = PlayerPrefs.GetFloat(KeySfxVolume, 1f);
            ReducedMotion = PlayerPrefs.GetInt(KeyReducedMotion, 0) == 1;
            NarrativeVerbosity = (NarrativeVerbosity)PlayerPrefs.GetInt(
                KeyNarrativeVerbosity, (int)NarrativeVerbosity.Light);
            Apply();
        }

        public static void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KeyMasterVolume, MasterVolume);
            ApplyAudio();
            PlayerPrefs.Save();
        }

        public static void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KeyMusicVolume, MusicVolume);
            PlayerPrefs.Save();
        }

        public static void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(KeySfxVolume, SfxVolume);
            PlayerPrefs.Save();
        }

        public static void SetReducedMotion(bool enabled)
        {
            ReducedMotion = enabled;
            PlayerPrefs.SetInt(KeyReducedMotion, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void SetNarrativeVerbosity(NarrativeVerbosity verbosity)
        {
            NarrativeVerbosity = verbosity;
            PlayerPrefs.SetInt(KeyNarrativeVerbosity, (int)verbosity);
            NarrativeVerbositySettings.Default = verbosity;
            PlayerPrefs.Save();
        }

        private static void Apply()
        {
            ApplyAudio();
            NarrativeVerbositySettings.Default = NarrativeVerbosity;
        }

        private static void ApplyAudio() => AudioListener.volume = MasterVolume;
    }
}
