using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Kismeta.Game.Bootstrap;

namespace Kismeta.Game.Editor
{
    /// <summary>
    /// Editor utilities for the Kismeta project accessible via the Kismeta menu in the Unity toolbar.
    /// </summary>
    public static class KismetaMenuItems
    {
        private const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";
        private const string StudioSplashScenePath = "Assets/_Project/Scenes/StudioSplashScreen.unity";
        private const string AndroidPackageName = "com.goodmagik.kismetaagy";
        private const string AndroidApkPath = "Builds/Android/KismetaAGY.apk";

        [MenuItem("Kismeta/Create Bootstrap Scene")]
        public static void CreateBootstrapScene()
        {
            // Create or overwrite the Bootstrap scene.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Remove the default directional light (not needed for a logic-only scene).
            var light = GameObject.Find("Directional Light");
            if (light != null) Object.DestroyImmediate(light);

            // Create the bootstrap host GameObject.
            var host = new GameObject("GameBootstrap");
            host.AddComponent<GameBootstrap>();

            // Save the scene.
            Directory.CreateDirectory(Path.GetDirectoryName(BootstrapScenePath)!);
            EditorSceneManager.SaveScene(scene, BootstrapScenePath);

            // Add to build settings at index 1 (after studio splash).
            EnsureStartupSceneOrder();

            Debug.Log($"[Kismeta] Bootstrap scene created at {BootstrapScenePath} and added to Build Settings.");
        }

        [MenuItem("Kismeta/Run data:sync (npm)")]
        public static void RunDataSync()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName  = "npm";
            proc.StartInfo.Arguments = "run data:sync";
            proc.StartInfo.WorkingDirectory = projectRoot;
            proc.StartInfo.UseShellExecute  = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError  = true;
            proc.Start();
            var stdout = proc.StandardOutput.ReadToEnd();
            var stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            if (!string.IsNullOrEmpty(stdout)) Debug.Log($"[data:sync]\n{stdout}");
            if (!string.IsNullOrEmpty(stderr)) Debug.LogWarning($"[data:sync stderr]\n{stderr}");

            AssetDatabase.Refresh();
            Debug.Log("[Kismeta] data:sync complete. Assets refreshed.");
        }

        [MenuItem("Kismeta/Android/Switch Platform to Android")]
        public static void SwitchPlatformToAndroid()
        {
            ConfigureAndroidPlayerSettings();
            EnsureStartupSceneOrder();

            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                Debug.Log("[Kismeta] Active build target is already Android.");
                return;
            }

            var switched = EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.Android,
                BuildTarget.Android);

            if (switched)
                Debug.Log("[Kismeta] Switched active build target to Android.");
            else
                Debug.LogError("[Kismeta] Failed to switch active build target to Android.");
        }

        [MenuItem("Kismeta/Android/Build And Run on Device")]
        public static void BuildAndRunOnAndroidDevice()
        {
            ConfigureAndroidPlayerSettings();
            EnsureStartupSceneOrder();

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                var switched = EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Android,
                    BuildTarget.Android);
                if (!switched)
                {
                    Debug.LogError("[Kismeta] Could not switch to Android before building.");
                    return;
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(AndroidApkPath)!);

            var buildOptions = BuildOptions.Development | BuildOptions.AutoRunPlayer;
            var report = BuildPipeline.BuildPlayer(
                EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                AndroidApkPath,
                BuildTarget.Android,
                buildOptions);

            if (report.summary.result == BuildResult.Succeeded)
                Debug.Log($"[Kismeta] Android build succeeded: {AndroidApkPath}");
            else
                Debug.LogError($"[Kismeta] Android build failed: {report.summary.result}");
        }

        private static void ConfigureAndroidPlayerSettings()
        {
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, AndroidPackageName);
        }

        private static void EnsureStartupSceneOrder()
        {
            var scenes = EditorBuildSettings.scenes.ToList();

            EnsureScene(scenes, StudioSplashScenePath, enabled: true);
            EnsureScene(scenes, BootstrapScenePath, enabled: true);

            scenes = scenes
                .OrderBy(scene => scene.path == StudioSplashScenePath ? 0 :
                    scene.path == BootstrapScenePath ? 1 : 2)
                .ToList();

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureScene(
            System.Collections.Generic.List<EditorBuildSettingsScene> scenes,
            string scenePath,
            bool enabled)
        {
            var index = scenes.FindIndex(scene => scene.path == scenePath);
            if (index < 0)
                scenes.Add(new EditorBuildSettingsScene(scenePath, enabled));
            else
                scenes[index] = new EditorBuildSettingsScene(scenePath, enabled);
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var current = EditorBuildSettings.scenes;
            foreach (var s in current)
                if (s.path == scenePath) return;

            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(current)
            {
                new EditorBuildSettingsScene(scenePath, true)
            };
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
