using System;
using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.UI.Controllers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>Confirm and result modals for Fateful Wager (Winter placement + Spring resolution).</summary>
    public sealed class WagerOverlayHost : MonoBehaviour
    {
        VisualTreeAsset? _wagerModals;

        ViewportLayout? _layout;
        FatefulWagerModalsController? _controller;

        ZodiacSign _pendingSign;
        readonly List<string> _pendingCardIds = new();
        GameSession? _pendingSession;
        Action? _onConfirmDismiss;
        Action? _onResultDismiss;

        public bool IsOpen => _layout != null && _layout.IsOverlayVisible;

        void Awake() => EnsureControllers();

        public void Configure(VisualTreeAsset wagerModals)
        {
            _wagerModals = wagerModals;
            EnsureControllers();
        }

        void EnsureControllers()
        {
            _layout ??= GetComponent<ViewportLayout>();
            _controller ??= GetComponent<FatefulWagerModalsController>();
        }

        public void ShowConfirm(GameSession session, ZodiacSign sign, IReadOnlyList<string> cardIds, Action onConfirm, Action onCancel)
        {
            EnsureControllers();
            if (_layout == null || _wagerModals == null || _controller == null)
            {
                Debug.LogWarning("[WagerOverlayHost] Missing layout, UXML, or controller.");
                return;
            }

            _pendingSession = session;
            _pendingSign = sign;
            _pendingCardIds.Clear();
            _pendingCardIds.AddRange(cardIds);

            _layout.ShowModal(_wagerModals);
            var root = _layout.OverlayContentRoot;
            if (root == null) return;

            _controller.AttachTo(root);
            _controller.BindConfirm(session, sign, cardIds);
            WireConfirm(onConfirm, onCancel);
        }

        public void ShowResult(FatefulWagerResolvedEvent resolved, Action? onDismiss = null)
        {
            EnsureControllers();
            if (_layout == null || _wagerModals == null || _controller == null)
            {
                Debug.LogWarning("[WagerOverlayHost] Missing layout, UXML, or controller.");
                onDismiss?.Invoke();
                return;
            }

            _onResultDismiss = onDismiss;

            _layout.ShowModal(_wagerModals);
            var root = _layout.OverlayContentRoot;
            if (root == null) return;

            _controller.AttachTo(root);
            _controller.BindResult(resolved);
            WireResult();
        }

        public void Dismiss()
        {
            _pendingSession = null;
            _pendingCardIds.Clear();
            _onConfirmDismiss = null;
            _onResultDismiss = null;
            _controller?.Detach();
            _layout?.DismissOverlay();
        }

        void WireConfirm(Action onConfirm, Action onCancel)
        {
            if (_controller == null) return;
            _controller.OnConfirm = () =>
            {
                onConfirm();
            };
            _controller.OnCancel = () =>
            {
                onCancel();
                Dismiss();
            };
        }

        void WireResult()
        {
            if (_controller == null) return;
            _controller.OnDismiss = () =>
            {
                var cb = _onResultDismiss;
                _onResultDismiss = null;
                Dismiss();
                cb?.Invoke();
            };
        }
    }
}
