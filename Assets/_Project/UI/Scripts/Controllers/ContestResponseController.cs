using System.Collections;
using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Rules;
using Kismeta.UI;
using Kismeta.UI.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    /// <summary>
    /// Defender response UI for pending contests (duel roll, accept/decline, gambit reagent pay).
    /// </summary>
    public sealed class ContestResponseController : ContestController
    {
        static readonly string[] TypeModifierClasses =
        {
            "contest-response--duel",
            "contest-response--gambit",
            "contest-response--trade",
            "contest-response--opposition"
        };

        static readonly string[] TipModifierClasses =
        {
            "tip--danger",
            "tip--caution",
            "tip--ok"
        };

        [SerializeField] VisualTreeAsset? _uxmlAsset;

        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;
        PendingContest? _pending;
        bool _wired;
        bool _rolling;
        bool _awaitingContinue;

        readonly Dictionary<ReagentType, int> _gambitPayments = new();

        public VisualTreeAsset? UxmlAsset => _uxmlAsset;
        public System.Action? OnCompleted;
        public bool IsDuelRollInProgress => _rolling;
        public bool IsAwaitingDuelContinue => _awaitingContinue;

        protected override void Wire()
        {
            _wired = false;
            if (!TryGetUiScope(out var scope))
            {
                Debug.LogError(
                    "[ContestResponseController] Contest response UXML scope not found. " +
                    "Assign Assets/_Project/UI/UXML/batch5/ContestResponse.uxml to GameBootstrap._contestResponse " +
                    "(Kismeta → UI → Wire Bootstrap UI).");
                return;
            }

            var accept = scope.Q<Button>("accept-btn");
            var decline = scope.Q<Button>("decline-btn");
            var close = scope.Q<Button>("close-btn");
            var roll = scope.Q<Button>("roll-btn");

            if (accept == null && roll == null)
            {
                Debug.LogError(
                    "[ContestResponseController] Contest response UXML is missing #accept-btn and #roll-btn. " +
                    "Assign Assets/_Project/UI/UXML/batch5/ContestResponse.uxml to GameBootstrap._contestResponse " +
                    "(Kismeta → UI → Wire Bootstrap UI).");
                return;
            }

            if (accept != null) accept.clicked += OnAccept;
            if (decline != null) decline.clicked += OnDecline;
            if (close != null) close.clicked += OnDecline;
            if (roll != null) roll.clicked += OnRollClicked;

            _wired = true;
        }

        void OnRollClicked()
        {
            if (_awaitingContinue)
            {
                _awaitingContinue = false;
                OnCompleted?.Invoke();
                return;
            }

            if (!_rolling)
                StartCoroutine(DoDuelRoll());
        }

        protected override void Unwire()
        {
            if (!_wired || !TryGetUiScope(out var scope))
                return;

            var accept = scope.Q<Button>("accept-btn");
            var decline = scope.Q<Button>("decline-btn");
            var close = scope.Q<Button>("close-btn");
            var roll = scope.Q<Button>("roll-btn");
            if (accept != null) accept.clicked -= OnAccept;
            if (decline != null) decline.clicked -= OnDecline;
            if (close != null) close.clicked -= OnDecline;
            if (roll != null) roll.clicked -= OnRollClicked;
            _wired = false;
        }

        void OnAccept() => Submit(true);
        void OnDecline() => Submit(false);

        public void BindState(GameSession session, CommandBridge bridge, GameLoop loop)
        {
            _session = session;
            _bridge = bridge;
            _pending = session.Board.PendingContest;
            _playerId = _pending?.DefenderId
                ?? (loop.ActivePlayerId >= 0 ? loop.ActivePlayerId : bridge.PendingController?.Slot.Index ?? -1);
            _gambitPayments.Clear();
            _rolling = false;
            _awaitingContinue = false;

            if (Root == null || _playerId < 0 || _pending == null) return;
            if (!TryGetUiScope(out _))
            {
                Debug.LogError("[ContestResponseController] Cannot populate UI — contest-response scope not found.");
                return;
            }

            PopulateUi();
        }

        bool TryGetUiScope(out VisualElement scope)
        {
            scope = ResolveUiScope();
            return scope != null;
        }

        VisualElement? ResolveUiScope()
        {
            if (Root == null) return null;

            if (Root.name == "contest-response" || Root.ClassListContains("contest-response"))
                return Root;

            var fromRoot = Root.Q<VisualElement>("contest-response");
            if (fromRoot != null) return fromRoot;

            for (var node = Root.parent; node != null; node = node.parent)
            {
                if (node.name == "contest-response" || node.ClassListContains("contest-response"))
                    return node;
            }

            if (Root.Q<Button>("accept-btn") != null || Root.Q<Button>("roll-btn") != null)
                return Root;

            return null;
        }

        Label? FindLabel(string name) => ResolveUiScope()?.Q<Label>(name);
        VisualElement? FindElement(string name) => ResolveUiScope()?.Q<VisualElement>(name);
        Button? FindButton(string name) => ResolveUiScope()?.Q<Button>(name);

        void PopulateUi()
        {
            if (Root == null || _session == null || _pending == null) return;

            var scope = ResolveUiScope();
            if (scope == null) return;

            ApplyTypeTheme(_pending.Kind, scope);
            BindNarrative(_pending.Kind);
            ConfigureFooterMode(_pending.Kind);

            var attackerName = CeremonyBindings.PlayerName(_session, _pending.AttackerId);
            SetText(FindLabel("response-title"), TitleForKind(_pending.Kind));
            SetText(FindLabel("response-sub"), $"from {attackerName}");
            ConfigureDuelStakes(_pending);
            if (_pending.Kind != ContestKind.Duel)
                SetText(FindLabel("response-desc"), DescribePending(_pending, attackerName));

            BuildPartyRow(_pending.AttackerId, _playerId);
            ConfigureDuelEffects(_pending);
            ConfigureTip(_pending);
            ConfigureFeeRow(_pending);
            ResetDuelRollUi(_pending, attackerName);
        }

        void ConfigureFooterMode(ContestKind kind)
        {
            bool isDuel = kind == ContestKind.Duel;
            SetHidden(FindElement("response-footer-choice"), isDuel);
            SetHidden(FindElement("response-footer-duel"), !isDuel);
            SetHidden(FindButton("close-btn"), isDuel);
        }

        void ResetDuelRollUi(PendingContest pending, string attackerName)
        {
            if (pending.Kind != ContestKind.Duel) return;

            SetText(FindLabel("die-you-pip"), "?");
            SetText(FindLabel("die-foe-pip"), "?");
            SetText(FindLabel("die-foe-name"), attackerName);
            SetHidden(FindLabel("roll-outcome"), true);
            var rollBtn = FindButton("roll-btn");
            if (rollBtn != null)
            {
                rollBtn.text = "Roll Die";
                rollBtn.SetEnabled(true);
            }
        }

        void ConfigureDuelEffects(PendingContest pending)
        {
            var host = FindElement("response-effects");
            if (host == null || _session == null) return;

            if (pending.Kind != ContestKind.Duel)
            {
                SetHidden(host, true);
                return;
            }

            var snapshot = ActiveEffectsService.BuildDuelRelevant(
                _session,
                _playerId,
                pending.AttackerId,
                pending.TargetCardId,
                pending.AnteCardId);

            bool showFeatured = _session.Board.BestOfThreeDuels
                && !string.IsNullOrWhiteSpace(snapshot.CosmicAge.Description);
            var featured = FindElement("response-effects-featured");
            if (featured != null)
            {
                featured.Clear();
                if (showFeatured)
                    ActiveEffectsRows.PopulateFeatured(featured, snapshot.CosmicAge);
                SetHidden(featured, !showFeatured);
            }

            var list = FindElement("response-effects-list");
            list?.Clear();
            if (list != null && snapshot.Sections.Count > 0)
                ActiveEffectsAccordion.Populate(list, snapshot.Sections);

            SetHidden(host, !showFeatured && snapshot.Sections.Count == 0);
        }

        void ConfigureDuelStakes(PendingContest pending)
        {
            var desc = FindLabel("response-desc");
            var stakes = FindElement("response-duel-stakes");
            if (pending.Kind != ContestKind.Duel)
            {
                SetHidden(desc, false);
                SetHidden(stakes, true);
                return;
            }

            SetHidden(desc, true);
            SetHidden(stakes, false);

            if (_session == null) return;

            PopulateStakeCard(
                FindElement("response-target-card"),
                FindLabel("response-target-name"),
                pending.TargetCardId);
            PopulateStakeCard(
                FindElement("response-ante-card"),
                FindLabel("response-ante-name"),
                pending.AnteCardId);
        }

        void PopulateStakeCard(VisualElement? chipHost, Label? nameLabel, string? cardId)
        {
            chipHost?.Clear();
            if (chipHost == null || nameLabel == null || _session == null)
                return;

            if (string.IsNullOrEmpty(cardId))
            {
                nameLabel.text = "Unknown card";
                return;
            }

            var db = _session.Rules?.CardDatabase;
            var inst = _session.GetCard(cardId);
            var def = inst != null && db != null ? db.GetById(inst.DefinitionId) : null;
            if (def == null)
            {
                nameLabel.text = "Unknown card";
                return;
            }

            var cosmic = _session.Board.CosmicAgeSign;
            int alignPts = AlignmentService.ScoreCard(def.Suit, def.Planet, cosmic);
            var chip = CardChipFactory.CreateFromDefinition(
                def,
                aligned: alignPts > 0,
                instanceId: cardId);
            chipHost.Add(chip);
            nameLabel.text = CardDisplayName(def);
        }

        static string CardDisplayName(CardDefinition def) =>
            def.IsMinorArcana ? $"{def.Rank} of {def.Suit}" : def.Name;

        IEnumerator DoDuelRoll()
        {
            if (_session == null || _bridge == null || _pending == null || _playerId < 0
                || _pending.Kind != ContestKind.Duel)
                yield break;

            _rolling = true;
            FindButton("roll-btn")?.SetEnabled(false);
            SetHidden(FindLabel("roll-outcome"), true);

            DuelResolvedEvent? resolved = null;
            _bridge.TrySubmit(new RespondDuelCommand(_playerId, accept: true));
            yield return WaitForEvent(_session, (DuelResolvedEvent e) => resolved = e);

            if (resolved != null)
            {
                yield return RollDie(FindLabel("die-you-pip"), resolved.DefendRoll);
                yield return RollDie(FindLabel("die-foe-pip"), resolved.AttackRoll);

                var outcome = FindLabel("roll-outcome");
                if (outcome != null)
                {
                    bool won = resolved.WinnerId == _playerId;
                    outcome.text = won ? "You defended your stake!" : "You lost the duel.";
                    SetHidden(outcome, false);
                }

                var rollBtn = FindButton("roll-btn");
                if (rollBtn != null)
                {
                    rollBtn.text = "Continue";
                    rollBtn.SetEnabled(true);
                }
                _awaitingContinue = true;
            }
            else
            {
                var outcome = FindLabel("roll-outcome");
                if (outcome != null)
                {
                    outcome.text = "Duel could not resolve — try again.";
                    SetHidden(outcome, false);
                }
                FindButton("roll-btn")?.SetEnabled(true);
            }

            _rolling = false;
        }

        static void SetText(Label? label, string text)
        {
            if (label != null)
                label.text = text;
        }

        static void SetHidden(VisualElement? element, bool hidden)
        {
            if (element == null) return;
            element.EnableInClassList("is-hidden", hidden);
        }

        void ApplyTypeTheme(ContestKind kind, VisualElement scope)
        {
            foreach (var cls in TypeModifierClasses)
                scope.RemoveFromClassList(cls);

            scope.AddToClassList(kind switch
            {
                ContestKind.Duel => "contest-response--duel",
                ContestKind.Gambit => "contest-response--gambit",
                ContestKind.Trade => "contest-response--trade",
                ContestKind.Opposition => "contest-response--opposition",
                _ => "contest-response--duel"
            });

            var icon = FindLabel("response-sigil-icon");
            if (icon == null) return;
            icon.text = kind switch
            {
                ContestKind.Duel => "\ueb4d",
                ContestKind.Gambit => "\uea2c",
                ContestKind.Trade => "\uebd9",
                ContestKind.Opposition => "\ueb4c",
                _ => "\ueb4d"
            };
        }

        void BindNarrative(ContestKind kind)
        {
            string stepId = kind switch
            {
                ContestKind.Trade => "summer.trade",
                ContestKind.Duel => "summer.duel",
                ContestKind.Gambit => "summer.gambit",
                ContestKind.Opposition => "autumn.opposition",
                _ => "summer.duel"
            };

            var mask = kind == ContestKind.Duel
                ? NarrativeSlotMask.Beat
                : NarrativeSlotMask.All;
            NarrativeSlotBindings.BindById(Root, stepId, mask: mask);
        }

        static string TitleForKind(ContestKind kind) => kind switch
        {
            ContestKind.Trade => "Trade Offer",
            ContestKind.Duel => "Duel Challenge",
            ContestKind.Gambit => "Gambit",
            ContestKind.Opposition => "Opposition",
            _ => "Contest Response"
        };

        string DescribePending(PendingContest pending, string attackerName)
        {
            return pending.Kind switch
            {
                ContestKind.Trade =>
                    BuildTradeDescription(pending),
                ContestKind.Gambit =>
                    $"{attackerName} offers a gambit for one of your active cards. Pay the ward fee to enter or decline.",
                ContestKind.Opposition =>
                    "Your forging stone is under attack. Alignment scores + dice decide.",
                _ => $"{attackerName} targets you."
            };
        }

        string BuildTradeDescription(PendingContest pending)
        {
            int offerCount = pending.OfferCardIds?.Count ?? 0;
            int requestCount = pending.RequestCardIds?.Count ?? 0;
            return $"They offer {offerCount} card(s) and request {requestCount} card(s) from your public spread.";
        }

        void BuildPartyRow(int attackerId, int defenderId)
        {
            var host = FindElement("response-party");
            if (host == null || _session == null) return;
            host.Clear();

            AddPartySide(host, attackerId, false);
            var vs = new Label("vs") { name = "response-party-vs" };
            vs.style.marginLeft = 8;
            vs.style.marginRight = 8;
            vs.style.color = new StyleColor(new Color(0.72f, 0.60f, 0.43f));
            vs.style.fontSize = 11;
            host.Add(vs);
            AddPartySide(host, defenderId, defenderId == _playerId);
        }

        void AddPartySide(VisualElement host, int playerId, bool isYou)
        {
            var side = new VisualElement();
            side.style.flexDirection = FlexDirection.Row;
            side.style.alignItems = Align.Center;

            var dot = new VisualElement();
            dot.AddToClassList("party__dot");
            dot.style.backgroundColor = new StyleColor(PlayerUiNames.PlayerColor(playerId));
            side.Add(dot);

            var name = CeremonyBindings.PlayerName(_session!, playerId);
            if (isYou) name += " (you)";
            var lbl = new Label(name);
            lbl.style.fontSize = 12;
            lbl.style.color = new StyleColor(new Color(0.81f, 0.77f, 0.72f));
            side.Add(lbl);
            host.Add(side);
        }

        void ConfigureTip(PendingContest pending)
        {
            var tip = FindElement("response-tip");
            var tipText = FindLabel("response-tip-text");
            if (tip == null || tipText == null) return;

            foreach (var cls in TipModifierClasses)
                tip.RemoveFromClassList(cls);

            string? text = null;
            string? tipClass = null;

            switch (pending.Kind)
            {
                case ContestKind.Gambit:
                    int wardCost = _session!.Players[_playerId].StoneWardCount;
                    if (wardCost > 0)
                    {
                        text = $"Entering costs {wardCost} reagent(s)";
                        tipClass = "tip--caution";
                    }
                    break;
                case ContestKind.Trade:
                    text = "Both parties swap immediately on acceptance";
                    tipClass = "tip--ok";
                    break;
                case ContestKind.Opposition:
                    text = "Your stone enters Stasis if you lose";
                    tipClass = "tip--caution";
                    break;
            }

            if (string.IsNullOrEmpty(text) || tipClass == null)
            {
                SetHidden(tip, true);
                return;
            }

            tipText.text = text;
            tip.AddToClassList(tipClass);
            SetHidden(tip, false);
        }

        void ConfigureFeeRow(PendingContest pending)
        {
            var row = FindElement("response-fee-row");
            var chips = FindElement("response-fee-chips");
            if (row == null || chips == null || _session == null) return;

            chips.Clear();
            if (pending.Kind != ContestKind.Gambit)
            {
                SetHidden(row, true);
                return;
            }

            int wardCost = _session.Players[_playerId].StoneWardCount;
            if (wardCost <= 0)
            {
                SetHidden(row, true);
                return;
            }

            SetHidden(row, false);
            foreach (var rt in ReagentSpendHelper.PriorityOrder)
            {
                int have = _session.Players[_playerId].GetReagent(rt);
                if (have <= 0) continue;
                _gambitPayments.TryAdd(rt, 0);
                chips.Add(ReagentChipFactory.Create(rt, have));
            }
        }

        void Submit(bool accept)
        {
            if (!_wired || _bridge == null || _session == null || _pending == null || _playerId < 0) return;
            if (_pending.Kind == ContestKind.Duel)
                return;

            IGameCommand? cmd = _pending.Kind switch
            {
                ContestKind.Trade => new RespondTradeCommand(_playerId, accept),
                ContestKind.Gambit => BuildGambitResponse(accept),
                ContestKind.Opposition => new RespondOppositionCommand(_playerId, accept),
                _ => null
            };

            if (cmd != null && _bridge.TrySubmit(cmd))
                OnCompleted?.Invoke();
        }

        RespondGambitCommand BuildGambitResponse(bool accept)
        {
            if (!accept || _session == null)
                return new RespondGambitCommand(_playerId, accept);

            int wardCost = _session.Players[_playerId].StoneWardCount;
            if (wardCost <= 0)
                return new RespondGambitCommand(_playerId, true);

            var payments = ReagentSpendHelper.BuildPriorityPayments(_session.Players[_playerId], wardCost);
            return new RespondGambitCommand(_playerId, true, payments);
        }
    }
}
