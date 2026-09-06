using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Ten.Pure;

namespace Ten.Tests.Harness;

/// <summary>
/// TC-023〜029 — 操作と排他（docs/40_test/cases/TC-pure-sim.md）。
/// </summary>
[TestFixture]
public sealed class OperationTests
{
    private static readonly string Seed = SimProbe.Seeds(1).Single();

    private static (BoardSpec Board, NightState State) Start()
    {
        var board = SimProbe.BoardOf(Seed);

        return (board, SimProbe.Begin(board));
    }

    [Test]
    public void TC023_行動中は別の行動を受け付けない()
    {
        var (board, s) = Start();

        // 行動中にする
        var acting = SimProbe.RunUntil(
            s, board, x => x.Baby == BabyPhase.Acting,
            new TickInput(ActionKind.Cry, false), what: "Baby = Acting");

        var other = SimProbe.Step(acting, new TickInput(ActionKind.Kick, false), board);

        Assert.That(other.ActKind, Is.EqualTo(acting.ActKind),
            "**行動中に別の行動が通った**（REQ-003）。実行中は受け付けない");
    }

    [Test]
    public void TC024_首の向きは状態列に入らない()
    {
        // 首振りは MOD-Input の Look が持ち、純粋層に渡さないことで
        // 「状態列に影響しない」を機械的に保証する（types.md 4 節 / data_model.md 5 節）。
        var fields = typeof(TickInput).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name).ToArray();

        Assert.That(fields, Does.Not.Contain("Look").IgnoreCase,
            "**`TickInput` が首の向きを持っている。**持たせた時点で、" +
            "首を振るだけで状態列が変わりうる形になる（REQ-038）");

        Assert.That(fields, Is.EquivalentTo(new[] { nameof(TickInput.Held), nameof(TickInput.ToggleEyes) }),
            "`TickInput` は「押しっぱなしの行動」と「目の開閉」の 2 つだけ（types.md 4 節）");
    }

    [Test]
    public void TC025_同じtickの2件目は捨てられ副作用も残さない()
    {
        // 1 tick 1 件への正規化は MOD-Input（IN-1）が行い、純粋層は正規化済みしか受け取らない。
        // 入力列の記法もそれを守らせる形になっている。
        var ex = Assert.Throws<TraceFormatException>(() => TraceFile.Parse(
            "# seed=s play=0 spec=1 tuning=t\n10 +cry\n10 +kick\n"));

        Assert.That(ex!.Message, Does.Contain("同じ tick"),
            "同じ tick に 2 件ある列を通してしまうと、2 件目の扱いがテストごとにぶれる");

        // 純粋層側: 同じ行動を押し続けても二重に発火しない（REQ-038 / IN-4）
        var (board, s) = Start();
        var held = new TickInput(ActionKind.Cry, false);

        var first = SimProbe.RunUntil(s, board, x => x.ActFired, held, what: "行動の発火");
        var after = SimProbe.RunTicks(first, board, 1, held);

        Assert.That(after.Arousal, Is.EqualTo(first.Arousal),
            "**押しっぱなしで二重に発火している**（REQ-038 / IN-4）");
    }

    [Test]
    public void TC026_閉眼中は覚醒行動が状態を変えずキューにも残らない()
    {
        var (board, s) = Start();
        var closed = SimProbe.RunTicks(SimProbe.CloseEyes(s, board), board, 1);

        Assert.That(closed.Baby, Is.EqualTo(BabyPhase.EyesClosed), "前提が作れていない");

        foreach (var kind in SimProbe.AllActions)
        {
            var pressed = SimProbe.Step(closed, new TickInput(kind, false), board);

            Assert.That(pressed with { Tick = closed.Tick, TClosed = closed.TClosed },
                Is.EqualTo(closed with { Tick = closed.Tick, TClosed = closed.TClosed }),
                $"**閉眼中に {kind} が状態を変えた**（REQ-059 / D-01）。" +
                "時刻と閉眼時計以外は動いてはいけない");

            // 開眼したときに遅れて発火しないこと（IN-2: キューに残さない）
            var opened = SimProbe.OpenEyes(pressed, board);
            var settled = SimProbe.RunTicks(opened, board, 2);

            Assert.That(settled.ActKind, Is.Null,
                $"**閉眼中に捨てたはずの {kind} が、開眼後に発火した**（REQ-059 / IN-2）");
        }
    }

    [Test]
    public void TC027_閉眼中に目を開けると開眼して閉眼時計が0に戻る()
    {
        var (board, s) = Start();
        var closed = SimProbe.RunTicks(SimProbe.CloseEyes(s, board), board, 3);
        var opened = SimProbe.OpenEyes(closed, board);

        Assert.Multiple(() =>
        {
            Assert.That(opened.Baby, Is.EqualTo(BabyPhase.Open), "REQ-059: 目を開けることはできる");
            Assert.That(opened.TClosed, Is.Zero, "I-8: 開眼中は TClosed = 0");
        });
    }

    [Test]
    public void TC028_元気が尽きている間は覚醒行動を実行できず回復後は実行できる()
    {
        var (board, s) = Start();
        var empty = s with { Vigor = 0 };

        var tried = SimProbe.Act(empty, board, ActionKind.Cry, hold: 1);

        Assert.That(tried.Arousal, Is.EqualTo(empty.Arousal),
            "**元気 0 で覚醒行動が通った**（REQ-014）");

        // 何もしない間に元気が回復し、行き止まりにならない（screens.md C-04）
        var recovered = SimProbe.RunUntil(empty, board, x => x.Vigor > 0, what: "元気の回復");
        var full = SimProbe.RunUntil(recovered, board, x => x.Vigor >= 100, what: "元気の全回復");
        var acted = SimProbe.Act(full, board, ActionKind.Cry, hold: 1);

        Assert.That(acted.Arousal, Is.GreaterThan(full.Arousal),
            "**回復しても実行できない**（REQ-014 の行き止まり）");
    }

    [Test]
    public void TC029_同じtickでは開眼が寝落ち判定より先に効く()
    {
        var (board, s) = Start();

        // 閉眼して寝落ち判定が走る直前まで進め、その tick に開眼を入れる
        var closed = SimProbe.CloseEyes(s, board);
        var justBefore = SimProbe.RunUntil(
            closed, board, x => x.Baby == BabyPhase.EyesClosed && x.TClosed > 0, what: "閉眼の継続");

        var opened = SimProbe.Step(justBefore, new TickInput(null, true), board);

        Assert.That(opened.Over, Is.Not.EqualTo(EndKind.FellAsleep),
            "**開眼より先に寝落ち判定が走った**（REQ-039。順 1 が順 5〜7 より先）");
    }
}
