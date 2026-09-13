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

        /// <summary>
        /// macOS で触るためのビルド。**配布先ではない**（ADR-0001 は Android）。
        /// 実機に挿す前に手触りを見るためだけのもの。
        /// </summary>
        [MenuItem("Ten/Build Mac")]
        public static void Mac()
        {
            EnsureScene();
            EnsureShaders();

            PlayerSettings.companyName = "ten";
            PlayerSettings.productName = "Ten";
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            // 縦長の端末を模す。**片手で下半分に届く**という前提を壊さないため（REQ-005）
            PlayerSettings.defaultScreenWidth = 480;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;

            var output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Build", "Mac"));

            Directory.CreateDirectory(output);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(output, "Ten.app"),
                target = BuildTarget.StandaloneOSX,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            });

            Debug.Log($"[TenBuild] Mac {report.summary.result} / {report.summary.totalSize} bytes");

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Mac ビルドに失敗した: {report.summary.result}");
            }
        }

        [MenuItem("Ten/Build Android")]
        public static void Android()
        {
            EnsureScene();
            EnsureShaders();
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
        /// **実行時に `Shader.Find` で引くシーダーを、ビルドに必ず入れる。**
        ///
        /// `RoomRig` はマテリアルをコードで作る（シーンにアセットを置かない方針のため）。
        /// シーンからもアセットからも参照されないシェーダーは**ビルドから落ちる。**
        /// エディタでは通り、アプリでだけ物が消える——という形で出る。
        /// </summary>
        private static void EnsureShaders()
        {
            var settings = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);

            var included = settings.FindProperty("m_AlwaysIncludedShaders");
            var want = Shader.Find("Unlit/Color");

            if (want == null)
            {
                throw new InvalidOperationException("Unlit/Color が見つからない");
            }

            for (var i = 0; i < included.arraySize; i++)
            {
                if (included.GetArrayElementAtIndex(i).objectReferenceValue == want)
                {
                    return;
                }
            }

            included.InsertArrayElementAtIndex(included.arraySize);
            included.GetArrayElementAtIndex(included.arraySize - 1).objectReferenceValue = want;
            settings.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            Debug.Log("[TenBuild] Unlit/Color を Always Included Shaders に追加した");
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

            ApplyIcons();
        }

        /// <summary>
        /// アプリのアイコン（ui 包み r1。presentation.md 2.3）。
        /// Adaptive は前景と背景の 432 px を重ねる。古いランチャー向けには 512 px の合成を使う。
        /// </summary>
        private static void ApplyIcons()
        {
            const string dir = "Assets/Art/Icon/";

            var foreground = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "ic_foreground.png");
            var background = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "ic_background.png");
            var flat = AssetDatabase.LoadAssetAtPath<Texture2D>(dir + "store-512.png");

            if (foreground == null || background == null || flat == null)
            {
                throw new InvalidOperationException($"アイコンが {dir} に揃っていない");
            }

            var target = NamedBuildTarget.Android;

            var adaptive = PlayerSettings.GetPlatformIcons(target, UnityEditor.Android.AndroidPlatformIconKind.Adaptive);

            foreach (var icon in adaptive)
            {
                icon.SetTextures(background, foreground);
            }

            PlayerSettings.SetPlatformIcons(target, UnityEditor.Android.AndroidPlatformIconKind.Adaptive, adaptive);

            foreach (var kind in new[] { UnityEditor.Android.AndroidPlatformIconKind.Round, UnityEditor.Android.AndroidPlatformIconKind.Legacy })
            {
                var icons = PlayerSettings.GetPlatformIcons(target, kind);

                foreach (var icon in icons)
                {
                    icon.SetTexture(flat);
                }

                PlayerSettings.SetPlatformIcons(target, kind, icons);
            }
        }
    }
}
