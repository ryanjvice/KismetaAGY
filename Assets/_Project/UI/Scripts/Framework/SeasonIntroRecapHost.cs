using System;
using Kismeta.Core.Domain;
using Kismeta.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>
    /// Shows season info recap (Overview + Focus tabs) as a dismissible overlay from hub toolbar info buttons.
    /// Does not advance <see cref="CeremonyGate"/>.
    /// </summary>
    public sealed class SeasonIntroRecapHost : MonoBehaviour
    {
#if UNITY_EDITOR
        const string RecapShellPath = "Assets/_Project/UI/UXML/shell/SeasonInfoRecap.uxml";
        const string SpringIntroPath = "Assets/_Project/UI/UXML/batch2/SpringIntro.uxml";
        const string SummerIntroPath = "Assets/_Project/UI/UXML/batch2/SummerIntro.uxml";
        const string AutumnIntroPath = "Assets/_Project/UI/UXML/batch2/AutumnIntro.uxml";
        const string WinterIntroPath = "Assets/_Project/UI/UXML/batch2/WinterIntro.uxml";
#endif

        VisualTreeAsset? _recapShell;
        VisualTreeAsset? _springIntro;
        VisualTreeAsset? _summerIntro;
        VisualTreeAsset? _autumnIntro;
        VisualTreeAsset? _winterIntro;

        ViewportLayout? _layout;
        Action? _closeClickHandler;
        Button? _wiredCloseBtn;
        Action? _overviewClickHandler;
        GameOverviewRecapHost? _overviewRecapHost;

        public bool IsOpen => _layout != null && _layout.IsOverlayVisible;

        public GameOverviewRecapHost? OverviewRecapHost
        {
            get => _overviewRecapHost;
            set => _overviewRecapHost = value;
        }

        void Awake()
        {
            _layout ??= GetComponent<ViewportLayout>();
            EnsureAssets();
        }

        public void Configure(
            VisualTreeAsset? recapShell,
            VisualTreeAsset? springIntro,
            VisualTreeAsset? summerIntro,
            VisualTreeAsset? autumnIntro,
            VisualTreeAsset? winterIntro)
        {
            _recapShell = recapShell;
            _springIntro = springIntro;
            _summerIntro = summerIntro;
            _autumnIntro = autumnIntro;
            _winterIntro = winterIntro;
            _layout ??= GetComponent<ViewportLayout>();
            EnsureAssets();
        }

        public void Show(Season season)
        {
            _layout ??= GetComponent<ViewportLayout>();
            EnsureAssets();

            if (_layout == null)
            {
                Debug.LogWarning("[SeasonIntroRecapHost] ViewportLayout missing — cannot show recap.");
                return;
            }

            if (_recapShell == null)
            {
                Debug.LogWarning("[SeasonIntroRecapHost] SeasonInfoRecap shell is not assigned. Run Kismeta → UI → Wire Bootstrap UI References.");
                return;
            }

            var introAsset = ResolveIntroAsset(season);
            if (introAsset == null)
            {
                Debug.LogWarning($"[SeasonIntroRecapHost] No intro asset for {season}.");
                return;
            }

            UnwireClose();
            if (_layout.OverlayContentRoot != null)
                UnwireOverview(_layout.OverlayContentRoot);
            _layout.ShowModal(_recapShell);
            _layout.ApplyBoundedOverlaySheet();

            var root = _layout.OverlayContentRoot;
            if (root == null)
                return;

            SeasonInfoRecapBindings.PopulateRecap(root, season, introAsset);
            SeasonInfoRecapBindings.WireTabs(root);
            WireOverview(root);

            var close = root.Q<Button>(SeasonInfoRecapBindings.PrimaryActionBtnName);
            if (close != null)
            {
                _closeClickHandler ??= Dismiss;
                close.clicked += _closeClickHandler;
                _wiredCloseBtn = close;
            }
        }

        public void Dismiss()
        {
            UnwireClose();
            if (_layout?.OverlayContentRoot != null)
                UnwireOverview(_layout.OverlayContentRoot);
            _layout?.DismissOverlay();
        }

        public VisualTreeAsset? GetOverviewAsset(Season season) => ResolveIntroAsset(season);

        VisualTreeAsset? ResolveIntroAsset(Season season) => season switch
        {
            Season.Spring => _springIntro,
            Season.Summer => _summerIntro,
            Season.Autumn => _autumnIntro,
            Season.Winter => _winterIntro,
            _ => null
        };

        void UnwireClose()
        {
            if (_wiredCloseBtn != null && _closeClickHandler != null)
                _wiredCloseBtn.clicked -= _closeClickHandler;
            _wiredCloseBtn = null;
        }

        void WireOverview(VisualElement root)
        {
            _overviewClickHandler ??= OpenGreatYearOverview;
            SeasonInfoRecapBindings.WireGreatYearOverview(root, _overviewClickHandler);
        }

        void UnwireOverview(VisualElement root)
        {
            if (_overviewClickHandler != null)
                SeasonInfoRecapBindings.UnwireGreatYearOverview(root);
        }

        void OpenGreatYearOverview()
        {
            Dismiss();
            _overviewRecapHost?.Show();
        }

        void EnsureAssets()
        {
#if UNITY_EDITOR
            _recapShell ??= UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(RecapShellPath);
            _springIntro ??= UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SpringIntroPath);
            _summerIntro ??= UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SummerIntroPath);
            _autumnIntro ??= UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AutumnIntroPath);
            _winterIntro ??= UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(WinterIntroPath);
#endif
        }
    }
}
