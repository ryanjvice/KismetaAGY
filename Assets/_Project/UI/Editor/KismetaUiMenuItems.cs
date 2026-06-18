using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Kismeta.UI;

namespace Kismeta.UI.Editor
{
    public static class KismetaUiMenuItems
    {
        private const string PanelSettingsPath = "Assets/_Project/UI/Settings/KismetaPanelSettings.asset";
        private const string TitleScreenPath = "Assets/_Project/UI/UXML/batch1/TitleScreen.uxml";
        private const string UiTestScenePath = "Assets/_Project/Scenes/UITest.unity";

        [MenuItem("Kismeta/UI/Create Panel Settings")]
        public static void CreatePanelSettings()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (existing != null)
            {
                Debug.Log($"[Kismeta.UI] PanelSettings already exists at {PanelSettingsPath}");
                Selection.activeObject = existing;
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(PanelSettingsPath)!);
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(380, 844);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = 0f;
            AssetDatabase.CreateAsset(settings, PanelSettingsPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = settings;
            Debug.Log($"[Kismeta.UI] Created PanelSettings at {PanelSettingsPath}");
        }

        [MenuItem("Kismeta/UI/Create UI Test Scene")]
        public static void CreateUiTestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var light = GameObject.Find("Directional Light");
            if (light != null) Object.DestroyImmediate(light);

            var host = new GameObject("UiShell");
            var doc = host.AddComponent<UIDocument>();
            host.AddComponent<UiShellHost>();

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panelSettings == null)
                CreatePanelSettings();
            panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);

            var titleScreen = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(TitleScreenPath);
            doc.panelSettings = panelSettings;
            doc.visualTreeAsset = titleScreen;

            var shell = host.GetComponent<UiShellHost>();
            var so = new SerializedObject(shell);
            so.FindProperty("_panelSettings").objectReferenceValue = panelSettings;
            so.FindProperty("_initialScreen").objectReferenceValue = titleScreen;
            so.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(UiTestScenePath)!);
            EditorSceneManager.SaveScene(scene, UiTestScenePath);
            Debug.Log($"[Kismeta.UI] UI test scene created at {UiTestScenePath}. Press Play to preview TitleScreen.");
        }
    }
}
