using System;
using UnityEngine.UIElements;
using Kismeta.UI;

namespace Kismeta.UI.Controllers
{
    public sealed class JoinScreenController : ScreenController
    {
        public override string ScreenId => ScreenIds.Join;

        public Action<string>? OnJoin;
        public Action? OnBack;

        protected override void Wire()
        {
            Btn("join-confirm-btn")!.clicked += Confirm;
            Btn("back-btn")!.clicked += OnBackClicked;
        }

        protected override void Unwire()
        {
            if (Btn("join-confirm-btn") != null)
                Btn("join-confirm-btn")!.clicked -= Confirm;
            if (Btn("back-btn") != null)
                Btn("back-btn")!.clicked -= OnBackClicked;
        }

        private void OnBackClicked() => OnBack?.Invoke();

        private void Confirm()
        {
            var field = Root?.Q<TextField>("room-code");
            var code = field?.value?.Trim().ToUpperInvariant();
            if (!string.IsNullOrEmpty(code) && code.Length == 6)
                OnJoin?.Invoke(code);
        }
    }
}
