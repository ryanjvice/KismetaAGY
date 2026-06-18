using System.IO;
using Kismeta.Game.Bootstrap;
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
        private const string AppShellPath = "Assets/_Project/UI/UXML/shell/AppShell.uxml";
        private const string TitleScreenPath = "Assets/_Project/UI/UXML/batch1/TitleScreen.uxml";
        private const string SetupSheetPath = "Assets/_Project/UI/UXML/batch1/SetupSheet.uxml";
        private const string JoinScreenPath = "Assets/_Project/UI/UXML/batch1/JoinScreen.uxml";
        private const string ResumeScreenPath = "Assets/_Project/UI/UXML/batch1/ResumeScreen.uxml";
        private const string CodexScreenPath = "Assets/_Project/UI/UXML/batch1/CodexScreen.uxml";
        private const string AgekeeperContestPath = "Assets/_Project/UI/UXML/batch1/AgekeeperContest.uxml";
        private const string CardModalsPath = "Assets/_Project/UI/UXML/batch7/CardModals.uxml";
        private const string UiTestScenePath = "Assets/_Project/Scenes/UITest.unity";
        private const string GameplayHudPath = "Assets/_Project/UI/UXML/shell/GameplayHud.uxml";
        private const string WaitingHudPath = "Assets/_Project/UI/UXML/shell/WaitingHud.uxml";

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
            EnsureComponent<GameplayHudController>(bootstrap.gameObject);
            EnsureComponent<WaitingHudController>(bootstrap.gameObject);
            EnsureComponent<JoinScreenController>(bootstrap.gameObject);
            EnsureComponent<ResumeScreenController>(bootstrap.gameObject);
            EnsureComponent<CodexScreenController>(bootstrap.gameObject);
            EnsureComponent<AgekeeperContestController>(bootstrap.gameObject);

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
            var gameplay = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(GameplayHudPath);
            var waiting = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(WaitingHudPath);
            var setup = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SetupSheetPath);
            var join = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(JoinScreenPath);
            var resume = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ResumeScreenPath);
            var codex = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CodexScreenPath);
            var agekeeperContest = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AgekeeperContestPath);

            var so = new SerializedObject(bootstrap);
            so.FindProperty("_panelSettings").objectReferenceValue = panelSettings;
            so.FindProperty("_appShell").objectReferenceValue = appShell;
            so.FindProperty("_titleScreen").objectReferenceValue = title;
            so.FindProperty("_gameplayHud").objectReferenceValue = gameplay;
            so.FindProperty("_waitingHud").objectReferenceValue = waiting;
            so.FindProperty("_setupSheet").objectReferenceValue = setup;
            so.FindProperty("_joinScreen").objectReferenceValue = join;
            so.FindProperty("_resumeScreen").objectReferenceValue = resume;
            so.FindProperty("_codexScreen").objectReferenceValue = codex;
            so.FindProperty("_agekeeperContest").objectReferenceValue = agekeeperContest;
            so.FindProperty("_useProductionUi").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            var doc = bootstrap.GetComponent<UIDocument>() ?? bootstrap.gameObject.AddComponent<UIDocument>();
            doc.panelSettings = panelSettings;
            doc.visualTreeAsset = appShell;

            var layout = bootstrap.GetComponent<ViewportLayout>();
            if (layout != null)
            {
                var layoutSo = new SerializedObject(layout);
                layoutSo.FindProperty("_panelSettings").objectReferenceValue = panelSettings;
                layoutSo.FindProperty("_initialScreen").objectReferenceValue = null;
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
            host.AddComponent<GameplayHudController>();
            host.AddComponent<WaitingHudController>();
            host.AddComponent<JoinScreenController>();
            host.AddComponent<ResumeScreenController>();
            host.AddComponent<CodexScreenController>();
            host.AddComponent<AgekeeperContestController>();
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
            var gameplayHud = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(GameplayHudPath);
            var waitingHud = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(WaitingHudPath);
            var cardModals = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CardModalsPath);

            var router = host.GetComponent<ScreenRouter>();
            router.ConfigureScreens(
                titleScreen, gameplayHud, waitingHud, setupSheet,
                joinScreen, resumeScreen, codexScreen, agekeeperContest);

            var doc = host.GetComponent<UIDocument>();
            doc.panelSettings = panelSettings;
            doc.visualTreeAsset = appShell;

            var layoutSo = new SerializedObject(host.GetComponent<ViewportLayout>());
            layoutSo.FindProperty("_panelSettings").objectReferenceValue = panelSettings;
            layoutSo.FindProperty("_initialScreen").objectReferenceValue = null;
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
