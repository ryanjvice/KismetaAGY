using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>Shared attach/detach contract for screen and overlay controllers.</summary>
    public interface IVisualRootController
    {
        void AttachTo(VisualElement root);
        void Detach();
    }
}
