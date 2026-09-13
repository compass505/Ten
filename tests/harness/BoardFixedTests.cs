using System;
using System.Linq;
using NUnit.Framework;
using Ten.Pure;

namespace Ten.Tests.Harness;

/// <summary>
/// TC-114 / 017 — プレイ中に日付が変わっても盤面が変わらない（CA-3 / REQ-036 / 048）。
/// </summary>
[TestFixture]
public sealed class BoardFixedTests
{
    [Test]
    public void TC114_プレイ中に日付が変わっても盤面が変わらない()
    {
        // 夜は 21:00 に始まり 6:00 に終わる。**その間に暦日は変わる**が、
        // 盤面は開始時に確定した BoardSpec を使い続ける（CA-3）
        var start = new DateTimeOffset(2026, 9, 6, 21, 0, 0, TimeSpan.Zero);
        var boardDate = BoardDateRule.From(start);
        var board = SimProbe.BoardOf(boardDate);

        var trace = TraceGenerator.Generate(
            new TraceHeader(boardDate, 1, Board.SpecVersion, "empty"), 0, SimProbe.NightTicks, 60);

        var states = SimRunner.Run(board, 1, SimProbe.Tuning, trace.Trace, SimProbe.NightTicks);

        // 暦日をまたいだあと（深夜 2:00）に改めて「その日」を引いても同じ
        var midnight = BoardDateRule.From(start.AddHours(5));

        Assert.That(midnight, Is.EqualTo(boardDate),
            "**夜の途中で「その日」が変わった**（CA-1 / CA-3）");

        // 盤面そのものが不変であること
        var again = SimProbe.BoardOf(boardDate);

        Assert.That(again, Is.EqualTo(board),
            "**同じ日付から違う盤面が出た**（REQ-019 / 048）");

        Assert.That(states, Is.Not.Empty, "再生されていない");
    }

    [Test]
    public void TC114_盤面は再生しても書き換わらない()
    {
        // Sim.Advance は BoardSpec を引数で受け取り、NightState は日付を持たない。
        // **盤面が状態に混ざっていないこと**が、プレイ中に変わらないことの根拠になる
        var members = typeof(NightState).GetProperties().Select(p => p.Name).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(members, Does.Not.Contain("Seed"),
                "**`NightState` が seed を持っている。**持つと保存・復元で盤面がずれうる");
            Assert.That(members, Does.Not.Contain("BoardDate"),
                "**`NightState` が日付を持っている。**同上");
        });
    }
}
