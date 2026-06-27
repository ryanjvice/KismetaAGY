using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.UI;
using Kismeta.UI.Components;
using Kismeta.UI.Narrative;
using UnityEngine.UIElements;

namespace Kismeta.UI.Controllers
{
    public sealed class CraftReagentController : ScreenController
    {
        public override string ScreenId => ScreenIds.CraftReagent;
        readonly HashSet<string> _selected = new();

        GameSession? _session;
        CommandBridge? _bridge;
        int _playerId = -1;
        ReagentType _reagent = ReagentType.Sulphur;
        bool _forged;
        bool _uiInitialized;

        public System.Action OnBack;
        public System.Action OnDone;

        public ReagentType? InitialReagent { get; set; }

        protected override void Wire()
        {
            Btn("back-btn")!.clicked += () => OnBack?.Invoke();
            Btn("forge-btn")!.clicked += OnForge;
            Btn("result-done-btn")!.clicked += () => OnDone?.Invoke();

            foreach (var key in CraftReagentPanelBindings.ReagentKeys)
                Btn($"pick-{key}")?.RegisterCallback<ClickEvent>(_ => PickReagent(key));
        }

        public void BindState(GameSession session, CommandBridge bridge)
        {
            _session = session;
            _bridge = bridge;
            if (Root == null) return;

            _playerId = SummerActionBindings.ResolvePlayerId(_session, _bridge);
            if (_playerId < 0) return;

            if (_forged)
            {
                SetPhaseVisibility(showResult: true);
                return;
            }

            if (!_uiInitialized)
            {
                RefreshUi(fullInit: true);
                return;
            }

            CraftReagentPanelBindings.LockReagentPicks(Root, _session.Players[_playerId]);
        }

        protected override void Bind()
        {
            _uiInitialized = false;
            if (_session != null && _bridge != null)
                RefreshUi(fullInit: true);
        }

        void RefreshUi(bool fullInit)
        {
            if (_session == null || _bridge == null || Root == null) return;

            _playerId = SummerActionBindings.ResolvePlayerId(_session, _bridge);
            if (_playerId < 0) return;

            MainSceneBindings.ApplySeasonClass(Root, _session.Phase.CurrentSeason);
            UpdateResultDoneLabel();

            if (_forged)
            {
                SetPhaseVisibility(showResult: true);
                NarrativeSlotBindings.BindById(Root, "summer.craft");
                return;
            }

            SetPhaseVisibility(showResult: false);

            var player = _session.Players[_playerId];
            CraftReagentPanelBindings.LockReagentPicks(Root, player);

            if (fullInit || !_uiInitialized)
            {
                _selected.Clear();
                SelectDefaultReagent();
                _uiInitialized = true;
            }
            else
                ApplyReagentUi();

            NarrativeSlotBindings.BindById(Root, "summer.craft");
        }

        void SelectDefaultReagent()
        {
            if (InitialReagent.HasValue)
            {
                var key = CraftReagentPanelBindings.ReagentKeyFor(InitialReagent.Value);
                InitialReagent = null;
                if (TryPickReagent(key)) return;
            }

            if (TryPickReagent("sulphur")) return;
            if (TryPickReagent("salt")) return;
            foreach (var key in CraftReagentPanelBindings.ReagentKeys)
            {
                if (key is "sulphur" or "salt") continue;
                if (TryPickReagent(key)) return;
            }

            ApplyReagentUi();
        }

        bool TryPickReagent(string key)
        {
            if (CraftReagentPanelBindings.IsReagentPickLocked(Root!, key)) return false;
            PickReagent(key);
            return true;
        }

        void PickReagent(string key)
        {
            if (Root != null && CraftReagentPanelBindings.IsReagentPickLocked(Root, key)) return;

            _reagent = CraftReagentPanelBindings.KeyToReagent(key);
            _selected.Clear();
            ApplyReagentUi();
        }

        void ApplyReagentUi()
        {
            CraftReagentPanelBindings.SetActiveReagentPick(Root!, _reagent);
            UpdateCauldronNote();
            RefreshPool();
            RefreshForgeBtn();
        }

        void UpdateCauldronNote()
        {
            if (_session == null || _playerId < 0 || Root == null) return;
            CraftReagentPanelBindings.UpdateCauldronNote(Root, _session, _session.Players[_playerId], _reagent);
        }

        void RefreshPool()
        {
            if (_session == null || _playerId < 0 || Root == null) return;

            var player = _session.Players[_playerId];
            var cards = SummerCardPickBindings.CollectMinorCards(_session, player);
            Suit? filter = _reagent == ReagentType.Salt ? null : Correspondence.SuitFor(_reagent);

            int need = EffectiveNeed();
            var pool = El("card-pool");
            var eyebrow = pool?.parent?.Q<Label>(className: "eyebrow");
            if (eyebrow != null)
            {
                if (filter.HasValue)
                    eyebrow.text = $"tap {need} {filter.Value} cards to discard";
                else
                    eyebrow.text = $"tap {need} cards to discard";
            }

            SummerCardPickBindings.RebuildPool(Root, _session, cards, _selected, filter, OnCardToggle);
            RefreshForgeBtn();
        }

        void OnCardToggle(string cardId)
        {
            if (_session == null) return;
            int need = EffectiveNeed();

            if (_selected.Contains(cardId))
                _selected.Remove(cardId);
            else if (_selected.Count < need)
                _selected.Add(cardId);

            RefreshPool();
        }

        int EffectiveNeed()
        {
            if (_session == null || _playerId < 0) return 3;
            return CraftReagentPanelBindings.EffectiveCost(_session, _session.Players[_playerId], _reagent);
        }

        void RefreshForgeBtn()
        {
            if (_session == null || _playerId < 0 || Root == null) return;
            CraftReagentPanelBindings.RefreshForgeButton(Root, _session, _session.Players[_playerId],
                _reagent, _selected.Count);
        }

        void OnForge()
        {
            if (_bridge == null || _session == null || _playerId < 0) return;
            if (_selected.Count < EffectiveNeed()) return;

            var ids = new List<string>(_selected);
            if (_bridge.TrySubmit(new CraftReagentCommand(_playerId, _reagent, ids)))
            {
                _forged = true;
                PopulateForgeResult(ids);
                SetPhaseVisibility(showResult: true);
            }
        }

        void SetPhaseVisibility(bool showResult)
        {
            var pick = El("craft-pick");
            var result = El("forge-result");
            if (pick != null)
                pick.style.display = showResult ? DisplayStyle.None : DisplayStyle.Flex;
            if (result != null)
                result.style.display = showResult ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void PopulateForgeResult(IReadOnlyList<string> cardIds)
        {
            if (_session == null || Root == null) return;

            var strip = El("txn-strip");
            if (strip == null) return;
            strip.Clear();

            var db = _session.Rules?.CardDatabase;
            if (db == null) return;

            var cardsRow = new VisualElement();
            cardsRow.style.flexDirection = FlexDirection.Row;

            foreach (var cardId in cardIds)
            {
                var inst = _session.GetCard(cardId);
                var def = inst != null ? db.GetById(inst.DefinitionId) : null;
                if (def == null) continue;

                var chip = CardChipFactory.CreateFromDefinition(def);
                chip.style.width = 24;
                chip.style.height = 34;
                chip.style.marginLeft = cardsRow.childCount > 0 ? 3 : 0;
                cardsRow.Add(chip);
            }

            strip.Add(cardsRow);

            var arrow = new Label("→");
            arrow.AddToClassList("txn__arrow");
            strip.Add(arrow);

            var output = new VisualElement();
            output.style.alignItems = Align.Center;

            var dot = new VisualElement();
            dot.AddToClassList("reagent-dot");
            dot.AddToClassList(CraftReagentPanelBindings.ReagentDotClass(_reagent));
            dot.style.width = 26;
            dot.style.height = 26;
            output.Add(dot);

            var name = new Label(CraftReagentPanelBindings.ReagentDisplayName(_reagent));
            name.style.fontSize = 9;
            name.style.color = new StyleColor(new UnityEngine.Color(184f / 255f, 154f / 255f, 110f / 255f));
            name.style.marginTop = 3;
            output.Add(name);
            strip.Add(output);

            var subtitle = Lbl("result-subtitle");
            if (subtitle != null)
                subtitle.text = $"Forged 1 {CraftReagentPanelBindings.ReagentDisplayName(_reagent)}";
        }

        void UpdateResultDoneLabel()
        {
            var doneBtn = Btn("result-done-btn");
            if (doneBtn == null || _session == null) return;
            doneBtn.text = _session.Phase.CurrentSeason == Season.Winter
                ? "Back to winter rites"
                : "Back to the workshop";
        }

        public void ResetForgeState()
        {
            _forged = false;
            _uiInitialized = false;
            InitialReagent = null;
        }
    }
}
