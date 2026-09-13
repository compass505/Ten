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

        /// <summary>
        /// クリップの切り出し表。**Codex の `r57-animation-map.json` をそのまま読む**
        /// （版が上がるとフレーム範囲がずれるので、表を手で写さない）。
        /// </summary>
        public const string MotherMapPath = "Assets/Art/Mother/animation-map.json";

        [System.Serializable]
        private sealed class ClipEntry
        {
            public string clip;
            public float[] fbx_frame_range;
            public bool loop;
        }

        [System.Serializable]
        private sealed class ClipMap
        {
            public ClipEntry[] clips;
        }

        public static (string Name, float First, float Last, bool Loop)[] MotherClips()
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath)!, MotherMapPath);

            if (!System.IO.File.Exists(path))
            {
                throw new System.InvalidOperationException($"{MotherMapPath} が無い。母の FBX のクリップを切り出せない");
            }

            var map = UnityEngine.JsonUtility.FromJson<ClipMap>(System.IO.File.ReadAllText(path));
            var clips = new (string, float, float, bool)[map.clips.Length];

            for (var i = 0; i < clips.Length; i++)
            {
                var c = map.clips[i];

                clips[i] = (c.clip, c.fbx_frame_range[0], c.fbx_frame_range[1], c.loop);
            }

            return clips;
        }

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

            foreach (var (name, first, last, loop) in MotherClips())
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
