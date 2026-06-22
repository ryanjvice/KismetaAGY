using System.IO;
using Kismeta.Game.Bootstrap;
using Kismeta.UI.Chronicle;
using Kismeta.UI;
using Kismeta.UI.Controllers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kismeta.UI.Editor
{
    public static class KismetaUiMenuItems
    {
        private const string PanelSettingsPath = "Assets/_Project/UI/Settings/KismetaPanelSettings.asset";
        private const string KismetaUssPath = "Assets/_Project/UI/USS/Kismeta.uss";
        private const string AppShellPath = "Assets/_Project/UI/UXML/shell/AppShell.uxml";
        private const string TitleScreenPath = "Assets/_Project/UI/UXML/batch1/TitleScreen.uxml";
        private const string SetupSheetPath = "Assets/_Project/UI/UXML/batch1/SetupSheet.uxml";
        private const string JoinScreenPath = "Assets/_Project/UI/UXML/batch1/JoinScreen.uxml";
        private const string ResumeScreenPath = "Assets/_Project/UI/UXML/batch1/ResumeScreen.uxml";
        private const string CodexScreenPath = "Assets/_Project/UI/UXML/batch1/CodexScreen.uxml";
        private const string AgekeeperContestPath = "Assets/_Project/UI/UXML/batch1/AgekeeperContest.uxml";
        private const string SpringHubPath = "Assets/_Project/UI/UXML/main/SpringHub.uxml";
        private const string SummerMainPath = "Assets/_Project/UI/UXML/main/SummerMainScene.uxml";
        private const string SummerHubPath = "Assets/_Project/UI/UXML/main/SummerHub.uxml";
        private const string AutumnMainPath = "Assets/_Project/UI/UXML/main/AutumnMainScene.uxml";
        private const string AutumnHubPath = "Assets/_Project/UI/UXML/main/AutumnHub.uxml";
        private const string WinterHubPath = "Assets/_Project/UI/UXML/main/WinterHub.uxml";
        private const string RoundOpenPath = "Assets/_Project/UI/UXML/batch2/RoundOpen.uxml";
        private const string SpringIntroPath = "Assets/_Project/UI/UXML/batch2/SpringIntro.uxml";
        private const string SummerIntroPath = "Assets/_Project/UI/UXML/batch2/SummerIntro.uxml";
        private const string AutumnIntroPath = "Assets/_Project/UI/UXML/batch2/AutumnIntro.uxml";
        private const string WinterIntroPath = "Assets/_Project/UI/UXML/batch2/WinterIntro.uxml";
        private const string AgeClosingPath = "Assets/_Project/UI/UXML/batch2/AgeClosing.uxml";
        private const string SpringHarvestPath = "Assets/_Project/UI/UXML/batch3/SpringHarvest.uxml";
        private const string WinterUnlockPath = "Assets/_Project/UI/UXML/batch3/WinterUnlock.uxml";
        private const string FatefulWagerPath = "Assets/_Project/UI/UXML/batch3/FatefulWager.uxml";
        private const string CardLimitsPath = "Assets/_Project/UI/UXML/batch3/CardLimits.uxml";
        private const string SummerSheetsPath = "Assets/_Project/UI/UXML/batch4/SummerSheets.uxml";
        private const string CraftReagentPath = "Assets/_Project/UI/UXML/batch4/CraftReagent.uxml";
        private const string ActivateCardPath = "Assets/_Project/UI/UXML/batch4/ActivateCard.uxml";
        private const string BuildHousePath = "Assets/_Project/UI/UXML/batch4/BuildHouse.uxml";
        private const string PlaceWardsPath = "Assets/_Project/UI/UXML/batch4/PlaceWards.uxml";
        private const string EndSummerPath = "Assets/_Project/UI/UXML/batch4/EndSummer.uxml";
        private const string TradePath = "Assets/_Project/UI/UXML/batch5/Trade.uxml";
        private const string DuelPath = "Assets/_Project/UI/UXML/batch5/Duel.uxml";
        private const string GambitPath = "Assets/_Project/UI/UXML/batch5/Gambit.uxml";
        private const string OppositionPath = "Assets/_Project/UI/UXML/batch5/Opposition.uxml";
        private const string FireStonePath = "Assets/_Project/UI/UXML/batch6/FireStone.uxml";
        private const string TemperStonePath = "Assets/_Project/UI/UXML/batch6/TemperStone.uxml";
        private const string ManageCardsPath = "Assets/_Project/UI/UXML/batch6/ManageCards.uxml";
        private const string LeaveStasisPath = "Assets/_Project/UI/UXML/batch6/LeaveStasis.uxml";
        private const string EndAutumnPath = "Assets/_Project/UI/UXML/batch6/EndAutumn.uxml";
        private const string VictoryPath = "Assets/_Project/UI/UXML/batch7/Victory.uxml";
        private const string ChroniclePath = "Assets/_Project/UI/UXML/batch7/Chronicle.uxml";
        private const string CardTablePath = "Assets/_Project/UI/UXML/batch7/CardTable.uxml";
        private const string CardModalsPath = "Assets/_Project/UI/UXML/batch7/CardModals.uxml";
        private const string UiTestScenePath = "Assets/_Project/Scenes/UITest.unity";
        private const string WaitingHudPath = "Assets/_Project/UI/UXML/shell/WaitingHud.uxml";
        private const string PlayerHudPath = "Assets/_Project/UI/UXML/shell/PlayerHud.uxml";

        [MenuItem("Kismeta/UI/Verify Font Imports")]
        public static void VerifyFontImports()
        {
            string[] fontPaths =
            {
                "Assets/_Project/UI/Fonts/Amarante-Regular.ttf",
                "Assets/_Project/UI/Fonts/GermaniaOne-Regular.ttf",
                "Assets/_Project/UI/Fonts/FuturaCyrillicDemi.ttf",
                "Assets/_Project/UI/Fonts/FuturaCyrillicBook.ttf",
                "Assets/_Project/UI/Fonts/NotoColorEmoji-Regular.ttf",
            };

            var missing = 0;
            foreach (var path in fontPaths)
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>(path);
                if (font == null)
                {
                    Debug.LogWarning($"[Kismeta.UI] Font not imported: {path}");
                    missing++;
                }
            }

            if (missing == 0)
                Debug.Log("[Kismeta.UI] All five UI fonts imported and loadable.");
            else
                Debug.LogWarning($"[Kismeta.UI] {missing} font(s) missing — focus Unity so AssetDatabase can import TTFs.");
        }

        [MenuItem("Kismeta/UI/Create Panel Settings")]
        public static void CreatePanelSettings()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (existing != null)
            {
                ApplyResponsivePanelSettings(existing);
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Kismeta.UI] Updated PanelSettings at {PanelSettingsPath}");
                Selection.activeObject = existing;
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(PanelSettingsPath)!);
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            ApplyResponsivePanelSettings(settings);
            AssetDatabase.CreateAsset(settings, PanelSettingsPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = settings;
            Debug.Log($"[Kismeta.UI] Created PanelSettings at {PanelSettingsPath}");
        }

        [MenuItem("Kismeta/UI/Configure Panel Settings (Responsive)")]
        public static void ConfigurePanelSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (settings == null)
            {
                CreatePanelSettings();
                return;
            }

            ApplyResponsivePanelSettings(settings);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Selection.activeObject = settings;
            Debug.Log("[Kismeta.UI] PanelSettings configured for responsive full-screen layout (380×844 reference).");
        }

        [MenuItem("Kismeta/UI/Setup Bootstrap Scene")]
        public static void SetupBootstrapScene()
        {
            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            if (bootstrap == null)
            {
                Debug.LogWarning("[Kismeta.UI] No GameBootstrap in the open scene.");
                return;
            }

            WireBootstrapUiReferences();

            EnsureComponent<UIDocument>(bootstrap.gameObject);
            EnsureComponent<ViewportLayout>(bootstrap.gameObject);
            EnsureComponent<ScreenRouter>(bootstrap.gameObject);
            EnsureComponent<GamePresenter>(bootstrap.gameObject);
            EnsureComponent<TitleScreenController>(bootstrap.gameObject);
            EnsureComponent<WaitingHudController>(bootstrap.gameObject);
            EnsureComponent<PlayerHudController>(bootstrap.gameObject);
            EnsureComponent<JoinScreenController>(bootstrap.gameObject);
            EnsureComponent<ResumeScreenController>(bootstrap.gameObject);
            EnsureComponent<CodexScreenController>(bootstrap.gameObject);
            EnsureComponent<AgekeeperContestController>(bootstrap.gameObject);
            EnsureComponent<SpringHubController>(bootstrap.gameObject);
            EnsureComponent<SummerSceneController>(bootstrap.gameObject);
            EnsureComponent<SummerHubController>(bootstrap.gameObject);
            EnsureComponent<AutumnSceneController>(bootstrap.gameObject);
            EnsureComponent<AutumnHubController>(bootstrap.gameObject);
            EnsureComponent<WinterHubController>(bootstrap.gameObject);
            EnsureComponent<RoundOpenController>(bootstrap.gameObject);
            EnsureComponent<AgeClosingController>(bootstrap.gameObject);
            EnsureComponent<SpringIntroController>(bootstrap.gameObject);
            EnsureComponent<SummerIntroController>(bootstrap.gameObject);
            EnsureComponent<AutumnIntroController>(bootstrap.gameObject);
            EnsureComponent<WinterIntroController>(bootstrap.gameObject);
            EnsureComponent<SpringHarvestController>(bootstrap.gameObject);
            EnsureComponent<WinterUnlockController>(bootstrap.gameObject);
            EnsureComponent<FatefulWagerController>(bootstrap.gameObject);
            EnsureComponent<CardLimitsController>(bootstrap.gameObject);
            EnsureComponent<SummerOverlayHost>(bootstrap.gameObject);
            EnsureComponent<SummerSheetsController>(bootstrap.gameObject);
            EnsureComponent<CraftReagentController>(bootstrap.gameObject);
            EnsureComponent<ActivateCardController>(bootstrap.gameObject);
            EnsureComponent<BuildHouseController>(bootstrap.gameObject);
            EnsureComponent<PlaceWardsController>(bootstrap.gameObject);
            EnsureComponent<EndSummerController>(bootstrap.gameObject);
            EnsureComponent<ContestOverlayHost>(bootstrap.gameObject);
            EnsureComponent<TradeController>(bootstrap.gameObject);
            EnsureComponent<DuelController>(bootstrap.gameObject);
            EnsureComponent<GambitController>(bootstrap.gameObject);
            EnsureComponent<OppositionController>(bootstrap.gameObject);
            EnsureComponent<FireStoneController>(bootstrap.gameObject);
            EnsureComponent<TemperStoneController>(bootstrap.gameObject);
            EnsureComponent<ManageCardsController>(bootstrap.gameObject);
            EnsureComponent<LeaveStasisController>(bootstrap.gameObject);
            EnsureComponent<EndAutumnController>(bootstrap.gameObject);
            EnsureComponent<AutumnOverlayHost>(bootstrap.gameObject);
            EnsureComponent<VictoryController>(bootstrap.gameObject);
            EnsureComponent<ChronicleController>(bootstrap.gameObject);
            EnsureComponent<CardTableController>(bootstrap.gameObject);
            EnsureComponent<CardModalsController>(bootstrap.gameObject);
            EnsureComponent<EndOverlayHost>(bootstrap.gameObject);
            EnsureComponent<GameChronicle>(bootstrap.gameObject);

            var layout = bootstrap.GetComponent<ViewportLayout>();
            var layoutSo = new SerializedObject(layout);
            layoutSo.FindProperty("_panelSettings").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            layoutSo.FindProperty("_initialScreen").objectReferenceValue = null;
            layoutSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);
            Debug.Log("[Kismeta.UI] Bootstrap scene UI stack configured. Save the scene (Ctrl+S).");
        }

        [MenuItem("Kismeta/UI/Wire Bootstrap UI References")]
        public static void WireBootstrapUiReferences()
        {
            var bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            if (bootstrap == null)
            {
                Debug.LogWarning("[Kismeta.UI] No GameBootstrap in the open scene.");
                return;
            }

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            var appShell = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AppShellPath);
            var title = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TitleScreenPath);
            var waiting = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(WaitingHudPath);
            var playerHud = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PlayerHudPath);
            var setup = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SetupSheetPath);
            var join = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(JoinScreenPath);
            var resume = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ResumeScreenPath);
            var codex = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CodexScreenPath);
            var agekeeperContest = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AgekeeperContestPath);
            var springHub = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SpringHubPath);
            var summerMain = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SummerMainPath);
            var summerHub = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SummerHubPath);
            var autumnMain = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AutumnMainPath);
            var autumnHub = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AutumnHubPath);
            var winterHub = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(WinterHubPath);
            var roundOpen = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(RoundOpenPath);
            var springIntro = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SpringIntroPath);
            var summerIntro = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SummerIntroPath);
            var autumnIntro = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AutumnIntroPath);
            var winterIntro = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(WinterIntroPath);
            var ageClosing = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AgeClosingPath);
            var springHarvest = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SpringHarvestPath);
            var winterUnlock = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(WinterUnlockPath);
            var fatefulWager = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(FatefulWagerPath);
            var cardLimits = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CardLimitsPath);
            var summerSheets = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SummerSheetsPath);
            var craftReagent = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CraftReagentPath);
            var activateCard = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ActivateCardPath);
            var buildHouse = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(BuildHousePath);
            var placeWards = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PlaceWardsPath);
            var endSummer = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(EndSummerPath);
            var trade = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TradePath);
            var duel = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(DuelPath);
            var gambit = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(GambitPath);
            var opposition = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(OppositionPath);
            var fireStone = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(FireStonePath);
            var temperStone = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TemperStonePath);
            var manageCards = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ManageCardsPath);
            var leaveStasis = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LeaveStasisPath);
            var endAutumn = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(EndAutumnPath);
            var victory = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(VictoryPath);
            var chronicle = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ChroniclePath);
            var cardTable = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CardTablePath);
            var cardModals = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CardModalsPath);

            var so = new SerializedObject(bootstrap);
            so.FindProperty("_panelSettings").objectReferenceValue = panelSettings;
            so.FindProperty("_appShell").objectReferenceValue = appShell;
            so.FindProperty("_titleScreen").objectReferenceValue = title;
            so.FindProperty("_waitingHud").objectReferenceValue = waiting;
            so.FindProperty("_playerHud").objectReferenceValue = playerHud;
            so.FindProperty("_setupSheet").objectReferenceValue = setup;
            so.FindProperty("_joinScreen").objectReferenceValue = join;
            so.FindProperty("_resumeScreen").objectReferenceValue = resume;
            so.FindProperty("_codexScreen").objectReferenceValue = codex;
            so.FindProperty("_agekeeperContest").objectReferenceValue = agekeeperContest;
            so.FindProperty("_springHub").objectReferenceValue = springHub;
            so.FindProperty("_summerMain").objectReferenceValue = summerMain;
            so.FindProperty("_summerHub").objectReferenceValue = summerHub;
            so.FindProperty("_autumnMain").objectReferenceValue = autumnMain;
            so.FindProperty("_autumnHub").objectReferenceValue = autumnHub;
            so.FindProperty("_winterHub").objectReferenceValue = winterHub;
            so.FindProperty("_roundOpen").objectReferenceValue = roundOpen;
            so.FindProperty("_springIntro").objectReferenceValue = springIntro;
            so.FindProperty("_summerIntro").objectReferenceValue = summerIntro;
            so.FindProperty("_autumnIntro").objectReferenceValue = autumnIntro;
            so.FindProperty("_winterIntro").objectReferenceValue = winterIntro;
            so.FindProperty("_ageClosing").objectReferenceValue = ageClosing;
            so.FindProperty("_springHarvest").objectReferenceValue = springHarvest;
            so.FindProperty("_winterUnlock").objectReferenceValue = winterUnlock;
            so.FindProperty("_fatefulWager").objectReferenceValue = fatefulWager;
            so.FindProperty("_cardLimits").objectReferenceValue = cardLimits;
            so.FindProperty("_summerSheets").objectReferenceValue = summerSheets;
            so.FindProperty("_craftReagent").objectReferenceValue = craftReagent;
            so.FindProperty("_activateCard").objectReferenceValue = activateCard;
            so.FindProperty("_buildHouse").objectReferenceValue = buildHouse;
            so.FindProperty("_placeWards").objectReferenceValue = placeWards;
            so.FindProperty("_endSummer").objectReferenceValue = endSummer;
            so.FindProperty("_trade").objectReferenceValue = trade;
            so.FindProperty("_duel").objectReferenceValue = duel;
            so.FindProperty("_gambit").objectReferenceValue = gambit;
            so.FindProperty("_opposition").objectReferenceValue = opposition;
            so.FindProperty("_fireStone").objectReferenceValue = fireStone;
            so.FindProperty("_temperStone").objectReferenceValue = temperStone;
            so.FindProperty("_manageCards").objectReferenceValue = manageCards;
            so.FindProperty("_leaveStasis").objectReferenceValue = leaveStasis;
            so.FindProperty("_endAutumn").objectReferenceValue = endAutumn;
            so.FindProperty("_victory").objectReferenceValue = victory;
            so.FindProperty("_chronicle").objectReferenceValue = chronicle;
            so.FindProperty("_cardTable").objectReferenceValue = cardTable;
            so.FindProperty("_cardModals").objectReferenceValue = cardModals;
            so.FindProperty("_useProductionUi").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            var doc = bootstrap.GetComponent<UIDocument>() ?? bootstrap.gameObject.AddComponent<UIDocument>();
            doc.panelSettings = panelSettings;
            doc.visualTreeAsset = appShell;

            var layout = bootstrap.GetComponent<ViewportLayout>();
            if (layout != null)
            {
                var tokenStylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(KismetaUssPath);
                var layoutSo = new SerializedObject(layout);
                layoutSo.FindProperty("_panelSettings").objectReferenceValue = panelSettings;
                layoutSo.FindProperty("_initialScreen").objectReferenceValue = null;
                layoutSo.FindProperty("_tokenStylesheet").objectReferenceValue = tokenStylesheet;
                layoutSo.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(bootstrap);
            EditorUtility.SetDirty(doc);
            Debug.Log("[Kismeta.UI] Wired GameBootstrap UI references (AppShell + screens).");
        }

        [MenuItem("Kismeta/UI/Create UI Test Scene")]
        public static void CreateUiTestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var light = GameObject.Find("Directional Light");
            if (light != null) Object.DestroyImmediate(light);

            var host = new GameObject("UiShell");
            host.AddComponent<UIDocument>();
            host.AddComponent<ViewportLayout>();
            host.AddComponent<ScreenRouter>();
            host.AddComponent<GamePresenter>();
            host.AddComponent<TitleScreenController>();
            host.AddComponent<WaitingHudController>();
            host.AddComponent<JoinScreenController>();
            host.AddComponent<ResumeScreenController>();
            host.AddComponent<CodexScreenController>();
            host.AddComponent<AgekeeperContestController>();
            host.AddComponent<SpringHubController>();
            host.AddComponent<SummerSceneController>();
            host.AddComponent<SummerHubController>();
            host.AddComponent<AutumnSceneController>();
            host.AddComponent<AutumnHubController>();
            host.AddComponent<WinterHubController>();
            host.AddComponent<RoundOpenController>();
            host.AddComponent<AgeClosingController>();
            host.AddComponent<SpringIntroController>();
            host.AddComponent<SummerIntroController>();
            host.AddComponent<AutumnIntroController>();
            host.AddComponent<WinterIntroController>();
            host.AddComponent<SpringHarvestController>();
            host.AddComponent<WinterUnlockController>();
            host.AddComponent<FatefulWagerController>();
            host.AddComponent<CardLimitsController>();
            host.AddComponent<SummerOverlayHost>();
            host.AddComponent<SummerSheetsController>();
            host.AddComponent<CraftReagentController>();
            host.AddComponent<ActivateCardController>();
            host.AddComponent<BuildHouseController>();
            host.AddComponent<PlaceWardsController>();
            host.AddComponent<EndSummerController>();
            host.AddComponent<ContestOverlayHost>();
            host.AddComponent<TradeController>();
            host.AddComponent<DuelController>();
            host.AddComponent<GambitController>();
            host.AddComponent<OppositionController>();
            host.AddComponent<FireStoneController>();
            host.AddComponent<TemperStoneController>();
            host.AddComponent<ManageCardsController>();
            host.AddComponent<LeaveStasisController>();
            host.AddComponent<EndAutumnController>();
            host.AddComponent<AutumnOverlayHost>();
            host.AddComponent<UiResponsiveTest>();

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panelSettings == null)
                CreatePanelSettings();
            panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            var appShell = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AppShellPath);

            var titleScreen = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TitleScreenPath);
            var setupSheet = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SetupSheetPath);
            var joinScreen = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(JoinScreenPath);
            var resumeScreen = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ResumeScreenPath);
            var codexScreen = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CodexScreenPath);
            var agekeeperContest = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AgekeeperContestPath);
            var springHub = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SpringHubPath);
            var summerMain = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SummerMainPath);
            var summerHub = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SummerHubPath);
            var autumnMain = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AutumnMainPath);
            var autumnHub = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AutumnHubPath);
            var winterHub = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(WinterHubPath);
            var roundOpen = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(RoundOpenPath);
            var springIntro = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SpringIntroPath);
            var summerIntro = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SummerIntroPath);
            var autumnIntro = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AutumnIntroPath);
            var winterIntro = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(WinterIntroPath);
            var ageClosing = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AgeClosingPath);
            var springHarvest = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SpringHarvestPath);
            var winterUnlock = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(WinterUnlockPath);
            var fatefulWager = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(FatefulWagerPath);
            var craftReagent = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CraftReagentPath);
            var cardLimits = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CardLimitsPath);
            var waitingHud = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(WaitingHudPath);
            var cardModals = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CardModalsPath);

            var router = host.GetComponent<ScreenRouter>();
            router.ConfigureScreens(
                titleScreen, null, waitingHud, setupSheet,
                joinScreen, resumeScreen, codexScreen, agekeeperContest,
                springHub, summerMain, summerHub, autumnMain, autumnHub, winterHub,
                roundOpen, springIntro, summerIntro,
                autumnIntro, winterIntro, ageClosing,
                springHarvest, winterUnlock, fatefulWager, craftReagent, cardLimits);

            var doc = host.GetComponent<UIDocument>();
            doc.panelSettings = panelSettings;
            doc.visualTreeAsset = appShell;

            var layoutSo = new SerializedObject(host.GetComponent<ViewportLayout>());
            layoutSo.FindProperty("_panelSettings").objectReferenceValue = panelSettings;
            layoutSo.FindProperty("_initialScreen").objectReferenceValue = null;
            layoutSo.FindProperty("_tokenStylesheet").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<StyleSheet>(KismetaUssPath);
            layoutSo.ApplyModifiedPropertiesWithoutUndo();

            host.GetComponent<ViewportLayout>()!.RunWhenReady(() =>
            {
                host.GetComponent<ScreenRouter>()!.GoTo(ScreenIds.Title);
                host.GetComponent<GamePresenter>()!.InitializeForTitle();
            });

            var demoSo = new SerializedObject(host.GetComponent<UiResponsiveTest>());
            demoSo.FindProperty("_setupSheet").objectReferenceValue = setupSheet;
            demoSo.FindProperty("_cardModals").objectReferenceValue = cardModals;
            demoSo.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(UiTestScenePath)!);
            EditorSceneManager.SaveScene(scene, UiTestScenePath);
            Debug.Log($"[Kismeta.UI] UI test scene at {UiTestScenePath}. Play: full-bleed Title; New game = sheet; Codex = inspect modal.");
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        private static void ApplyResponsivePanelSettings(PanelSettings settings)
        {
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(380, 844);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
            settings.clearColor = true;
            settings.colorClearValue = new Color(22f / 255f, 10f / 255f, 28f / 255f, 1f);
        }
    }
}
