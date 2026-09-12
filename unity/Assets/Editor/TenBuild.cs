using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ten.Editor
{
    /// <summary>
    /// Android ビルド（NFR-002 / 003 の成果物検査に使う）。
    ///
    /// **`tests/harness/BuildInspectionTests.cs` は「生成された AndroidManifest.xml」を読む。**
    /// 成果物を見ずに「権限 0 件」「通信 0 件」とは言えない、というのが TC-115 / 119 / 147 の立場。
    /// だから**ビルドを 1 度通すこと自体がテストの前提**になっている。
    ///
    /// 使い方:
    /// <code>
    /// "/Applications/Unity/Hub/Editor/6000.0.83f1/Unity.app/Contents/MacOS/Unity" \
    ///   -batchmode -quit -projectPath unity -executeMethod Ten.Editor.TenBuild.Android -logFile -
    /// </code>
    /// </summary>
    public static class TenBuild
    {
        private const string SceneDir = "Assets/Scenes";
        private const string ScenePath = SceneDir + "/Night.unity";
        private const string OutputDir = "Build/Android";
        private const string PackageName = "jp.ten.app";

        [MenuItem("Ten/Build Android")]
        public static void Android()
        {
            EnsureScene();
            ApplyPlayerSettings();

            var output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", OutputDir));

            Directory.CreateDirectory(output);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(output, "ten.apk"),
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);

            Debug.Log($"[TenBuild] {report.summary.result} / {report.summary.totalSize} bytes");

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Android ビルドに失敗した: {report.summary.result}");
            }
        }

        /// <summary>
        /// 本編のシーン。**中身は実行時に組み立てる**（<c>Ten.View.RoomRig</c>）。
        /// シーンアセットに寝室を置かないのは、e2e が「画面に何が映っているか」を
        /// 撮って測るため（MOD-View / ScreenProbe）。
        /// </summary>
        private static void EnsureScene()
        {
            Directory.CreateDirectory(Path.GetFullPath(Path.Combine(Application.dataPath, "..", SceneDir)));

            if (!File.Exists(Path.GetFullPath(Path.Combine(Application.dataPath, "..", ScenePath))))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                new GameObject("TenBoot").AddComponent<Ten.View.TenBoot>();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        /// <summary>
        /// **要求権限 0 件・通信 0 件**（NFR-002 / 003）。
        /// 既定のままだと Unity が INTERNET を足しうるので、明示的に切る。
        /// </summary>
        private static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "ten";
            PlayerSettings.productName = "Ten";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);

            // **通信しない。**Auto のままだと、参照している module 次第で INTERNET が付く
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.Android.forceSDCardPermission = false;

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);

            // 暗い部屋で遊ぶ（NFR-007）。起動時の白い画面を出さない
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            // 解析の類を入れない（NFR-002 / TC-147）
            PlayerSettings.gcIncremental = true;
        }
    }
}
