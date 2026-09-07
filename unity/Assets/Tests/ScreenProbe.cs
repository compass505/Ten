using System;
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
    /// **これはシグネチャだけの空実装である**（test_first.md 5.1）。
    /// 実体はフェーズ 5 で `MOD-View` と一緒に書く。
    /// </summary>
    public static class ScreenProbe
    {
        private const string NotYet = "フェーズ 5（実装）で書く（test_first.md 5.1）";

        /// <summary>いまの画面を撮る。</summary>
        public static Texture2D Capture() => throw new NotImplementedException(NotYet);

        /// <summary>
        /// 画素の相対輝度の平均（NFR-007）。
        /// sRGB を線形に戻してから 0.2126R + 0.7152G + 0.0722B。
        /// </summary>
        public static float AverageRelativeLuminance(Texture2D shot) =>
            throw new NotImplementedException(NotYet);

        /// <summary>
        /// **窓の領域以外に、寝室由来の輝度差があるか**（REQ-004 / TC-123）。
        /// 暗転が半透明だと、ここで拾える。
        /// </summary>
        public static bool HasRoomDetailOutsideWindow(Texture2D shot) =>
            throw new NotImplementedException(NotYet);

        /// <summary>閉眼中の描画に半透明の重ねが使われているか（V-4 / TC-124）。</summary>
        public static bool UsesAlphaBlendWhileEyesClosed() => throw new NotImplementedException(NotYet);

        /// <summary>数値・ゲージ・アイコンが画面に出ているか（REQ-044 / TC-126）。</summary>
        public static int CountNumericOrGaugeElements() => throw new NotImplementedException(NotYet);

        /// <summary>画面の下半分だけで到達できない操作の数（REQ-005 / TC-129）。</summary>
        public static int CountControlsOutOfLowerHalf() => throw new NotImplementedException(NotYet);

        /// <summary>同時接触を要求する操作の数（REQ-005 / TC-129）。</summary>
        public static int CountMultiTouchControls() => throw new NotImplementedException(NotYet);

        /// <summary>首をその向きに振る。</summary>
        public static void LookAt(float yawDeg, float pitchDeg) => throw new NotImplementedException(NotYet);

        /// <summary>視界に入っている対象の数（D-11 / TC-122）。</summary>
        public static int VisibleTargetCount() => throw new NotImplementedException(NotYet);

        /// <summary>2 枚の画面が画素として同一か（REQ-016 / TC-125）。</summary>
        public static bool PixelsEqual(Texture2D a, Texture2D b) => throw new NotImplementedException(NotYet);
    }
}
