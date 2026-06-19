using System;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
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
        private readonly CommandBridge _bridge = new();
        private GameSession? _session;
        private GameLoop? _loop;
        private CeremonyGate? _ceremonyGate;

        private bool _inGame;
        private bool _humanPending;
        private ActionHint _lastHint = ActionHint.None;
        private Season _lastSeason = Season.Spring;
        private CeremonyStep? _lastCeremonyStep;

        public CommandBridge Bridge => _bridge;
        public bool IsInGame => _inGame;
        public CeremonyGate? CeremonyGate => _ceremonyGate;

        public event Action<UiSetupConfig>? SetupBeginRequested;

        private void Awake()
        {
            _router = GetComponent<ScreenRouter>();
            _layout = GetComponent<ViewportLayout>();
            _summerOverlays = GetComponent<SummerOverlayHost>();
            _contestOverlays = GetComponent<ContestOverlayHost>();
            _autumnOverlays = GetComponent<AutumnOverlayHost>();
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

        public void Bind(GameSession session, GameLoop loop, CeremonyGate? ceremonyGate = null)
        {
            Unbind();

            _session = session;
            _loop = loop;
            _ceremonyGate = ceremonyGate;
            _bridge.Bind(loop);

            session.OnEvent += OnSessionEvent;
            loop.OnLog += OnLoopLog;

            EnsureOverlayHosts();

            WireTitleScreen();
            WireShellScreens();
            WireAgekeeperContest();
            WireStepScreens();
            WireSummerNavigation();
            WireContestNavigation();
            WireAutumnNavigation();
        }

        public void Unbind()
        {
            if (_session != null)
                _session.OnEvent -= OnSessionEvent;
            if (_loop != null)
                _loop.OnLog -= OnLoopLog;
            _session = null;
            _loop = null;
            _ceremonyGate = null;
            _inGame = false;
            _lastCeremonyStep = null;
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
                RefreshActiveScreen();
                return;
            }

            var humanPending = _loop.PendingHumanController != null;
            var hint = _loop.PendingHint;
            var season = _session.Phase.CurrentSeason;

            if (_session.IsOver)
            {
                RouteIfNeeded(ResolveSeasonMainScreen(_session.Phase.CurrentSeason));
                RefreshActiveScreen();
                return;
            }

            if (humanPending)
            {
                _humanPending = true;
                _lastHint = hint;
                _lastSeason = season;
                RouteGameplay();
                RefreshActiveScreen();
                return;
            }

            if (_humanPending || hint != _lastHint || season != _lastSeason)
            {
                _humanPending = false;
                _lastHint = hint;
                _lastSeason = season;
                RouteGameplay();
            }
        }

        private void OnSessionEvent(IGameEvent _) => RefreshActiveScreenIfNeeded();
        private void OnLoopLog(string _) => RefreshActiveScreenIfNeeded();

        private void RefreshActiveScreenIfNeeded()
        {
            if (_session == null || _loop == null) return;

            if (_layout.IsOverlayVisible)
                return;

            // Step screens bind via ActivePlayerId; skip refresh while the loop clears it post-submit.
            var activeId = _router.CurrentScreenId;
            if (_loop.PendingHumanController == null && _loop.ActivePlayerId < 0
                && activeId is ScreenIds.Commune or ScreenIds.WinterUnlock
                    or ScreenIds.FatefulWager or ScreenIds.CardLimits)
                return;

            RouteGameplay();
            RefreshActiveScreen();
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
            _router.GoTo(ScreenIds.AgekeeperContest);
            _router.GetController<AgekeeperContestController>(ScreenIds.AgekeeperContest)
                ?.BeginContest(_pendingSetupConfig.Players);
        }

        public void NotifySetupBegin(UiSetupConfig config)
        {
            _router.DismissOverlay();
            SetupBeginRequested?.Invoke(config);
            _inGame = true;
            _humanPending = false;
            _lastHint = ActionHint.None;
            _lastCeremonyStep = null;
            RouteGameplay();
        }

        public void ReturnToTitle()
        {
            _inGame = false;
            _setupSheetState.Detach();
            _router.GoTo(ScreenIds.Title);
        }

        private void RouteGameplay()
        {
            if (_session!.IsOver)
            {
                RouteIfNeeded(ResolveSeasonMainScreen(_session.Phase.CurrentSeason));
                RefreshActiveScreen();
                return;
            }

            if (_loop.PendingHumanController == null)
            {
                _summerOverlays?.DismissIfNotHumanTurn();
                _contestOverlays?.DismissIfNotHumanTurn();
                _autumnOverlays?.DismissIfNotHumanTurn();
                RouteIfNeeded(ScreenIds.Waiting);
                RefreshActiveScreen();
                return;
            }

            RouteIfNeeded(ResolveGameplayScreen(_session.Phase.CurrentSeason, _loop.PendingHint));
            RefreshActiveScreen();
        }

        private static string ResolveGameplayScreen(Season season, ActionHint hint) => hint switch
        {
            ActionHint.Commune => ScreenIds.Commune,
            ActionHint.DiscardToLimit => ScreenIds.CardLimits,
            _ => ResolveSeasonMainScreen(season)
        };

        private void WireStepScreens()
        {
            var spring = _router.GetController<SpringHubController>(ScreenIds.SpringHub);
            if (spring != null)
                spring.OnOpenCommune = () => _router.GoTo(ScreenIds.Commune);

            var winter = _router.GetController<WinterHubController>(ScreenIds.WinterHub);
            if (winter != null)
            {
                winter.OnOpenUnlock = () => _router.GoTo(ScreenIds.WinterUnlock);
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
            if (_summerOverlays == null) return;

            var summer = _router.GetController<SummerSceneController>(ScreenIds.SummerMain);
            if (summer == null) return;

            summer.OnCraftBuild = () => _summerOverlays.ShowCraftBuildSheet();
            summer.OnConsort = () => _summerOverlays.ShowConsortSheet();
            summer.OnActivate = () => _summerOverlays.ShowActivate();
            summer.OnPass = () => _summerOverlays.ShowEndSummer();
            summer.OnOpenCardTable = () =>
                Debug.Log("[UI] Card table — Batch 7");
        }

        private void WireContestNavigation()
        {
            if (_summerOverlays == null || _contestOverlays == null) return;

            _summerOverlays.OnTrade = () => { _summerOverlays.Dismiss(); _contestOverlays.ShowTrade(); };
            _summerOverlays.OnDuel = () => { _summerOverlays.Dismiss(); _contestOverlays.ShowDuel(); };
            _summerOverlays.OnGambit = () => { _summerOverlays.Dismiss(); _contestOverlays.ShowGambit(); };

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
        }

        private void EnsureOverlayHosts()
        {
            _summerOverlays = GetComponent<SummerOverlayHost>();
            _contestOverlays = GetComponent<ContestOverlayHost>();
            _autumnOverlays = GetComponent<AutumnOverlayHost>();
        }

        private static string MapCeremonyScreen(CeremonyStep step) => step switch
        {
            CeremonyStep.RoundOpen => ScreenIds.RoundOpen,
            CeremonyStep.AgeOpening => ScreenIds.AgeOpening,
            CeremonyStep.SpringIntro => ScreenIds.SpringIntro,
            CeremonyStep.SummerIntro => ScreenIds.SummerIntro,
            CeremonyStep.AutumnIntro => ScreenIds.AutumnIntro,
            CeremonyStep.WinterIntro => ScreenIds.WinterIntro,
            CeremonyStep.AgeClosing => ScreenIds.AgeClosing,
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
            if (_router.CurrentScreenId != screenId)
                _router.GoTo(screenId);
        }

        private void RefreshActiveScreen()
        {
            if (_session == null || _loop == null) return;

            var controller = _router.ActiveController;
            var gate = _ceremonyGate;
            if (gate != null)
            {
                if (controller is RoundOpenController roundOpen)
                    roundOpen.BindState(_session, gate);
                else if (controller is AgeOpeningController ageOpening)
                    ageOpening.BindState(_session, gate);
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
                spring.BindState(_session, _loop, _bridge);
            else if (controller is SummerSceneController summer)
            {
                summer.BindState(_session, _loop, _bridge);
                _summerOverlays?.BindState(_session, _bridge);
                _contestOverlays?.BindState(_session, _bridge);
            }
            else if (controller is AutumnSceneController autumn)
            {
                autumn.BindState(_session, _loop, _bridge);
                _autumnOverlays?.BindState(_session, _bridge);
                _contestOverlays?.BindState(_session, _bridge);
            }
            else if (controller is WinterHubController winter)
                winter.BindState(_session, _loop, _bridge);
            else if (controller is CommuneController commune)
                commune.BindState(_session, _bridge);
            else if (controller is WinterUnlockController winterUnlock)
                winterUnlock.BindState(_session, _bridge);
            else if (controller is FatefulWagerController fatefulWager)
                fatefulWager.BindState(_session, _bridge);
            else if (controller is CardLimitsController cardLimits)
                cardLimits.BindState(_session, _bridge);
            else if (controller is GameplayHudController hud)
                hud.BindState(_session, _loop, _bridge);
            else if (controller is WaitingHudController waiting)
                waiting.BindState(_session, _loop);
            else if (controller is ResumeScreenController resumeCtrl)
                resumeCtrl.RebuildList();
            else
                controller?.Refresh();
        }
    }
}
