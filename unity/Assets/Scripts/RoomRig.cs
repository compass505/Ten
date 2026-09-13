using System.Collections.Generic;
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
    /// ここには判断を置かない（V-1）。状態を受け取って見せるだけ。
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
            _targets.Add((GazeTargetKind.Window, MakeTarget("Window", WindowYawDeg, PrimitiveType.Cube,
                new Color(0.55f, 0.57f, 0.72f, 1f), new Vector3(1.8f, 1.2f, 0.08f))));
            _targets.Add((GazeTargetKind.ParentFace, MakeTarget("ParentFace", FaceYawDeg, PrimitiveType.Sphere,
                new Color(0.38f, 0.35f, 0.36f, 1f), new Vector3(1.5f, 1.5f, 1.5f))));
            _targets.Add((GazeTargetKind.ParentHand, MakeTarget("ParentHand", HandYawDeg, PrimitiveType.Cube,
                new Color(0.34f, 0.30f, 0.31f, 1f))));

            BuildEyelid();
            BuildControls();
            Apply();
        }

        /// <summary>対象を 1 つ置く。**見た目は仮**（親の造形は後で差し替える）。</summary>
        private Renderer MakeTarget(string name, float yawDeg, PrimitiveType shape, Color color) =>
            MakeTarget(name, yawDeg, shape, color, Vector3.one);

        private Renderer MakeTarget(
            string name, float yawDeg, PrimitiveType shape, Color color, Vector3 shapeScale)
        {
            var go = GameObject.CreatePrimitive(shape);

            go.name = name;
            go.transform.SetParent(transform, false);

            var rad = yawDeg * Mathf.Deg2Rad;

            go.transform.localPosition = new Vector3(Mathf.Sin(rad) * TargetDistance, 0f, Mathf.Cos(rad) * TargetDistance);
            go.transform.localRotation = Quaternion.Euler(0f, yawDeg + 180f, 0f);
            go.transform.localScale = shapeScale * TargetSize;

            var renderer = go.GetComponent<Renderer>();

            renderer.sharedMaterial = OpaqueMaterial(color);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            // **当たり判定は要らない**（物理を使わない）。
            // Physics モジュールを参照しないので、CreatePrimitive も Collider を付けない


            return renderer;
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

            _windowPatchMaterial = OpaqueMaterial(new Color(0.20f, 0.21f, 0.30f, 1f));

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

            // **元気は視線に依存せず常に分かる**（REQ-006）。自分の体なので、視界の揺れで出す。
            // 元気が減るほど大きく揺れる（setting.md 8 節）
            var swayAmp = _vigorStage < 0 ? 0f : (2 - _vigorStage) * 0.35f;
            var sway = swayAmp * Mathf.Sin(Time.time * 1.9f);
            var bob = swayAmp * 0.6f * Mathf.Sin(Time.time * 1.3f);

            _eye.transform.localRotation = Quaternion.Euler(-PitchDeg + bob, YawDeg + sway, 0f);
            _eyelid.gameObject.SetActive(EyesClosed);

            // **閉眼中に窓の明かりが見えるのは、窓を向いているときだけ**（V-9 / D-12）
            _windowPatch.gameObject.SetActive(EyesClosed && LookingAt(GazeTargetKind.Window));
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
        /// 盲目区間（`Settling` / `Feint`）の動き（V-10 / ADR-0017）。
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
                // 運ばれている間は明かりの見え方が動く。**動き方は成否に依存しない**
                var sway = carried ? Mathf.Sin(settleTicks * 0.12f) * 0.03f : 0f;

                _windowPatch.localPosition = new Vector3(sway, 0.1f + sway * 0.5f, 0.04f);
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

        private void LateUpdate()
        {
            if (EyesClosed)
            {
                return;
            }

            var t = Time.time;

            for (var i = 0; i < _targets.Count; i++)
            {
                var tr = _targets[i].Renderer.transform;

                if (_targets[i].Kind == GazeTargetKind.ParentFace && _arousalStage >= 0)
                {
                    // 覚醒度が高いほど呼吸が速く、姿勢が起きる（明度差に頼らない）
                    var cycle = 0.6f + _arousalStage * 0.6f;

                    tr.localScale = Vector3.one * (TargetSize + Mathf.Sin(t * cycle) * 0.01f);
                    tr.localRotation = Quaternion.Euler(_arousalStage * -6f, FaceYawDeg + 180f, 0f);
                }
                else if (_targets[i].Kind == GazeTargetKind.ParentHand && _handStage >= 0)
                {
                    var cycle = 0.5f + _handStage * 0.4f;

                    tr.localRotation = Quaternion.Euler(Mathf.Sin(t * cycle) * 6f, HandYawDeg + 180f, 0f);
                }
            }
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
