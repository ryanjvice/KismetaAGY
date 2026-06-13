using Kismeta.Core.Domain;

namespace Kismeta.Core.Players
{
    /// <summary>Identifies a player slot in the game — index, color, and controller type.</summary>
    public sealed class PlayerSlot
    {
        public int Index { get; }
        public PlayerColor Color { get; }
        public PlayerControllerType ControllerType { get; }

        public PlayerSlot(int index, PlayerColor color, PlayerControllerType controllerType)
        {
            Index = index;
            Color = color;
            ControllerType = controllerType;
        }
    }

    public enum PlayerControllerType
    {
        LocalHuman = 0,
        LocalAI,
        NetworkRemote
    }
}
