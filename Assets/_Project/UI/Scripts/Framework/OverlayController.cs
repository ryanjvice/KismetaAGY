using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>
    /// Base for controllers attached to overlay UXML (bottom sheets / modals).
    /// Unlike <see cref="ScreenController"/>, these are not routed via <see cref="ScreenRouter"/>.
    /// </summary>
    public abstract class OverlayController : MonoBehaviour, IVisualRootController
    {
        protected VisualElement Root { get; private set; }
        protected bool IsAttached => Root != null;

        public void AttachTo(VisualElement root)
        {
            if (ReferenceEquals(Root, root))
            {
                Bind();
                return;
            }

            if (Root != null)
                Unwire();

            Root = root;
            Wire();
            Bind();
        }

        public void Detach()
        {
            if (Root == null)
                return;
            Unwire();
            Root = null;
        }

        public void Refresh() => Bind();

        protected abstract void Wire();
        protected virtual void Unwire() { }
        protected virtual void Bind() { }

        protected Button Btn(string name) => Root?.Q<Button>(name);
        protected Label Lbl(string name) => Root?.Q<Label>(name);
        protected VisualElement El(string name) => Root?.Q<VisualElement>(name);
    }
}
