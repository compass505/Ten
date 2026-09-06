using System;

namespace Ten.Pure;

/// <summary>
/// MOD-Sim — 夜の状態と 1 tick。
///
/// 仕様: docs/30_detailed_design/MOD-Sim.md（処理順 1〜9 / 性質 S-1〜S-13）
///
/// > **このモジュールが全 Must の大半を持つ。**ハーネス層のテストはここに集中する。
///
/// **これはシグネチャだけの空実装である**（test_first.md 5.1）。
/// </summary>
public static class Sim
{
    private const string NotYet = "フェーズ 5（実装）で書く（test_first.md 5.1）";

    /// <summary>夜の開始状態。盤面とプレイ回数から作る。</summary>
    public static NightState Begin(BoardSpec board, int playIndex, Tuning tuning) =>
        throw new NotImplementedException(NotYet);

    /// <summary>
    /// 1 tick 進める。**純関数**（引数以外の一切に触らない）。
    ///
    /// **`Advance` はこれ 1 本だけ。**内部の段階を公開しない。
    /// テストは「状態 → 状態」だけを見る。
    ///
    /// **得点と終了はここでは行わない。**呼び出し側が
    /// <see cref="Score.Apply"/> → <see cref="NightEnd.Evaluate"/> の順に適用する（REQ-051）。
    /// **型で強制できない唯一の場所**なので、TC-142 / TC-080 が見張っている。
    /// </summary>
    public static NightState Advance(NightState s, TickInput input, BoardSpec board, Tuning tuning) =>
        throw new NotImplementedException(NotYet);
}

/// <summary>
/// MOD-Score — 得点。
///
/// 仕様: docs/30_detailed_design/MOD-Score.md
/// </summary>
public static class Score
{
    private const string NotYet = "フェーズ 5（実装）で書く（test_first.md 5.1）";

    /// <summary>
    /// 覚醒度が上限に達した瞬間に 1 回だけ加点する。**`NightEnd` より先に呼ぶ**（REQ-051）。
    /// </summary>
    public static NightState Apply(NightState s, Tuning tuning) =>
        throw new NotImplementedException(NotYet);

    /// <summary>その日の最高成績を選ぶ。同点なら**先に遊んだほう**を残す（REQ-025）。</summary>
    public static bool IsBetter(BestPlay candidate, BestPlay? current) =>
        throw new NotImplementedException(NotYet);
}

/// <summary>
/// MOD-End — 夜の終わり。
///
/// 仕様: docs/30_detailed_design/MOD-End.md
/// </summary>
public static class NightEnd
{
    private const string NotYet = "フェーズ 5（実装）で書く（test_first.md 5.1）";

    /// <summary>
    /// 終了していれば <see cref="EndKind"/>、していなければ null。
    /// **`Score` の後に呼ぶ**（REQ-051。最後の 1 枚の得点は終了判定より先）。
    /// </summary>
    public static EndKind? Evaluate(NightState s, Tuning tuning) =>
        throw new NotImplementedException(NotYet);
}
