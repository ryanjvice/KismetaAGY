using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Kismeta.UI;

namespace Kismeta.UI.Controllers
{
    public sealed class ResumeScreenController : ScreenController
    {
        public override string ScreenId => ScreenIds.Resume;

        public Action<string>? OnResumeGame;
        public Action? OnBack;

        /// <summary>Placeholder saves until persistence lands.</summary>
        public IReadOnlyList<(string id, string label, string detail)> SavedGames { get; set; } =
            Array.Empty<(string, string, string)>();

        protected override void Wire()
        {
            Btn("back-btn")!.clicked += () => OnBack?.Invoke();
        }

        protected override void Bind() => RebuildList();

        public void RebuildList()
        {
            if (Root == null) return;

            var list = Root.Q<ScrollView>("resume-list");
            var empty = Lbl("resume-empty");
            if (list == null) return;

            list.Clear();

            if (SavedGames.Count == 0)
            {
                if (empty != null)
                    empty.style.display = DisplayStyle.Flex;
                return;
            }

            if (empty != null)
                empty.style.display = DisplayStyle.None;

            foreach (var save in SavedGames)
            {
                var row = new VisualElement();
                row.AddToClassList("panel");
                row.style.marginBottom = 8;
                row.style.paddingTop = 10;
                row.style.paddingBottom = 10;
                row.style.paddingLeft = 12;
                row.style.paddingRight = 12;

                var title = new Label(save.label) { name = "save-title" };
                title.style.fontSize = 12;
                title.style.color = new StyleColor(new UnityEngine.Color(0.95f, 0.91f, 0.82f));

                var detail = new Label(save.detail) { name = "save-detail" };
                detail.style.fontSize = 10;
                detail.style.color = new StyleColor(new UnityEngine.Color(0.72f, 0.6f, 0.43f));
                detail.style.whiteSpace = WhiteSpace.Normal;

                row.Add(title);
                row.Add(detail);

                var id = save.id;
                row.RegisterCallback<ClickEvent>(_ => OnResumeGame?.Invoke(id));
                list.Add(row);
            }
        }
    }
}
