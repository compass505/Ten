using System;
using Ten.Pure;

namespace Ten.Boundary;

/// <summary>チュートリアルの段（REQ-034。**3 つだけ**）。</summary>
public enum TutorialStep
{
    /// <summary>覚醒度 — 行動で上がり、待つと下がる。低いほど効く</summary>
    Arousal,

    /// <summary>元気 — 行動で減り、待つと回復する。尽きると動けない</summary>
    Vigor,

    /// <summary>寝たふり — 目を閉じ、置かれた直後に泣く</summary>
    Pretend,
}

/// <summary>
/// MOD-Tutorial — 初回起動の導線。
///
/// 仕様: docs/30_detailed_design/MOD-Tutorial.md（TU-1〜TU-5）/ REQ-034 / D-09
/// </summary>
public interface ITutorial
{
    /// <summary>進行中の段。完了したら null。</summary>
    TutorialStep? Current { get; }

    /// <summary>状態を見て、条件を満たしたら次へ。</summary>
    void Advance(NightState s);

    bool IsDone { get; }
}

/// <summary>
/// **段が進む条件。**状態だけを見る純関数。
///
/// **説明文を読ませるのではなく、操作しながら知る**（TU-1）。
/// なので条件はすべて「達成したか」で、「読んだか」ではない。
///
/// **これはシグネチャだけの空実装である**（test_first.md 5.1）。
/// </summary>
public static class TutorialRule
{
    private const string NotYet = "フェーズ 5（実装）で書く（test_first.md 5.1）";

    /// <summary>
    /// いまの段と状態から、次の段を返す。**完了したら null。**
    ///
    /// | 段 | 完了条件 |
    /// | --- | --- |
    /// | `Arousal` | 覚醒度を上限まで上げて 1 点取る |
    /// | `Vigor` | 元気を尽きさせ、回復を待つ |
    /// | `Pretend` | 寝たふりを 1 回成功させる |
    ///
    /// **段の条件が長時間満たされなくても止めない。**
    /// 夜が終わったら同じ段からやり直す（MOD-Tutorial のエラー時）。
    /// </summary>
    public static TutorialStep? Advance(TutorialStep current, NightState s) =>
        throw new NotImplementedException(NotYet);

    /// <summary>その段の完了条件を満たしているか。</summary>
    public static bool IsSatisfied(TutorialStep step, NightState s) =>
        throw new NotImplementedException(NotYet);
}
