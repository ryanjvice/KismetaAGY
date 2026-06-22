using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>
    /// Base for screen controllers driven by <see cref="ScreenRouter"/>.
    /// Controllers live on the UI host GameObject; the router calls <see cref="AttachTo"/>
    /// when a screen UXML is loaded into the content layer.
    /// </summary>
    public abstract class ScreenController : MonoBehaviour, IVisualRootController
    {
        protected VisualElement Root { get; private set; }
        protected bool IsAttached => Root != null;

        public abstract string ScreenId { get; }

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

        protected static void SetState(VisualElement el, string baseClass, string activeState)
        {
            if (el == null) return;
            el.RemoveFromClassList($"{baseClass}--done");
            el.RemoveFromClassList($"{baseClass}--active");
            el.RemoveFromClassList($"{baseClass}--locked");
            if (!string.IsNullOrEmpty(activeState))
                el.AddToClassList($"{baseClass}--{activeState}");
        }
    }
}
