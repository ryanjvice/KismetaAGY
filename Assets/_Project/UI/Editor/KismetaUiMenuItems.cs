using System.IO;
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
        private const string TitleScreenPath = "Assets/_Project/UI/UXML/batch1/TitleScreen.uxml";
        private const string SetupSheetPath = "Assets/_Project/UI/UXML/batch1/SetupSheet.uxml";
        private const string CardModalsPath = "Assets/_Project/UI/UXML/batch7/CardModals.uxml";
        private const string UiTestScenePath = "Assets/_Project/Scenes/UITest.unity";

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

        private const string GameplayHudPath = "Assets/_Project/UI/UXML/shell/GameplayHud.uxml";
        private const string WaitingHudPath = "Assets/_Project/UI/UXML/shell/WaitingHud.uxml";

        [MenuItem("Kismeta/UI/Wire Bootstrap UI References")]
        public static void WireBootstrapUiReferences()
        {
            var bootstrap = Object.FindFirstObjectByType<Kismeta.Game.Bootstrap.GameBootstrap>();
            if (bootstrap == null)
            {
                Debug.LogWarning("[Kismeta.UI] No GameBootstrap in the open scene.");
                return;
            }

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            var title = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TitleScreenPath);
            var gameplay = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(GameplayHudPath);
            var waiting = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(WaitingHudPath);
            var setup = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SetupSheetPath);

            var so = new SerializedObject(bootstrap);
            so.FindProperty("_panelSettings").objectReferenceValue = panelSettings;
            so.FindProperty("_titleScreen").objectReferenceValue = title;
            so.FindProperty("_gameplayHud").objectReferenceValue = gameplay;
            so.FindProperty("_waitingHud").objectReferenceValue = waiting;
            so.FindProperty("_setupSheet").objectReferenceValue = setup;
            so.FindProperty("_useProductionUi").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            var doc = bootstrap.GetComponent<UIDocument>();
            if (doc != null)
                doc.visualTreeAsset = null;

            EditorUtility.SetDirty(bootstrap);
            if (doc != null)
                EditorUtility.SetDirty(doc);
            Debug.Log("[Kismeta.UI] Wired GameBootstrap UI references.");
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
            host.AddComponent<UiResponsiveTest>();

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panelSettings == null)
                CreatePanelSettings();
            panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);

            var titleScreen = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TitleScreenPath);
            var setupSheet = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SetupSheetPath);
            var gameplayHud = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(GameplayHudPath);
            var waitingHud = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(WaitingHudPath);
            var cardModals = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(CardModalsPath);

            var router = host.GetComponent<ScreenRouter>();
            router.ConfigureScreens(titleScreen, gameplayHud, waitingHud, setupSheet);

            var doc = host.GetComponent<UIDocument>();
            doc.panelSettings = panelSettings;
            doc.visualTreeAsset = null;

            var layoutSo = new SerializedObject(host.GetComponent<ViewportLayout>());
            layoutSo.FindProperty("_panelSettings").objectReferenceValue = panelSettings;
            layoutSo.FindProperty("_initialScreen").objectReferenceValue = null;
            layoutSo.ApplyModifiedPropertiesWithoutUndo();

            host.GetComponent<ViewportLayout>()!.RunWhenReady(() =>
            {
                host.GetComponent<ScreenRouter>()!.GoTo(ScreenIds.Title);
            });

            var demoSo = new SerializedObject(host.GetComponent<UiResponsiveTest>());
            demoSo.FindProperty("_setupSheet").objectReferenceValue = setupSheet;
            demoSo.FindProperty("_cardModals").objectReferenceValue = cardModals;
            demoSo.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(UiTestScenePath)!);
            EditorSceneManager.SaveScene(scene, UiTestScenePath);
            Debug.Log($"[Kismeta.UI] UI test scene at {UiTestScenePath}. Play: full-bleed Title; New game = sheet; Codex = inspect modal.");
        }

        private static void ApplyResponsivePanelSettings(PanelSettings settings)
        {
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(380, 844);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0.5f;
        }
    }
}
