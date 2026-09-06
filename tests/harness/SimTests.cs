using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using Ten.Pure;

namespace Ten.Tests.Harness;

/// <summary>
/// TC-020〜022 — 夜の状態機械の前提（docs/40_test/cases/TC-pure-sim.md）。
///
/// **ここが全 TC の土台。**不変条件が破れる、あるいは決定論が崩れる状態では、
/// 個別の要件を見ても意味が無い。
///
/// 失敗時は入力列をファイルに残す（D4 / harness.md 4 節）。
/// </summary>
[TestFixture]
public sealed class SimTests
{
    /// <summary>1 夜の長さ。**バランス値なので期待値には使わない**（再生する長さとしてだけ使う）。</summary>
    private const int NightTicks = 5400;

    /// <summary>TC-020 が定めた 20 シード。要件値。</summary>
    private const int SeedCount = 20;

    /// <summary>TC-020 が定めた 1000 本。要件値。</summary>
    private const int TraceCount = 1000;

    /// <summary>TC-021 が定めた 10 回。要件値。</summary>
    private const int RepeatCount = 10;

    private static readonly Tuning Tuning = new();

    private static string SeedOf(int i) =>
        new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddDays(i)
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static TraceHeader HeaderOf(string seed, int play) =>
        new(seed, play, Board.SpecVersion, "empty");

    // ------------------------------------------------------------------
    // TC-020: 20 シード × ランダム入力列 1000 本で、I-1〜I-11 が破れない
    // ------------------------------------------------------------------

    [Test]
    public void TC020_どのtickでも不変条件が破れない()
    {
        // 本数は 20 × 1000。1 本あたりの変化の数は密度であって期待値ではない
        const int changesPerTrace = 60;

        for (var s = 0; s < SeedCount; s++)
        {
            var seed = SeedOf(s);
            var board = Board.Generate(seed, Tuning);

            for (var t = 0; t < TraceCount; t++)
            {
                var trace = TraceGenerator.Generate(HeaderOf(seed, 1), t, NightTicks, changesPerTrace);
                var states = SimRunner.Run(board, playIndex: 1, Tuning, trace.Trace, NightTicks);

                foreach (var state in states)
                {
                    var violation = Invariants.FirstViolation(state);

                    if (violation is not null)
                    {
                        Assert.Fail(FailureReport.Build(
                            "TC-020",
                            "I-1〜I-11 が常に成り立つ",
                            violation,
                            trace,
                            state.Tick));
                    }
                }

                var regression = Invariants.FirstOrdinalRegression(states);

                if (regression is not null)
                {
                    Assert.Fail(FailureReport.Build(
                        "TC-020", "通番が単調非減少", regression, trace,
                        states.Count > 0 ? states[^1].Tick : 0));
                }
            }
        }
    }

    // ------------------------------------------------------------------
    // TC-021: 同じ (seed, playIndex, 入力列) を 10 回再生して状態列の差分が 0
    // ------------------------------------------------------------------

    [Test]
    public void TC021_同じ入力列を何度再生しても状態列が一致する()
    {
        const int changesPerTrace = 60;

        for (var s = 0; s < SeedCount; s++)
        {
            var seed = SeedOf(s);
            var board = Board.Generate(seed, Tuning);
            var trace = TraceGenerator.Generate(HeaderOf(seed, 1), 0, NightTicks, changesPerTrace);

            var first = SimRunner.Run(board, playIndex: 1, Tuning, trace.Trace, NightTicks);

            for (var r = 1; r < RepeatCount; r++)
            {
                var again = SimRunner.Run(board, playIndex: 1, Tuning, trace.Trace, NightTicks);
                var diverged = SimRunner.FirstDivergence(first, again);

                if (diverged is not null)
                {
                    Assert.Fail(FailureReport.Build(
                        "TC-021",
                        $"{RepeatCount} 回とも同じ状態列",
                        $"{r + 1} 回目が {diverged} 件目で食い違った",
                        trace,
                        diverged.Value));
                }
            }
        }
    }

    // ------------------------------------------------------------------
    // TC-022: 1 フレーム 1 tick と 1 フレーム 5 tick で状態列が一致する
    // ------------------------------------------------------------------

    [Test]
    public void TC022_1フレームあたりのtick数を変えても状態列が一致する()
    {
        const int changesPerTrace = 60;
        const int ticksPerFrame = 5;

        var seed = SeedOf(0);
        var board = Board.Generate(seed, Tuning);
        var trace = TraceGenerator.Generate(HeaderOf(seed, 1), 0, NightTicks, changesPerTrace);

        var oneAtATime = SimRunner.Run(board, playIndex: 1, Tuning, trace.Trace, NightTicks);
        var inChunks = RunInChunks(board, trace.Trace, ticksPerFrame);

        var diverged = SimRunner.FirstDivergence(oneAtATime, inChunks);

        if (diverged is not null)
        {
            Assert.Fail(FailureReport.Build(
                "TC-022",
                "1 フレームで進める tick 数に結果が依存しない",
                $"{ticksPerFrame} tick ずつ進めると {diverged} 件目で食い違った",
                trace,
                diverged.Value));
        }
    }

    /// <summary>
    /// フレームごとにまとめて tick を消化する（`StepClock` が返す tick 数の代わり）。
    /// **状態の進め方は 1 tick ずつと同じでなければならない**（NFR-005）。
    /// </summary>
    private static IReadOnlyList<NightState> RunInChunks(BoardSpec board, InputTrace trace, int ticksPerFrame)
    {
        var states = new List<NightState>();
        var state = Sim.Begin(board, 1, Tuning);
        var pending = new List<TickInput>(TraceFile.Expand(trace, NightTicks));

        for (var i = 0; i < pending.Count; i += ticksPerFrame)
        {
            for (var j = i; j < Math.Min(i + ticksPerFrame, pending.Count); j++)
            {
                state = SimRunner.Step(state, pending[j], board, Tuning);
                states.Add(state);

                if (state.Over is not null)
                {
                    return states;
                }
            }
        }

        return states;
    }
}
