using System.Linq;
using NUnit.Framework;
using Ten.Boundary;
using Ten.Pure;

namespace Ten.Tests.Harness;

/// <summary>
/// TC-108〜112 — 保存と復元（docs/40_test/cases/TC-boundary.md）。
///
/// 出典: MOD-Storage ST-1〜ST-6 / REQ-010 / 020 / 033 / 053
///
/// **TC-108 がこのモジュールの一番重いテスト。**中断復帰で状態列が変わると、
/// REQ-020（同じ入力列なら同じ結果）が根元から崩れる。
///
/// **Unity は要らない。**`IStorage` は口で、`MemoryStorage` に差し替えて検証する（D3）。
/// 端末の保存先を触るのは `FileStorage` だけで、そこはビルドと実機で見る（TC-109）。
/// </summary>
[TestFixture]
public sealed class StorageTests
{
    /// <summary>TC-108 が定めた 10 シード。</summary>
    private const int SeedCount = 10;

    /// <summary>TC-108 が定めた「各 5 箇所の中断位置」。</summary>
    private static readonly int[] BreakPoints = [1, 500, 1500, 3000, 5000];

    [Test]
    public void TC108_任意のtickで中断して復帰しても状態列が一致する()
    {
        foreach (var seed in SimProbe.Seeds(SeedCount))
        {
            var board = SimProbe.BoardOf(seed);
            var trace = TraceGenerator.Generate(
                new TraceHeader(seed, 1, Board.SpecVersion, "empty"), 0, SimProbe.NightTicks, 60);

            var straight = SimRunner.Run(board, 1, SimProbe.Tuning, trace.Trace, SimProbe.NightTicks);

            foreach (var at in BreakPoints)
            {
                var resumed = RunWithBreak(board, trace.Trace, at);
                var diverged = SimRunner.FirstDivergence(straight, resumed);

                if (diverged is not null)
                {
                    Assert.Fail(FailureReport.Build(
                        "TC-108",
                        "中断しなかった場合と状態列が一致",
                        $"tick={at} で中断・復帰すると {diverged} 件目で食い違った",
                        trace,
                        diverged.Value));
                }
            }
        }
    }

    /// <summary>指定の tick で保存 → 読み直し → 続きを再生する。</summary>
    private static System.Collections.Generic.IReadOnlyList<NightState> RunWithBreak(
        BoardSpec board, InputTrace trace, int at)
    {
        var storage = new MemoryStorage();
        var inputs = TraceFile.Expand(trace, SimProbe.NightTicks).ToArray();

        var states = new System.Collections.Generic.List<NightState>();
        var s = SimProbe.Begin(board);

        for (var tick = 0; tick < inputs.Length; tick++)
        {
            if (tick == at)
            {
                // 保存して、読み直したものから続ける
                storage.SaveRun(new SavedRun(board.Seed, s));

                var loaded = storage.LoadRun();

                Assert.That(loaded, Is.Not.Null, "**保存した進行中のプレイが読めない**（REQ-010）");
                Assert.That(storage.LastStatus, Is.EqualTo(LoadStatus.Ok));

                s = loaded!.Value.State;
            }

            s = SimRunner.Step(s, inputs[tick], board, SimProbe.Tuning);
            states.Add(s);

            if (s.Over is not null)
            {
                break;
            }
        }

        return states;
    }

    [Test]
    public void TC110_進行中のプレイだけ壊れてもその日のデータは読める()
    {
        var storage = new MemoryStorage();

        storage.SaveToday(new TodayData("2026-09-06", 3, null));
        storage.SaveRun(new SavedRun("2026-09-06", default));

        storage.CorruptRun();

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => storage.LoadRun(),
                "**壊れたデータで例外を投げている**（REQ-033）。起動できなくなる");

            Assert.That(storage.LoadRun(), Is.Null);
            Assert.That(storage.LastStatus, Is.EqualTo(LoadStatus.Corrupt));

            Assert.That(storage.LoadToday(), Is.Not.Null,
                "**Run が壊れただけで Today まで読めなくなっている**（ST-2）。" +
                "捨てる範囲を分ける");
        });
    }

    [Test]
    public void TC111_範囲外の値を含むデータは壊れているとして扱う()
    {
        var storage = new MemoryStorage();

        // 覚醒度が範囲外（I-1 違反）の状態を保存する
        var broken = default(NightState) with { Arousal = 999 };

        storage.SaveRun(new SavedRun("2026-09-06", broken));

        Assert.Multiple(() =>
        {
            Assert.That(storage.LoadRun(), Is.Null,
                "**壊れた状態をそのまま返している**（ST-3 / REQ-033）。" +
                "範囲外の状態で再生すると、どこで壊れたか分からなくなる");

            Assert.That(storage.LastStatus, Is.EqualTo(LoadStatus.Corrupt));
        });
    }

    [Test]
    public void TC112_未知のschemaVersionは版違いとして扱う()
    {
        var storage = new MemoryStorage();

        storage.SaveDevice(new DeviceData(1, Board.SpecVersion, TutorialDone: true));
        storage.SetFutureSchemaVersion();

        Assert.Multiple(() =>
        {
            Assert.DoesNotThrow(() => storage.LoadDevice(),
                "**未来の版で例外を投げている**（REQ-033）");

            Assert.That(storage.LastStatus, Is.EqualTo(LoadStatus.VersionMismatch),
                "**未知の版を Corrupt と区別していない**（ST-4）。" +
                "壊れているのか新しいのかで、捨てる範囲が変わる");
        });
    }

    [Test]
    public void TC112_何も保存していなければMissingになる()
    {
        var storage = new MemoryStorage();

        Assert.Multiple(() =>
        {
            Assert.That(storage.LoadRun(), Is.Null);
            Assert.That(storage.LastStatus, Is.EqualTo(LoadStatus.Missing),
                "**未保存を Corrupt と区別していない。**初回起動が復旧画面になる");
        });
    }

    [Test]
    public void TC108_進行中のプレイを消してもその日のデータが残る()
    {
        var storage = new MemoryStorage();

        storage.SaveToday(new TodayData("2026-09-06", 3, null));
        storage.SaveRun(new SavedRun("2026-09-06", default));

        storage.ClearRun();

        Assert.Multiple(() =>
        {
            Assert.That(storage.LoadRun(), Is.Null, "放棄したプレイが残っている（REQ-037）");
            Assert.That(storage.LoadToday(), Is.Not.Null,
                "**放棄でその日の記録まで消えた**（REQ-024 / 037）。回数は数える");
        });
    }
}
