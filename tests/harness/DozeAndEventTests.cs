using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Ten.Pure;

namespace Ten.Tests.Harness;

/// <summary>
/// TC-062〜067 — 寝落ちと出来事（docs/40_test/cases/TC-pure-sim.md）。
/// </summary>
[TestFixture]
public sealed class DozeAndEventTests
{
    private static BoardSpec BoardOf(string seed) => SimProbe.BoardOf(seed);

    private static BoardSpec Board => BoardOf(SimProbe.Seeds(1).Single());

    // ------------------------------------------------------------------
    // 寝落ち
    // ------------------------------------------------------------------

    [Test]
    public void TC062_何もしないでいるといずれ寝落ちし行動すると先延ばしになる()
    {
        var board = Board;

        var idle = SimProbe.RunTicks(SimProbe.Begin(board), board, SimProbe.NightTicks);

        Assert.That(idle.Over, Is.EqualTo(EndKind.FellAsleep),
            "**何もしないまま夜が明けた**（REQ-047）。放置が成立してはいけない");

        // 行動を挟むと先延ばしになる
        var s = SimProbe.Begin(board);
        var acted = 0;

        while (s.Over is null && acted < 40)
        {
            s = SimProbe.RunTicks(s, board, 60);

            if (s.Over is not null)
            {
                break;
            }

            s = SimProbe.Act(s, board, ActionKind.Cry, hold: 1);
            acted++;
        }

        Assert.That(s.Tick, Is.GreaterThan(idle.Tick),
            $"**行動しても寝落ちが先延ばしにならない**（REQ-047）。" +
            $"放置 {idle.Tick} tick / 行動あり {s.Tick} tick");
    }

    [Test]
    public void TC063_一度も行動しない入力列でも夜が終わり所要時間が5分以内()
    {
        // NFR-006: 1 本の再生が実用的な時間で終わること。**5 分は要件値**
        var board = Board;
        var sw = Stopwatch.StartNew();

        var s = SimProbe.RunTicks(SimProbe.Begin(board), board, SimProbe.NightTicks);

        sw.Stop();

        Assert.Multiple(() =>
        {
            Assert.That(s.Over, Is.Not.Null, "**夜が終わらない**（REQ-047 / 054）");
            Assert.That(sw.Elapsed.TotalMinutes, Is.LessThan(5),
                $"**再生に {sw.Elapsed.TotalSeconds:F1} 秒かかった**（NFR-006）。" +
                "遅いと改善ループが回らなくなる");
        });
    }

    [Test]
    public void TC064_寝たふりの判定tickでは寝落ち判定を二重に引かない()
    {
        var board = Board;
        var closed = SimProbe.CloseEyes(SimProbe.Begin(board), board);

        var before = closed;
        var judged = SimProbe.RunUntil(closed, board,
            x => x.PretendN > before.PretendN || x.Over is not null, what: "寝たふりの判定");

        // 判定が走った tick では DozeOff を引かない（D-05。順 7 は 5 / 6 を引いた tick をスキップ）
        var prev = SimProbe.RunTicks(closed, board, judged.Tick - closed.Tick - 1);

        Assert.That(judged.DozeN, Is.EqualTo(prev.DozeN),
            $"**寝たふりの判定 tick で DozeOff も引いている**（D-05）。" +
            $"二重判定になり REQ-018 と REQ-047 が同じ tick で競合する");
    }

    // ------------------------------------------------------------------
    // 出来事
    // ------------------------------------------------------------------

    [Test]
    public void TC065_盤面の出来事が全件予定のtickに順番どおり発生する()
    {
        var board = Board;

        Assert.That(board.Events, Is.Not.Empty, $"seed={board.Seed} の盤面に出来事が無い");

        var s = SimProbe.Begin(board);
        var fired = 0;

        foreach (var e in board.Events)
        {
            var before = SimProbe.RunUntil(s, board, x => x.Tick >= e.Tick - 1,
                what: $"tick={e.Tick} の直前");
            var after = SimProbe.RunTicks(before, board, 2);

            Assert.That(after.EventsFired, Is.GreaterThan(fired),
                $"**tick={e.Tick} の {e.Kind} が発生していない**（REQ-043）");

            fired = after.EventsFired;
            s = after;
        }

        Assert.That(fired, Is.EqualTo(board.Events.Count),
            "**発生した件数が盤面と合わない**（REQ-043）");
    }

    [Test]
    public void TC066_出来事5種はいずれも何かを変える()
    {
        // 5 種すべてを踏む盤面を探す
        foreach (var kind in System.Enum.GetValues<NightEventKind>())
        {
            var found = false;

            foreach (var seed in SimProbe.Seeds(40))
            {
                var board = BoardOf(seed);
                var scheduled = board.Events.FirstOrDefault(e => e.Kind == kind);

                if (!board.Events.Any(e => e.Kind == kind))
                {
                    continue;
                }

                var before = SimProbe.RunUntil(SimProbe.Begin(board), board,
                    x => x.Tick >= scheduled.Tick - 1, what: $"{kind} の直前");
                var after = SimProbe.RunTicks(before, board, 2);

                Assert.That(Changed(before, after), Is.True,
                    $"**{kind} が何も変えていない**（REQ-055）。" +
                    "山札・赤ちゃん・親・視界のいずれかが変わらなければ、出来事である意味が無い");

                found = true;
                break;
            }

            Assert.That(found, Is.True, $"**{kind} を含む盤面が 40 シードで見つからない**");
        }
    }

    private static bool Changed(NightState a, NightState b) =>
        b.Hand != a.Hand
        || b.Vigor != a.Vigor
        || b.Arousal != a.Arousal
        || b.Parent != a.Parent
        || b.Baby != a.Baby
        || b.PartnerHere != a.PartnerHere
        || b.RollUntil != a.RollUntil
        || b.HungryUntil != a.HungryUntil
        || b.LockUntil != a.LockUntil
        || b.DampUntil != a.DampUntil;

    [Test]
    public void TC067_出来事の時刻と種類は入力に依存しない()
    {
        var board = Board;

        // 盤面は日付シードから決まる（REQ-019 / 048）。入力列で変わってはいけない
        foreach (var index in new[] { 0, 1, 2 })
        {
            var trace = TraceGenerator.Generate(
                new TraceHeader(board.Seed, 1, Board.SpecVersion, "empty"),
                index, SimProbe.NightTicks, 60);

            var replayed = BoardOf(board.Seed);

            Assert.That(replayed.Events, Is.EqualTo(board.Events),
                "**入力列によって出来事が変わった**（REQ-043 / 019）");

            // 再生しても盤面側は不変
            SimRunner.Run(replayed, 1, SimProbe.Tuning, trace.Trace, SimProbe.NightTicks);

            Assert.That(replayed.Events, Is.EqualTo(board.Events),
                "**再生が盤面を書き換えた。**盤面は不変でなければならない");
        }
    }
}
