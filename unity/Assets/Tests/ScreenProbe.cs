using System;
using Ten.View;
using UnityEngine;

namespace Ten.Tests.E2E
{
    /// <summary>
    /// e2e が画面を見るための口。
    ///
    /// **「実際に見えているか」は撮って測るしかない。**
    /// REQ-004（目を閉じたら寝室の像が消える）と NFR-007（相対輝度 0.1 以下）は、
    /// 実装の主張ではなく**画素**で確かめる。
    /// balance.md 11 節で「暗転が半透明だった」を実際に踏んでいる。
    ///
    /// **ここに答えを書かない。**すべて <see cref="RoomRig"/> を実際に描いて測る。
    /// 定数を返すと、テストは緑になるが**何も測っていない**
    /// （DeviceNfrTests の「黙って通すと『測っていないのに緑』になる」と同じ事故）。
    /// </summary>
    public static class ScreenProbe
    {
        /// <summary>寝室由来の輝度差と見なす下限（線形輝度）。**撮影の丸め誤差より大きく取る。**</summary>
        private const float RoomDetailEpsilon = 0.003f;

        /// <summary>窓の領域を除くときの余白（ビューポート比）。輪郭の 1 画素ぶんを逃がす。</summary>
        private const float WindowMargin = 0.03f;

        private static RoomRig Rig => RoomRig.Instance;

        /// <summary>いまの画面を撮る。**実際にレンダリングして画素を読む。**</summary>
        public static Texture2D Capture() => Rig.Capture();

        /// <summary>
        /// 画素の相対輝度の平均（NFR-007）。
        /// sRGB を線形に戻してから 0.2126R + 0.7152G + 0.0722B。
        /// </summary>
        public static float AverageRelativeLuminance(Texture2D shot)
        {
            if (shot == null)
            {
                throw new ArgumentNullException(nameof(shot));
            }

            var pixels = shot.GetPixels();

            if (pixels.Length == 0)
            {
                return 0f;
            }

            var sum = 0f;

            for (var i = 0; i < pixels.Length; i++)
            {
                sum += Luminance(pixels[i]);
            }

            return sum / pixels.Length;
        }

        /// <summary>
        /// **窓の領域以外に、寝室由来の輝度差があるか**（REQ-004 / TC-123）。
        /// 暗転が半透明だと、ここで拾える。
        /// </summary>
        public static bool HasRoomDetailOutsideWindow(Texture2D shot)
        {
            if (shot == null)
            {
                throw new ArgumentNullException(nameof(shot));
            }

            var window = Rig.WindowViewportRect();
            var pixels = shot.GetPixels();
            var width = shot.width;
            var height = shot.height;

            var min = float.MaxValue;
            var max = float.MinValue;
            var seen = 0;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var u = (x + 0.5f) / width;
                    var v = (y + 0.5f) / height;

                    if (window.width > 0f
                        && u >= window.xMin - WindowMargin && u <= window.xMax + WindowMargin
                        && v >= window.yMin - WindowMargin && v <= window.yMax + WindowMargin)
                    {
                        continue;
                    }

                    var l = Luminance(pixels[y * width + x]);

                    min = Mathf.Min(min, l);
                    max = Mathf.Max(max, l);
                    seen++;
                }
            }

            // 窓以外が 1 画素も残らないなら、測れていない。**黙って通さない**
            if (seen == 0)
            {
                throw new InvalidOperationException(
                    "窓の領域が画面全体を覆っていて、寝室の像を測れない。撮り方か配置を疑う");
            }

            return max - min > RoomDetailEpsilon;
        }

        /// <summary>閉眼中の描画に半透明の重ねが使われているか（V-4 / TC-124）。</summary>
        public static bool UsesAlphaBlendWhileEyesClosed()
        {
            Rig.SetEyesClosed(true);

            return Rig.EyelidUsesAlphaBlend();
        }

        /// <summary>数値・ゲージ・アイコンが画面に出ているか（REQ-044 / TC-126）。</summary>
        public static int CountNumericOrGaugeElements()
        {
            var count = 0;

            foreach (var mesh in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
            {
                if (mesh.gameObject.activeInHierarchy)
                {
                    count++;
                }
            }

            // UI の実体は別アセンブリにあるので、**型の名前で数える**
            // （asmdef の参照を増やさずに、実際に置かれているものを見る）
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                foreach (var component in canvas.GetComponentsInChildren<Component>(includeInactive: false))
                {
                    if (component == null)
                    {
                        continue;
                    }

                    var ns = component.GetType().Namespace;

                    if (ns != null && ns.StartsWith("UnityEngine.UI", StringComparison.Ordinal))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>画面の下半分だけで到達できない操作の数（REQ-005 / TC-129）。</summary>
        public static int CountControlsOutOfLowerHalf()
        {
            var controls = Rig.Controls;
            var count = 0;

            for (var i = 0; i < controls.Count; i++)
            {
                if (controls[i].yMax > 0.5f)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>同時接触を要求する操作の数（REQ-005 / TC-129）。</summary>
        public static int CountMultiTouchControls() => Rig.MultiTouchControlCount;

        /// <summary>首をその向きに振る。</summary>
        public static void LookAt(float yawDeg, float pitchDeg) => Rig.LookAt(yawDeg, pitchDeg);

        /// <summary>視界に入っている対象の数（D-11 / TC-122）。</summary>
        public static int VisibleTargetCount() => Rig.VisibleTargetCount();

        /// <summary>対象端末の縦画面（NFR-001 で固定。1080 × 2400）の縦横比。</summary>
        private const float PortraitAspect = 1080f / 2400f;

        /// <summary>縦画面で、窓が視界に入っているか（ADR-0024 / TC-122）。</summary>
        public static bool SeesWindowOnPortrait()
        {
            foreach (var kind in Rig.VisibleTargets(PortraitAspect))
            {
                if (kind == RoomRig.GazeTargetKind.Window)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>縦画面で、母の顔か手が視界に入っているか（ADR-0024 / TC-122）。</summary>
        public static bool SeesMotherOnPortrait()
        {
            foreach (var kind in Rig.VisibleTargets(PortraitAspect))
            {
                if (kind is RoomRig.GazeTargetKind.ParentFace or RoomRig.GazeTargetKind.ParentHand)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>2 枚の画面が画素として同一か（REQ-016 / TC-125）。</summary>
        public static bool PixelsEqual(Texture2D a, Texture2D b)
        {
            if (a == null || b == null)
            {
                throw new ArgumentNullException(a == null ? nameof(a) : nameof(b));
            }

            if (ReferenceEquals(a, b))
            {
                throw new InvalidOperationException(
                    "同じテクスチャを 2 回渡している。**比較になっていない**（TC-125）");
            }

            if (a.width != b.width || a.height != b.height)
            {
                return false;
            }

            var x = a.GetPixels32();
            var y = b.GetPixels32();

            for (var i = 0; i < x.Length; i++)
            {
                if (!x[i].Equals(y[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>sRGB の画素を線形に戻して相対輝度にする。</summary>
        private static float Luminance(Color c) =>
            0.2126f * c.linear.r + 0.7152f * c.linear.g + 0.0722f * c.linear.b;
    }
}
