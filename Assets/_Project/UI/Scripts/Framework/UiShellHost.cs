using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>
    /// Phase 0 host: attaches a UIDocument to PanelSettings and an initial screen UXML.
    /// Adds <c>kismeta-root</c> so USS design tokens resolve on the visual tree root.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class UiShellHost : MonoBehaviour
    {
        [SerializeField] private PanelSettings _panelSettings;
        [SerializeField] private VisualTreeAsset _initialScreen;

        private UIDocument _document;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            if (_panelSettings != null)
                _document.panelSettings = _panelSettings;
            if (_initialScreen != null)
                _document.visualTreeAsset = _initialScreen;
        }

        private void OnEnable()
        {
            var root = _document != null ? _document.rootVisualElement : null;
            root?.AddToClassList("kismeta-root");
        }

        public void SetScreen(VisualTreeAsset screen)
        {
            if (_document == null) return;
            _document.visualTreeAsset = screen;
            _document.rootVisualElement?.AddToClassList("kismeta-root");
        }
    }
}
