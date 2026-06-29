using Kismeta.Core.Commands;
using Kismeta.Core.Entities;
using Kismeta.UI.Components;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class ResourceExchangeController : OverlayController
    {
        GameSession? _session;
        PlayerExchangeEvent? _exchange;

        public System.Action? OnConfirmed;

        protected override void Wire()
        {
            Btn("exchange-confirm-btn")!.clicked += () => OnConfirmed?.Invoke();
        }

        public void BindState(GameSession session, PlayerExchangeEvent exchange)
        {
            _session = session;
            _exchange = exchange;
            if (Root == null) return;
            ExchangeBindings.Populate(Root, session, exchange);
        }

        protected override void Bind()
        {
            if (_session != null && _exchange != null && Root != null)
                ExchangeBindings.Populate(Root, _session, _exchange);
        }
    }
}
