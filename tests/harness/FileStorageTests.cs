using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Ten.Boundary;
using Ten.Pure;

namespace Ten.Tests.Harness;

/// <summary>
/// TC-176 / 177 — 実ファイルでの往復（MOD-Storage ST-5 / REQ-010 / 025 / 032）。
///
/// **TC-108 は `MemoryStorage` で見ている。**値をそのまま持つので、
/// 「ファイルに書いて読み直すと入れ子の値（山札・慣れ・診断）が消える」は拾えない。
/// 実機で続きから再開できるかは、ここでしか分からない。
/// </summary>
[TestFixture]
public sealed class FileStorageTests
{
    private string _dir = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ten-storage-" + Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, recursive: true);
            }
        }
        catch (IOException)
        {
        }
    }

    /// <summary>入れ子の値がすべて既定値から動いている、夜の途中の状態。</summary>
    private static (NightState State, BoardSpec Board) MidNight()
    {
        var board = SimProbe.BoardOf(SimProbe.Seeds(1).Single());
        var s = SimProbe.Begin(board);

        for (var tick = 0; tick < 2500 && s.Over is null; tick++)
        {
            var input = (tick % 200) < 25 ? new TickInput(ActionKind.Cry, false) : new TickInput(null, false);

            s = SimProbe.Step(s, input, board);
        }

        return (s, board);
    }

    [Test]
    public void TC176_進行中のプレイを実ファイルに保存して読み直すと同じ状態に戻る()
    {
        var (s, _) = MidNight();

        Assume.That(s.Habit, Is.Not.EqualTo(default(Habit)), "前提: 慣れが動いている");
        Assume.That(s.Diag, Is.Not.EqualTo(default(Diagnosis)), "前提: 診断が動いている");

        var storage = new FileStorage(_dir);
        storage.SaveRun(new SavedRun("2026-09-13", s));

        var reopened = new FileStorage(_dir);
        var loaded = reopened.LoadRun();

        Assert.Multiple(() =>
        {
            Assert.That(reopened.LastStatus, Is.EqualTo(LoadStatus.Ok),
                "**保存した進行中のプレイが読めない**（REQ-010）。入れ子の値の書き方を疑う");
            Assert.That(loaded?.State, Is.EqualTo(s), "**読み直した状態が保存した状態と違う**（ST-5 / REQ-032）");
            Assert.That(loaded?.BoardDate, Is.EqualTo("2026-09-13"));
        });
    }

    [Test]
    public void TC176_実ファイルから再開して続きを走らせると中断しなかった場合と一致する()
    {
        var (s, board) = MidNight();

        var storage = new FileStorage(_dir);
        storage.SaveRun(new SavedRun(board.Seed, s));

        var restored = new FileStorage(_dir).LoadRun()!.Value.State;

        var a = SimProbe.RunTicks(s, board, 600, new TickInput(ActionKind.Fuss, false));
        var b = SimProbe.RunTicks(restored, board, 600, new TickInput(ActionKind.Fuss, false));

        Assert.That(b, Is.EqualTo(a), "**実ファイルから再開すると状態列がずれる**（REQ-010 / 020）");
    }

    [Test]
    public void TC177_その日の最高成績を実ファイルに保存して読み直せる()
    {
        var best = new BestPlay(2, 3, EndKind.HandEmpty, "FirstScore@120;DawnReached@5400",
            new Diagnosis(10, 20, 30, 40, 50, 60, 70), "DX-07");

        var storage = new FileStorage(_dir);
        storage.SaveToday(new TodayData("2026-09-13", 4, best));

        var loaded = new FileStorage(_dir).LoadToday();

        Assert.Multiple(() =>
        {
            Assert.That(loaded?.Best, Is.EqualTo(best),
                "**その日の最高成績が保存されていない / 読み直すと変わる**（REQ-025 / 032）");
            Assert.That(loaded?.PlayCount, Is.EqualTo(4));
        });
    }

    [Test]
    public void TC177_最高成績が無い日も読み直せる()
    {
        var storage = new FileStorage(_dir);
        storage.SaveToday(new TodayData("2026-09-13", 0, null));

        var loaded = new FileStorage(_dir).LoadToday();

        Assert.That(loaded?.Best, Is.Null);
    }

    [Test]
    public void TC177_範囲外の値を含む進行中のプレイは壊れているとして扱う()
    {
        var (s, _) = MidNight();

        var storage = new FileStorage(_dir);
        storage.SaveRun(new SavedRun("2026-09-13", s with { Arousal = 999 }));

        var reopened = new FileStorage(_dir);

        Assert.Multiple(() =>
        {
            Assert.That(reopened.LoadRun(), Is.Null);
            Assert.That(reopened.LastStatus, Is.EqualTo(LoadStatus.Corrupt), "**範囲外の値を黙って読んだ**（ST-3 / REQ-033）");
        });
    }
}
