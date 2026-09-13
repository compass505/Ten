using System;
using System.Collections.Generic;
using System.Linq;
using Ten.Pure;

namespace Ten.Tests.Harness;

/// <summary>
/// 状態を作る・進める・測るための道具。
///
/// **前提の状態は `Sim.Begin` から作り、変えたい 1 つだけを `with` で差し替える。**
/// 「覚醒度 A &lt; B（他は同一）」のような前提は、入力列だけでは作れないため。
///
/// **バランス値を書かない**（ADR-0012）。長さや刻みは「測るための手順」であって
/// 期待値ではない。上昇量は**比較にしか使わない。**
/// </summary>
public static class SimProbe
{
    /// <summary>1 夜の長さ。**再生する上限としてだけ使う。**</summary>
    public const int NightTicks = 5400;

    public static readonly Tuning Tuning = new();

    private static readonly TickInput Idle = new(null, false);
    private static readonly TickInput Eyes = new(null, true);

    public static BoardSpec BoardOf(string seed) => Board.Generate(seed, Tuning);

    public static NightState Begin(BoardSpec board, int playIndex = 1) =>
        Sim.Begin(board, playIndex, Tuning);

    public static NightState Step(NightState s, TickInput input, BoardSpec board) =>
        SimRunner.Step(s, input, board, Tuning);

    /// <summary>同じ入力で n tick 進める。</summary>
    public static NightState RunTicks(NightState s, BoardSpec board, int ticks, TickInput? input = null)
    {
        var use = input ?? Idle;

        for (var i = 0; i < ticks && s.Over is null; i++)
        {
            s = Step(s, use, board);
        }

        return s;
    }

    /// <summary>
    /// 条件が成り立つまで進める。**成り立たないまま夜が終わったら例外。**
    /// 黙って諦めると、条件が一度も成立していないのにテストが通ってしまう。
    /// </summary>
    public static NightState RunUntil(
        NightState s, BoardSpec board, Func<NightState, bool> until,
        TickInput? input = null, int maxTicks = NightTicks, string? what = null)
    {
        var use = input ?? Idle;

        for (var i = 0; i < maxTicks; i++)
        {
            if (until(s))
            {
                return s;
            }

            if (s.Over is not null)
            {
                break;
            }

            s = Step(s, use, board);
        }

        return until(s)
            ? s
            : throw new InvalidOperationException(
                $"{what ?? "条件"}が {maxTicks} tick 以内に成立しなかった。" +
                $"seed={board.Seed} tick={s.Tick} Baby={s.Baby} Parent={s.Parent} Over={s.Over}");
    }

    /// <summary>目を閉じる（開いていれば）。</summary>
    public static NightState CloseEyes(NightState s, BoardSpec board) =>
        s.Baby == BabyPhase.EyesClosed ? s : Step(s, Eyes, board);

    /// <summary>目を開ける（閉じていれば）。</summary>
    public static NightState OpenEyes(NightState s, BoardSpec board) =>
        s.Baby == BabyPhase.EyesClosed ? Step(s, Eyes, board) : s;

    /// <summary>
    /// 行動を 1 回、押して離す。<paramref name="hold"/> tick 押し続ける。
    /// 離したあと、行動が終わる（`Baby` が `Acting` を抜ける）まで進める。
    /// </summary>
    public static NightState Act(NightState s, BoardSpec board, ActionKind kind, int hold)
    {
        s = RunTicks(s, board, hold, new TickInput(kind, false));
        s = Step(s, Idle, board);   // 離す = 発火

        for (var i = 0; i < NightTicks && s.Over is null && s.Baby == BabyPhase.Acting; i++)
        {
            s = Step(s, Idle, board);
        }

        return s;
    }

    /// <summary>強度が上限（1000。REQ-060 が定める範囲の端）に達するまで押す。</summary>
    public static NightState ActFullStrength(NightState s, BoardSpec board, ActionKind kind)
    {
        var held = new TickInput(kind, false);
        var ticks = 0;

        while (s.Over is null && s.ActStrengthMilli < 1000 && ticks < NightTicks)
        {
            s = Step(s, held, board);
            ticks++;
        }

        s = Step(s, Idle, board);

        for (var i = 0; i < NightTicks && s.Over is null && s.Baby == BabyPhase.Acting; i++)
        {
            s = Step(s, Idle, board);
        }

        return s;
    }

    /// <summary>
    /// その行動による覚醒度の上昇量。**絶対値に意味は無い。比較にだけ使う**（ADR-0012）。
    /// </summary>
    public static int ArousalGain(NightState s, BoardSpec board, ActionKind kind, int hold)
    {
        var before = s.Arousal;
        var after = Act(s, board, kind, hold);

        return after.Arousal - before;
    }

    /// <summary>元気の減り。同上、比較にだけ使う。</summary>
    public static int VigorCost(NightState s, BoardSpec board, ActionKind kind, int hold)
    {
        var before = s.Vigor;
        var after = Act(s, board, kind, hold);

        return before - after.Vigor;
    }

    /// <summary>すべての覚醒行動。</summary>
    public static IReadOnlyList<ActionKind> AllActions { get; } = Enum.GetValues<ActionKind>();

    /// <summary>すべての対処。</summary>
    public static IReadOnlyList<CareKind> AllCares { get; } = Enum.GetValues<CareKind>();

    /// <summary>連続する日付シード。</summary>
    public static IEnumerable<string> Seeds(int count) =>
        Enumerable.Range(0, count).Select(i =>
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i).ToString("yyyy-MM-dd"));
}
