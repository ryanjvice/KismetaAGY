using System;
using Kismeta.Core.Domain;
using Kismeta.Core.Rules;
using Kismeta.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class AgekeeperContestController : ScreenController
    {
        static readonly string[] ColorNames = { "Red", "Green", "Blue", "White" };

        public override string ScreenId => ScreenIds.AgekeeperContest;

        public Action<int>? OnComplete;

        int _playerCount;
        int _winnerId = -1;
        bool _resolved;

        public void BeginContest(int playerCount)
        {
            _playerCount = Mathf.Clamp(playerCount, 2, 4);
            _winnerId = -1;
            _resolved = false;
            if (!IsAttached) return;
            ResetUi();
            BuildPlayerRows();
        }

        protected override void Wire()
        {
            Btn("roll-btn")!.clicked += OnRollClicked;
            Btn("continue-btn")!.clicked += OnContinueClicked;
        }

        protected override void Bind() => ResetUi();

        private void OnRollClicked()
        {
            if (_resolved || _playerCount < 2) return;

            var result = AgekeeperContestService.Resolve(_playerCount, new System.Random());
            _winnerId = result.WinnerPlayerId;
            _resolved = true;

            ShowRollResults(result);
            if (Lbl("status-line") != null)
                Lbl("status-line").text = "The dice have spoken.";

            var winnerLine = Lbl("winner-line");
            if (winnerLine != null)
            {
                winnerLine.style.display = DisplayStyle.Flex;
                winnerLine.text = $"{ColorNames[_winnerId]} alchemist becomes Agekeeper for the first round.";
            }

            if (Btn("roll-btn") != null)
                Btn("roll-btn").style.display = DisplayStyle.None;
            if (Btn("continue-btn") != null)
                Btn("continue-btn").style.display = DisplayStyle.Flex;
        }

        private void OnContinueClicked()
        {
            if (!_resolved || _winnerId < 0) return;
            OnComplete?.Invoke(_winnerId);
        }

        private void ResetUi()
        {
            if (Lbl("status-line") != null)
                Lbl("status-line").text =
                    "Each alchemist rolls their zodiac die — highest holds the key first.";

            var winnerLine = Lbl("winner-line");
            if (winnerLine != null)
            {
                winnerLine.text = "";
                winnerLine.style.display = DisplayStyle.None;
            }

            if (Btn("roll-btn") != null)
                Btn("roll-btn").style.display = DisplayStyle.Flex;
            if (Btn("continue-btn") != null)
                Btn("continue-btn").style.display = DisplayStyle.None;

            El("player-rolls")?.Clear();
        }

        private void BuildPlayerRows()
        {
            var list = El("player-rolls");
            if (list == null) return;
            list.Clear();

            for (int i = 0; i < _playerCount; i++)
            {
                var row = new VisualElement();
                row.AddToClassList("panel");
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 8;
                row.style.paddingTop = 10;
                row.style.paddingBottom = 10;
                row.style.paddingLeft = 12;
                row.style.paddingRight = 12;

                var name = new Label($"{ColorNames[i]} alchemist");
                name.style.flexGrow = 1;
                name.style.fontSize = 12;
                name.style.color = new StyleColor(new Color(0.95f, 0.91f, 0.82f));

                var die = new Label("—");
                die.name = $"die-{i}";
                die.style.fontSize = 22;
                die.style.color = new StyleColor(new Color(0.8f, 0.69f, 0.88f));
                die.style.minWidth = 36;
                die.style.unityTextAlign = TextAnchor.MiddleCenter;

                row.Add(name);
                row.Add(die);
                list.Add(row);
            }
        }

        private void ShowRollResults(AgekeeperContestService.ContestResult result)
        {
            foreach (var roll in result.FinalRolls)
            {
                var die = Root?.Q<Label>($"die-{roll.PlayerId}");
                if (die != null)
                    die.text = roll.DieValue.ToString();
            }

            for (int i = 0; i < _playerCount; i++)
            {
                var row = Root?.Q<Label>($"die-{i}")?.parent;
                if (row == null) continue;
                if (i == _winnerId)
                    row.style.borderTopWidth = row.style.borderBottomWidth =
                        row.style.borderLeftWidth = row.style.borderRightWidth = 2;
                else
                    row.style.borderTopWidth = row.style.borderBottomWidth =
                        row.style.borderLeftWidth = row.style.borderRightWidth = 1;
            }
        }
    }
}
