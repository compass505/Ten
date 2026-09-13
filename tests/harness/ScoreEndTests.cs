using System;
using System.Linq;
using NUnit.Framework;
using Ten.Pure;

namespace Ten.Tests.Harness;

/// <summary>
/// TC-070〜083 — 得点と終了（docs/40_test/cases/TC-pure-out.md）。
///
/// **再加点の閾値はバランス値**なので期待値に書かない（ADR-0012）。
/// 「閾値まで下げる」は `Tuning` を読まずに、状態を段階的に下げて**境界を探して**作る。
/// </summary>
[TestFixture]
public sealed class ScoreEndTests
{
    private static BoardSpec Board => SimProbe.BoardOf(SimProbe.Seeds(1).Single());

    private static NightState Start(BoardSpec board) => SimProbe.Begin(board);

    // ------------------------------------------------------------------
    // 得点（MOD-Score）
    // ------------------------------------------------------------------

    [Test]
    public void TC070_上限に滞在している間は加点されない()
    {
        var board = Board;
        var s = Score.Apply(Start(board) with { Arousal = 100 }, SimProbe.Tuning);

        var after = SimProbe.RunTicks(s with { Parent = ParentPhase.Up }, board, 100);

        Assert.That(after.Score, Is.EqualTo(s.Score),
            "**上限に留まっている間に加点が増えた**（REQ-052）。加点は到達の瞬間に 1 回だけ");
    }

    [Test]
    public void TC071_再加点閾値のすぐ上まで下げてから戻しても加点されない()
    {
        var board = Board;
        var (justAbove, _) = FindRescoreBoundary(board);

        var s = Score.Apply(justAbove with { Arousal = 100 }, SimProbe.Tuning);

        Assert.That(s.Score, Is.EqualTo(justAbove.Score),
            "**閾値のすぐ上まで戻しただけで再加点された**（REQ-052）");
    }

    [Test]
    public void TC072_再加点閾値まで下げてから戻すと加点される()
    {
        var board = Board;
        var (_, atThreshold) = FindRescoreBoundary(board);

        var s = Score.Apply(atThreshold with { Arousal = 100 }, SimProbe.Tuning);

        Assert.That(s.Score, Is.EqualTo(atThreshold.Score + 1),
            "**閾値まで下げたのに再加点されない**（REQ-052）");
    }

    /// <summary>
    /// 再加点の境界を探す。**閾値の具体値を書かないため**（ADR-0012）、
    /// 覚醒度を 1 ずつ下げて `ScoredEdge` が落ちる点を見つける。
    /// </summary>
    private static (NightState JustAbove, NightState AtThreshold) FindRescoreBoundary(BoardSpec board)
    {
        var scored = Score.Apply(Start(board) with { Arousal = 100 }, SimProbe.Tuning);

        Assert.That(scored.ScoredEdge, Is.True, "上限に達したのに加点済みになっていない（I-10）");

        for (var a = 99; a >= 0; a--)
        {
            var lowered = SimProbe.RunTicks(scored with { Arousal = a }, board, 1);

            if (!lowered.ScoredEdge)
            {
                return (SimProbe.RunTicks(scored with { Arousal = a + 1 }, board, 1), lowered);
            }
        }

        throw new InvalidOperationException(
            "覚醒度を 0 まで下げても加点済みが解けない。再加点が永久にできない（REQ-052）");
    }

    [Test]
    public void TC073_終わり方で得点が加減されない()
    {
        var board = Board;
        var s = Start(board) with { Score = 3 };

        foreach (var kind in Enum.GetValues<EndKind>())
        {
            var ended = Score.Apply(s with { Over = kind }, SimProbe.Tuning);

            Assert.That(ended.Score, Is.EqualTo(3),
                $"**終わり方 {kind} で得点が動いた**（REQ-023）。得点は覚醒させた回数だけ");
        }
    }

    [Test]
    public void TC074_同点なら先に遊んだほうが残る()
    {
        var earlier = Best(score: 3, playIndex: 1);
        var later = Best(score: 3, playIndex: 2);

        Assert.That(Score.IsBetter(later, earlier), Is.False,
            "**同点で後発が上書きした**（REQ-025）");
    }

    [Test]
    public void TC075_得点が高ければ後発でも残る()
    {
        var earlier = Best(score: 3, playIndex: 1);
        var later = Best(score: 4, playIndex: 2);

        Assert.That(Score.IsBetter(later, earlier), Is.True, "REQ-025");
    }

    [Test]
    public void TC076_再加点が不可能な調整値は例外になる()
    {
        // 再加点閾値 >= 上限だと、一度上限に達したあと二度と加点できない。
        // **丸めて続行しない**（壊れた設定で夜が進むと、原因が分からなくなる）
        Assert.Throws<ArgumentException>(
            () => Score.Apply(Start(Board), BrokenTuning()),
            "**再加点が不可能な Tuning を弾いていない**");
    }

    /// <summary>
    /// 再加点が不可能な調整値（再加点閾値 &gt;= 上限）。
    /// **フェーズ 5 で `Tuning` の項目を起こしたので実際の値にした**（ADR-0020）。
    /// </summary>
    private static Tuning BrokenTuning() => new() { Rearm = 100, ArousalMax = 100 };

    private static BestPlay Best(int score, int playIndex) =>
        new(score, playIndex, EndKind.Dawn, string.Empty, default, "DX-41");

    // ------------------------------------------------------------------
    // 終了（MOD-End）
    // ------------------------------------------------------------------

    [Test]
    public void TC077_夜明けで終わる()
    {
        var board = Board;
        var s = Start(board) with { Tick = 5399 };

        var after = SimProbe.RunTicks(s, board, 1);

        Assert.That(after.Over, Is.EqualTo(EndKind.Dawn), "REQ-007");
    }

    [Test]
    public void TC078_最後の対処が終わった時点で山札切れになる()
    {
        var board = Board;
        var s = Start(board) with { Hand = new HandCount(1, 0, 0, 0) };

        var caring = SimProbe.RunUntil(s, board, x => x.Parent == ParentPhase.Caring,
            new TickInput(ActionKind.Cry, false), what: "最後の対処");

        Assert.That(caring.Over, Is.Null, "**対処の最中に終わっている**（REQ-042）");

        var done = SimProbe.RunUntil(caring, board, x => x.Parent != ParentPhase.Caring || x.Over is not null,
            what: "対処の終わり");

        Assert.That(done.Over, Is.EqualTo(EndKind.HandEmpty), "REQ-042 / 050");
    }

    [Test]
    public void TC079_山札0でも対処中は夜が終わらない()
    {
        var board = Board;
        var s = Start(board) with { Hand = new HandCount(0, 0, 0, 0) };

        var caring = s with { Parent = ParentPhase.Caring, ActiveCare = CareKind.PatPat, CareRemain = 50 };

        var mid = SimProbe.RunTicks(caring, board, 10);

        Assert.That(mid.Over, Is.Null,
            "**対処の途中で夜が終わった**（REQ-042）。最後の対処は最後まで受けられる");
    }

    [Test]
    public void TC080_最後の1枚で上限に達すると加点されたうえで山札切れになる()
    {
        var board = Board;
        var s = Start(board) with
        {
            Hand = new HandCount(1, 0, 0, 0),
            Arousal = 99,
        };

        var before = s.Score;
        var run = SimRunner.Run(board, 1, SimProbe.Tuning,
            new InputTrace(board.Seed, 1, [(0, new TickInput(ActionKind.Cry, false))]),
            SimProbe.NightTicks);

        var end = run.LastOrDefault(x => x.Over is not null);

        Assert.That(end.Over, Is.EqualTo(EndKind.HandEmpty), "前提: 山札切れで終わる");
        Assert.That(end.Score, Is.GreaterThan(before),
            "**最後の 1 枚で得た点が入っていない**（REQ-051）。" +
            "Score.Apply より先に NightEnd.Evaluate を呼ぶと、この点が消える");
    }

    [Test]
    public void TC081_同じtickに夜明けと山札切れなら夜明けが記録される()
    {
        var board = Board;
        var s = Start(board) with
        {
            Tick = 5399,
            Hand = new HandCount(0, 0, 0, 0),
        };

        var after = SimProbe.RunTicks(s, board, 1);

        Assert.That(after.Over, Is.EqualTo(EndKind.Dawn),
            "**判定順が一意でない**（REQ-054）。EndKind の並び順が判定順（Dawn → FellAsleep → HandEmpty）");
    }

    [Test]
    public void TC082_出来事で山札が0になっても終わる()
    {
        var board = Board;
        var s = Start(board) with { Hand = new HandCount(1, 0, 0, 0) };

        // 対処による消費ではなく、山札そのものが 0 になった場合
        var drained = SimProbe.RunTicks(s with { Hand = new HandCount(0, 0, 0, 0) }, board, 2);

        Assert.That(drained.Over, Is.EqualTo(EndKind.HandEmpty),
            "**対処による消費でないと終わらない**（REQ-050）");
    }

    [Test]
    public void TC083_終了後に進めても状態が変わらない()
    {
        var board = Board;
        var ended = Start(board) with { Over = EndKind.Dawn };

        var after = SimProbe.RunTicks(ended, board, 50);

        Assert.That(after, Is.EqualTo(ended),
            "**終了後に状態が動いた。**中断復帰や二重呼び出しで結果が変わる");
    }
}
