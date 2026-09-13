using System.IO;
using System.Text;
using Ten.Boundary;
using Ten.Pure;
using Ten.View;
using UnityEngine;

namespace Ten.Editor
{
    /// <summary>
    /// 寝室を決まった姿勢で撮って、**縦画面の見た目と平均相対輝度**を残す（目視と NFR-007 の目安）。
    ///
    /// <code>
    /// Unity -batchmode -quit -projectPath unity -executeMethod Ten.Editor.TenShots.Take -logFile -
    /// </code>
    ///
    /// 出力は unity/Build/Shots（Git に入れない）。**e2e の代わりではない**（TC-128 は ScreenProbe で測る）。
    /// </summary>
    public static class TenShots
    {
        private const int Width = 540;
        private const int Height = 1200;

        public static void Take()
        {
            var output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Build", "Shots"));

            Directory.CreateDirectory(output);

            var rig = RoomRig.Instance;
            var log = new StringBuilder();

            Shot(rig, output, log, "initial", 0f, 0f, Awake(BodyClip.BreathDeep));
            Shot(rig, output, log, "face-deep", 36f, 25f, Awake(BodyClip.BreathDeep));
            Shot(rig, output, log, "face-half", 36f, 25f, Awake(BodyClip.BreathHalf));
            Shot(rig, output, log, "face-awake", 36f, 25f, Awake(BodyClip.BreathAwake), 2.5f);
            Shot(rig, output, log, "face-drop", 36f, 25f, Awake(BodyClip.DozeDrop), 1.5f);
            Shot(rig, output, log, "face-turnaway", 36f, 25f, Awake(BodyClip.TurnAway), 2f);
            Shot(rig, output, log, "hand-steady", 50f, -15f, Awake(BodyClip.BreathLight, hand: HandClip.PatSteady));
            Shot(rig, output, log, "hand-reach-milk", 50f, -15f, Awake(BodyClip.BreathLight, hand: HandClip.Reach, reaching: CareKind.Milk));
            Shot(rig, output, log, "care-milk", 20f, 0f, Awake(BodyClip.BreathLight, hand: HandClip.CareMilk), 0.6f);
            Shot(rig, output, log, "care-milk-face", 0f, 0f, Awake(BodyClip.BreathLight, hand: HandClip.CareMilk, bottleInFace: true), 2f);
            Shot(rig, output, log, "care-hold", 20f, 0f, Awake(BodyClip.BreathLight, hand: HandClip.CareHold, cameraLifted: true), 3f);
            Shot(rig, output, log, "window-night", -50f, 0f, Awake(BodyClip.BreathDeep));
            Shot(rig, output, log, "window-dawn", -50f, 0f, Awake(BodyClip.BreathDeep, window: 5));
            Shot(rig, output, log, "dawn-lamp-window", -50f, 0f, Awake(BodyClip.Hidden, window: 5, lampOn: true));
            Shot(rig, output, log, "lamp-on-up", 0f, 0f, Awake(BodyClip.Hidden, lampOn: true));
            Shot(rig, output, log, "phone-flash", 36f, 25f, Awake(BodyClip.BreathDeep, phoneFlash: true));
            Shot(rig, output, log, "partner", -30f, 20f, Awake(BodyClip.BreathDeep, partnerHere: true));

            // 縦画面で首を振ったとき、何が同時に入るか（TC-122 の事実確認。1 1080 × 2400 相当）
            rig.DebugCamera.aspect = 1080f / 2400f;

            for (var yaw = -55; yaw <= 55; yaw += 5)
            {
                rig.LookAt(yaw, 0f);
                rig.DebugCamera.aspect = 1080f / 2400f;
                rig.DebugCamera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(RoomRig.FieldOfViewDeg, rig.DebugCamera.aspect);

                var planes = GeometryUtility.CalculateFrustumPlanes(rig.DebugCamera);
                var seen = new StringBuilder();

                foreach (var name in new[] { "Window", "ParentFace", "ParentHand" })
                {
                    var target = rig.transform.Find(name);

                    if (target != null && GeometryUtility.TestPlanesAABB(planes, target.GetComponent<Renderer>().bounds))
                    {
                        seen.Append(name).Append(' ');
                    }
                }

                log.AppendLine($"portrait yaw={yaw} pitch=0: {seen}");
            }

            Debug.Log("[TenShots]\n" + log);
            Object.DestroyImmediate(rig.gameObject);
        }

        private static Presentation Awake(
            BodyClip body, HandClip hand = HandClip.Rest, CareKind? reaching = null, bool lampOn = false, bool phoneFlash = false,
            bool partnerHere = false, int window = 0, bool cameraLifted = false, bool bottleInFace = false) =>
            new(false, body, hand, reaching, false, lampOn, phoneFlash, partnerHere, window, 0, cameraLifted, bottleInFace, BabyMotion.Breathe, 1000, 0);

        private static void Shot(RoomRig rig, string dir, StringBuilder log, string name, float yaw, float pitch, Presentation p, float at = 1f)
        {
            rig.SetEyesClosed(false);
            rig.Present(p);

            // 手の層・視点の上がりは時間で進むので、少しずつ進めてから止める
            for (var t = 0f; t <= at; t += 0.1f)
            {
                rig.Step(t, 0.1f);
            }

            rig.LookAt(yaw, pitch);

            // 姿勢が撮れているかの確かめ（骨が動いたか / 皮膚が追従したか）
            var mother = rig.transform.Find("Mother");

            if (mother != null)
            {
                var states = new StringBuilder();
                var animation = mother.GetComponent<Animation>();
                var count = 0;

                if (animation != null)
                {
                    foreach (AnimationState state in animation)
                    {
                        count++;

                        if (state.enabled)
                        {
                            states.Append($"{state.name}@{state.time:F2}/L{state.layer} ");
                        }
                    }
                }

                Transform head = null, hand = null;

                foreach (var t in mother.GetComponentsInChildren<Transform>())
                {
                    if (t.name == "mixamorig:Head") head = t;
                    if (t.name == "mixamorig:RightHand") hand = t;
                }

                var baked = new Mesh();
                var body = mother.Find("Mother_Body")?.GetComponent<SkinnedMeshRenderer>();

                body?.BakeMesh(baked);

                var sample = baked.vertexCount > 0 ? baked.vertices[baked.vertexCount / 2] : Vector3.zero;

                log.AppendLine($"  [{name}] states={count} on={states} head={head?.position}/{head?.rotation.eulerAngles} hand={hand?.position} vtx={sample} bodyOn={body?.enabled}");
                Object.DestroyImmediate(baked);
            }

            var camera = rig.DebugCamera;
            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);

            // **エディタの Camera.Render は皮膚（SkinnedMeshRenderer）を更新しない。**
            // 骨は動いているので、いまの姿勢を焼いた静的メッシュに一時的に置き換えて撮る
            var standIns = new System.Collections.Generic.List<(SkinnedMeshRenderer Skin, GameObject Stand)>();

            foreach (var skin in rig.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!skin.enabled)
                {
                    continue;
                }

                var mesh = new Mesh();

                skin.BakeMesh(mesh, true);

                var stand = new GameObject(skin.name + "_Baked");

                stand.transform.SetPositionAndRotation(skin.transform.position, skin.transform.rotation);
                stand.AddComponent<MeshFilter>().sharedMesh = mesh;
                stand.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
                skin.enabled = false;
                standIns.Add((skin, stand));
            }

            camera.targetTexture = rt;
            camera.aspect = (float)Width / Height;
            camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(RoomRig.FieldOfViewDeg, camera.aspect);
            camera.Render();

            foreach (var (skin, stand) in standIns)
            {
                skin.enabled = true;
                Object.DestroyImmediate(stand.GetComponent<MeshFilter>().sharedMesh);
                Object.DestroyImmediate(stand);
            }

            var previous = RenderTexture.active;

            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0, false);
            tex.Apply(false, false);
            RenderTexture.active = previous;
            camera.targetTexture = null;

            var sum = 0f;

            foreach (var c in tex.GetPixels())
            {
                sum += 0.2126f * c.linear.r + 0.7152f * c.linear.g + 0.0722f * c.linear.b;
            }

            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            log.AppendLine($"{name}: luminance={sum / (Width * Height):F4}");

            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
        }
    }
}
