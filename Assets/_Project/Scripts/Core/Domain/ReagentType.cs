namespace Kismeta.Core.Domain
{
    /// <summary>
    /// The five alchemical reagents. Salt is element-neutral and requires no Cauldron.
    /// The elemental four correspond to Suits: Sulphur=Wands/Fire, AquaRegia=Cups/Water,
    /// Vitriol=Pentacles/Earth, Quicksilver=Swords/Air.
    /// </summary>
    public enum ReagentType
    {
        Sulphur = 0,
        AquaRegia,
        Vitriol,
        Quicksilver,
        Salt
    }
}
