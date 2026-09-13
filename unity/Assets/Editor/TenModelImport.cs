using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Ten.Editor
{
    /// <summary>
    /// Codex の母（r56-approved）と寝室（room-r1）の取り込み設定（presentation.md 2.1 / 2.2）。
    ///
    /// **母の FBX は 12 クリップを 1 本の時間軸に並べてある**（12 fps、1〜528 フレーム）。
    /// 切り出しの範囲は `r56-animation-map.json` の `fbx_frame_range` をそのまま写した。
    /// </summary>
    public sealed class TenModelImport : AssetPostprocessor
    {
        public const string MotherPath = "Assets/Resources/Models/Mother/mother-runtime.fbx";

        /// <summary>(名前, 開始, 終了, ループ)。**フレーム番号は FBX の時間軸**（r56-animation-map.json）。</summary>
        public static readonly (string Name, int First, int Last, bool Loop)[] MotherClips =
        {
            ("A0", 1, 36, true),      // 熟睡
            ("A1", 37, 72, true),     // 浅い
            ("A2", 73, 108, true),    // 半分起きる
            ("A3", 109, 144, false),  // 覚醒（末尾で消える演出は Unity 側で出さない）
            ("B", 145, 240, false),   // 寝入りばな（前半 4.5 秒が予兆、続く 1.8 秒が本体）
            ("C0", 241, 288, true),   // 手: 規則正しい
            ("C1", 289, 336, true),   // 手: 乱れる
            ("C2", 337, 384, false),  // 手: 途中で止まる
            ("D0", 385, 420, true),   // 対処: トントン
            ("D1", 421, 456, false),  // 対処: ミルク
            ("D2", 457, 492, false),  // 対処: 抱っこ
            ("D3", 493, 528, false),  // 対処: オムツ替え
        };

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/Resources/Models/"))
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;

            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.importBlendShapes = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.isReadable = false;

            if (assetPath != MotherPath)
            {
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
                return;
            }

            // **Legacy にする。**Animator Controller のアセットを作らずに、実行時に組める
            // （寝室を実行時に組み立てる方針。RoomRig の冒頭）
            importer.animationType = ModelImporterAnimationType.Legacy;
            importer.importAnimation = true;
            importer.optimizeGameObjects = false;
        }

        /// <summary>
        /// 光るマテリアル（窓・天井灯・スマホ）は**取り込み時に Emission を有効にしておく。**
        /// 実行時に有効にするだけだと、そのシェーダーの組み合わせがビルドから落ちて光らない。
        /// </summary>
        private void OnPostprocessMaterial(Material material)
        {
            if (!assetPath.StartsWith("Assets/Resources/Models/Room/"))
            {
                return;
            }

            if (material.name.StartsWith("WindowGlass") || material.name.StartsWith("LampEmit") || material.name.StartsWith("PhoneEmit"))
            {
                material.color = Color.black;
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(0.05f, 0.05f, 0.05f, 1f));
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
        }

        private void OnPreprocessAnimation()
        {
            if (assetPath != MotherPath)
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            var clips = new List<ModelImporterClipAnimation>();

            foreach (var (name, first, last, loop) in MotherClips)
            {
                clips.Add(new ModelImporterClipAnimation
                {
                    name = name,
                    takeName = importer.defaultClipAnimations.Length > 0 ? importer.defaultClipAnimations[0].takeName : name,
                    firstFrame = first,
                    lastFrame = last,
                    loopTime = loop,
                    wrapMode = loop ? WrapMode.Loop : WrapMode.ClampForever,
                });
            }

            importer.clipAnimations = clips.ToArray();
        }

        /// <summary>取り込み結果を文字にする（batchmode で確かめるための口）。</summary>
        [MenuItem("Ten/Dump Models")]
        public static void Dump()
        {
            var log = new StringBuilder();

            foreach (var path in new[] { MotherPath, "Assets/Resources/Models/Room/room.fbx", "Assets/Resources/Models/Room/bottle.fbx", "Assets/Resources/Models/Room/phone.fbx" })
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                log.AppendLine($"== {path} loaded={go != null}");

                if (go == null)
                {
                    continue;
                }

                var instance = Object.Instantiate(go);

                log.AppendLine($"root rot={instance.transform.localEulerAngles} scale={instance.transform.localScale}");

                foreach (var t in instance.GetComponentsInChildren<Transform>())
                {
                    var r = t.GetComponent<Renderer>();

                    if (r != null)
                    {
                        var smr = r as SkinnedMeshRenderer;
                        var mats = string.Join(",", System.Array.ConvertAll(r.sharedMaterials, m => m == null ? "null" : $"{m.name}[{m.shader.name}]{(m.mainTexture != null ? "tex" : "")}"));

                        log.AppendLine($"  mesh {t.name} pos={t.position} bounds={r.bounds.center}/{r.bounds.size} blend={(smr != null && smr.sharedMesh != null ? smr.sharedMesh.blendShapeCount : 0)} mats={mats}");
                    }
                    else if (t.name.Contains("Head") || t.name.Contains("RightHand") && !t.name.Contains("Index") && !t.name.Contains("Thumb") && !t.name.Contains("Middle") && !t.name.Contains("Ring") && !t.name.Contains("Pinky"))
                    {
                        log.AppendLine($"  bone {t.name} pos={t.position}");
                    }
                }

                var animation = instance.GetComponent<Animation>();

                if (animation != null)
                {
                    foreach (AnimationState state in animation)
                    {
                        log.AppendLine($"  clip {state.name} len={state.length} wrap={state.wrapMode}");
                    }

                    foreach (var clipName in new[] { "A0", "C0", "D1" })
                    {
                        var clip = animation.GetClip(clipName);

                        if (clip == null)
                        {
                            continue;
                        }

                        clip.SampleAnimation(instance, clip.length * 0.5f);

                        foreach (var t in instance.GetComponentsInChildren<Transform>())
                        {
                            if (t.name == "mixamorig:Head" || t.name == "mixamorig:RightHand")
                            {
                                log.AppendLine($"  {clipName} mid {t.name} pos={t.position}");
                            }
                        }
                    }

                    var bindings = AnimationUtility.GetCurveBindings(animation.GetClip("A0"));
                    var shapes = 0;

                    foreach (var b in bindings)
                    {
                        if (b.propertyName.StartsWith("blendShape."))
                        {
                            shapes++;
                        }
                    }

                    log.AppendLine($"  A0 curves={bindings.Length} blendShapeCurves={shapes}");
                }

                Object.DestroyImmediate(instance);
            }

            Debug.Log("[TenModelImport]\n" + log);
        }
    }
}
