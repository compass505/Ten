using System;
using System.Collections.Generic;

namespace Ten.Boundary;

/// <summary>視線の対象（screens.md 4.2.1 / D-11）。</summary>
public enum GazeTarget
{
    /// <summary>窓 — 残り時間が分かる</summary>
    Window,

    /// <summary>親の顔 — 覚醒度が分かる</summary>
    ParentFace,

    /// <summary>親の手 — 次に来る対処と、今している対処が分かる</summary>
    ParentHand,

    /// <summary>天井（どれでもない） — 何も分からない</summary>
    Ceiling,
}

/// <summary>
/// 対象が見える首の角度の区間（setting.md 3 節）。
///
/// **値は setting.md / balance.md にある。**
/// [test_first.md](../../docs/00_process/test_first.md) 4.1 が「首の可動角度」を
/// バランス値としているので、**ここに既定値を書かず外から渡す**（ADR-0012）。
/// </summary>
public readonly record struct GazeBand(GazeTarget Target, float MinYawDeg, float MaxYawDeg);

/// <summary>
/// **視線と情報の対応**（D-11 / REQ-006）。
///
/// > 3 つは同時に視界へ入らない。この配置が「見ることが選択になる」の根拠。
///
/// **遮蔽の判断は表示層（MOD-View V-3）が行うが、
/// 「区間が重ならない」という配置の性質はここで見られる。**
///
/// **これはシグネチャだけの空実装である**（test_first.md 5.1）。
/// </summary>
public static class GazeRule
{
    private const string NotYet = "フェーズ 5（実装）で書く（test_first.md 5.1）";

    /// <summary>その首の向きで見えている対象。どれでもなければ <see cref="GazeTarget.Ceiling"/>。</summary>
    public static GazeTarget At(float yawDeg, IReadOnlyList<GazeBand> bands) =>
        throw new NotImplementedException(NotYet);

    /// <summary>
    /// 2 つ以上の対象が同時に見える角度があるか（**あってはいけない**。D-11 / V-3）。
    /// </summary>
    public static bool HasOverlap(IReadOnlyList<GazeBand> bands) =>
        throw new NotImplementedException(NotYet);
}

/// <summary>
/// 段階を「見せ方」に写す（V-6 / REQ-044）。
///
/// **明度差だけに依存しない。**輪郭・姿勢・動きの周期で冗長化する。
/// グレースケールにしても段階が区別できることが条件（TC-127）。
/// </summary>
/// <param name="Silhouette">輪郭の種類</param>
/// <param name="Posture">姿勢の種類</param>
/// <param name="CycleMilli">動きの周期（1/1000 秒）。0 は静止</param>
/// <param name="Brightness">明度。**これだけに頼らない**</param>
public readonly record struct Look(int Silhouette, int Posture, int CycleMilli, int Brightness);

/// <summary>
/// **段階 → 見せ方**。表示層が使う写像だが、判断が無いのでここで検証できる。
///
/// **これはシグネチャだけの空実装である**（test_first.md 5.1）。
/// </summary>
public static class LookRule
{
    private const string NotYet = "フェーズ 5（実装）で書く（test_first.md 5.1）";

    /// <summary>
    /// 段階に対応する見せ方。**`stage` が -1（見えない）なら null を返す。**
    /// 推測して埋めない（MOD-View のエラー時 / TC-130）。
    /// </summary>
    public static Look? Of(GazeTarget target, int stage) => throw new NotImplementedException(NotYet);

    /// <summary>その対象が持つ段階の数。</summary>
    public static int StageCount(GazeTarget target) => throw new NotImplementedException(NotYet);
}
