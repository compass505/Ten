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
    /// **母・部屋・小物の形は仮**（Codex の包みが入るまでの代役）。
    /// 差し替えの口は docs/20_basic_design/presentation.md 2 節。
    /// </summary>
    public sealed class RoomRig : MonoBehaviour
    {
        /// <summary>視界の水平画角。**3 対象が同時に入らない狭さ**（V-3 / D-11）。</summary>
        public const float FieldOfViewDeg = 30f;

        /// <summary>首の可動範囲（balance.md 9 節 / REQ-002）。</summary>
        public const float YawLimitDeg = 55f;

        /// <summary>対象の置き場（yaw）。**間隔が画角より広いので同時には入らない。**</summary>
        public const float WindowYawDeg = -45f;
        public const float FaceYawDeg = 0f;
        public const float HandYawDeg = 45f;

        /// <summary>初期視線。**どの対象にも向いていない**（screens.md 4.2.1: 初期視線は天井）。</summary>
        public const float InitialYawDeg = 22.5f;

        private const float TargetDistance = 4f;

        /// <summary>
        /// 対象の大きさ。**画角と間隔に対して、同時に 2 つ入らない大きさ**（V-3）。
        ///
        /// 視界の判定は軸に沿った箱（AABB）で行うので、**実際の形より一回り大きく見積もられる。**
        /// 45° 間隔・水平画角 30° に対して、見積もりを含めた半径が 20° を超えないこと。
        /// ここを大きくすると、天井を向いているのに親の手が「見えている」ことになる。
        /// </summary>
        private const float TargetSize = 0.35f;
        private const int ShotSize = 128;

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

        // --- 仮の姿（presentation.md 2 節の差し込み口に対応する） ---
        private Presentation? _present;
        private Transform _face;
        private Transform _hand;
        private Transform _torso;
        private Transform _eyeL;
        private Transform _eyeR;
        private Transform _mouth;
        private Transform _handTwin;
        private Transform _propPalm;
        private Transform _propBottle;
        private Transform _propCloth;
        private Transform _bottleInFace;
        private Transform _partner;
        private Renderer _faceRenderer;
        private Renderer _handRenderer;
        private Material _windowMaterial;
        private Material _lampMaterial;
        private Material _phoneMaterial;
        private readonly List<Renderer> _motherParts = new();
        private readonly List<(Material Material, Color Off, Color On)> _lampLit = new();
        private readonly Transform[] _vignette = new Transform[4];
        private Vector3 _faceBase;
        private Vector3 _handBase;
        private Vector3 _torsoBase;
        private BodyClip _lastBody = (BodyClip)(-1);
        private HandClip _lastHand = (HandClip)(-1);
        private float _bodyStarted;
        private float _handStarted;
        private float _lift;
        private int _appliedWindow = -1;
        private int _appliedLamp = -1;
        private int _appliedPhone = -1;

        /// <summary>いまの首の向き。</summary>
        public float YawDeg { get; private set; } = InitialYawDeg;

        public float PitchDeg { get; private set; }

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
            // 寝室は暗い（NFR-007）。環境光も落とす
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.01f, 0.01f, 0.014f, 1f);
            RenderSettings.fog = false;

            var eye = new GameObject("Eye");

            eye.transform.SetParent(transform, false);
            _eye = eye.AddComponent<Camera>();
            _eye.clearFlags = CameraClearFlags.SolidColor;
            _eye.backgroundColor = new Color(0.004f, 0.004f, 0.008f, 1f);
            _eye.fieldOfView = FieldOfViewDeg;
            _eye.nearClipPlane = 0.02f;
            _eye.farClipPlane = 50f;
            _eye.allowHDR = false;
            // 画面に出す。撮るとき（e2e）は targetTexture を差し替えて Render を呼ぶ
            _eye.enabled = true;

            // **窓は平たい箱。**Quad は片面しか描かないので、向きによっては消える
            BuildRoom();

            _targets.Add((GazeTargetKind.Window, MakeTarget("Window", WindowYawDeg, PrimitiveType.Cube,
                WindowColor(0), new Vector3(1.8f, 1.2f, 0.08f))));
            _targets.Add((GazeTargetKind.ParentFace, MakeTarget("ParentFace", FaceYawDeg, PrimitiveType.Sphere,
                new Color(0.42f, 0.38f, 0.39f, 1f), new Vector3(2f, 2f, 2f))));
            _targets.Add((GazeTargetKind.ParentHand, MakeTarget("ParentHand", HandYawDeg, PrimitiveType.Cube,
                new Color(0.34f, 0.30f, 0.31f, 1f))));

            _windowMaterial = _targets[0].Renderer.sharedMaterial;

            BuildEyelid();
            BuildMother();
            BuildProps();
            BuildVignette();
            BuildControls();
            Apply();
        }

        /// <summary>
        /// 寝室そのもの（天井・壁・床）。
        ///
        /// **これが無いと「暗い部屋」ではなく「虚空」になる。**
        /// 一人称で寝室にいること（REQ-001）は、視線を向けた先に何かがあることで初めて成り立つ。
        /// 首を振っても何も動かなければ、振っている実感も出ない。
        ///
        /// **暗いままにする**（NFR-007: 画面全体の相対輝度の平均が 0.1 以下）。
        /// 面は広いので、1 面の明るさは対象よりずっと低く取る。
        /// </summary>
        private void BuildRoom()
        {
            // 天井灯が点くと暖色に寄る（setting.md 7 節）。**点いても 0.1 を超えない明るさに留める**
            MakeSurface("Ceiling", new Vector3(0f, 4f, 2f), new Vector3(30f, 0.4f, 30f),
                new Color(0.17f, 0.17f, 0.20f, 1f), new Color(0.27f, 0.23f, 0.18f, 1f));

            MakeSurface("Floor", new Vector3(0f, -3f, 2f), new Vector3(30f, 0.4f, 30f),
                new Color(0.085f, 0.08f, 0.085f, 1f), new Color(0.14f, 0.12f, 0.10f, 1f));

            MakeSurface("BackWall", new Vector3(0f, 0f, 9f), new Vector3(30f, 14f, 0.4f),
                new Color(0.13f, 0.125f, 0.145f, 1f), new Color(0.21f, 0.18f, 0.15f, 1f));

            MakeSurface("LeftWall", new Vector3(-9f, 0f, 2f), new Vector3(0.4f, 14f, 30f),
                new Color(0.11f, 0.10f, 0.125f, 1f), new Color(0.18f, 0.155f, 0.13f, 1f));

            MakeSurface("RightWall", new Vector3(9f, 0f, 2f), new Vector3(0.4f, 14f, 30f),
                new Color(0.11f, 0.10f, 0.125f, 1f), new Color(0.18f, 0.155f, 0.13f, 1f));

            // 布団の縁。**自分がどこに寝ているかが分かる**（REQ-001）。
            // 寸法の正は setting.md（床の布団）。取り込み時に部屋ごと置き直す（handoff.md H-07）
            MakeSurface("FutonEdge", new Vector3(0f, -1.1f, 1.6f), new Vector3(6f, 0.12f, 0.12f),
                new Color(0.25f, 0.22f, 0.20f, 1f), new Color(0.33f, 0.29f, 0.25f, 1f));

            // 天井灯（setting.md 3 節）。**電球部分だけ色が変わる**（room 包みの `LampEmit`）
            var lamp = MakePart("Lamp", transform, PrimitiveType.Cylinder,
                new Vector3(1.6f, 3.76f, 5f), new Vector3(1.1f, 0.03f, 1.1f), new Color(0.10f, 0.10f, 0.11f, 1f));

            _lampMaterial = lamp.GetComponent<Renderer>().sharedMaterial;
        }

        /// <summary>部屋の面を 1 枚置く。**視線の対象ではない**（数に入れない）。</summary>
        private void MakeSurface(string name, Vector3 position, Vector3 scale, Color off, Color on)
        {
            var go = MakePart(name, transform, PrimitiveType.Cube, position, scale, off);

            _lampLit.Add((go.GetComponent<Renderer>().sharedMaterial, off, on));
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

        /// <summary>対象を 1 つ置く。**見た目は仮**（親の造形は後で差し替える）。</summary>
        private Renderer MakeTarget(string name, float yawDeg, PrimitiveType shape, Color color) =>
            MakeTarget(name, yawDeg, shape, color, Vector3.one);

        private Renderer MakeTarget(
            string name, float yawDeg, PrimitiveType shape, Color color, Vector3 shapeScale)
        {
            var rad = yawDeg * Mathf.Deg2Rad;
            var go = MakePart(name, transform, shape,
                new Vector3(Mathf.Sin(rad) * TargetDistance, 0f, Mathf.Cos(rad) * TargetDistance),
                shapeScale * TargetSize, color);

            go.transform.localRotation = Quaternion.Euler(0f, yawDeg + 180f, 0f);

            // **当たり判定は要らない**（物理を使わない）。
            // Physics モジュールを参照しないので、CreatePrimitive も Collider を付けない
            return go.GetComponent<Renderer>();
        }

        /// <summary>
        /// 仮の母（顔・髪・目・口・体・手）。**視線の対象は顔と手の 2 つだけ**で、ほかは数に入れない。
        /// 形は presentation.md 2.1 の見せ方を、Codex の母モデルが入るまで代わりに担う。
        /// </summary>
        private void BuildMother()
        {
            _faceRenderer = _targets[1].Renderer;
            _face = _faceRenderer.transform;
            _faceBase = _face.localPosition;
            _handRenderer = _targets[2].Renderer;
            _hand = _handRenderer.transform;
            _handBase = _hand.localPosition;

            _motherParts.Add(_faceRenderer);
            _motherParts.Add(_handRenderer);

            var hairColor = new Color(0.06f, 0.05f, 0.06f, 1f);
            var featureColor = new Color(0.07f, 0.05f, 0.06f, 1f);

            // 顔の前は +Z（対象は 180° 回してカメラに向けてある）
            _motherParts.Add(MakePart("Hair", _face, PrimitiveType.Sphere,
                new Vector3(0f, 0.10f, -0.07f), new Vector3(1.08f, 1.02f, 1.02f), hairColor).GetComponent<Renderer>());
            _motherParts.Add(MakePart("HairLong", _face, PrimitiveType.Cube,
                new Vector3(0f, -0.55f, -0.18f), new Vector3(1.0f, 1.0f, 0.45f), hairColor).GetComponent<Renderer>());

            _eyeL = MakePart("EyeL", _face, PrimitiveType.Cube,
                new Vector3(-0.17f, 0.06f, 0.47f), new Vector3(0.17f, 0.03f, 0.08f), featureColor).transform;
            _eyeR = MakePart("EyeR", _face, PrimitiveType.Cube,
                new Vector3(0.17f, 0.06f, 0.47f), new Vector3(0.17f, 0.03f, 0.08f), featureColor).transform;
            _mouth = MakePart("Mouth", _face, PrimitiveType.Cube,
                new Vector3(0f, -0.22f, 0.46f), new Vector3(0.15f, 0.02f, 0.08f), featureColor).transform;

            _motherParts.Add(_eyeL.GetComponent<Renderer>());
            _motherParts.Add(_eyeR.GetComponent<Renderer>());
            _motherParts.Add(_mouth.GetComponent<Renderer>());

            _torsoBase = _faceBase + new Vector3(0f, -1.05f, 0.25f);
            _torso = MakePart("Torso", transform, PrimitiveType.Cube,
                _torsoBase, new Vector3(0.95f, 1.3f, 0.55f), new Color(0.19f, 0.18f, 0.22f, 1f)).transform;
            _motherParts.Add(_torso.GetComponent<Renderer>());

            _handTwin = MakePart("HandTwin", transform, PrimitiveType.Cube,
                _handBase, Vector3.one * TargetSize, new Color(0.34f, 0.30f, 0.31f, 1f)).transform;
            _motherParts.Add(_handTwin.GetComponent<Renderer>());
        }

        /// <summary>
        /// 小物（SET-02: 動作に要る哺乳瓶とスマホだけ）と、もう一人の親（SET-03: 表情なしのシルエット）。
        /// </summary>
        private void BuildProps()
        {
            // 手の予告に持たせる形。**明度ではなく形で区別する**（REQ-044）
            _propPalm = MakePart("PropPalm", _hand, PrimitiveType.Cube,
                new Vector3(0f, 0.55f, 0f), new Vector3(1.4f, 0.25f, 1.1f), new Color(0.34f, 0.30f, 0.31f, 1f)).transform;
            _propBottle = MakePart("PropBottle", _hand, PrimitiveType.Cylinder,
                new Vector3(0f, 1.1f, 0f), new Vector3(0.5f, 0.8f, 0.5f), new Color(0.30f, 0.30f, 0.28f, 1f)).transform;
            _propCloth = MakePart("PropCloth", _hand, PrimitiveType.Cube,
                new Vector3(0f, 0.75f, 0f), new Vector3(1.8f, 0.12f, 1.3f), new Color(0.28f, 0.28f, 0.31f, 1f)).transform;

            // 対処「ミルク」: **視界がほぼ塞がる**（setting.md 5 節）。暗く保つ（NFR-007）
            _bottleInFace = MakePart("BottleInFace", _eye.transform, PrimitiveType.Cylinder,
                new Vector3(0.015f, -0.01f, 0.23f), new Vector3(0.12f, 0.14f, 0.12f), new Color(0.20f, 0.20f, 0.19f, 1f)).transform;
            _bottleInFace.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // スマホ（`PhoneEmit`）。枕元の床
            var phone = MakePart("Phone", transform, PrimitiveType.Cube,
                _faceBase + new Vector3(-0.75f, -1.75f, -0.3f), new Vector3(0.32f, 0.04f, 0.55f), new Color(0.03f, 0.03f, 0.035f, 1f));

            phone.transform.localRotation = Quaternion.Euler(-35f, 0f, 0f);
            _phoneMaterial = phone.GetComponent<Renderer>().sharedMaterial;

            // もう一人の親。**窓の側に座る暗い影**。窓は隠さない
            _partner = new GameObject("Partner").transform;
            _partner.SetParent(transform, false);

            var rad = -22f * Mathf.Deg2Rad;

            _partner.localPosition = new Vector3(Mathf.Sin(rad) * 5.5f, 0f, Mathf.Cos(rad) * 5.5f);

            var silhouette = new Color(0.035f, 0.035f, 0.045f, 1f);

            MakePart("PartnerHead", _partner, PrimitiveType.Sphere, new Vector3(0f, -0.2f, 0f), Vector3.one * 0.75f, silhouette);
            MakePart("PartnerBody", _partner, PrimitiveType.Cube, new Vector3(0f, -1.45f, 0f), new Vector3(1.3f, 1.7f, 0.6f), silhouette);

            _partner.gameObject.SetActive(false);
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

            // 近接面を覆いきる大きさ
            var height = 2f * 0.05f * Mathf.Tan(FieldOfViewDeg * 0.5f * Mathf.Deg2Rad);

            lid.transform.localScale = new Vector3(height * 4f, height * 4f, 1f);

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

        /// <summary>窓の色（setting.md 7 節: 濃紺 → 白む → 夜明け直前が最も明るい）。</summary>
        private static Color WindowColor(int level) => level switch
        {
            0 => new Color(0.07f, 0.08f, 0.19f, 1f),
            1 => new Color(0.09f, 0.10f, 0.22f, 1f),
            2 => new Color(0.12f, 0.13f, 0.26f, 1f),
            3 => new Color(0.18f, 0.20f, 0.33f, 1f),
            4 => new Color(0.30f, 0.33f, 0.45f, 1f),
            _ => new Color(0.52f, 0.55f, 0.64f, 1f),
        };

        private static Color PatchColor(int level) => WindowColor(level) * 0.75f + new Color(0f, 0f, 0f, 0.25f);

        // ------------------------------------------------------------------
        // 見せる
        // ------------------------------------------------------------------

        /// <summary>首を振る。可動範囲で止める（REQ-002）。</summary>
        public void LookAt(float yawDeg, float pitchDeg)
        {
            YawDeg = Mathf.Clamp(yawDeg, -YawLimitDeg, YawLimitDeg);
            PitchDeg = Mathf.Clamp(pitchDeg, -15f, 30f);
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

            _eye.transform.localRotation = Quaternion.Euler(-PitchDeg + bob + shakeY, YawDeg + sway + shakeX, roll);
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
        /// 明度だけに頼らず、輪郭（大きさ）・姿勢（傾き）・動きの周期で冗長化する。
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
                _appliedWindow = p.Window;
                _windowMaterial.color = WindowColor(p.Window);
                _windowPatchMaterial.color = PatchColor(p.Window);
            }

            var lamp = p.LampOn ? 1 : 0;

            if (lamp != _appliedLamp)
            {
                _appliedLamp = lamp;
                _lampMaterial.color = p.LampOn ? new Color(0.95f, 0.78f, 0.52f, 1f) : new Color(0.10f, 0.10f, 0.11f, 1f);

                foreach (var (material, off, on) in _lampLit)
                {
                    material.color = p.LampOn ? on : off;
                }
            }

            var phone = p.PhoneFlash ? 1 : 0;

            if (phone != _appliedPhone)
            {
                _appliedPhone = phone;
                _phoneMaterial.color = p.PhoneFlash ? new Color(0.62f, 0.74f, 0.92f, 1f) : new Color(0.03f, 0.03f, 0.035f, 1f);
            }
        }

        private void LateUpdate()
        {
            if (_present is not Presentation p)
            {
                return;
            }

            var t = Time.time;
            var dt = Time.deltaTime;

            // 抱っこ・抱き上げで視点が上がる（setting.md 5 節）。**なめらかに**（NFR-009）
            _lift = Mathf.MoveTowards(_lift, p.CameraLifted ? 1f : 0f, dt * 1.2f);
            _eye.transform.localPosition = new Vector3(0f, Smooth(_lift) * 0.6f, -Smooth(_lift) * 0.3f);

            _bottleInFace.gameObject.SetActive(p.BottleInFace && !p.EyesClosed);
            _partner.gameObject.SetActive(p.PartnerHere && !p.EyesClosed);

            AnimateBody(p, t);
            AnimateHand(p, t);
            PlaceVignette(p.EyesClosed ? 0 : p.Vignette);
        }

        private void AnimateBody(Presentation p, float t)
        {
            if (p.Body != _lastBody)
            {
                _lastBody = p.Body;
                _bodyStarted = t;
            }

            var visible = p.Body is not (BodyClip.Hidden or BodyClip.Carry);

            foreach (var part in _motherParts)
            {
                part.enabled = visible;
            }

            if (!visible)
            {
                return;
            }

            var bt = t - _bodyStarted;
            var offset = Vector3.zero;
            float tilt = 0f, turn = 0f, eyeOpen = 0f, mouthOpen = 0.02f, squash = 1f, period = 3f, amp = 0.02f;

            switch (p.Body)
            {
                case BodyClip.BreathDeep:      // 熟睡: 頭が後ろに落ち、口が開く。こちらを向いていない
                    tilt = -18f; turn = 25f; mouthOpen = 0.09f; period = 4f; amp = 0.035f;
                    break;

                case BodyClip.BreathLight:     // 浅い: 閉じているが眉が動く
                    tilt = -8f; turn = 12f; period = 3f; amp = 0.022f;
                    offset.y += Mathf.Sin(t * 9f) > 0.96f ? 0.01f : 0f;
                    break;

                case BodyClip.BreathHalf:      // 半分起きる: 薄く開く。こちらを見ようとする
                    eyeOpen = 0.35f; period = 2f; amp = 0.014f;
                    break;

                case BodyClip.BreathAwake:     // 覚醒: 開く。上体を起こす。呼吸は不規則
                    tilt = 6f; eyeOpen = 1f; offset.y += 0.22f; period = 1.3f;
                    amp = 0.01f * (1f + Mathf.Sin(t * 0.37f));
                    break;

                case BodyClip.DozeWarn:        // 予兆: 呼吸が深く遅くなる
                    tilt = -10f; period = 4.5f; amp = 0.06f;
                    break;

                case BodyClip.DozeDrop:        // 寝入りばな: かくっと首が落ちる
                {
                    var k = Mathf.Clamp01(bt / 0.22f);

                    offset.y -= 0.28f * k; tilt = 28f * k; squash = 1f - 0.08f * k; mouthOpen = 0.05f;
                    period = 4f; amp = 0.01f;
                    break;
                }

                case BodyClip.TurnAway:        // 寝返り: 顔が向こうを向く
                    turn = 180f; period = 3.5f; amp = 0.02f;
                    break;
            }

            var breath = Mathf.Sin(t * Mathf.PI * 2f / period) * amp;

            if (p.Sniff)
            {
                offset.y += Mathf.Sin(t * 40f) * 0.006f;
                mouthOpen = 0.035f;
            }

            _face.localPosition = _faceBase + offset + Vector3.up * breath;
            _face.localRotation = Quaternion.Euler(tilt, FaceYawDeg + 180f + turn, 0f);
            _face.localScale = new Vector3(2f, 2f * squash, 2f) * TargetSize;

            var features = turn < 90f;

            _eyeL.gameObject.SetActive(features);
            _eyeR.gameObject.SetActive(features);
            _mouth.gameObject.SetActive(features);

            var eyeScale = new Vector3(0.17f, 0.02f + eyeOpen * 0.09f, 0.08f);

            _eyeL.localScale = eyeScale;
            _eyeR.localScale = eyeScale;
            _mouth.localScale = new Vector3(0.15f, mouthOpen, 0.08f);

            _torso.localPosition = _torsoBase + new Vector3(0f, offset.y * 0.6f + breath * 0.6f, 0f);
            _torso.localScale = new Vector3(0.95f, p.Body == BodyClip.BreathAwake ? 1.45f : 1.3f, 0.55f);
        }

        private void AnimateHand(Presentation p, float t)
        {
            if (p.Hand != _lastHand)
            {
                _lastHand = p.Hand;
                _handStarted = t;
            }

            var ht = t - _handStarted;
            var toward = _handBase * 0.75f;
            var position = _handBase;
            var twin = false;

            _propPalm.gameObject.SetActive(false);
            _propBottle.gameObject.SetActive(false);
            _propCloth.gameObject.SetActive(false);

            switch (p.Hand)
            {
                case HandClip.Rest:
                    position += Vector3.up * Mathf.Sin(t * 0.8f) * 0.02f;
                    break;

                case HandClip.Reach:           // 次の対処の予告。**何が来るかを形で持つ**（REQ-006）
                {
                    position = Vector3.Lerp(_handBase, toward + Vector3.up * 0.25f, Smooth(Mathf.Clamp01(ht / 0.8f)));

                    switch (p.Reaching)
                    {
                        case Ten.Pure.CareKind.PatPat: _propPalm.gameObject.SetActive(true); break;
                        case Ten.Pure.CareKind.Milk: _propBottle.gameObject.SetActive(true); break;
                        case Ten.Pure.CareKind.Hold: twin = true; break;
                        case Ten.Pure.CareKind.DiaperChange: _propCloth.gameObject.SetActive(true); break;
                    }

                    break;
                }

                case HandClip.PatSteady:       // 序盤: 規則正しい
                    position = toward + Vector3.up * Pat(t * 1.7f) * 0.25f;
                    break;

                case HandClip.PatRough:        // 中盤: リズムが乱れる、雑になる
                    position = toward + Vector3.up * Pat(t * 1.5f + 0.8f * Mathf.Sin(t * 0.9f)) * (0.14f + 0.12f * Mathf.Abs(Mathf.Sin(t * 0.6f)));
                    break;

                case HandClip.PatStall:        // 終盤: 手が途中で止まる
                {
                    var cycle = Mathf.Repeat(t, 3.2f);

                    position = toward + Vector3.up * (cycle < 1.1f ? Pat(t * 1.3f) * 0.2f : 0.12f);
                    break;
                }

                case HandClip.CareMilk:
                    position = _handBase * 0.55f;
                    break;

                case HandClip.CareHold:
                    position = toward + Vector3.up * 0.4f;
                    twin = true;
                    break;

                case HandClip.CareDiaper:      // 下半身側で手が動く
                    position = _handBase + Vector3.down * 1.3f + Vector3.right * Mathf.Sin(t * 2.2f) * 0.25f;
                    _propCloth.gameObject.SetActive(true);
                    break;
            }

            _hand.localPosition = position;
            _hand.localRotation = Quaternion.Euler(Mathf.Sin(t * 1.4f) * 6f, HandYawDeg + 180f, 0f);

            _handTwin.gameObject.SetActive(twin);
            _handTwin.localPosition = position + new Vector3(-0.55f, 0.05f, 0f);
        }

        /// <summary>トントンの 1 拍（上がって、すっと下りる）。</summary>
        private static float Pat(float phase)
        {
            var s = Mathf.Sin(phase * Mathf.PI * 2f);

            return s > 0f ? s * s : 0f;
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

            Destroy(tex);
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
