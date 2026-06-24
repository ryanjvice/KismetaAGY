using Kismeta.Core.Entities;
using UnityEngine;

namespace Kismeta.UI.Components
{
    /// <summary>Sprite catalog for forge progress icons indexed by <see cref="StonePosition.Value"/> (0–8).</summary>
    [CreateAssetMenu(fileName = "CrucibleForgeArt", menuName = "Kismeta/UI/Crucible Forge Art")]
    public sealed class CrucibleForgeArt : ScriptableObject
    {
        [SerializeField] Sprite[] _byPosition = new Sprite[9];

        public Sprite? Get(StonePosition position)
        {
            int index = position.Value;
            if (index < 0 || index >= _byPosition.Length)
                return null;
            return _byPosition[index];
        }
    }
}
