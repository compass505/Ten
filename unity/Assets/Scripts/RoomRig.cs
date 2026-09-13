using System.Collections.Generic;
using Ten.Boundary;
using UnityEngine;

namespace Ten.View
{
    /// <summary>
    /// 一人称の寝室そのもの（MOD-View）。**実行時に組み立てる。**
    ///
    /// シーンアセットに置かずコードで組むのは、
    /// **e2e が「いま画面に何が映っているか」を撮って測る**ため（ScreenProbe）。
    /// シーンに置くと、テストが読むのはシーンの中身であって画面ではなくなる。
    ///
    /// ここには判断を置かない（V-1）。<see cref="Presentation"/> を受け取って見せるだけ。
    ///
    /// **形は Codex の包み**（母 r56-approved / 寝室 room-r1。presentation.md 2 節）。
    /// 寸法は setting.md 3 節（2026-09-14 に実寸へ置き直した。handoff.md H-10）。
    /// </summary>
    public sealed class RoomRig : MonoBehaviour
    {
        /// <summary>
        /// 視界の水平画角。**1080 × 2400 の縦画面で垂直 70°**（r56-approved で承認した撮影条件）に当たる。
        /// 縦横比に任せず水平で固定するのは、横長の画面で視界が広がって V-3 が崩れないため。
        /// </summary>
        public const float FieldOfViewDeg = 35f;

        /// <summary>首の可動範囲（balance.md 9 節 / REQ-002）。</summary>
        public const float YawLimitDeg = 55f;

        /// <summary>初期視線。**真上の天井**（screens.md 4.2.1）。どの対象にも向いていない。</summary>
        public const float InitialYawDeg = 0f;

        public const float InitialPitchDeg = 0f;

        /// <summary>Ten の目の高さ（setting.md 3 節）と、抱っこで上がった高さ（r56-approved D2）。</summary>
        private const float EyeHeight = 0.28f;
        private const float LiftHeight = 0.83f;

        private const int ShotSize = 128;

        // --- 実寸の配置（Unity 座標。原点は Ten の頭の下の床、足が +Z、窓が +X） ---

        /// <summary>母の足元の置き場と向き（r56-animation-map.json の scene_anchor_unity / Z 90°）。</summary>
        private static readonly Vector3 MotherAnchor = new(-0.3947f, 0f, 0.3975f);
        private static readonly Quaternion MotherTurn = Quaternion.Euler(0f, 90f, 0f);

        /// <summary>
        /// 視線の判定用の代理物（見えない）。**モデルの実測に合わせて置く**
        /// （scratch/view-geometry: r56-approved の頭と、トントンの右手の範囲）。
        /// </summary>
        private static readonly Vector3 FaceCenter = new(-0.38f, 0.81f, 0.40f);
        private const float FaceDiameter = 0.26f;
        private static readonly Vector3 HandCenter = new(-0.47f, 0.72f, -0.18f);
        private const float HandSize = 0.16f;
        private static readonly Vector3 WindowCenter = new(2.19f, 1.50f, -0.30f);
        private static readonly Vector3 WindowSize = new(0.06f, 1.30f, 1.70f);

        private static readonly Vector3 LampCenter = new(0.75f, 2.30f, -0.75f);
        private static readonly Vector3 PhoneBase = new(-0.78f, 0f, -0.17f);

        /// <summary>room.fbx の各メッシュは原点に重なって入っている。**置き場は room-r1-placement.json。**</summary>
        private static readonly (string Mesh, Vector3 Position)[] RoomPlacement =
        {
            ("RoomR1_Ceiling_Runtime", new Vector3(0f, 2.5f, 0f)),
            ("RoomR1_Floor_Runtime", new Vector3(0f, -0.05f, 0f)),
            ("RoomR1_WallMinusX_Runtime", new Vector3(-2.225f, 0f, 0f)),
            ("RoomR1_WallY-2.625_Runtime", new Vector3(0f, 0f, -2.625f)),
            ("RoomR1_WallY2.625_Runtime", new Vector3(0f, 0f, 2.625f)),
            ("RoomR1_WindowWall_below_Runtime", new Vector3(2.225f, 0f, 0f)),
            ("RoomR1_WindowWall_above_Runtime", new Vector3(2.225f, 2.15f, 0f)),
            ("RoomR1_WindowWall_head_Runtime", new Vector3(2.225f, 0.85f, -1.875f)),
            ("RoomR1_WindowWall_foot_Runtime", new Vector3(2.225f, 0.85f, 1.575f)),
            ("RoomR1_WindowGlass_Runtime", new Vector3(2.19f, 0.85f, -0.30f)),
            ("RoomR1_WindowFrameY-1.15_Runtime", new Vector3(2.17f, 0.85f, -1.15f)),
            ("RoomR1_WindowFrameY0.55_Runtime", new Vector3(2.17f, 0.85f, 0.55f)),
            ("RoomR1_WindowFrameZ0.85_Runtime", new Vector3(2.17f, 0.8325f, -0.30f)),
            ("RoomR1_WindowFrameZ2.15_Runtime", new Vector3(2.17f, 2.1325f, -0.30f)),
            ("RoomR1_WindowMullion_Runtime", new Vector3(2.165f, 0.85f, -0.30f)),
            ("RoomR1_CeilingLamp_Runtime", new Vector3(0.75f, 2.30f, -0.75f)),
            ("RoomR1_LampMount_Runtime", new Vector3(0.75f, 2.35f, -0.75f)),
            ("RoomR1_BabyMattress_Runtime", new Vector3(0f, 0f, 0.40f)),
            ("RoomR1_FoldedFutonSeat_Runtime", new Vector3(-0.49f, 0f, 0.50f)),
            ("RoomR1_BabyQuilt_Runtime", new Vector3(0f, 0.0745f, 0.63f)),
            ("RoomR1_FoldedFutonBack_Runtime", new Vector3(-0.575f, 0.03f, 0.405f)),
        };

        private static RoomRig _instance;

        private Camera _eye;
        private Transform _eyelid;
        private Material _eyelidMaterial;
        private Material _windowPatchMaterial;
        private Transform _windowPatch;
        private readonly List<(GazeTargetKind Kind, Renderer Renderer)> _targets = new();
        private readonly List<Rect> _controls = new();
        private RenderTexture _shotTarget;
        private Texture2D _shot;
        private bool _carried;
        private int _settleTicks;
        private int _arousalStage = -1;
        private int _vigorStage = -1;
        private int _handStage = -1;
        private int _timeStage = -1;

        // --- 見せ方（presentation.md 2 節の差し込み口） ---
        private Presentation? _present;
        private Quaternion _look = Quaternion.identity;
        private Material _windowMaterial;
        private Material _lampMaterial;
        private Material _phoneMaterial;
        private Light _moon;
        private Light _lampLight;
        private Light _phoneLight;
        private GameObject _mother;
        private Animation _motherAnimation;
        private Transform _motherHead;
        private readonly List<Renderer> _motherParts = new();
        private readonly List<Transform> _rightArm = new();
        private readonly List<Transform> _bothArms = new();
        private readonly HashSet<string> _mixed = new();
        private Transform _bottleInHand;
        private Transform _reachBottle;
        private Transform _reachCloth;
        private Transform _bottleInFace;
        private Transform _partner;
        private readonly Transform[] _vignette = new Transform[4];
        private BodyClip _lastBody = (BodyClip)(-1);
        private HandClip _lastHand = (HandClip)(-1);
        private Ten.Pure.CareKind? _lastReaching;
        private float _bodyStarted;
        private float _handStarted;
        private float _lift;
        private float _turn;
        private int _appliedWindow = -1;
        private int _appliedLamp = -1;
        private int _appliedPhone = -1;

        /// <summary>いまの首の向き。</summary>
        public float YawDeg { get; private set; } = InitialYawDeg;

        public float PitchDeg { get; private set; } = InitialPitchDeg;

        /// <summary>目を閉じているか。**閉じている間は完全に黒**（V-4）。</summary>
        public bool EyesClosed { get; private set; } = true;

        /// <summary>操作の当たり領域（ビューポート座標）。**下半分だけ**（V-8 / REQ-005）。</summary>
        public IReadOnlyList<Rect> Controls => _controls;

        /// <summary>同時接触を要求する操作の数。**0 でなければ片手で遊べない**（REQ-005）。</summary>
        public int MultiTouchControlCount { get; private set; }

        public enum GazeTargetKind { Window, ParentFace, ParentHand }

        /// <summary>いまのカメラ（調べるためだけに出す）。</summary>
        public Camera DebugCamera => _eye;

        /// <summary>いまの縦横比（調べるためだけに出す）。</summary>
        public float DebugAspect => _eye.aspect;

        /// <summary>いまの縦の画角（調べるためだけに出す）。</summary>
        public float DebugFov => _eye.fieldOfView;

        // ------------------------------------------------------------------

        public static RoomRig Instance
        {
            get
            {
                if (_instance == null)
                {
                    var host = new GameObject("TenRoomRig");
                    _instance = host.AddComponent<RoomRig>();
                    _instance.Build();
                }

                return _instance;
            }
        }

        private void Build()
        {
            // 寝室は暗い（NFR-007）。環境光も落とす。明かりは窓・天井灯・スマホの 3 つだけ
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.012f, 0.013f, 0.018f, 1f);
            RenderSettings.fog = false;

            var eye = new GameObject("Eye");

            eye.transform.SetParent(transform, false);
            eye.transform.localPosition = new Vector3(0f, EyeHeight, 0f);
            _eye = eye.AddComponent<Camera>();
            _eye.clearFlags = CameraClearFlags.SolidColor;
            _eye.backgroundColor = new Color(0.004f, 0.004f, 0.008f, 1f);
            _eye.fieldOfView = FieldOfViewDeg;
            _eye.nearClipPlane = 0.015f;
            _eye.farClipPlane = 20f;
            _eye.allowHDR = false;
            // 画面に出す。撮るとき（e2e）は targetTexture を差し替えて Render を呼ぶ
            _eye.enabled = true;

            BuildRoom();
            BuildTargets();
            BuildEyelid();
            BuildMother();
            BuildProps();
            BuildVignette();
            BuildControls();
            ApplyWindow(0);
            ApplyLamp(false);
            ApplyPhone(false);
            Apply();
        }

        /// <summary>
        /// 寝室そのもの（room-r1）。
        ///
        /// **これが無いと「暗い部屋」ではなく「虚空」になる。**
        /// 一人称で寝室にいること（REQ-001）は、視線を向けた先に何かがあることで初めて成り立つ。
        /// </summary>
        private void BuildRoom()
        {
            var room = Spawn("Models/Room/room", "Room", transform);

            foreach (var (mesh, position) in RoomPlacement)
            {
                var part = FindDeep(room.transform, mesh);

                if (part != null)
                {
                    part.localPosition = position;
                }
            }

            _windowMaterial = MaterialOf(room, "WindowGlass");
            _lampMaterial = MaterialOf(room, "LampEmit");

            // 月明かり。**窓の内側から部屋へ**（room-r1 の Area light の代わり）
            _moon = MakeLight("Moon", new Vector3(1.95f, 1.55f, -0.30f), new Color(0.49f, 0.61f, 0.79f), 7f);
            _moon.renderMode = LightRenderMode.ForcePixel;

            _lampLight = MakeLight("LampLight", LampCenter + Vector3.down * 0.15f, new Color(1f, 0.65f, 0.38f), 6f);
        }

        /// <summary>視線の判定用の代理物。**描かない**（位置と大きさだけを持つ。TC-122）。</summary>
        private void BuildTargets()
        {
            _targets.Add((GazeTargetKind.Window, MakeProxy("Window", PrimitiveType.Cube, WindowCenter, WindowSize)));
            _targets.Add((GazeTargetKind.ParentFace, MakeProxy("ParentFace", PrimitiveType.Sphere, FaceCenter, Vector3.one * FaceDiameter)));
            _targets.Add((GazeTargetKind.ParentHand, MakeProxy("ParentHand", PrimitiveType.Cube, HandCenter, Vector3.one * HandSize)));
        }

        private Renderer MakeProxy(string name, PrimitiveType shape, Vector3 position, Vector3 size)
        {
            var go = MakePart(name, transform, shape, position, size, Color.black);
            var renderer = go.GetComponent<Renderer>();

            // **当たり判定は要らない**（物理を使わない）。見た目はモデルが持つ
            renderer.forceRenderingOff = true;

            return renderer;
        }

        /// <summary>母（r56-approved）。**Legacy の Animation で、体と手の 2 層に流す。**</summary>
        private void BuildMother()
        {
            _mother = Spawn("Models/Mother/mother-runtime", "Mother", transform);

            if (_mother == null)
            {
                return;
            }

            _mother.transform.localPosition = MotherAnchor;
            _mother.transform.localRotation = MotherTurn;

            foreach (var renderer in _mother.GetComponentsInChildren<Renderer>())
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                _motherParts.Add(renderer);

                if (renderer is SkinnedMeshRenderer skinned)
                {
                    // 首を振って画面から外れても、戻したときに姿勢が飛ばない
                    skinned.updateWhenOffscreen = true;
                }
            }

            _motherHead = FindDeep(_mother.transform, "mixamorig:Head");
            _motherAnimation = _mother.GetComponent<Animation>();

            if (_motherAnimation == null)
            {
                return;
            }

            _motherAnimation.playAutomatically = false;
            _motherAnimation.cullingType = AnimationCullingType.AlwaysAnimate;

            var rightShoulder = FindDeep(_mother.transform, "mixamorig:RightShoulder");
            var leftShoulder = FindDeep(_mother.transform, "mixamorig:LeftShoulder");

            if (rightShoulder != null)
            {
                _rightArm.Add(rightShoulder);
                _bothArms.Add(rightShoulder);
            }

            if (leftShoulder != null)
            {
                _bothArms.Add(leftShoulder);
            }

            foreach (AnimationState state in _motherAnimation)
            {
                state.layer = LayerOf(state.name);
                state.blendMode = AnimationBlendMode.Blend;
                state.enabled = false;
                state.weight = 0f;
            }

            // 鼻をひくつかせるのは顔まわりだけ（r57 の注記: 上顔のマスクで重ねる）
            var sniff = _motherAnimation["Sniff"];

            if (sniff != null && _motherHead != null)
            {
                sniff.AddMixingTransform(_motherHead, true);
            }
        }

        /// <summary>
        /// 小物（SET-02: 動作に要る哺乳瓶とスマホだけ）と、もう一人の親（SET-03: 表情なしのシルエット）。
        /// </summary>
        private void BuildProps()
        {
            // 手に持つ哺乳瓶（Care_Milk の配置式。room-r1 README）
            var bottle = Spawn("Models/Room/bottle", "BottleInHand", transform);

            _bottleInHand = bottle != null ? bottle.transform : new GameObject("BottleInHand").transform;
            _bottleInHand.gameObject.SetActive(false);

            // 予告の手が持つ小物（r57 の props.fbx。置き場は r57-animation-map.json の scene_controls を Unity 座標に写した）
            var props = Spawn("Models/Mother/props", "ReachProps", transform);

            if (props != null)
            {
                _reachBottle = FindDeep(props.transform, "Prop_Bottle");
                _reachCloth = FindDeep(props.transform, "Prop_Cloth");

                PlaceProp(_reachBottle, new Vector3(-0.47f, 0.715f, -0.16f));
                PlaceProp(_reachCloth, new Vector3(-0.455f, 0.70f, -0.18f));
            }

            // 対処「ミルク」: **視界がほぼ塞がる**（setting.md 5 節）。カメラから 0.165 m（room-r1 R-6）
            var inFace = Spawn("Models/Room/bottle", "BottleInFace", _eye.transform);

            _bottleInFace = inFace != null ? inFace.transform : new GameObject("BottleInFace").transform;
            _bottleInFace.SetParent(_eye.transform, false);
            _bottleInFace.localPosition = new Vector3(0f, 0f, 0.165f + 0.09f);
            _bottleInFace.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            _bottleInFace.gameObject.SetActive(false);

            // スマホ（`PhoneEmit`）。母の枕元の床
            var phone = Spawn("Models/Room/phone", "Phone", transform);

            if (phone != null)
            {
                phone.transform.localPosition = PhoneBase;
                _phoneMaterial = MaterialOf(phone, "PhoneEmit");
            }

            _phoneLight = MakeLight("PhoneLight", PhoneBase + Vector3.up * 0.12f, new Color(0.62f, 0.74f, 0.92f), 2.5f);

            // もう一人の親。**窓の側、足もとに座る暗い影**。窓は隠さない
            _partner = new GameObject("Partner").transform;
            _partner.SetParent(transform, false);
            _partner.localPosition = new Vector3(1.25f, 0f, 1.7f);

            var silhouette = new Color(0.035f, 0.035f, 0.045f, 1f);

            MakePart("PartnerHead", _partner, PrimitiveType.Sphere, new Vector3(0f, 0.82f, 0f), Vector3.one * 0.24f, silhouette);
            MakePart("PartnerBody", _partner, PrimitiveType.Cube, new Vector3(0f, 0.38f, 0f), new Vector3(0.42f, 0.62f, 0.30f), silhouette);

            _partner.gameObject.SetActive(false);
        }

        /// <summary>Blender で X 軸まわり 90°（底面原点）の置き方は、Unity では −90°。</summary>
        private void PlaceProp(Transform prop, Vector3 position)
        {
            if (prop == null)
            {
                return;
            }

            prop.SetParent(transform, false);
            prop.localPosition = position;
            prop.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            prop.gameObject.SetActive(false);
        }

        private GameObject Spawn(string resource, string name, Transform parent)
        {
            var prefab = Resources.Load<GameObject>(resource);

            if (prefab == null)
            {
                Debug.LogWarning($"[RoomRig] {resource} が Resources に無い");
                return null;
            }

            var go = Instantiate(prefab, parent, false);

            go.name = name;

            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            return go;
        }

        private Light MakeLight(string name, Vector3 position, Color color, float range)
        {
            var go = new GameObject(name);

            go.transform.SetParent(transform, false);
            go.transform.localPosition = position;

            var light = go.AddComponent<Light>();

            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = 0f;
            light.shadows = LightShadows.None;

            return light;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                {
                    return t;
                }
            }

            return null;
        }

        /// <summary>名前でマテリアルを引き、**このレンダラー専用の複製**を返す（色を変えるため）。</summary>
        private static Material MaterialOf(GameObject root, string materialName)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var shared = renderer.sharedMaterials;

                for (var i = 0; i < shared.Length; i++)
                {
                    if (shared[i] != null && shared[i].name.StartsWith(materialName))
                    {
                        var copy = new Material(shared[i]);

                        shared[i] = copy;
                        renderer.sharedMaterials = shared;

                        return copy;
                    }
                }
            }

            return null;
        }

        private GameObject MakePart(
            string name, Transform parent, PrimitiveType shape, Vector3 localPosition, Vector3 localScale, Color color)
        {
            var go = GameObject.CreatePrimitive(shape);

            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

            var renderer = go.GetComponent<Renderer>();

            renderer.sharedMaterial = OpaqueMaterial(color);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return go;
        }

        /// <summary>
        /// 視界の周辺の沈み（空腹 / 元気が尽きた。setting.md 6・8 節）。
        /// **不透明な黒の帯**で描く（半透明を使わない。V-4 と同じ理由で、暗所の透けを作らない）。
        /// </summary>
        private void BuildVignette()
        {
            for (var i = 0; i < _vignette.Length; i++)
            {
                var bar = MakePart($"Vignette{i}", _eye.transform, PrimitiveType.Quad,
                    new Vector3(0f, 0f, 0.06f), Vector3.one * 0.01f, Color.black);

                _vignette[i] = bar.transform;
                bar.SetActive(false);
            }
        }

        /// <summary>
        /// まぶた。**不透明な黒**（V-4）。
        ///
        /// alpha を混ぜると寝室が透け、REQ-004 と寝たふりの賭けが丸ごと壊れる
        /// （balance.md 11 節で実際に踏んだ）。**半透明のシェーダを使わない。**
        /// </summary>
        private void BuildEyelid()
        {
            var lid = GameObject.CreatePrimitive(PrimitiveType.Quad);

            lid.name = "Eyelid";
            lid.transform.SetParent(_eye.transform, false);
            lid.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            lid.transform.localRotation = Quaternion.identity;

            // 近接面を覆いきる大きさ（縦長の画面の縦方向まで覆う）
            var height = 2f * 0.05f * Mathf.Tan(FieldOfViewDeg * 0.5f * Mathf.Deg2Rad);

            lid.transform.localScale = new Vector3(height * 8f, height * 8f, 1f);

            _eyelidMaterial = OpaqueMaterial(Color.black);

            var renderer = lid.GetComponent<Renderer>();

            renderer.sharedMaterial = _eyelidMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            _eyelid = lid.transform;

            // 閉眼中の窓の明かり（V-9 / D-12）。**まぶたの手前に、不透明な色で描く**
            var patch = GameObject.CreatePrimitive(PrimitiveType.Quad);

            patch.name = "WindowPatch";
            patch.transform.SetParent(_eye.transform, false);
            patch.transform.localPosition = new Vector3(0f, 0.1f, 0.04f);
            patch.transform.localScale = new Vector3(height * 0.8f, height * 0.5f, 1f);

            _windowPatchMaterial = OpaqueMaterial(PatchColor(0));

            var patchRenderer = patch.GetComponent<Renderer>();

            patchRenderer.sharedMaterial = _windowPatchMaterial;
            patchRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            _windowPatch = patch.transform;
        }

        /// <summary>
        /// 操作の当たり領域。**画面の下半分だけ**（V-8 / REQ-005）。
        /// 首振りのドラッグは画面全体で受けるが、これは「操作対象」ではない。
        /// </summary>
        private void BuildControls()
        {
            _controls.Clear();

            // 泣く / ぐずる / ばたつかせる / 目の開閉
            _controls.Add(new Rect(0.04f, 0.06f, 0.21f, 0.30f));
            _controls.Add(new Rect(0.27f, 0.06f, 0.21f, 0.30f));
            _controls.Add(new Rect(0.50f, 0.06f, 0.21f, 0.30f));
            _controls.Add(new Rect(0.73f, 0.06f, 0.23f, 0.30f));

            MultiTouchControlCount = 0;
        }

        private static Material OpaqueMaterial(Color color)
        {
            var shader = Shader.Find("Unlit/Color");

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            var material = new Material(shader);

            if (material.HasProperty("_Color"))
            {
                material.color = color;
            }

            // **不透明。**renderQueue を透明側へ動かさない（V-4 / TC-124）
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;

            return material;
        }

        /// <summary>窓の色（setting.md 7 節: 濃紺 → 白む → 夜明け直前が最も明るい。presentation.md 3.1）。</summary>
        private static Color WindowColor(int level) => level switch
        {
            0 => new Color(0.07f, 0.08f, 0.19f, 1f),
            1 => new Color(0.09f, 0.10f, 0.22f, 1f),
            2 => new Color(0.12f, 0.13f, 0.26f, 1f),
            3 => new Color(0.18f, 0.20f, 0.33f, 1f),
            4 => new Color(0.30f, 0.33f, 0.45f, 1f),
            _ => new Color(0.52f, 0.55f, 0.64f, 1f),
        };

        /// <summary>月明かりの強さ。**窓が白むほど部屋も明るい**（夜明け直前が最大。NFR-007 はここで測る）。</summary>
        private static float MoonIntensity(int level) => level switch
        {
            0 => 0.35f,
            1 => 0.40f,
            2 => 0.48f,
            3 => 0.55f,
            4 => 0.55f,
            _ => 0.55f,
        };

        private static Color PatchColor(int level) => WindowColor(level) * 0.75f + new Color(0f, 0f, 0f, 0.25f);

        private void ApplyWindow(int level)
        {
            _appliedWindow = level;
            SetEmission(_windowMaterial, WindowColor(level));
            _windowPatchMaterial.color = PatchColor(level);

            if (_moon != null)
            {
                _moon.intensity = MoonIntensity(level);
            }
        }

        private void ApplyLamp(bool on)
        {
            _appliedLamp = on ? 1 : 0;
            SetEmission(_lampMaterial, on ? new Color(0.95f, 0.78f, 0.52f, 1f) : new Color(0.10f, 0.10f, 0.11f, 1f));

            if (_lampLight != null)
            {
                _lampLight.intensity = on ? 0.45f : 0f;
            }
        }

        private void ApplyPhone(bool on)
        {
            _appliedPhone = on ? 1 : 0;
            SetEmission(_phoneMaterial, on ? new Color(0.62f, 0.74f, 0.92f, 1f) : new Color(0.03f, 0.03f, 0.035f, 1f));

            if (_phoneLight != null)
            {
                _phoneLight.intensity = on ? 1.4f : 0f;
            }
        }

        /// <summary>光るものは**光だけで色を出す**（ベースは黒。照明の当たり方で色が変わらない）。</summary>
        private static void SetEmission(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            material.color = Color.black;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color);
        }

        // ------------------------------------------------------------------
        // 見せる
        // ------------------------------------------------------------------

        /// <summary>首を振る。可動範囲で止める（REQ-002）。</summary>
        public void LookAt(float yawDeg, float pitchDeg)
        {
            YawDeg = Mathf.Clamp(yawDeg, -YawLimitDeg, YawLimitDeg);
            PitchDeg = Mathf.Clamp(pitchDeg, -15f, 30f);

            // setting.md 3 節: 初期は真上。yaw の + が母の側（−X）、pitch の + が足の側（+Z）。
            // 画面の上は足の側（r56-approved の撮影条件）
            var yaw = YawDeg * Mathf.Deg2Rad;
            var pitch = PitchDeg * Mathf.Deg2Rad;
            var forward = new Vector3(-Mathf.Sin(yaw) * Mathf.Cos(pitch), Mathf.Cos(yaw) * Mathf.Cos(pitch), Mathf.Sin(pitch));

            _look = Quaternion.LookRotation(forward, Vector3.forward);
            Apply();
        }

        public void SetEyesClosed(bool closed)
        {
            EyesClosed = closed;
            Apply();
        }

        /// <summary>夜以外の画面では寝室を描かない（電池と、画面の裏で動くものを減らすため）。</summary>
        public void SetCameraEnabled(bool on) => _eye.enabled = on;

        /// <summary>
        /// **水平の画角を固定する。**縦横比に任せると、横長の画面で視界が広がり、
        /// 「3 対象が同時に視界へ入らない」（V-3 / D-11）が画面の形しだいで崩れる。
        /// 撮るとき（e2e）と画面に出すときの両方で、必ずこれを通す。
        /// </summary>
        private void FixHorizontalFieldOfView()
        {
            _eye.ResetAspect();
            _eye.fieldOfView = Camera.HorizontalToVerticalFieldOfView(FieldOfViewDeg, _eye.aspect);
        }

        private void Apply()
        {
            FixHorizontalFieldOfView();

            var t = Time.time;
            float sway, bob, shakeX = 0f, shakeY = 0f, roll = 0f;

            if (_present is Presentation p && !p.EyesClosed)
            {
                // 自分の体は視界の揺れで伝える（setting.md 8 節）。**元気が少ないほど小さく遅い**
                var s = p.BabyStrengthMilli / 1000f;
                var breathe = p.Baby == BabyMotion.Breathe ? 0.12f + s * 0.25f : 0.3f;
                var rate = p.Baby == BabyMotion.Breathe ? 1.0f + s * 0.9f : 1.9f;

                sway = breathe * Mathf.Sin(t * rate);
                bob = breathe * 0.6f * Mathf.Sin(t * rate * 0.7f);

                var shake = p.Baby switch
                {
                    BabyMotion.Charge => 0.25f + s * 1.1f,     // 溜めるほど揺れが大きい（ISS-19）
                    BabyMotion.Cry => 2.2f,                    // 泣くほど大きい
                    BabyMotion.Kick => 1.4f,
                    BabyMotion.Fuss => 0.8f,
                    _ => 0f,
                };

                shakeX = (Mathf.PerlinNoise(t * 17f, 0.3f) - 0.5f) * 2f * shake;
                shakeY = (Mathf.PerlinNoise(0.7f, t * 19f) - 0.5f) * 2f * shake * (p.Baby == BabyMotion.Kick ? 1.6f : 1f);

                // 抱っこ中の揺れは小さく（NFR-009 / SET-04。±3° 以内）
                roll = _lift * 3f * Mathf.Sin(t * 1.1f);
            }
            else
            {
                // **元気は視線に依存せず常に分かる**（REQ-006）
                var swayAmp = _vigorStage < 0 ? 0f : (2 - _vigorStage) * 0.35f;

                sway = swayAmp * Mathf.Sin(t * 1.9f);
                bob = swayAmp * 0.6f * Mathf.Sin(t * 1.3f);
            }

            _eye.transform.localRotation = _look * Quaternion.Euler(bob + shakeY, sway + shakeX, roll);
            _eyelid.gameObject.SetActive(EyesClosed);

            // **閉眼中に窓の明かりが見えるのは、窓を向いているときだけ**（V-9 / D-12）
            _windowPatch.gameObject.SetActive(EyesClosed && LookingAt(GazeTargetKind.Window));
        }

        /// <summary>
        /// 盲目区間（`Settling` / `Feint`）の動き（V-10 / ADR-0017 / ADR-0022）。
        ///
        /// **成功側と失敗側で呼び分けない。**呼び出し側（<see cref="RoomView"/>）が
        /// 「運ばれている」という 1 つの真偽値に潰してから渡す。
        /// 引数に親の状態そのものを取らないのは、**ここで分岐できないようにするため。**
        /// </summary>
        public void SetCarried(bool carried, int settleTicks)
        {
            _carried = carried;
            _settleTicks = settleTicks;

            if (_windowPatch != null && _windowPatch.gameObject.activeSelf)
            {
                // 抱き上げ → 揺らす → 置く。**動き方は成否に依存しない**
                var rise = carried ? Mathf.Sin(Mathf.Clamp01(settleTicks / 60f) * Mathf.PI) : 0f;
                var sway = carried ? Mathf.Sin(settleTicks * 0.12f) * 0.03f : 0f;

                _windowPatch.localPosition = new Vector3(sway, 0.1f - rise * 0.05f + sway * 0.5f, 0.04f);
            }
        }

        /// <summary>
        /// 段階を見せ方に写す（V-6 / REQ-044）。**-1 は出さない。**
        /// </summary>
        public void SetStages(int arousal, int vigor, int hand, int timeLeft)
        {
            _arousalStage = arousal;
            _vigorStage = vigor;
            _handStage = hand;
            _timeStage = timeLeft;
        }

        /// <summary>1 フレームの見せ方を受け取る（MOD-Present）。**動かすのは LateUpdate**。</summary>
        public void Present(Presentation p)
        {
            _present = p;

            if (p.Window != _appliedWindow)
            {
                ApplyWindow(p.Window);
            }

            if ((p.LampOn ? 1 : 0) != _appliedLamp)
            {
                ApplyLamp(p.LampOn);
            }

            if ((p.PhoneFlash ? 1 : 0) != _appliedPhone)
            {
                ApplyPhone(p.PhoneFlash);
            }
        }

        private void LateUpdate() => Step(Time.time, Time.deltaTime);

        /// <summary>
        /// 1 フレーム進める。**時刻を引数で受け取る**（エディタで止めた姿を撮るため。Editor/TenShots）。
        /// </summary>
        public void Step(float t, float dt)
        {
            if (_present is not Presentation p)
            {
                return;
            }

            // 抱っこ・抱き上げで視点が上がる（setting.md 5 節）。**なめらかに**（NFR-009）
            _lift = Mathf.MoveTowards(_lift, p.CameraLifted ? 1f : 0f, dt * 1.2f);
            _eye.transform.localPosition = new Vector3(0f, Mathf.Lerp(EyeHeight, LiftHeight, Smooth(_lift)), 0f);

            _bottleInFace.gameObject.SetActive(p.BottleInFace && !p.EyesClosed);
            _partner.gameObject.SetActive(p.PartnerHere && !p.EyesClosed);

            AnimateBody(p, t);
            AnimateHand(p, t);

            if (_motherAnimation != null && !Application.isPlaying)
            {
                SampleInEditor();
            }

            PoseHead(p, t, dt);
            PlaceVignette(p.EyesClosed ? 0 : p.Vignette);
        }

        /// <summary>体の層（覚醒度・寝入りばな）。**r56-approved の A0〜A3 / B**。</summary>
        private void AnimateBody(Presentation p, float t)
        {
            if (p.Body != _lastBody)
            {
                _lastBody = p.Body;
                _bodyStarted = t;
            }

            // 目を閉じているときも描かない（盲目区間の Carry を含む）
            var visible = p.Body is not (BodyClip.Hidden or BodyClip.Carry);

            foreach (var part in _motherParts)
            {
                part.enabled = visible;
            }

            if (!visible || _motherAnimation == null)
            {
                return;
            }

            var bt = t - _bodyStarted;

            // r57 の名前（presentation.md 2.1）。ループは継ぎ目なしに整形済み
            switch (p.Body)
            {
                case BodyClip.BreathDeep: Loop("Breath_Deep", t); break;
                case BodyClip.BreathLight: Loop("Breath_Light", t); break;
                case BodyClip.BreathHalf: Loop("Breath_Half", t); break;
                case BodyClip.BreathAwake: Loop("Breath_Awake", t); break;
                case BodyClip.DozeWarn: Hold("Doze_Warn", Mathf.Repeat(bt, 4.5f)); break;
                case BodyClip.DozeDrop: Hold("Doze_Drop", Mathf.Min(bt, 1.8f)); break;
                case BodyClip.TurnAway: Hold("TurnAway", bt); break;     // 1.5 秒で向き、そのまま保持
            }

            var sniff = _motherAnimation["Sniff"];

            if (sniff != null)
            {
                sniff.enabled = p.Sniff;
                sniff.weight = p.Sniff ? 1f : 0f;
                sniff.speed = 0f;
                sniff.time = Mathf.Repeat(t, sniff.length);
            }
        }

        /// <summary>手の層。**腕から先だけを上書きする**（顔の段階は体の層に残す）。</summary>
        private void AnimateHand(Presentation p, float t)
        {
            if (p.Hand != _lastHand || p.Reaching != _lastReaching)
            {
                _lastHand = p.Hand;
                _lastReaching = p.Reaching;
                _handStarted = t;
            }

            var ht = t - _handStarted;
            var bottle = false;
            var bottleT = 0f;
            string clip = null;
            var time = ht;
            IReadOnlyList<Transform> mix = _rightArm;

            switch (p.Hand)
            {
                // 予告（REQ-006）。**来る対処を手の形で持つ**（r57 の 4 形。差し出す途中で止めたポーズ）
                case HandClip.Reach:
                    clip = p.Reaching switch
                    {
                        Ten.Pure.CareKind.PatPat => "Reach_PatPat",
                        Ten.Pure.CareKind.Milk => "Reach_Milk",
                        Ten.Pure.CareKind.Hold => "Reach_Hold",
                        Ten.Pure.CareKind.DiaperChange => "Reach_Diaper",
                        _ => null,
                    };
                    time = Mathf.Min(ht, 1f);
                    break;

                case HandClip.PatSteady: clip = "Pat_Steady"; time = Mathf.Repeat(t, 4f); break;
                case HandClip.PatRough: clip = "Pat_Rough"; time = Mathf.Repeat(t, 4f); break;
                case HandClip.PatStall: clip = "Pat_Stall"; time = Mathf.Min(ht, 4f); break;
                case HandClip.CareMilk: clip = "Care_Milk"; time = Mathf.Min(ht, 3f); bottle = true; bottleT = ht; break;
                case HandClip.CareHold: clip = "Care_Hold"; time = Mathf.Min(ht, 3f); break;
                case HandClip.CareDiaper: clip = "Care_Diaper"; time = Mathf.Min(ht, 3f); break;
            }

            if (clip != null && UsesBothArms(clip))
            {
                mix = _bothArms;
            }

            var motherShown = _motherParts.Count > 0 && _motherParts[0].enabled;

            PlaceBottle(bottle && !p.BottleInFace && motherShown, bottleT);

            if (_reachBottle != null)
            {
                _reachBottle.gameObject.SetActive(motherShown && clip == "Reach_Milk");
            }

            if (_reachCloth != null)
            {
                _reachCloth.gameObject.SetActive(motherShown && clip == "Reach_Diaper");
            }

            if (_motherAnimation == null)
            {
                return;
            }

            foreach (AnimationState state in _motherAnimation)
            {
                if (state.layer == 1 && state.name != clip)
                {
                    state.enabled = false;
                    state.weight = 0f;
                }
            }

            if (clip == null)
            {
                return;
            }

            var hand = _motherAnimation[clip];

            if (hand == null)
            {
                return;
            }

            if (!_mixed.Contains(clip))
            {
                _mixed.Add(clip);

                foreach (var bone in mix)
                {
                    hand.AddMixingTransform(bone, true);
                }
            }

            hand.enabled = true;
            hand.weight = 1f;
            hand.speed = 0f;
            hand.time = time;
        }

        /// <summary>体の層（0）/ 手の層（1）/ 顔まわりに重ねる層（2）。</summary>
        private static int LayerOf(string clip) =>
            clip == "Sniff" ? 2
            : clip.StartsWith("Breath_") || clip.StartsWith("Doze_") || clip == "TurnAway" || clip[0] is 'A' or 'B' && clip.Length <= 2 ? 0
            : 1;

        /// <summary>両腕を使う手つき（抱っこ・オムツ替え）。</summary>
        private static bool UsesBothArms(string clip) =>
            clip is "Reach_Hold" or "Reach_Diaper" or "Care_Hold" or "Care_Diaper" or "D2" or "D3";

        private void Loop(string clip, float t)
        {
            var state = _motherAnimation[clip];

            if (state != null)
            {
                Hold(clip, Mathf.Repeat(t, state.length));
            }
        }

        /// <summary>体の層を 1 本だけ有効にして、その時刻の姿勢にする。**時刻は自分で持つ**（speed 0）。</summary>
        private void Hold(string clip, float time)
        {
            foreach (AnimationState state in _motherAnimation)
            {
                if (state.layer != 0)
                {
                    continue;
                }

                var on = state.name == clip;

                state.enabled = on;
                state.weight = on ? 1f : 0f;

                if (on)
                {
                    state.speed = 0f;
                    state.time = Mathf.Min(time, state.length);
                }
            }
        }

        /// <summary>
        /// **エディタで止めた姿を撮るときだけ使う。**Legacy の Animation は再生中でないと層を合成しないので、
        /// 手の層 → 体の層の順に直接サンプルし、腕から先だけを手の層の姿勢に戻す（再生中の合成と同じ結果）。
        /// </summary>
        private void SampleInEditor()
        {
            AnimationState body = null, hand = null;

            foreach (AnimationState state in _motherAnimation)
            {
                if (!state.enabled)
                {
                    continue;
                }

                if (state.layer == 0)
                {
                    body = state;
                }
                else if (state.layer == 1)
                {
                    hand = state;
                }
            }

            var arm = new List<(Transform Bone, Vector3 Position, Quaternion Rotation)>();

            if (hand != null)
            {
                hand.clip.SampleAnimation(_mother, hand.time);

                foreach (var root in UsesBothArms(hand.name) ? _bothArms : _rightArm)
                {
                    foreach (var bone in root.GetComponentsInChildren<Transform>())
                    {
                        arm.Add((bone, bone.localPosition, bone.localRotation));
                    }
                }
            }

            if (body != null)
            {
                body.clip.SampleAnimation(_mother, body.time);
            }

            foreach (var (bone, position, rotation) in arm)
            {
                bone.localPosition = position;
                bone.localRotation = rotation;
            }
        }

        /// <summary>
        /// 寝返りと鼻のひくつきは r57 のクリップが持つ。**クリップが無い古い FBX のときだけ首で代わりをする。**
        /// </summary>
        private void PoseHead(Presentation p, float t, float dt)
        {
            _turn = Mathf.MoveTowards(_turn, p.Body == BodyClip.TurnAway ? 1f : 0f, dt / 1.5f);

            if (_motherHead == null || _motherAnimation == null || _motherAnimation["TurnAway"] != null)
            {
                return;
            }

            var sniff = p.Sniff ? Mathf.Sin(t * 40f) * 1.5f : 0f;

            _motherHead.localRotation *= Quaternion.Euler(sniff, -75f * Smooth(_turn), 0f);
        }

        /// <summary>
        /// 手に持つ哺乳瓶。**Codex の配置式を Unity 座標に写した**（room-r1 README。Blender の Y と Z を入れ替える）。
        /// </summary>
        private void PlaceBottle(bool on, float t)
        {
            _bottleInHand.gameObject.SetActive(on);

            if (!on)
            {
                return;
            }

            var u = Mathf.Min(1f, t / 0.9f);
            var center = Vector3.Lerp(new Vector3(-0.29f, 0.85f, 0.04f), new Vector3(-0.046f, 0.408f, 0.003f), u);
            var rotation = Quaternion.Euler(0f, 0f, -160f);

            _bottleInHand.localRotation = rotation;
            _bottleInHand.localPosition = center + new Vector3(0f, 0.03f, 0f) - rotation * new Vector3(0f, 0.09f, 0f);
        }

        private static float Smooth(float x) => x * x * (3f - 2f * x);

        private void PlaceVignette(int level)
        {
            var on = level > 0;
            var depth = 0.06f;
            var halfH = depth * Mathf.Tan(_eye.fieldOfView * 0.5f * Mathf.Deg2Rad);
            var halfW = halfH * _eye.aspect;
            var f = level * 0.12f;

            for (var i = 0; i < _vignette.Length; i++)
            {
                _vignette[i].gameObject.SetActive(on);
            }

            if (!on)
            {
                return;
            }

            // 左・右・下・上。外側に余白を持たせ、揺れても縁が見えないようにする
            var bandW = 2f * f * halfW + halfW;
            var bandH = 2f * f * halfH + halfH;

            _vignette[0].localPosition = new Vector3(-halfW * 1.5f + f * halfW, 0f, depth);
            _vignette[0].localScale = new Vector3(bandW, halfH * 4f, 1f);
            _vignette[1].localPosition = new Vector3(halfW * 1.5f - f * halfW, 0f, depth);
            _vignette[1].localScale = new Vector3(bandW, halfH * 4f, 1f);
            _vignette[2].localPosition = new Vector3(0f, -halfH * 1.5f + f * halfH, depth);
            _vignette[2].localScale = new Vector3(halfW * 4f, bandH, 1f);
            _vignette[3].localPosition = new Vector3(0f, halfH * 1.5f - f * halfH, depth);
            _vignette[3].localScale = new Vector3(halfW * 4f, bandH, 1f);
        }

        // ------------------------------------------------------------------
        // 測る（e2e が使う）
        // ------------------------------------------------------------------

        /// <summary>いま視界に入っている対象の数。**実際の視錐台で測る**（V-3 / TC-122）。</summary>
        public int VisibleTargetCount() => VisibleTargets().Count;

        /// <summary>いま視界に入っている対象。**実際の視錐台で測る**（V-3 / TC-122）。</summary>
        public IReadOnlyList<GazeTargetKind> VisibleTargets()
        {
            FixHorizontalFieldOfView();

            var planes = GeometryUtility.CalculateFrustumPlanes(_eye);
            var visible = new List<GazeTargetKind>();

            for (var i = 0; i < _targets.Count; i++)
            {
                if (GeometryUtility.TestPlanesAABB(planes, _targets[i].Renderer.bounds))
                {
                    visible.Add(_targets[i].Kind);
                }
            }

            return visible;
        }

        /// <summary>
        /// **縦横比を指定して**視界に入っている対象を測る（ADR-0024 / TC-122）。
        /// 画面の縦横比に任せると、batchmode のように縦長でない画面では縦の画角が狭くなり、
        /// 対象端末の縦画面での見え方を測れない。
        /// </summary>
        public IReadOnlyList<GazeTargetKind> VisibleTargets(float aspect)
        {
            _eye.aspect = aspect;
            _eye.fieldOfView = Camera.HorizontalToVerticalFieldOfView(FieldOfViewDeg, aspect);

            var planes = GeometryUtility.CalculateFrustumPlanes(_eye);
            var visible = new List<GazeTargetKind>();

            for (var i = 0; i < _targets.Count; i++)
            {
                if (GeometryUtility.TestPlanesAABB(planes, _targets[i].Renderer.bounds))
                {
                    visible.Add(_targets[i].Kind);
                }
            }

            FixHorizontalFieldOfView();

            return visible;
        }

        private bool LookingAt(GazeTargetKind kind)
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(_eye);

            for (var i = 0; i < _targets.Count; i++)
            {
                if (_targets[i].Kind == kind)
                {
                    return GeometryUtility.TestPlanesAABB(planes, _targets[i].Renderer.bounds);
                }
            }

            return false;
        }

        /// <summary>いまの画面を撮る。**実際にレンダリングして画素を読む。**</summary>
        public Texture2D Capture()
        {
            if (_shotTarget == null)
            {
                _shotTarget = new RenderTexture(ShotSize, ShotSize, 24, RenderTextureFormat.ARGB32)
                {
                    name = "TenShot",
                };
                _shot = new Texture2D(ShotSize, ShotSize, TextureFormat.RGBA32, false);
            }

            var previous = RenderTexture.active;

            _eye.targetTexture = _shotTarget;
            _eye.aspect = 1f;
            _eye.fieldOfView = Camera.HorizontalToVerticalFieldOfView(FieldOfViewDeg, 1f);
            _eye.Render();

            RenderTexture.active = _shotTarget;
            _shot.ReadPixels(new Rect(0, 0, ShotSize, ShotSize), 0, 0, false);
            _shot.Apply(false, false);
            RenderTexture.active = previous;
            _eye.targetTexture = null;
            FixHorizontalFieldOfView();

            // **撮るたびに別のテクスチャを返す。**同じ参照を返すと TC-125 が
            // 「同じものを 2 回見ただけ」になり、比較になっていないことに気づけない
            var copy = new Texture2D(ShotSize, ShotSize, TextureFormat.RGBA32, false);

            copy.SetPixels32(_shot.GetPixels32());
            copy.Apply(false, false);

            return copy;
        }

        /// <summary>窓の領域を、いまの画面のどこに写しているか（ビューポート座標）。</summary>
        public Rect WindowViewportRect()
        {
            if (EyesClosed)
            {
                return _windowPatch.gameObject.activeSelf
                    ? RendererViewportRect(_windowPatch.GetComponent<Renderer>())
                    : Rect.zero;
            }

            for (var i = 0; i < _targets.Count; i++)
            {
                if (_targets[i].Kind == GazeTargetKind.Window)
                {
                    return RendererViewportRect(_targets[i].Renderer);
                }
            }

            return Rect.zero;
        }

        private Rect RendererViewportRect(Renderer renderer)
        {
            var bounds = renderer.bounds;
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);

            for (var i = 0; i < 8; i++)
            {
                var corner = new Vector3(
                    (i & 1) == 0 ? bounds.min.x : bounds.max.x,
                    (i & 2) == 0 ? bounds.min.y : bounds.max.y,
                    (i & 4) == 0 ? bounds.min.z : bounds.max.z);

                var p = _eye.WorldToViewportPoint(corner);

                if (p.z <= 0f)
                {
                    continue;
                }

                min = Vector2.Min(min, new Vector2(p.x, p.y));
                max = Vector2.Max(max, new Vector2(p.x, p.y));
            }

            return max.x < min.x ? Rect.zero : Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        /// <summary>
        /// **いま画面に出ているものを、そのままの縦横比で文字にする。**
        /// 撮った絵（<see cref="Capture"/>）は縦横比を 1 に固定するので、
        /// 「アプリで見えているもの」とは別物になる。食い違いを見るためだけの口。
        /// </summary>
        public string AsciiFrame(int cols, int rows)
        {
            var rt = RenderTexture.GetTemporary(cols * 4, rows * 4, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(cols * 4, rows * 4, TextureFormat.RGBA32, false);
            var previousTarget = _eye.targetTexture;
            var previousActive = RenderTexture.active;

            _eye.targetTexture = rt;
            _eye.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
            tex.Apply(false, false);
            RenderTexture.active = previousActive;
            _eye.targetTexture = previousTarget;

            var px = tex.GetPixels();
            var art = new System.Text.StringBuilder();

            for (var r = rows - 1; r >= 0; r--)
            {
                for (var c = 0; c < cols; c++)
                {
                    var p = px[(r * 4 + 2) * rt.width + c * 4 + 2];
                    var l = 0.2126f * p.linear.r + 0.7152f * p.linear.g + 0.0722f * p.linear.b;

                    art.Append(l > 0.12f ? '#' : l > 0.04f ? '+' : l > 0.012f ? '*' : l > 0.004f ? '.' : l > 0.0008f ? ':' : ' ');
                }

                art.Append('/');
            }

            DestroyImmediate(tex);
            RenderTexture.ReleaseTemporary(rt);

            return art.ToString();
        }

        /// <summary>まぶたの描画に半透明が混ざっているか（V-4 / TC-124）。</summary>
        public bool EyelidUsesAlphaBlend()
        {
            if (_eyelidMaterial == null)
            {
                return true;
            }

            if (_eyelidMaterial.renderQueue >= (int)UnityEngine.Rendering.RenderQueue.Transparent)
            {
                return true;
            }

            if (_eyelidMaterial.HasProperty("_Color") && _eyelidMaterial.color.a < 1f)
            {
                return true;
            }

            if (_eyelidMaterial.HasProperty("_SrcBlend")
                && (int)_eyelidMaterial.GetFloat("_SrcBlend") != (int)UnityEngine.Rendering.BlendMode.One)
            {
                return true;
            }

            return false;
        }
    }
}
