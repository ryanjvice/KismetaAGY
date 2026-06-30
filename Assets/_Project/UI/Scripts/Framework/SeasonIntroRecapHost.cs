using System;
using Kismeta.Core.Domain;
using Kismeta.UI.Components;
using Kismeta.UI.Controllers;
using Kismeta.UI.Narrative;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>
    /// Shows season intro UXML as a dismissible recap overlay from hub toolbar info buttons.
    /// Does not advance <see cref="CeremonyGate"/>.
    /// </summary>
    public sealed class SeasonIntroRecapHost : MonoBehaviour
    {
        VisualTreeAsset? _springIntro;
        VisualTreeAsset? _summerIntro;
        VisualTreeAsset? _autumnIntro;
        VisualTreeAsset? _winterIntro;

        ViewportLayout? _layout;
        Action? _beginClickHandler;
        Button? _wiredBeginBtn;

        public bool IsOpen => _layout != null && _layout.IsOverlayVisible;

        void Awake() => _layout ??= GetComponent<ViewportLayout>();

        public void Configure(
            VisualTreeAsset? springIntro,
            VisualTreeAsset? summerIntro,
            VisualTreeAsset? autumnIntro,
            VisualTreeAsset? winterIntro)
        {
            _springIntro = springIntro;
            _summerIntro = summerIntro;
            _autumnIntro = autumnIntro;
            _winterIntro = winterIntro;
            _layout ??= GetComponent<ViewportLayout>();
        }

        public void Show(Season season)
        {
            var asset = ResolveAsset(season);
            if (_layout == null || asset == null)
                return;

            UnwireBegin();
            _layout.ShowModal(asset);
            _layout.ApplyBoundedOverlaySheet();

            var root = _layout.OverlayContentRoot;
            if (root == null)
                return;

            CeremonyBindings.ApplySeasonIntroClass(root, season);
            UiArtBindings.ApplyIntroSigil(root);
            NarrativeSlotBindings.BindById(root, NarrativeStepResolver.ResolveSeasonIntro(season));

            var begin = root.Q<Button>("begin-btn");
            if (begin != null)
            {
                begin.text = "Close";
                _beginClickHandler ??= Dismiss;
                begin.clicked += _beginClickHandler;
                _wiredBeginBtn = begin;
            }
        }

        public void Dismiss()
        {
            UnwireBegin();
            _layout?.DismissOverlay();
        }

        VisualTreeAsset? ResolveAsset(Season season) => season switch
        {
            Season.Spring => _springIntro,
            Season.Summer => _summerIntro,
            Season.Autumn => _autumnIntro,
            Season.Winter => _winterIntro,
            _ => null
        };

        void UnwireBegin()
        {
            if (_wiredBeginBtn != null && _beginClickHandler != null)
                _wiredBeginBtn.clicked -= _beginClickHandler;
            _wiredBeginBtn = null;
        }
    }
}
