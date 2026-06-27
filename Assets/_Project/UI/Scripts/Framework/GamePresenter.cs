using System;
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
        private ContestOverlayHost? _contestOverlays;
        private AutumnOverlayHost? _autumnOverlays;
        private EndOverlayHost? _endOverlays;
        private PlayerHudController? _playerHud;
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
            _contestOverlays = GetComponent<ContestOverlayHost>();
            _autumnOverlays = GetComponent<AutumnOverlayHost>();
            _endOverlays = GetComponent<EndOverlayHost>();
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

            RefreshActiveScreenIfNeeded();
        }

        private void OnLoopLog(string _) => RefreshActiveScreenIfNeeded();

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
                && activeId is ScreenIds.WinterUnlock
                    or ScreenIds.FatefulWager or ScreenIds.CraftReagent or ScreenIds.CardLimits)
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
            title.OnHowToPlay = OpenHowToPlay;
            title.OnCodex = OpenCodex;
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
        }

        private void OpenCodex()
        {
            _router.GoTo(ScreenIds.Codex);
            _router.GetController<CodexScreenController>(ScreenIds.Codex)?.ShowTab(CodexTabs.Cards);
        }

        private void OpenHowToPlay()
        {
            _router.GoTo(ScreenIds.Codex);
            _router.GetController<CodexScreenController>(ScreenIds.Codex)?.ShowTab(CodexTabs.Terms);
        }

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
            ActionHint.ConfirmHarvest => ScreenIds.SpringHarvest,
            ActionHint.DiscardToLimit => ScreenIds.CardLimits,
            _ => ResolveSeasonMainScreen(season)
        };

        private void WireStepScreens()
        {
            var winter = _router.GetController<WinterHubController>(ScreenIds.WinterHub);
            if (winter != null)
            {
                winter.OnOpenUnlock = () => _router.GoTo(ScreenIds.WinterUnlock);
                winter.OnOpenCraft = OpenWinterCraft;
                winter.OnOpenWager = () => _router.GoTo(ScreenIds.FatefulWager);
            }

            var unlock = _router.GetController<WinterUnlockController>(ScreenIds.WinterUnlock);
            if (unlock != null)
                unlock.OnDone = () => _router.GoTo(ScreenIds.WinterHub);

            var wager = _router.GetController<FatefulWagerController>(ScreenIds.FatefulWager);
            if (wager != null)
                wager.OnCompleted = () => _router.GoTo(ScreenIds.WinterHub);
        }

        private void WireSummerNavigation()
        {
            var summer = _router.GetController<SummerSceneController>(ScreenIds.SummerMain);
            if (summer != null)
            {
                summer.OnOpenCardTable = () => _endOverlays?.ShowCardTable();
                summer.OnOpenActiveEffects = () => _endOverlays?.ShowActiveEffects();
                summer.OnInspectCard = id => _endOverlays?.ShowInspect(id);
            }

            if (_summerOverlays == null) return;

            if (summer == null) return;

            summer.OnCraftBuild = () => _summerOverlays.ShowCraftBuildSheet();
            summer.OnConsort = () => _summerOverlays.ShowConsortSheet();
            summer.OnActivate = () => _summerOverlays.ShowActivate();
            summer.OnPass = () => _summerOverlays.ShowEndSummer();
            summer.OnCauldronClicked = suit =>
            {
                if (_session == null || _loop == null) return;

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
        }

        private void WireSeasonHubNavigation()
        {
            var summerHub = _router.GetController<SummerHubController>(ScreenIds.SummerHub);
            if (summerHub != null)
            {
                summerHub.OnOpenCardTable = () => _endOverlays?.ShowCardTable();
                summerHub.OnOpenActiveEffects = () => _endOverlays?.ShowActiveEffects();
                summerHub.OnInspectCard = id => _endOverlays?.ShowInspect(id);
            }

            var autumnHub = _router.GetController<AutumnHubController>(ScreenIds.AutumnHub);
            if (autumnHub != null)
            {
                autumnHub.OnOpenCardTable = () => _endOverlays?.ShowCardTable();
                autumnHub.OnOpenActiveEffects = () => _endOverlays?.ShowActiveEffects();
                autumnHub.OnInspectCard = id => _endOverlays?.ShowInspect(id);
            }
        }

        private void WireContestNavigation()
        {
            if (_summerOverlays == null || _contestOverlays == null) return;

            _summerOverlays.OnTrade = () => _contestOverlays.ShowTrade();
            _summerOverlays.OnDuel = () => _contestOverlays.ShowDuel();
            _summerOverlays.OnGambit = () => _contestOverlays.ShowGambit();

            var autumn = _router.GetController<AutumnSceneController>(ScreenIds.AutumnMain);
            if (autumn != null && autumn.OnOppose == null)
                autumn.OnOppose = () => _contestOverlays.ShowOpposition();
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

            autumn.OnFire = () => _autumnOverlays.ShowFire();
            autumn.OnTemper = () => _autumnOverlays.ShowTemper();
            autumn.OnManageCards = () => _autumnOverlays.ShowManageCards();
            autumn.OnLeaveStasis = () => _autumnOverlays.ShowLeaveStasis();
            autumn.OnPass = () => _autumnOverlays.ShowEndAutumn();
            autumn.OnOppose = () => _contestOverlays?.ShowOpposition();
            autumn.OnOpenCardTable = () => _endOverlays?.ShowCardTable();
            autumn.OnOpenActiveEffects = () => _endOverlays?.ShowActiveEffects();
            autumn.OnInspectCard = id => _endOverlays?.ShowInspect(id);
        }

        private void WireActionGroupRailRefresh()
        {
            void Refresh() => RefreshActionGroupRails();

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

        private void WireCardOverlays()
        {
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
            };

            var spring = _router.GetController<SpringHubController>(ScreenIds.SpringHub);
            if (spring != null)
            {
                spring.OnOpenCardTable = () => _endOverlays.ShowCardTable();
                spring.OnOpenActiveEffects = () => _endOverlays.ShowActiveEffects();
                spring.OnInspectCard = id => _endOverlays.ShowInspect(id);
            }

            var winter = _router.GetController<WinterHubController>(ScreenIds.WinterHub);
            if (winter != null)
            {
                winter.OnOpenCardTable = () => _endOverlays.ShowCardTable();
                winter.OnOpenActiveEffects = () => _endOverlays.ShowActiveEffects();
                winter.OnInspectCard = id => _endOverlays.ShowInspect(id);
            }
        }

        private void EnsureOverlayHosts()
        {
            _summerOverlays = GetComponent<SummerOverlayHost>();
            _contestOverlays = GetComponent<ContestOverlayHost>();
            _autumnOverlays = GetComponent<AutumnOverlayHost>();
            _endOverlays = GetComponent<EndOverlayHost>();
        }

        private void DismissAllOverlays()
        {
            _summerOverlays?.Dismiss();
            _contestOverlays?.Dismiss();
            _autumnOverlays?.Dismiss();
            _endOverlays?.Dismiss();
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

        private static string ResolveSpectatorScreen(Season season) => season switch
        {
            Season.Summer => ScreenIds.SummerHub,
            Season.Autumn => ScreenIds.AutumnHub,
            _ => ScreenIds.Waiting
        };

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
            screenId is ScreenIds.WinterUnlock or ScreenIds.FatefulWager or ScreenIds.CraftReagent;

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
            else if (controller is AutumnSceneController autumn)
            {
                autumn.BindState(_session, _loop, _bridge);
                _autumnOverlays?.BindState(_session, _bridge);
                _contestOverlays?.BindState(_session, _bridge);
                _endOverlays?.BindState(_session, _loop, _bridge);
            }
            else if (controller is AutumnHubController autumnHub)
            {
                autumnHub.BindState(_session, _loop, _bridge);
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
