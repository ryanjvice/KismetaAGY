using Kismeta.Core.Entities;
using Kismeta.UI.Components;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class AutumnForgeInspectController : OverlayController
    {
        GameSession? _session;

        public System.Action? OnBack;

        protected override void Wire()
        {
            Btn("back-btn")!.clicked += () => OnBack?.Invoke();
        }

        public void BindState(GameSession session)
        {
            _session = session;
            if (Root == null) return;
            AutumnForgeInspectBindings.Bind(Root, session);
        }

        protected override void Bind()
        {
            if (_session != null)
                AutumnForgeInspectBindings.Bind(Root, _session);
        }
    }
}
