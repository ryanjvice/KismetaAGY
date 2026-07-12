namespace Kismeta.Core.Domain
{
    /// <summary>
    /// Zone a card currently occupies. Spread and Arcanum are public; Hand is hidden.
    /// </summary>
    public enum CardZone
    {
        Deck = 0,
        Discard,
        Spread,
        Hand,
        Arcanum,
        /// <summary>Shared Fool Fate altar — one Crucible card awaiting claim.</summary>
        Altar
    }
}
