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
        Arcanum
    }
}
