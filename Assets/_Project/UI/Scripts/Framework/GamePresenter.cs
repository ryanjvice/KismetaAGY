using System;
using System.Collections.Generic;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.UI.Chronicle;
using Kismeta.UI.Components;
using Kismeta.UI.Controllers;
using Kismeta.UI.Setup;
using UnityEngine;

namespace Kismeta.UI
{
    /// <summary>
    /// Connects <see cref="GameLoop"/> state to <see cref="ScreenRouter"/> navigation
    /// and refreshes active screen controllers when the session changes.
    /// </summary>
    [RequireComponent(typeof(ScreenRouter))]
    public sealed class GamePresenter : MonoBehaviour
    {
        [SerializeField] private bool _startAtTitle = true;

        private readonly SetupSheetState _setupSheetState = new();
        private UiSetupConfig _pendingSetupConfig;

        private ScreenRouter _router;
        private ViewportLayout _layout;
        private SummerOverlayHost? _summerOverlays;
        private SpringOverlayHost? _springOverlays;
        private ContestOverlayHost? _contestOverlays;
        private AutumnOverlayHost? _autumnOverlays;
        private EndOverlayHost? _endOverlays;
        private ExchangeOverlayHost? _exchangeOverlays;
        private PlayerHudController? _playerHud;
        private readonly Queue<PlayerExchangeEvent> _exchangeQueue = new();
        private readonly CommandBridge _bridge = new();
        private GameSession? _session;
        private GameLoop? _loop;
        private CeremonyGate? _ceremonyGate;
        private GameChronicle? _chronicle;

        private bool _inGame;
        private bool _humanPending;
        private ActionHint _lastHint = ActionHint.None;
        private Season _lastSeason = Season.Spring;
        private CeremonyStep? _lastCeremonyStep;
        private bool _adeptModalOpen;
        private bool _fateModalOpen;
        private string? _lastAdeptModalCardId;
        private string? _completedAdeptModalCardId;
        private string? _lastFateModalKey;
        private string? _completedFateModalKey;

        public CommandBridge Bridge => _bridge;
        public bool IsInGame => _inGame;
        public CeremonyGate? CeremonyGate => _ceremonyGate;

        public event Action<UiSetupConfig>? SetupBeginRequested;
        public event Action? NewGameRequested;

        /// <summary>Supplied by bootstrap to resolve human vs AI seats before a session exists.</summary>
        public Func<int>? ResolveHumanPlayerCount;

        private void Awake()
        {
            _router = GetComponent<ScreenRouter>();
            _layout = GetComponent<ViewportLayout>();
            _summerOverlays = GetComponent<SummerOverlayHost>();
            _springOverlays = GetComponent<SpringOverlayHost>();
            _contestOverlays = GetComponent<ContestOverlayHost>();
            _autumnOverlays = GetComponent<AutumnOverlayHost>();
            _endOverlays = GetComponent<EndOverlayHost>();
            _exchangeOverlays = GetComponent<ExchangeOverlayHost>();
            _playerHud = GetComponent<PlayerHudController>();
        }

        private void Start()
        {
            if (!_startAtTitle || _layout == null)
                return;

            // Fallback when ScreenRouter already has assets but bootstrap did not navigate yet.
            _layout.RunWhenReady(EnsureTitleScreenVisible);
        }

        private void EnsureTitleScreenVisible()
        {
            if (!string.IsNullOrEmpty(_router.CurrentScreenId))
                return;

            InitializeForTitle();
        }

        /// <summary>Wire title menu and show the title screen before a session exists.</summary>
        public void InitializeForTitle()
        {
            WireTitleScreen();
            WireShellScreens();
            WireAgekeeperContest();
            if (_router.CurrentScreenId != ScreenIds.Title)
                _router.GoTo(ScreenIds.Title);
        }

        public void Bind(GameSession session, GameLoop loop, CeremonyGate? ceremonyGate = null,
            GameChronicle? chronicle = null)
        {
            Unbind();

            _session = session;
            _loop = loop;
            _ceremonyGate = ceremonyGate;
            _chronicle = chronicle;
            _bridge.Bind(loop);
            _bridge.OnSideEffectApplied = RefreshActiveScreen;

            session.OnEvent += OnSessionEvent;
            loop.OnLog += OnLoopLog;

            EnsureOverlayHosts();
            HeaderOverlayBindings.ConfigureRivalSelection(id =>
            {
                if (_endOverlays == null)
                {
                    Debug.LogWarning("[GamePresenter] EndOverlayHost missing — rival click cannot open Card Table.");
                    return;
                }
                _endOverlays.ShowCardTable(id);
            });

            WireTitleScreen();
            WireShellScreens();
            WireAgekeeperContest();
            WireStepScreens();
            WireSummerNavigation();
            WireSeasonHubNavigation();
            WireContestNavigation();
            WireAutumnNavigation();
            WireActionGroupRailRefresh();
            WireEndNavigation();
            WireCardOverlays();
            WireExchangeOverlays();
        }

        public void Unbind()
        {
            if (_session != null)
                _session.OnEvent -= OnSessionEvent;
            if (_loop != null)
                _loop.OnLog -= OnLoopLog;
            _bridge.OnSideEffectApplied = null;
            _session = null;
            _loop = null;
            _ceremonyGate = null;
            _chronicle = null;
            _inGame = false;
            _lastCeremonyStep = null;
            _adeptModalOpen = false;
            _fateModalOpen = false;
            _lastAdeptModalCardId = null;
            _completedAdeptModalCardId = null;
            _lastFateModalKey = null;
            _completedFateModalKey = null;
            _exchangeQueue.Clear();
            HeaderOverlayBindings.ConfigureRivalSelection(null);
            DismissAllOverlays();
            _playerHud?.Hide();
        }

        public void ShowNewGameSetup()
        {
            _inGame = false;
            _setupSheetState.Detach();
            _router.ShowSetupSheet(
                onOpened: sheet => _setupSheetState.Attach(sheet),
                onBegin: BeginAgekeeperContest);
        }

        private void Update()
        {
            if (_loop == null || _session == null || !_inGame)
                return;

            var ceremony = _ceremonyGate?.ActiveStep;
            if (ceremony != _lastCeremonyStep)
            {
                _lastCeremonyStep = ceremony;
                if (ceremony != null)
                    RouteIfNeeded(MapCeremonyScreen(ceremony.Value));
                else
                    RouteGameplay();
                RefreshActiveScreen();
                return;
            }

            if (ceremony != null)
            {
                RouteIfNeeded(MapCeremonyScreen(ceremony.Value));
                RefreshActiveScreen();
                return;
            }

            if (_session.IsOver)
            {
                DismissAllOverlays();
                RouteIfNeeded(ScreenIds.Victory);
                RefreshActiveScreen();
                return;
            }

            var humanPending = _loop.PendingHumanController != null;
            var hint = _loop.PendingHint;
            var season = _session.Phase.CurrentSeason;

            if (humanPending)
            {
                _humanPending = true;
                _lastHint = hint;
                _lastSeason = season;
                RouteGameplay();
                TryOpenAdeptModal(hint);
                TryOpenFateModal(hint);
                RefreshActiveScreen();
                return;
            }

            if (_humanPending || hint != _lastHint || season != _lastSeason)
            {
                _humanPending = false;
                _lastHint = hint;
                _lastSeason = season;
                if (!IsFateDecisionHint(hint))
                {
                    _fateModalOpen = false;
                    _lastFateModalKey = null;
                }
                if (hint != ActionHint.AdeptDecision)
                {
                    _adeptModalOpen = false;
                    _lastAdeptModalCardId = null;
                    _completedAdeptModalCardId = null;
                }
                RouteGameplay();
                RefreshActiveScreen();
            }
        }

        private void OnSessionEvent(IGameEvent evt)
        {
            if (evt is GameEndedEvent)
            {
                DismissAllOverlays();
                RouteIfNeeded(ScreenIds.Victory);
                RefreshActiveScreen();
                return;
            }

            if (evt is FateResolvedEvent fate && _session != null && _endOverlays != null)
            {
                int localId = _bridge.ActivePlayerId;
                if (localId < 0 && _loop?.PendingHumanController != null)
                    localId = _bridge.PendingController?.Slot.Index ?? -1;
                if (fate.PlayerId == localId)
                    _endOverlays.ShowFate(fate.FateCardId, fate.ArcanaNum);
            }

            if (evt is PlayerExchangeEvent exchange)
            {
                _exchangeQueue.Enqueue(exchange);
                TryShowQueuedExchange();
            }

            RefreshActiveScreenIfNeeded();
        }

        private void OnLoopLog(string message)
        {
            NotifyPassedScreenActivity(message);
            RefreshActiveScreenIfNeeded();
        }

        private void NotifyPassedScreenActivity(string message)
        {
            if (_loop == null || !_loop.IsLocalHumanYielded || string.IsNullOrWhiteSpace(message))
                return;

            var controller = _router.ActiveController;
            if (controller is SpringPassedController springPassed)
                springPassed.NotifyActivity(message);
            else if (controller is SummerPassedController summerPassed)
                summerPassed.NotifyActivity(message);
            else if (controller is AutumnPassedController autumnPassed)
                autumnPassed.NotifyActivity(message);
        }

        private void RefreshActiveScreenIfNeeded()
        {
            if (_session == null || _loop == null) return;

            if (_ceremonyGate?.ActiveStep != null)
            {
                RouteIfNeeded(MapCeremonyScreen(_ceremonyGate.ActiveStep.Value));
                RefreshActiveScreen();
                return;
            }

            RefreshPlayerHud();

            if (_session.IsOver)
            {
                if (!_layout.IsOverlayVisible)
                {
                    RouteIfNeeded(ScreenIds.Victory);
                    RefreshActiveScreen();
                }
                return;
            }

            if (_layout.IsOverlayVisible)
                return;

            var activeId = _router.CurrentScreenId;
            if (_loop.PendingHumanController == null && _loop.ActivePlayerId < 0
                && activeId is ScreenIds.FatefulWager or ScreenIds.CraftReagent or ScreenIds.CardLimits)
                return;

            if (activeId is ScreenIds.Victory or ScreenIds.Chronicle)
                return;

            RouteGameplay();
            RefreshActiveScreen();
        }

        private void TryOpenFateModal(ActionHint hint)
        {
            if (_endOverlays == null || _session == null) return;
            if (!IsFateDecisionHint(hint)) return;

            var fateKey = $"{hint}:{_loop?.PendingCardId ?? ""}";
            if (fateKey != _lastFateModalKey)
            {
                _fateModalOpen = false;
                _lastFateModalKey = fateKey;
                _completedFateModalKey = null;
            }
            if (fateKey == _completedFateModalKey) return;
            if (_fateModalOpen && _endOverlays.IsOpen) return;

            _fateModalOpen = true;
            switch (hint)
            {
                case ActionHint.FateMoonDecision:
                    _endOverlays.ShowMoonDecision();
                    break;
                case ActionHint.FateReagentChoice:
                    _endOverlays.ShowFateReagentChoice(_loop?.PendingCardId);
                    break;
                case ActionHint.FateLoversTargetPick:
                    _endOverlays.ShowLoversTargetPick();
                    break;
                case ActionHint.FateLoversChoice:
                    var fateId = _loop?.PendingCardId;
                    int drawerId = FindFateDrawerId(fateId);
                    _endOverlays.ShowLoversChoice(drawerId >= 0 ? drawerId : 0);
                    break;
            }
        }

        static bool IsFateDecisionHint(ActionHint hint) => hint switch
        {
            ActionHint.FateMoonDecision or ActionHint.FateReagentChoice
                or ActionHint.FateLoversTargetPick or ActionHint.FateLoversChoice => true,
            _ => false
        };

        int FindFateDrawerId(string? fateCardId)
        {
            if (_session == null || string.IsNullOrEmpty(fateCardId)) return -1;
            foreach (var p in _session.Players)
            {
                foreach (var id in p.Arcanum)
                {
                    if (id == fateCardId)
                        return p.PlayerId;
                }
            }
            return -1;
        }

        private void TryOpenAdeptModal(ActionHint hint)
        {
            if (hint != ActionHint.AdeptDecision || _endOverlays == null || _loop == null) return;

            var adeptId = _loop.PendingCardId;
            if (string.IsNullOrEmpty(adeptId)) return;

            if (adeptId != _lastAdeptModalCardId)
            {
                _adeptModalOpen = false;
                _lastAdeptModalCardId = adeptId;
                _completedAdeptModalCardId = null;
            }
            if (adeptId == _completedAdeptModalCardId) return;
            if (_adeptModalOpen && _endOverlays.IsOpen) return;

            _adeptModalOpen = true;
            _endOverlays.ShowAdept(adeptId);
        }

        private void WireTitleScreen()
        {
            var title = _router.GetController<TitleScreenController>(ScreenIds.Title);
            if (title == null) return;

            title.OnNewGame = () =>
            {
                _router.ShowSetupSheet(
                    onOpened: sheet => _setupSheetState.Attach(sheet),
                    onBegin: BeginAgekeeperContest);
            };
            title.OnResume = () => _router.GoTo(ScreenIds.Resume);
            title.OnJoin = () => _router.GoTo(ScreenIds.Join);
            title.OnCodex = OpenCodex;
            title.OnSettings = OpenSettings;
        }

        private void WireShellScreens()
        {
            var join = _router.GetController<JoinScreenController>(ScreenIds.Join);
            if (join != null)
            {
                join.OnBack = ReturnToTitle;
                join.OnJoin = code =>
                    Debug.Log($"[UI] Join room '{code}' — multiplayer not implemented.");
            }

            var resume = _router.GetController<ResumeScreenController>(ScreenIds.Resume);
            if (resume != null)
            {
                resume.OnBack = ReturnToTitle;
                resume.OnResumeGame = id =>
                    Debug.Log($"[UI] Resume save '{id}' — persistence not implemented.");
                resume.RebuildList();
            }

            var codex = _router.GetController<CodexScreenController>(ScreenIds.Codex);
            if (codex != null)
                codex.OnBack = ReturnToTitle;

            var settings = _router.GetController<SettingsScreenController>(ScreenIds.Settings);
            if (settings != null)
                settings.OnBack = ReturnToTitle;
        }

        private void OpenCodex()
        {
            _router.GoTo(ScreenIds.Codex);
            _router.GetController<CodexScreenController>(ScreenIds.Codex)?.ShowTab(CodexTabs.Cards);
        }

        private void OpenSettings() => _router.GoTo(ScreenIds.Settings);

        private void WireAgekeeperContest()
        {
            var contest = _router.GetController<AgekeeperContestController>(ScreenIds.AgekeeperContest);
            if (contest == null) return;

            contest.OnComplete = winnerId =>
            {
                var cfg = _pendingSetupConfig;
                cfg.FirstAgekeeperPlayerId = winnerId;
                NotifySetupBegin(cfg);
            };
        }

        private void BeginAgekeeperContest()
        {
            _pendingSetupConfig = _setupSheetState.Current;
            _setupSheetState.Detach();
            int humanPlayers = ResolveHumanPlayerCount?.Invoke() ?? 1;
            _router.GoTo(ScreenIds.AgekeeperContest);
            _router.GetController<AgekeeperContestController>(ScreenIds.AgekeeperContest)
                ?.BeginContest(_pendingSetupConfig.Players, humanPlayers);
        }

        public void NotifySetupBegin(UiSetupConfig config)
        {
            _router.DismissOverlay();
            SetupBeginRequested?.Invoke(config);
            _inGame = true;
            _humanPending = false;
            _lastHint = ActionHint.None;
            _lastCeremonyStep = null;
            _adeptModalOpen = false;
            _fateModalOpen = false;
            _lastAdeptModalCardId = null;
            _lastFateModalKey = null;
            RouteGameplay();
        }

        public void ReturnToTitle()
        {
            _inGame = false;
            _setupSheetState.Detach();
            _playerHud?.Hide();
            _router.GoTo(ScreenIds.Title);
        }

        private void RouteGameplay()
        {
            if (_session!.IsOver)
            {
                DismissAllOverlays();
                RouteIfNeeded(ScreenIds.Victory);
                RefreshActiveScreen();
                return;
            }

            if (_loop.PendingHumanController == null)
            {
                _springOverlays?.DismissIfNotHumanTurn();
                _summerOverlays?.DismissIfNotHumanTurn();
                _contestOverlays?.DismissIfNotHumanTurn();
                _autumnOverlays?.DismissIfNotHumanTurn();
                _endOverlays?.DismissIfNotHumanTurn();
                _adeptModalOpen = false;
                RouteIfNeeded(ResolveSpectatorScreen(_session.Phase.CurrentSeason));
                RefreshActiveScreen();
                return;
            }

            RouteIfNeeded(ResolveGameplayScreen(_session.Phase.CurrentSeason, _loop.PendingHint));
            RefreshActiveScreen();
        }

        private static string ResolveGameplayScreen(Season season, ActionHint hint) => hint switch
        {
            ActionHint.Commune => ScreenIds.SpringHub,
            ActionHint.SpringAction => ScreenIds.SpringHub,
            ActionHint.SpringHubResponse => ScreenIds.SpringHub,
            ActionHint.ConfirmHarvest => ScreenIds.SpringHarvest,
            ActionHint.DiscardToLimit => ScreenIds.CardLimits,
            ActionHint.SummerContestResponse => ScreenIds.SummerMain,
            ActionHint.AutumnForgeResponse => ScreenIds.AutumnMain,
            _ => ResolveSeasonMainScreen(season)
        };

        private void WireStepScreens()
        {
            var winter = _router.GetController<WinterHubController>(ScreenIds.WinterHub);
            if (winter != null)
            {
                winter.OnOpenCraft = OpenWinterCraft;
                winter.OnOpenWager = OpenWinterWager;
            }

            var wager = _router.GetController<FatefulWagerController>(ScreenIds.FatefulWager);
            if (wager != null)
                wager.OnCompleted = () => _router.GoTo(ScreenIds.WinterHub);
        }

        private void WireSummerNavigation()
        {
            var summer = _router.GetController<SummerSceneController>(ScreenIds.SummerMain);
            if (summer == null) return;

            summer.OnOpenCardTable = () => _endOverlays?.ShowCardTable();
            summer.OnRivalSelected = id => _endOverlays?.ShowCardTable(id);
            summer.OnOpenActiveEffects = () => _endOverlays?.ShowActiveEffects();
            summer.OnOpenPlayerEffects = id => _endOverlays?.ShowActiveEffects(id);
            summer.OnInspectCard = id => _endOverlays?.ShowInspect(id);

            summer.OnPass = () => _summerOverlays?.ShowEndSummer();

            // Summer is now a contest hub: Trade / Duel / Gambit / Opposition.
            summer.OnTrade = () => _contestOverlays?.ShowTrade();
            summer.OnDuel = () => _contestOverlays?.ShowDuel();
            summer.OnGambit = () => _contestOverlays?.ShowGambit();
            summer.OnOpposition = () => _contestOverlays?.ShowOpposition();
        }

        private void WireSeasonHubNavigation()
        {
            var springHub = _router.GetController<SpringHubController>(ScreenIds.SpringHub);
            if (springHub != null)
            {
                springHub.OnOpenCardTable = () => _endOverlays?.ShowCardTable();
                springHub.OnRivalSelected = id => _endOverlays?.ShowCardTable(id);
                springHub.OnOpenActiveEffects = () => _endOverlays?.ShowActiveEffects();
                springHub.OnInspectCard = id => _endOverlays?.ShowInspect(id);
            }

            var springPassed = _router.GetController<SpringPassedController>(ScreenIds.SpringPassed);
            if (springPassed != null)
            {
                springPassed.OnOpenCardTable = () => _endOverlays?.ShowCardTable();
                springPassed.OnRivalSelected = id => _endOverlays?.ShowCardTable(id);
                springPassed.OnOpenActiveEffects = () => _endOverlays?.ShowActiveEffects();
                springPassed.OnInspectCard = id => _endOverlays?.ShowInspect(id);
            }

            var summerHub = _router.GetController<SummerHubController>(ScreenIds.SummerHub);
            if (summerHub != null)
            {
                summerHub.OnOpenCardTable = () => _endOverlays?.ShowCardTable();
                summerHub.OnRivalSelected = id => _endOverlays?.ShowCardTable(id);
                summerHub.OnOpenActiveEffects = () => _endOverlays?.ShowActiveEffects();
                summerHub.OnInspectCard = id => _endOverlays?.ShowInspect(id);
            }

            var summerPassed = _router.GetController<SummerPassedController>(ScreenIds.SummerPassed);
            if (summerPassed != null)
            {
                summerPassed.OnOpenCardTable = () => _endOverlays?.ShowCardTable();
                summerPassed.OnRivalSelected = id => _endOverlays?.ShowCardTable(id);
                summerPassed.OnOpenActiveEffects = () => _endOverlays?.ShowActiveEffects();
                summerPassed.OnInspectCard = id => _endOverlays?.ShowInspect(id);
            }

            var autumnHub = _router.GetController<AutumnHubController>(ScreenIds.AutumnHub);
            if (autumnHub != null)
            {
                autumnHub.OnOpenCardTable = () => _endOverlays?.ShowCardTable();
                autumnHub.OnRivalSelected = id => _endOverlays?.ShowCardTable(id);
                autumnHub.OnOpenActiveEffects = () => _endOverlays?.ShowActiveEffects();
                autumnHub.OnInspectCard = id => _endOverlays?.ShowInspect(id);
            }

            var autumnPassed = _router.GetController<AutumnPassedController>(ScreenIds.AutumnPassed);
            if (autumnPassed != null)
            {
                autumnPassed.OnOpenCardTable = () => _endOverlays?.ShowCardTable();
                autumnPassed.OnRivalSelected = id => _endOverlays?.ShowCardTable(id);
                autumnPassed.OnOpenActiveEffects = () => _endOverlays?.ShowActiveEffects();
                autumnPassed.OnInspectCard = id => _endOverlays?.ShowInspect(id);
            }
        }

        private void WireContestNavigation()
        {
            if (_summerOverlays == null || _contestOverlays == null) return;

            _summerOverlays.OnTrade = () => _contestOverlays.ShowTrade();
            _summerOverlays.OnDuel = () => _contestOverlays.ShowDuel();
            _summerOverlays.OnGambit = () => _contestOverlays.ShowGambit();
        }

        private void WireAutumnNavigation()
        {
            if (_autumnOverlays == null)
            {
                Debug.LogWarning("[UI] AutumnOverlayHost missing — autumn action buttons will not open overlays.");
                return;
            }

            var autumn = _router.GetController<AutumnSceneController>(ScreenIds.AutumnMain);
            if (autumn == null) return;

            // Contextual forge actions hosted by the Autumn overlay host.
            autumn.OnFire = () => _autumnOverlays.ShowFire();
            autumn.OnTemper = () => _autumnOverlays.ShowTemper();
            autumn.OnLeaveStasis = () => _autumnOverlays.ShowLeaveStasis();
            autumn.OnPass = () => _autumnOverlays.ShowEndAutumn();

            // Craft / Activate reuse the season-agnostic Summer overlay controllers.
            autumn.OnCraft = () => _summerOverlays?.ShowCraftReagent();
            autumn.OnActivate = () => _summerOverlays?.ShowActivate();
            autumn.OnActivateSlot = slot => _summerOverlays?.ShowActivate(slot);
            autumn.OnCrucibleDetail = slot => _summerOverlays?.ShowCrucibleDetail(slot);
            autumn.OnCauldronClicked = suit =>
            {
                if (_session == null || _loop == null || _summerOverlays == null) return;

                var localId = MainSceneBindings.ResolveLocalPlayerId(_session, _loop, _bridge);
                if (localId < 0 || localId >= _session.Players.Count) return;

                var player = _session.Players[localId];
                if (player.IsCauldronLit(suit))
                    _summerOverlays.ShowCraftReagent(Correspondence.ReagentFor(suit));
                else if (CauldronHubBindings.TrySlotIndexForSuit(_session, localId, suit, out int slot))
                    _summerOverlays.ShowActivate(slot);
                else
                    _summerOverlays.ShowActivate();
            };

            autumn.OnOpenCardTable = () => _endOverlays?.ShowCardTable();
            autumn.OnRivalSelected = id => _endOverlays?.ShowCardTable(id);
            autumn.OnOpenActiveEffects = () => _endOverlays?.ShowActiveEffects();
            autumn.OnInspectCard = id => _endOverlays?.ShowInspect(id);
        }

        private void WireActionGroupRailRefresh()
        {
            void Refresh()
            {
                RefreshActionGroupRails();
                TryShowQueuedExchange();
            }

            if (_summerOverlays != null)
                _summerOverlays.OverlayChanged = Refresh;
            if (_contestOverlays != null)
                _contestOverlays.OverlayChanged = Refresh;
            if (_autumnOverlays != null)
                _autumnOverlays.OverlayChanged = Refresh;

            _router.GetController<SummerSceneController>(ScreenIds.SummerMain)
                ?.ConfigureOverlays(_summerOverlays, _contestOverlays);
            _router.GetController<AutumnSceneController>(ScreenIds.AutumnMain)
                ?.ConfigureOverlays(_autumnOverlays, _contestOverlays);
        }

        private void RefreshActionGroupRails()
        {
            _router.GetController<SummerSceneController>(ScreenIds.SummerMain)?.RefreshActionGroupRail();
            _router.GetController<SummerHubController>(ScreenIds.SummerHub)?.RefreshActionGroupRail();
            _router.GetController<AutumnSceneController>(ScreenIds.AutumnMain)?.RefreshActionGroupRail();
            _router.GetController<AutumnHubController>(ScreenIds.AutumnHub)?.RefreshActionGroupRail();
        }

        private void WireEndNavigation()
        {
            var victory = _router.GetController<VictoryController>(ScreenIds.Victory);
            if (victory != null)
            {
                victory.OnChronicle = () => _router.GoTo(ScreenIds.Chronicle);
                victory.OnNewGame = () => NewGameRequested?.Invoke();
            }

            var chronicle = _router.GetController<ChronicleController>(ScreenIds.Chronicle);
            if (chronicle != null)
            {
                chronicle.OnBack = () => _router.GoTo(ScreenIds.Victory);
                chronicle.OnNewGame = () => NewGameRequested?.Invoke();
            }
        }

        private void WireExchangeOverlays()
        {
            if (_exchangeOverlays == null || _session == null) return;

            _exchangeOverlays.BindState(_session);
            _exchangeOverlays.OnConfirmed = () =>
            {
                TryShowQueuedExchange();
                RefreshActiveScreenIfNeeded();
            };
        }

        private void TryShowQueuedExchange()
        {
            if (_session == null || _exchangeOverlays == null || _exchangeQueue.Count == 0)
                return;
            if (_exchangeOverlays.IsOpen)
                return;
            if (IsBlockingOverlayOpen())
                return;

            var next = _exchangeQueue.Dequeue();
            _exchangeOverlays.Show(next);
        }

        private bool IsBlockingOverlayOpen()
        {
            if (_contestOverlays?.IsOpen == true) return true;
            if (_summerOverlays?.IsOpen == true) return true;
            if (_springOverlays?.IsOpen == true) return true;
            if (_autumnOverlays?.IsOpen == true) return true;
            if (_endOverlays?.IsOpen == true) return true;
            return false;
        }

        private void WireCardOverlays()
        {
            if (_endOverlays != null)
            {
                var spring = _router.GetController<SpringHubController>(ScreenIds.SpringHub);
                if (spring != null)
                {
                    spring.OnOpenCardTable = () => _endOverlays.ShowCardTable();
                    spring.OnRivalSelected = id => _endOverlays.ShowCardTable(id);
                    spring.OnOpenActiveEffects = () => _endOverlays.ShowActiveEffects();
                    spring.OnInspectCard = id => _endOverlays.ShowInspect(id);
                    spring.OnBuildHouse = () => _springOverlays?.ShowBuildHouse();
                }

                var winter = _router.GetController<WinterHubController>(ScreenIds.WinterHub);
                if (winter != null)
                {
                    winter.OnOpenCardTable = () => _endOverlays.ShowCardTable();
                    winter.OnRivalSelected = id => _endOverlays.ShowCardTable(id);
                    winter.OnOpenActiveEffects = () => _endOverlays.ShowActiveEffects();
                    winter.OnInspectCard = id => _endOverlays.ShowInspect(id);
                }
            }

            if (_endOverlays == null || _contestOverlays == null) return;

            _endOverlays.OnDuelFromTable = id => _contestOverlays.ShowDuel(id);
            _endOverlays.OnGambitFromTable = id => _contestOverlays.ShowGambit(id);
            _endOverlays.OnTradeFromTable = id => _contestOverlays.ShowTrade(id);
            _endOverlays.OnOverlayDismissed = () =>
            {
                if (_adeptModalOpen && !string.IsNullOrEmpty(_lastAdeptModalCardId))
                    _completedAdeptModalCardId = _lastAdeptModalCardId;
                if (_fateModalOpen && !string.IsNullOrEmpty(_lastFateModalKey))
                    _completedFateModalKey = _lastFateModalKey;
                _adeptModalOpen = false;
                _fateModalOpen = false;
                TryShowQueuedExchange();
            };
        }

        private void EnsureOverlayHosts()
        {
            _summerOverlays = GetComponent<SummerOverlayHost>();
            _springOverlays = GetComponent<SpringOverlayHost>();
            _contestOverlays = GetComponent<ContestOverlayHost>();
            _autumnOverlays = GetComponent<AutumnOverlayHost>();
            _endOverlays = GetComponent<EndOverlayHost>();
            _exchangeOverlays = GetComponent<ExchangeOverlayHost>();

            if (_session != null)
            {
                _summerOverlays?.BindState(_session, _bridge);
                _springOverlays?.BindState(_session, _bridge);
                _contestOverlays?.BindState(_session, _bridge);
                _autumnOverlays?.BindState(_session, _bridge);
                _endOverlays?.BindState(_session, _loop!, _bridge);
                _exchangeOverlays?.BindState(_session);
            }
        }

        private void DismissAllOverlays()
        {
            _springOverlays?.Dismiss();
            _summerOverlays?.Dismiss();
            _contestOverlays?.Dismiss();
            _autumnOverlays?.Dismiss();
            _endOverlays?.Dismiss();
            _exchangeOverlays?.Dismiss();
            _exchangeQueue.Clear();
            _adeptModalOpen = false;
            _fateModalOpen = false;
            _lastAdeptModalCardId = null;
            _completedAdeptModalCardId = null;
            _lastFateModalKey = null;
            _completedFateModalKey = null;
        }

        private static string MapCeremonyScreen(CeremonyStep step) => step switch
        {
            CeremonyStep.RoundOpen => ScreenIds.RoundOpen,
            CeremonyStep.SpringIntro => ScreenIds.SpringIntro,
            CeremonyStep.SummerIntro => ScreenIds.SummerIntro,
            CeremonyStep.AutumnIntro => ScreenIds.AutumnIntro,
            CeremonyStep.WinterIntro => ScreenIds.WinterIntro,
            CeremonyStep.AgeClosing => ScreenIds.AgeClosing,
            _ => ScreenIds.Waiting
        };

        private string ResolveSpectatorScreen(Season season)
        {
            if (_loop != null && _loop.IsLocalHumanYielded)
            {
                return season switch
                {
                    Season.Spring => ScreenIds.SpringPassed,
                    Season.Summer => ScreenIds.SummerPassed,
                    Season.Autumn => ScreenIds.AutumnPassed,
                    _ => ScreenIds.Waiting
                };
            }

            return season switch
            {
                Season.Spring => ScreenIds.Waiting,
                Season.Summer => ScreenIds.SummerHub,
                Season.Autumn => ScreenIds.AutumnHub,
                _ => ScreenIds.Waiting
            };
        }

        private static string ResolveSeasonMainScreen(Season season) => season switch
        {
            Season.Spring => ScreenIds.SpringHub,
            Season.Summer => ScreenIds.SummerMain,
            Season.Autumn => ScreenIds.AutumnMain,
            Season.Winter => ScreenIds.WinterHub,
            _ => ScreenIds.SpringHub
        };

        private void RouteIfNeeded(string screenId)
        {
            if (_router.CurrentScreenId == screenId)
                return;

            if (screenId == ScreenIds.WinterHub && IsWinterHubSubScreen(_router.CurrentScreenId))
                return;

            if (screenId == ScreenIds.SpringHub && IsSpringHubSubScreen(_router.CurrentScreenId))
                return;

            if (screenId == ScreenIds.Victory && _router.CurrentScreenId == ScreenIds.Chronicle)
                return;

            _router.GoTo(screenId);
        }

        private static bool IsWinterHubSubScreen(string? screenId) =>
            screenId is ScreenIds.FatefulWager or ScreenIds.CraftReagent;

        void OpenWinterCraft()
        {
            var craft = _router.GetController<CraftReagentController>(ScreenIds.CraftReagent);
            if (craft != null)
            {
                craft.ResetForgeState();
                craft.OnBack = () => _router.GoTo(ScreenIds.WinterHub);
                craft.OnDone = () =>
                {
                    craft.ResetForgeState();
                    _router.GoTo(ScreenIds.WinterHub);
                };
            }
            _router.GoTo(ScreenIds.CraftReagent);
        }

        void OpenWinterWager()
        {
            var wager = _router.GetController<FatefulWagerController>(ScreenIds.FatefulWager);
            if (wager != null)
                wager.OnBack = () => _router.GoTo(ScreenIds.WinterHub);
            _router.GoTo(ScreenIds.FatefulWager);
            RefreshActiveScreen();
        }

        private static bool IsSpringHubSubScreen(string? screenId) =>
            screenId is ScreenIds.SpringHarvest;

        private void RefreshActiveScreen()
        {
            if (_session == null || _loop == null) return;

            var controller = _router.ActiveController;
            var gate = _ceremonyGate;
            if (gate != null)
            {
                if (controller is RoundOpenController roundOpen)
                    roundOpen.BindState(_session, gate);
                else if (controller is AgeClosingController ageClosing)
                    ageClosing.BindState(_session, gate);
                else if (controller is SpringIntroController springIntro)
                    springIntro.BindState(_session, gate);
                else if (controller is SummerIntroController summerIntro)
                    summerIntro.BindState(_session, gate);
                else if (controller is AutumnIntroController autumnIntro)
                    autumnIntro.BindState(_session, gate);
                else if (controller is WinterIntroController winterIntro)
                    winterIntro.BindState(_session, gate);
            }

            if (controller is SpringHubController spring)
            {
                spring.BindState(_session, _loop, _bridge);
                _springOverlays?.BindState(_session, _bridge);
                _endOverlays?.BindState(_session, _loop, _bridge);
            }
            else if (controller is SpringPassedController springPassed)
            {
                springPassed.BindState(_session, _loop, _bridge);
                _endOverlays?.BindState(_session, _loop, _bridge);
            }
            else if (controller is SummerSceneController summer)
            {
                summer.BindState(_session, _loop, _bridge);
                _summerOverlays?.BindState(_session, _bridge);
                _contestOverlays?.BindState(_session, _bridge);
                _endOverlays?.BindState(_session, _loop, _bridge);
            }
            else if (controller is SummerHubController summerHub)
            {
                summerHub.BindState(_session, _loop, _bridge);
                _endOverlays?.BindState(_session, _loop, _bridge);
            }
            else if (controller is SummerPassedController summerPassed)
            {
                summerPassed.BindState(_session, _loop, _bridge);
                _endOverlays?.BindState(_session, _loop, _bridge);
            }
            else if (controller is AutumnSceneController autumn)
            {
                autumn.BindState(_session, _loop, _bridge);
                _autumnOverlays?.BindState(_session, _bridge);
                _summerOverlays?.BindState(_session, _bridge);
                _contestOverlays?.BindState(_session, _bridge);
                _endOverlays?.BindState(_session, _loop, _bridge);
            }
            else if (controller is AutumnHubController autumnHub)
            {
                autumnHub.BindState(_session, _loop, _bridge);
                _endOverlays?.BindState(_session, _loop, _bridge);
            }
            else if (controller is AutumnPassedController autumnPassed)
            {
                autumnPassed.BindState(_session, _loop, _bridge);
                _endOverlays?.BindState(_session, _loop, _bridge);
            }
            else if (controller is WinterHubController winter)
            {
                winter.BindState(_session, _loop, _bridge);
                _endOverlays?.BindState(_session, _loop, _bridge);
            }
            else if (controller is SpringHarvestController springHarvest)
                springHarvest.BindState(_session, _loop, _bridge);
            else if (controller is WinterUnlockController winterUnlock)
                winterUnlock.BindState(_session, _bridge);
            else if (controller is FatefulWagerController fatefulWager)
                fatefulWager.BindState(_session, _bridge);
            else if (controller is CraftReagentController craftReagent)
                craftReagent.BindState(_session, _bridge);
            else if (controller is CardLimitsController cardLimits)
                cardLimits.BindState(_session, _bridge);
            else if (controller is VictoryController victory && _chronicle != null)
                victory.BindState(_session, _chronicle);
            else if (controller is ChronicleController chronicleCtrl && _chronicle != null)
                chronicleCtrl.BindState(_session, _chronicle);
            else if (controller is WaitingHudController waiting)
                waiting.BindState(_session, _loop);
            else if (controller is ResumeScreenController resumeCtrl)
                resumeCtrl.RebuildList();
            else
                controller?.Refresh();

            RefreshPlayerHud();
        }

        void RefreshPlayerHud()
        {
            if (_playerHud == null || _session == null || _loop == null)
                return;

            bool show = _inGame && ShouldShowPlayerHud(_router.CurrentScreenId);
            _playerHud.SetVisible(show);
            if (show)
                _playerHud.BindState(_session, _loop, _bridge);
        }

        static bool ShouldShowPlayerHud(string? screenId) => screenId switch
        {
            ScreenIds.Title or ScreenIds.Join or ScreenIds.Resume or ScreenIds.Codex
                or ScreenIds.AgekeeperContest or ScreenIds.Victory or ScreenIds.Chronicle => false,
            _ => true
        };
    }
}
