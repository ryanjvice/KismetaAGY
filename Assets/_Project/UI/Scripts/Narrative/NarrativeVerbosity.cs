namespace Kismeta.UI.Narrative
{
    public enum NarrativeVerbosity
    {
        Full = 0,
        Light = 1,
        Terse = 2
    }

    /// <summary>Global narrative verbosity; driven by <see cref="Settings.GameSettings"/>.</summary>
    public static class NarrativeVerbositySettings
    {
        public static NarrativeVerbosity Default { get; set; } = NarrativeVerbosity.Light;
    }
}
