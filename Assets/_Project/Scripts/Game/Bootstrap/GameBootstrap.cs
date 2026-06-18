using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kismeta.Core.Commands;
using Kismeta.Core.Domain;
using Kismeta.Core.Entities;
using Kismeta.Core.Players;
using Kismeta.Core.Rules;
using Kismeta.Data.Loaders;
using Kismeta.UI;
using Kismeta.UI.Controllers;
using Kismeta.UI.Setup;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.Game.Bootstrap
{
    /// <summary>
    /// Entry-point MonoBehaviour. Wires the game stack and starts <see cref="GameLoop"/>.
    /// With production UI enabled, defers session creation until the setup sheet Begin action.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Players (IMGUI / fallback)")]
        [Tooltip("Number of human (hot-seat) players. Remaining slots fill with AI.")]
        [Range(0, 4)]
        [SerializeField] private int _humanPlayers = 1;

        [Tooltip("Total number of players (2–4). Used when production UI is off.")]
        [Range(2, 4)]
        [SerializeField] private int _totalPlayers = 2;

        [SerializeField] private GameMode _gameMode = GameMode.Quickplay;

        [Header("Production UI")]
        [SerializeField] private bool _useProductionUi = true;
        [SerializeField] private bool _debugUiFallback;
        [SerializeField] private PanelSettings _panelSettings;
        [SerializeField] private VisualTreeAsset _appShell;
        [SerializeField] private VisualTreeAsset _titleScreen;
        [SerializeField] private VisualTreeAsset _gameplayHud;
        [SerializeField] private VisualTreeAsset _waitingHud;
        [SerializeField] private VisualTreeAsset _setupSheet;
        [SerializeField] private VisualTreeAsset _joinScreen;
        [SerializeField] private VisualTreeAsset _resumeScreen;
        [SerializeField] private VisualTreeAsset _codexScreen;
        [SerializeField] private VisualTreeAsset _agekeeperContest;
        [SerializeField] private VisualTreeAsset _springHub;
        [SerializeField] private VisualTreeAsset _summerMain;
        [SerializeField] private VisualTreeAsset _autumnMain;
        [SerializeField] private VisualTreeAsset _winterHub;
        [SerializeField] private VisualTreeAsset _roundOpen;
        [SerializeField] private VisualTreeAsset _ageOpening;
        [SerializeField] private VisualTreeAsset _springIntro;
        [SerializeField] private VisualTreeAsset _summerIntro;
        [SerializeField] private VisualTreeAsset _autumnIntro;
        [SerializeField] private VisualTreeAsset _winterIntro;
        [SerializeField] private VisualTreeAsset _ageClosing;

        private CardDatabase? _db;
        private CrucibleCodexDatabase? _codexDb;
        private GameSession? _session;
        private GameLoop? _loop;
        private CeremonyGate? _ceremonyGate;
        private List<IPlayerController>? _controllers;
        private GameDebugUI? _ui;
        private GamePresenter? _presenter;
        private CancellationTokenSource _cts = new();
        private bool _loopStarted;

        private void Awake()
        {
            if (_useProductionUi)
                PrepareUiDocument();
        }

        private void Start()
        {
            try
            {
                _db = CardDatabase.Load();
                _codexDb = CrucibleCodexDatabase.Load();

                if (_useProductionUi)
                {
                    EnsureProductionUi();
                    _presenter!.SetupBeginRequested += OnSetupBegin;
                }
                else
                {
                    _ui = gameObject.AddComponent<GameDebugUI>();
                    BuildSession(_totalPlayers, _humanPlayers, _gameMode,
                        CrucibleBuildMode.Curated, 0);
                    _ = StartLoopAsync(_cts.Token);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void OnDestroy()
        {
            if (_presenter != null)
                _presenter.SetupBeginRequested -= OnSetupBegin;
            _cts.Cancel();
            _cts.Dispose();
        }

        private void PrepareUiDocument()
        {
            var doc = GetComponent<UIDocument>() ?? gameObject.AddComponent<UIDocument>();
            if (_panelSettings != null)
                doc.panelSettings = _panelSettings;

            // AppShell.uxml is the stable root; ScreenRouter swaps content-layer only.
            doc.visualTreeAsset = _appShell;
        }

        private void EnsureProductionUi()
        {
            PrepareUiDocument();

            var layout = GetComponent<ViewportLayout>() ?? gameObject.AddComponent<ViewportLayout>();
            var router = GetComponent<ScreenRouter>() ?? gameObject.AddComponent<ScreenRouter>();
            _presenter = GetComponent<GamePresenter>() ?? gameObject.AddComponent<GamePresenter>();

            EnsureController<TitleScreenController>();
            EnsureController<GameplayHudController>();
            EnsureController<WaitingHudController>();
            EnsureController<JoinScreenController>();
            EnsureController<ResumeScreenController>();
            EnsureController<CodexScreenController>();
            EnsureController<AgekeeperContestController>();
            EnsureController<SpringHubController>();
            EnsureController<SummerSceneController>();
            EnsureController<AutumnSceneController>();
            EnsureController<WinterHubController>();
            EnsureController<RoundOpenController>();
            EnsureController<AgeOpeningController>();
            EnsureController<AgeClosingController>();
            EnsureController<SpringIntroController>();
            EnsureController<SummerIntroController>();
            EnsureController<AutumnIntroController>();
            EnsureController<WinterIntroController>();

            router.RefreshControllers();

            if (_titleScreen != null && _gameplayHud != null && _waitingHud != null)
            {
                if (_appShell == null)
                {
                    Debug.LogError("[GameBootstrap] App Shell UXML not assigned. Run Kismeta → UI → Wire Bootstrap UI References.");
                    return;
                }

                router.ConfigureScreens(
                    _titleScreen, _gameplayHud, _waitingHud, _setupSheet,
                    _joinScreen, _resumeScreen, _codexScreen, _agekeeperContest,
                    _springHub, _summerMain, _autumnMain, _winterHub,
                    _roundOpen, _ageOpening, _springIntro, _summerIntro,
                    _autumnIntro, _winterIntro, _ageClosing);
                layout.RunWhenReady(ShowTitleScreen);
            }
            else
            {
                Debug.LogWarning("[GameBootstrap] Assign UI VisualTreeAssets or run Kismeta → UI → Wire Bootstrap UI.");
            }
        }

        private void EnsureController<T>() where T : Component
        {
            if (GetComponent<T>() == null)
                gameObject.AddComponent<T>();
        }

        private void ShowTitleScreen()
        {
            var router = GetComponent<ScreenRouter>();
            router!.GoTo(ScreenIds.Title);
            _presenter!.InitializeForTitle();
        }

        private void OnSetupBegin(UiSetupConfig config)
        {
            if (_loopStarted) return;

            var (count, mode, crucibleBuild) = UiSetupConfigMapper.ToSessionConfig(config);
            int humans = Mathf.Clamp(_humanPlayers, 0, count);
            BuildSession(count, humans, mode, crucibleBuild, config.FirstAgekeeperPlayerId);

            _presenter!.Bind(_session!, _loop!, _ceremonyGate);

            if (_debugUiFallback)
            {
                _ui = gameObject.AddComponent<GameDebugUI>();
                _ui.Bind(_session!, _loop!, _db!, _codexDb);
            }

            _loopStarted = true;
            _ = StartLoopAsync(_cts.Token);
        }

        private void BuildSession(int totalPlayers, int humanPlayers, GameMode mode,
            CrucibleBuildMode crucibleBuild, int firstAgekeeperPlayerId)
        {
            int count = Mathf.Clamp(totalPlayers, 2, 4);
            int humans = Mathf.Clamp(humanPlayers, 0, count);

            var colors = new[] { PlayerColor.Red, PlayerColor.Green, PlayerColor.Blue, PlayerColor.White };

            var players = new List<PlayerState>(count);
            _controllers = new List<IPlayerController>(count);

            for (int i = 0; i < count; i++)
            {
                var ps = new PlayerState(i, colors[i]);
                players.Add(ps);

                var slot = new PlayerSlot(i, colors[i],
                    i < humans ? PlayerControllerType.LocalHuman : PlayerControllerType.LocalAI);

                IPlayerController ctrl = i < humans
                    ? new HotSeatController(slot)
                    : new SimpleAIController(slot);

                _controllers.Add(ctrl);
            }

            var cosmicSvc = new CosmicEffectService();
            var fateResolver = new FateCardResolver(_db!);
            var alchemicalValidator = new AlchemicalAlignmentValidator();
            var alignmentService = new AlignmentService(_db!);
            var rules = new GameRuleSet(
                cardDatabase: _db!,
                codexDatabase: _codexDb!,
                setup: new GameSetupService(_db!),
                harvest: new SpringRules(_db!, cosmicEffect: cosmicSvc, fateResolver: fateResolver),
                crucible: new CrucibleRules(_db!, _codexDb!, alchemicalValidator, alignmentService),
                crafting: new CraftingRules(_db!),
                winter: new WinterRules(_db!),
                validator: new ActionValidator(),
                astralHouse: new AstralHouseService(_db!),
                cosmicEffect: cosmicSvc,
                fateResolver: fateResolver,
                alchemicalValidator: alchemicalValidator,
                alignment: alignmentService,
                combat: new CombatRules(),
                trade: new TradeService(_db!));

            _session = new GameSession(
                sessionId: Guid.NewGuid().ToString(),
                mode: mode,
                players: players,
                rules: rules,
                crucibleBuild: crucibleBuild)
            {
                FirstAgekeeperPlayerId = firstAgekeeperPlayerId
            };

            _session.OnEvent += evt =>
            {
                string detail = evt switch
                {
                    FatefulWagerPlacedEvent e => $"[Wager] P{e.PlayerId} placed {e.CardCount} card(s) on {e.PredictedSign}",
                    FatefulWagerResolvedEvent e => $"[Wager] P{e.PlayerId} {(e.Won ? "WON" : "LOST")} vs CosmicAge={e.Sign} ({e.CardCount} cards)",
                    _ => $"[Event] {evt.GetType().Name}"
                };
                Debug.Log(detail);
            };

            _ceremonyGate = new CeremonyGate();
            _loop = new GameLoop(_session, _controllers);
            _loop.BindCeremonyGate(_ceremonyGate);
            _loop.OnLog += msg => Debug.Log($"[GameLoop] {msg}");

            if (_ui != null)
                _ui.Bind(_session, _loop, _db!, _codexDb);
        }

        private async Task StartLoopAsync(CancellationToken ct)
        {
            if (_loop == null) return;
            try
            {
                await _loop.RunAsync(ct);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
