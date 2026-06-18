using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>
    /// Base for every screen controller. Caches the root VisualElement and
    /// exposes small Q helpers. Derive a controller per UXML, override Wire()
    /// to hook buttons, and (optionally) Bind() to push game state into the view.
    /// Attach alongside a UIDocument that has the screen's UXML as its sourceAsset.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public abstract class ScreenController : MonoBehaviour
    {
        protected UIDocument Doc { get; private set; }
        protected VisualElement Root { get; private set; }

        protected virtual void OnEnable()
        {
            Doc = GetComponent<UIDocument>();
            Root = Doc.rootVisualElement;
            Wire();
            Bind();
        }

        protected virtual void OnDisable() { Unwire(); }

        /// <summary>Hook button.clicked handlers here.</summary>
        protected abstract void Wire();

        /// <summary>Optional: detach handlers. Default no-op.</summary>
        protected virtual void Unwire() { }

        /// <summary>Optional: populate dynamic content from game state.</summary>
        protected virtual void Bind() { }

        // --- query helpers ---
        protected Button Btn(string name) => Root.Q<Button>(name);
        protected Label Lbl(string name) => Root.Q<Label>(name);
        protected VisualElement El(string name) => Root.Q<VisualElement>(name);

        /// <summary>Swap a state class on an element (e.g. step done/active/locked).</summary>
        protected static void SetState(VisualElement el, string baseClass, string activeState)
        {
            el.RemoveFromClassList($"{baseClass}--done");
            el.RemoveFromClassList($"{baseClass}--active");
            el.RemoveFromClassList($"{baseClass}--locked");
            if (!string.IsNullOrEmpty(activeState))
                el.AddToClassList($"{baseClass}--{activeState}");
        }
    }
}
