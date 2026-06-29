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

        public bool IsOpen => _layout != null && _layout.IsOverlayVisible;

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

        public void Show(PlayerExchangeEvent exchange)
        {
            EnsureControllers();
            if (_layout == null || _resourceExchange == null || _controller == null)
            {
                Debug.LogWarning("[ExchangeOverlayHost] Missing layout, UXML, or controller.");
                return;
            }

            _pendingExchange = exchange;
            _layout.ShowModal(_resourceExchange);
            var root = _layout.OverlayContentRoot;
            if (root == null) return;

            _controller.AttachTo(root);
            if (_session != null)
                _controller.BindState(_session, exchange);
            WireController();
        }

        public void Dismiss()
        {
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
