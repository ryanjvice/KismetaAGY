namespace Kismeta.Core.Domain
{
    /// <summary>
    /// Minor Arcana come in pairs per rank/suit. Variant 1 carries the base Active Effect;
    /// Variant 2 carries a Wildcard Link to a Major Arcana plus its own Active Effect.
    /// </summary>
    public enum CardVariant
    {
        NotApplicable = 0,
        One = 1,
        Two = 2
    }
}
