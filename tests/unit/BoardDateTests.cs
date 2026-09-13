using System;
using System.Linq;
using NUnit.Framework;
using Ten.Pure;

namespace Ten.Tests.Unit;

/// <summary>
/// TC-113 — 正午境界（docs/40_test/cases/TC-boundary.md）。
///
/// 出典: MOD-Calendar CA-1 / CA-2 / CA-6 / REQ-019
///
/// **2026-09-06 に IF を変えて書けるようになった。**もとは `ICalendar.BoardDate` が
/// 端末時計を直接読む形で、11:59:59 と 12:00:00 を与える手段が無かった
/// （→ decisions_pending.md G-01）。
/// </summary>
[TestFixture]
public sealed class BoardDateTests
{
    private static DateTimeOffset At(int year, int month, int day, int h, int m, int s) =>
        new(year, month, day, h, m, s, TimeSpan.Zero);

    [Test]
    public void TC113_正午の直前と直後で日付が変わる()
    {
        var before = BoardDateRule.From(At(2026, 9, 6, 11, 59, 59));
        var after = BoardDateRule.From(At(2026, 9, 6, 12, 0, 0));

        Assert.That(after, Is.Not.EqualTo(before),
            $"**正午で日付が変わっていない**（REQ-019 / CA-1）。" +
            $"11:59:59 → {before} / 12:00:00 → {after}");
    }

    [Test]
    public void TC113_正午未満は前日として扱う()
    {
        // 夜に日付境界が来ないようにするための正午境界（CA-1）。
        // 21:00 に始めた夜が、日付をまたいでも同じ盤面のまま終われる
        Assert.Multiple(() =>
        {
            Assert.That(BoardDateRule.From(At(2026, 9, 7, 0, 0, 0)), Is.EqualTo("2026-09-06"),
                "深夜 0 時は前日");
            Assert.That(BoardDateRule.From(At(2026, 9, 7, 11, 59, 59)), Is.EqualTo("2026-09-06"),
                "正午の直前まで前日");
            Assert.That(BoardDateRule.From(At(2026, 9, 7, 12, 0, 0)), Is.EqualTo("2026-09-07"),
                "正午から当日");
            Assert.That(BoardDateRule.From(At(2026, 9, 7, 23, 59, 59)), Is.EqualTo("2026-09-07"),
                "その日の終わりまで当日");
        });
    }

    [Test]
    public void TC113_夜のあいだ日付が変わらない()
    {
        // 21:00 に始めて 6:00 に終わる（REQ-007）。**その間ずっと同じ日付**
        var start = At(2026, 9, 6, 21, 0, 0);
        var expected = BoardDateRule.From(start);

        for (var minutes = 0; minutes <= 9 * 60; minutes += 10)
        {
            var at = start.AddMinutes(minutes);

            Assert.That(BoardDateRule.From(at), Is.EqualTo(expected),
                $"**夜の途中（{at:HH:mm}）で日付が変わった**（CA-1）。" +
                "変わると、遊んでいる最中に盤面が別の日のものになる");
        }
    }

    [Test]
    public void TC113_形式はyyyyMMdd()
    {
        var date = BoardDateRule.From(At(2026, 1, 2, 15, 0, 0));

        Assert.That(date, Is.EqualTo("2026-01-02"),
            "**形式が `yyyy-MM-dd` でない**（CA-2）。" +
            "seed の入力は ASCII に限る（harness.md 2 節）ので、月日は 0 詰め");
    }

    [Test]
    public void TC113_同じ時刻からは常に同じ日付が出る()
    {
        var at = At(2026, 9, 6, 21, 0, 0);
        var first = BoardDateRule.From(at);

        for (var i = 0; i < 100; i++)
        {
            Assert.That(BoardDateRule.From(at), Is.EqualTo(first),
                "**同じ時刻から違う日付が出た**（NFR-004 / CA-6）。" +
                "引数以外のものを読んでいる");
        }
    }

    [Test]
    public void TC113_規則が環境に触っていない()
    {
        // CA-6。純粋層なので端末時計にも UnityEngine にも触らない。
        // TC-006 と同じ静的検査を BoardDateRule にも当てる
        var path = System.IO.Path.Combine(RepoPaths.PureSource, "BoardDateRule.cs");

        Assert.That(System.IO.File.Exists(path), Is.True, $"実装が見つからない: {path}");

        var text = System.IO.File.ReadAllText(path);

        foreach (var word in new[] { "DateTime.Now", "DateTime.UtcNow", "DateTimeOffset.Now", "UnityEngine" })
        {
            Assert.That(text, Does.Not.Contain(word),
                $"**`{word}` を参照している**（CA-6 / D2）。時刻は引数で受ける");
        }
    }
}
