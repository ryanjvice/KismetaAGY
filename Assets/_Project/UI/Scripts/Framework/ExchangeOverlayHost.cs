using Kismeta.Core.Commands;
using Kismeta.Core.Entities;
using Kismeta.UI.Controllers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI
{
    /// <summary>Post-action exchange summary modal (trade, duel, gambit, fate transfers).</summary>
    public sealed class ExchangeOverlayHost : MonoBehaviour
    {
        VisualTreeAsset? _resourceExchange;

        ViewportLayout? _layout;
        GameSession? _session;
        ResourceExchangeController? _controller;

        public System.Action? OnConfirmed;

        bool _isShowing;

        public bool IsOpen => _isShowing
            && _layout != null
            && _layout.IsOverlayVisible;

        /// <summary>Clears internal open state when another overlay replaced ours without Dismiss().</summary>
        public void ClearShowingState()
        {
            _isShowing = false;
            _pendingExchange = null;
            _controller?.Detach();
        }

        PlayerExchangeEvent? _pendingExchange;

        void Awake() => EnsureControllers();

        public void Configure(VisualTreeAsset resourceExchange)
        {
            _resourceExchange = resourceExchange;
            EnsureControllers();
        }

        void EnsureControllers()
        {
            _layout ??= GetComponent<ViewportLayout>();
            _controller ??= GetComponent<ResourceExchangeController>();
        }

        public void BindState(GameSession session)
        {
            _session = session;
        }

        public bool Show(PlayerExchangeEvent exchange)
        {
            EnsureControllers();
            if (_layout == null || _resourceExchange == null || _controller == null)
            {
                Debug.LogWarning("[ExchangeOverlayHost] Missing layout, UXML, or controller.");
                return false;
            }

            _layout.ShowModal(_resourceExchange);
            var root = _layout.OverlayContentRoot;
            if (root == null)
            {
                Debug.LogWarning("[ExchangeOverlayHost] Overlay content root missing.");
                _layout.DismissOverlay();
                return false;
            }

            _pendingExchange = exchange;
            _isShowing = true;
            _controller.AttachTo(root);
            if (_session != null)
                _controller.BindState(_session, exchange);
            WireController();
            return true;
        }

        public void Dismiss()
        {
            _isShowing = false;
            _pendingExchange = null;
            _controller?.Detach();
            _layout?.DismissOverlay();
        }

        void WireController()
        {
            if (_controller == null) return;
            _controller.OnConfirmed = () =>
            {
                OnConfirmed?.Invoke();
                Dismiss();
            };
        }
    }
}
