using System;
using System.Collections;
using System.Collections.Generic;
using Kismeta.Core.Domain;
using Kismeta.Core.Rules;
using Kismeta.UI;
using Kismeta.UI.Components;
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
        int _humanPlayerCount = 1;
        int _winnerId = -1;
        bool _resolved;
        bool _rolling;
        AgekeeperContestService.ContestResult? _pendingResult;

        public void BeginContest(int playerCount, int humanPlayerCount = 1)
        {
            _playerCount = Mathf.Clamp(playerCount, 2, 4);
            _humanPlayerCount = Mathf.Clamp(humanPlayerCount, 0, _playerCount);
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
            if (_resolved || _rolling || _playerCount < 2) return;
            _pendingResult = AgekeeperContestService.Resolve(_playerCount, new System.Random());
            _winnerId = _pendingResult.WinnerPlayerId;
            StartCoroutine(AnimateRolls());
        }

        IEnumerator AnimateRolls()
        {
            _rolling = true;
            Btn("roll-btn")?.SetEnabled(false);
            if (_pendingResult == null) yield break;

            var rounds = _pendingResult.Rounds;
            for (int r = 0; r < rounds.Count; r++)
            {
                var round = rounds[r];
                if (r > 0)
                {
                    ShowPreRollHero();
                    if (Lbl("status-line") != null)
                        Lbl("status-line")!.text = FormatTieRerollMessage(round);
                    ClearWinnerHighlight();
                    foreach (var roll in round)
                    {
                        var die = Root?.Q<Label>($"die-{roll.PlayerId}");
                        if (die != null) die.text = "—";
                    }
                    yield return new WaitForSeconds(0.6f);
                }

                var sorted = new List<AgekeeperContestService.RollResult>(round);
                sorted.Sort((a, b) => a.PlayerId.CompareTo(b.PlayerId));
                foreach (var roll in sorted)
                {
                    var die = Root?.Q<Label>($"die-{roll.PlayerId}");
                    yield return DieAnimator.RollLabel(die, roll.DieValue);
                }
            }

            if (!AllPlayersRolled())
            {
                _rolling = false;
                Btn("roll-btn")?.SetEnabled(true);
                yield break;
            }

            _resolved = true;
            _rolling = false;
            ShowRollResults(_pendingResult);
            ShowResultHero($"{ColorNames[_winnerId]} Alchemist becomes Agekeeper for the first round.");

            if (Btn("roll-btn") != null)
                Btn("roll-btn").style.display = DisplayStyle.None;
            if (Btn("continue-btn") != null)
            {
                Btn("continue-btn").style.display = DisplayStyle.Flex;
                UiMotion.FadeIn(Btn("continue-btn"));
            }
        }

        private string FormatTieRerollMessage(IReadOnlyList<AgekeeperContestService.RollResult> round)
        {
            var names = new List<string>(round.Count);
            var sorted = new List<AgekeeperContestService.RollResult>(round);
            sorted.Sort((a, b) => a.PlayerId.CompareTo(b.PlayerId));
            foreach (var roll in sorted)
                names.Add($"{ColorNames[roll.PlayerId]} Alchemist");
            return names.Count switch
            {
                1 => $"Tie — {names[0]} rerolls.",
                2 => $"Tie — {names[0]} and {names[1]} reroll.",
                _ => $"Tie — {string.Join(", ", names.GetRange(0, names.Count - 1))}, and {names[^1]} reroll."
            };
        }

        private bool AllPlayersRolled()
        {
            for (int i = 0; i < _playerCount; i++)
            {
                var die = Root?.Q<Label>($"die-{i}");
                if (die == null || die.text == "—") return false;
            }
            return true;
        }

        private void OnContinueClicked()
        {
            if (!_resolved || _winnerId < 0) return;
            OnComplete?.Invoke(_winnerId);
        }

        private void ResetUi()
        {
            ShowPreRollHero();

            if (Btn("roll-btn") != null)
            {
                Btn("roll-btn").style.display = DisplayStyle.Flex;
                Btn("roll-btn").SetEnabled(true);
            }
            if (Btn("continue-btn") != null)
                Btn("continue-btn").style.display = DisplayStyle.None;

            El("player-rolls")?.Clear();
        }

        private void ShowPreRollHero()
        {
            var heroTitle = Lbl("hero-title");
            if (heroTitle != null)
                heroTitle.style.display = DisplayStyle.Flex;

            var resultTitle = Lbl("result-title");
            if (resultTitle != null)
            {
                resultTitle.text = "";
                resultTitle.style.display = DisplayStyle.None;
            }

            if (Lbl("status-line") != null)
                Lbl("status-line")!.text =
                    "Each Alchemist rolls their zodiac die — highest holds the key first.";
        }

        private void ShowResultHero(string resultMessage)
        {
            if (Lbl("hero-title") != null)
                Lbl("hero-title")!.style.display = DisplayStyle.None;

            var resultTitle = Lbl("result-title");
            if (resultTitle != null)
            {
                resultTitle.text = resultMessage;
                resultTitle.style.display = DisplayStyle.Flex;
            }

            if (Lbl("status-line") != null)
                Lbl("status-line")!.text = "The dice have spoken.";
        }

        private void BuildPlayerRows()
        {
            var list = El("player-rolls");
            if (list == null) return;
            list.Clear();

            for (int i = 0; i < _playerCount; i++)
            {
                var row = new VisualElement();
                row.name = $"player-row-{i}";
                row.AddToClassList("panel");
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 8;
                row.style.paddingTop = 10;
                row.style.paddingBottom = 10;
                row.style.paddingLeft = 12;
                row.style.paddingRight = 12;

                var nameBlock = new VisualElement();
                nameBlock.style.flexDirection = FlexDirection.Row;
                nameBlock.style.alignItems = Align.Center;
                nameBlock.style.flexGrow = 1;

                var name = new Label($"{ColorNames[i]} Alchemist");
                name.name = $"player-name-{i}";
                name.AddToClassList("agekeeper-contest__player-name");
                nameBlock.Add(name);

                var roleSuffix = PlayerUiNames.RoleSuffix(i, _humanPlayerCount);
                if (!string.IsNullOrEmpty(roleSuffix))
                {
                    var role = new Label(roleSuffix);
                    role.AddToClassList("agekeeper-contest__player-role");
                    role.style.color = new StyleColor(i == 0 && i < _humanPlayerCount
                        ? new Color(0.91f, 0.73f, 0.29f)
                        : new Color(0.54f, 0.42f, 0.48f));
                    nameBlock.Add(role);
                }

                var die = new Label("—");
                die.name = $"die-{i}";
                die.style.fontSize = 22;
                die.style.color = new StyleColor(new Color(0.8f, 0.69f, 0.88f));
                die.style.minWidth = 36;
                die.style.unityTextAlign = TextAnchor.MiddleCenter;

                row.Add(nameBlock);
                row.Add(die);
                list.Add(row);
            }
        }

        private void ShowRollResults(AgekeeperContestService.ContestResult result)
        {
            var display = BuildDisplayRolls(result);
            foreach (var roll in display)
            {
                var die = Root?.Q<Label>($"die-{roll.PlayerId}");
                if (die != null)
                    die.text = roll.DieValue.ToString();
            }

            ClearWinnerHighlight();

            for (int i = 0; i < _playerCount; i++)
            {
                var row = El($"player-row-{i}");
                if (row == null) continue;

                if (i == _winnerId)
                {
                    row.AddToClassList("panel--winner");
                    row.RemoveFromClassList("panel--dim");
                    row.style.opacity = 1f;

                    var gold = new StyleColor(UiTheme.GoldBright);
                    row.style.borderTopColor = row.style.borderRightColor =
                        row.style.borderBottomColor = row.style.borderLeftColor = gold;

                    var name = Root?.Q<Label>($"player-name-{i}");
                    if (name != null)
                        name.style.color = new StyleColor(UiTheme.PlayerColor(i));

                    var die = Root?.Q<Label>($"die-{i}");
                    if (die != null)
                        die.style.color = new StyleColor(UiTheme.GoldBright);
                }
                else
                {
                    row.RemoveFromClassList("panel--winner");
                    row.AddToClassList("panel--dim");
                    row.style.borderTopColor = StyleKeyword.Null;
                    row.style.borderRightColor = StyleKeyword.Null;
                    row.style.borderBottomColor = StyleKeyword.Null;
                    row.style.borderLeftColor = StyleKeyword.Null;
                }
            }
        }

        private void ClearWinnerHighlight()
        {
            for (int i = 0; i < _playerCount; i++)
            {
                var row = El($"player-row-{i}");
                if (row == null) continue;

                row.RemoveFromClassList("panel--winner");
                row.RemoveFromClassList("panel--dim");
                row.style.opacity = StyleKeyword.Null;
                row.style.borderTopColor = StyleKeyword.Null;
                row.style.borderRightColor = StyleKeyword.Null;
                row.style.borderBottomColor = StyleKeyword.Null;
                row.style.borderLeftColor = StyleKeyword.Null;

                var name = Root?.Q<Label>($"player-name-{i}");
                if (name != null)
                    name.style.color = new StyleColor(new Color(0.95f, 0.91f, 0.82f));

                var die = Root?.Q<Label>($"die-{i}");
                if (die != null)
                    die.style.color = new StyleColor(new Color(0.8f, 0.69f, 0.88f));
            }
        }

        private static List<AgekeeperContestService.RollResult> BuildDisplayRolls(
            AgekeeperContestService.ContestResult result)
        {
            var byPlayer = new Dictionary<int, int>();
            foreach (var round in result.Rounds)
            {
                foreach (var roll in round)
                    byPlayer[roll.PlayerId] = roll.DieValue;
            }

            var display = new List<AgekeeperContestService.RollResult>(byPlayer.Count);
            foreach (var kv in byPlayer)
                display.Add(new AgekeeperContestService.RollResult(kv.Key, kv.Value));
            display.Sort((a, b) => a.PlayerId.CompareTo(b.PlayerId));
            return display;
        }
    }
}
