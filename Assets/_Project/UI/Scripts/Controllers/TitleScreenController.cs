using System;
using Kismeta.UI.Components;
using UnityEngine.UIElements;
using Kismeta.UI;

namespace Kismeta.UI.Controllers
{
    public sealed class TitleScreenController : ScreenController
    {
        public override string ScreenId => ScreenIds.Title;

        public Action OnNewGame;
        public Action OnResume;
        public Action OnJoin;
        public Action OnHowToPlay;
        public Action OnCodex;

        protected override void Bind()
        {
            UiArtBindings.ApplyTitleHero(Root);
        }

        protected override void Wire()
        {
            Btn("new-game-btn")!.clicked += () => OnNewGame?.Invoke();
            Btn("resume-btn")!.clicked += () => OnResume?.Invoke();
            Btn("join-btn")!.clicked += () => OnJoin?.Invoke();
            Btn("howto-btn")!.clicked += () => OnHowToPlay?.Invoke();
            Btn("codex-btn")!.clicked += () => OnCodex?.Invoke();
        }
    }
}
