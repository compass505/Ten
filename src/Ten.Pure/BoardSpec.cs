using System;
using System.Collections.Generic;

namespace Ten.Pure;

/// <summary>
/// 山札の内訳（REQ-041）。
///
/// 定義元: docs/30_detailed_design/types.md 2 節
/// **本体はフェーズ 5 で書く**（test_first.md 5.1）。
/// </summary>
public readonly record struct HandCount(int PatPat, int Milk, int Hold, int DiaperChange)
{
    private const string NotYet = "フェーズ 5（実装）で書く（test_first.md 5.1）";

    /// <summary>types.md では <c>PatPat + Milk + Hold + DiaperChange</c> と定めている。</summary>
    public int Total => throw new NotImplementedException(NotYet);

    public int Of(CareKind k) => throw new NotImplementedException(NotYet);

    /// <summary>0 を下回らない。</summary>
    public HandCount Minus(CareKind k) => throw new NotImplementedException(NotYet);

    public HandCount Plus(CareKind k, int n) => throw new NotImplementedException(NotYet);
}

/// <summary>その夜に起きる出来事とその時刻。</summary>
public readonly record struct ScheduledEvent(int Tick, NightEventKind Kind);

/// <summary>
/// 日付シードから決まる、その日の盤面。**プレイ回数では変わらない**（ADR-0011）。
///
/// 定義元: docs/30_detailed_design/types.md 2 節
/// </summary>
/// <param name="SpecVersion">盤面仕様の版（REQ-040）</param>
/// <param name="Seed">端末ローカル日付（正午境界）から作る `yyyy-MM-dd`</param>
/// <param name="Events">`Tick` の昇順。同じ tick に 2 件は置かない</param>
public readonly record struct BoardSpec(
    int SpecVersion,
    string Seed,
    int InitialArousal,
    HandCount Hand,
    IReadOnlyList<ScheduledEvent> Events
);

/// <summary>
/// 対処への慣れ（REQ-029）。
///
/// 定義元: docs/30_detailed_design/types.md 3 節
/// </summary>
public readonly record struct Habit(int PatPat, int Milk, int Hold, int DiaperChange)
{
    private const string NotYet = "フェーズ 5（実装）で書く（test_first.md 5.1）";

    public int Of(CareKind k) => throw new NotImplementedException(NotYet);

    public int Max => throw new NotImplementedException(NotYet);

    /// <summary>0〜100 に丸める。</summary>
    public Habit Plus(CareKind k, int n) => throw new NotImplementedException(NotYet);

    public Habit Decay(int n) => throw new NotImplementedException(NotYet);
}

/// <summary>
/// 一晩の診断パラメータ。値は 1/10 単位の整数（diagnosis.md 1 節 / ADR-0016 / REQ-062）。
///
/// 定義元: docs/30_detailed_design/types.md 3 節
/// </summary>
public readonly record struct Diagnosis(
    int Irritation, int Fatigue, int Futility, int SleepLoss,
    int Anxiety, int Jerked, int Fondness)
{
    private const string NotYet = "フェーズ 5（実装）で書く（test_first.md 5.1）";

    public int Total => throw new NotImplementedException(NotYet);

    /// <summary>合計で割った割合。合計 0 のときは既定値を返す。</summary>
    public IReadOnlyList<int> Ratios => throw new NotImplementedException(NotYet);
}

/// <summary>
/// 調整値。**数値を型に埋め込まず、外から渡す**（types.md 5 節）。
///
/// **項目はまだ空。**値の実体は balance.md にあり、
/// types.md は「ここには複製しない」と定めている（documentation.md 8 節）。
/// 項目を起こすのはフェーズ 5。
///
/// **ハーネスはこの中身を読まない。**
/// [ADR-0012](../../docs/10_requirements/decisions/ADR-0012-balance-vs-tests.md) が
/// 「テストの期待値に `Tuning` の具体値を書かない」と定めているので、
/// **中身が空のまま素通しできることが、その規約が守られている証拠になる。**
/// </summary>
public readonly record struct Tuning();

/// <summary>
/// その日の最高成績（REQ-025）。
///
/// 定義元: docs/30_detailed_design/types.md 6 節
/// </summary>
public readonly record struct BestPlay(
    int Score, int PlayIndex, EndKind EndKind,
    string Commentary, Diagnosis Diag, string DiagId
);
