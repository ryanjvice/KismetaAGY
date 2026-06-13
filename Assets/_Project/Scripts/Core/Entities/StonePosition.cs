namespace Kismeta.Core.Entities
{
    /// <summary>
    /// Philosopher's Stone position on the Transmutation Track (0–8).
    /// Even positions (0,2,4,6) are Mantle Ring; odd (1,3,5,7) are Forge; 8 is the Altar.
    /// </summary>
    public readonly struct StonePosition
    {
        public readonly int Value;

        public StonePosition(int value) => Value = System.Math.Clamp(value, 0, 8);

        public bool IsMantle => Value % 2 == 0 && Value < 8;
        public bool IsForge  => Value % 2 != 0;
        public bool IsAltar  => Value == 8;

        public static readonly StonePosition Start = new(0);
        public static readonly StonePosition Altar = new(8);

        public StonePosition Advance() => new(Value + 1);

        public override string ToString() => Value switch
        {
            0 => "Lead (Mantle)",
            1 => "Lead Forge",
            2 => "Bronze (Mantle)",
            3 => "Bronze Forge",
            4 => "Silver (Mantle)",
            5 => "Silver Forge",
            6 => "Gold (Mantle)",
            7 => "Gold Forge",
            8 => "Altar",
            _ => $"Position {Value}"
        };
    }
}
