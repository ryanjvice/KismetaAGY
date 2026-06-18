using UnityEngine;

namespace Kismeta.UI
{
    /// <summary>
    /// Backward-compatible alias for <see cref="ViewportLayout"/>.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.UIElements.UIDocument))]
    public sealed class UiShellHost : ViewportLayout
    {
    }
}
