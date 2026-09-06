using System;
using System.Collections.Generic;
using Ten.Pure;

namespace Ten.Tests.Harness;

/// <summary>
/// 入力列を流し込んで状態列を取る、ハーネスの再生系。
///
/// <code>
/// 初期状態 + シード + 入力列  →  [ここ]  →  観測できる状態の列
/// </code>
///
/// **`Sim` → `Score` → `NightEnd` の順序をここ 1 か所に閉じ込める**（REQ-051）。
/// この順序は**型で強制できない唯一の場所**なので、呼ぶ側が各テストでばらばらに
/// 組み立てると、順序違反が混ざっても誰も気づかない。
/// → docs/30_detailed_design/MOD-Sim.md / TC-142 / TC-080
///
/// **実時間を待たない。**tick は引数で進む（D2）。
/// </summary>
public static class SimRunner
{
    /// <summary>
    /// 1 夜を通しで再生し、**各 tick の後の状態**を返す。
    ///
    /// 返るのは `tickCount` 件か、夜が終わった時点までのどちらか短いほう。
    /// 終わった tick の状態を最後に含める（終わり方を見るため）。
    /// </summary>
    /// <param name="board">その日の盤面</param>
    /// <param name="playIndex">その日の何回目のプレイか（REQ-048）</param>
    /// <param name="tuning">調整値。**ハーネスは中身を読まない**（ADR-0012）</param>
    /// <param name="trace">入力列</param>
    /// <param name="tickCount">再生する長さ</param>
    public static IReadOnlyList<NightState> Run(
        BoardSpec board,
        int playIndex,
        Tuning tuning,
        InputTrace trace,
        int tickCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tickCount);

        var states = new List<NightState>(tickCount);
        var state = Sim.Begin(board, playIndex, tuning);

        foreach (var input in TraceFile.Expand(trace, tickCount))
        {
            state = Step(state, input, board, tuning);
            states.Add(state);

            if (state.Over is not null)
            {
                break;
            }
        }

        return states;
    }

    /// <summary>
    /// 1 tick を、決められた順序で進める。
    ///
    /// | 順 | 呼ぶもの | 根拠 |
    /// | --- | --- | --- |
    /// | 1 | <see cref="Sim.Advance"/> | 状態を進める |
    /// | 2 | <see cref="Score.Apply"/> | **`NightEnd` より先**（REQ-051。最後の 1 枚の得点は終了判定より先） |
    /// | 3 | <see cref="NightEnd.Evaluate"/> | 終了判定 |
    ///
    /// **入れ替えると、山札の最後の 1 枚で得た点が消える。**
    /// </summary>
    public static NightState Step(NightState s, TickInput input, BoardSpec board, Tuning tuning)
    {
        // 終了後に進めても壊れない（MOD-Sim のエラー時。s をそのまま返す）
        if (s.Over is not null)
        {
            return s;
        }

        var advanced = Sim.Advance(s, input, board, tuning);
        var scored = Score.Apply(advanced, tuning);
        var over = NightEnd.Evaluate(scored, tuning);

        return over is null ? scored : scored with { Over = over };
    }

    /// <summary>
    /// 同じ入力を 2 回流して、状態列が一致することを確かめる（D1〜D3 / NFR-004）。
    ///
    /// **落ちたら決定論が壊れている。**その先のテストは全部信用できなくなるので、
    /// 個別の性質を見る前にこれを通す。
    /// </summary>
    public static (IReadOnlyList<NightState> First, IReadOnlyList<NightState> Second) RunTwice(
        BoardSpec board, int playIndex, Tuning tuning, InputTrace trace, int tickCount) =>
        (Run(board, playIndex, tuning, trace, tickCount),
         Run(board, playIndex, tuning, trace, tickCount));

    /// <summary>
    /// 2 つの状態列が最初に食い違う位置。一致していれば null。
    /// **失敗報告に tick を載せるため**（D4）。
    /// </summary>
    public static int? FirstDivergence(IReadOnlyList<NightState> a, IReadOnlyList<NightState> b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        var shared = Math.Min(a.Count, b.Count);

        for (var i = 0; i < shared; i++)
        {
            if (!a[i].Equals(b[i]))
            {
                return i;
            }
        }

        return a.Count == b.Count ? null : shared;
    }
}
