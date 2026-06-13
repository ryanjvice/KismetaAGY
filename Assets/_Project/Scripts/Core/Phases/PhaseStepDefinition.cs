namespace Kismeta.Core.Phases
{
    /// <summary>
    /// Describes a single named step within a season (e.g. "Harvest" = Spring step 3).
    /// Summer steps are a free-order pool rather than a linear sequence; IsOrdered = false
    /// marks those action types.
    /// </summary>
    public sealed class PhaseStepDefinition
    {
        public int StepNumber { get; }
        public string Name { get; }
        public string Description { get; }
        /// <summary>When false the step belongs to Summer's free-order action pool.</summary>
        public bool IsOrdered { get; }

        public PhaseStepDefinition(int stepNumber, string name, string description, bool isOrdered = true)
        {
            StepNumber = stepNumber;
            Name = name;
            Description = description;
            IsOrdered = isOrdered;
        }
    }
}
