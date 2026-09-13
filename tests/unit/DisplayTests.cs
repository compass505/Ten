using System;
using System.Linq;
using NUnit.Framework;
using Ten.Pure;

namespace Ten.Tests.Unit;

/// <summary>
/// TC-090〜097 — 段階表示（docs/40_test/cases/TC-pure-out.md）。
///
/// 出典: MOD-Display / ADR-0010（数値を出さない）/ ADR-0014（閉眼中は窓の明るさだけ）
///
/// **段階の境界値はバランス値**なので期待値に書かない（ADR-0012）。
/// 見るのは「同じ値なら同じ段階」「見えないときは -1」「端点で確定する」だけ。
/// </summary>
[TestFixture]
public sealed class DisplayTests
{
    private static readonly Tuning Tuning = new();

    private static BoardSpec Board => Ten.Pure.Board.Generate("2026-01-01", Tuning);

    private static NightState Start(BoardSpec board) => Sim.Begin(board, 1, Tuning);

    [Test]
    public void TC090_同じ状態からは常に同じ段階が出る()
    {
        var board = Board;
        var s = Start(board);

        var first = Display.Map(s, board, Tuning);

        for (var i = 0; i < 100; i++)
        {
            Assert.That(Display.Map(s, board, Tuning), Is.EqualTo(first),
                $"**{i + 1} 回目で段階が変わった**（REQ-045）。対応は固定");
        }
    }

    [Test]
    public void TC091_同じ段階の中では値が違っても区別できない()
    {
        var board = Board;
        var s = Start(board);

        // 覚醒度を 1 ずつ動かし、同じ段階になる 2 点を探す。
        // **境界値を書かずに「同じ段階の中」を作る**
        var byStage = Enumerable.Range(0, 101)
            .Select(a => (Arousal: a, Stage: Display.Map(s with { Arousal = a }, board, Tuning).Arousal))
            .Where(x => x.Stage >= 0)
            .GroupBy(x => x.Stage)
            .FirstOrDefault(g => g.Count() >= 2);

        Assert.That(byStage, Is.Not.Null,
            "**どの段階にも値が 1 つしか入っていない。**それは段階ではなく数値の露出（ADR-0010）");

        var pair = byStage!.Take(2).ToArray();

        var a = Display.Map(s with { Arousal = pair[0].Arousal }, board, Tuning);
        var b = Display.Map(s with { Arousal = pair[1].Arousal }, board, Tuning);

        Assert.That(b.Arousal, Is.EqualTo(a.Arousal),
            $"覚醒度 {pair[0].Arousal} と {pair[1].Arousal} が違う段階になった（REQ-045 / 058）");
    }

    [Test]
    public void TC092_端点は区間ではなく確定して分かる()
    {
        var board = Board;
        var s = Start(board);

        Assert.Multiple(() =>
        {
            Assert.That(Display.Map(s with { Arousal = 100 }, board, Tuning).ArousalExact, Is.True,
                "**覚醒度が上限に達したことが確定して分からない**（REQ-045）");

            Assert.That(Display.Map(s with { Hand = new HandCount(0, 0, 0, 0) }, board, Tuning).HandExact,
                Is.True, "**山札が空であることが確定して分からない**（REQ-045）");

            Assert.That(Display.Map(s with { Tick = 5399 }, board, Tuning).DawnImminent, Is.True,
                "**夜明け直前が確定して分からない**（REQ-045）");
        });
    }

    [Test]
    public void TC093_閉眼中は覚醒度と山札が見えない()
    {
        var board = Board;
        var closed = Start(board) with { Baby = BabyPhase.EyesClosed, TClosed = 1 };

        var stages = Display.Map(closed, board, Tuning);

        Assert.Multiple(() =>
        {
            Assert.That(stages.Arousal, Is.EqualTo(-1),
                "**閉眼中に覚醒度が見えている**（REQ-004 / ADR-0014）");
            Assert.That(stages.Hand, Is.EqualTo(-1),
                "**閉眼中に山札が見えている**（REQ-004 / ADR-0014）");
        });
    }

    [Test]
    public void TC094_親が視界から消えている間は覚醒度が見えない()
    {
        var board = Board;
        var up = Start(board) with { Parent = ParentPhase.Up, Arousal = 100 };

        Assert.That(Display.Map(up, board, Tuning).Arousal, Is.EqualTo(-1),
            "**親がいないのに顔が読めている**（screens.md 4.2 / REQ-030）");
    }

    [Test]
    public void TC095_寝返りの間は覚醒度が見えない()
    {
        var board = Board;
        var rolling = Start(board) with { RollUntil = 999, Tick = 500 };

        Assert.That(Display.Map(rolling, board, Tuning).Arousal, Is.EqualTo(-1),
            "**寝返り中に顔が読めている**（REQ-055）");
    }

    [Test]
    public void TC096_元気は常に見える()
    {
        var board = Board;
        var s = Start(board);

        foreach (var variant in new[]
        {
            s with { Baby = BabyPhase.EyesClosed, TClosed = 1 },
            s with { Parent = ParentPhase.Up },
            s with { RollUntil = 999, Tick = 500 },
        })
        {
            Assert.That(Display.Map(variant, board, Tuning).Vigor, Is.GreaterThanOrEqualTo(0),
                "**元気が見えない状態がある**（REQ-006）。自分の体なので視線に依存しない");
        }
    }

    [Test]
    public void TC097_範囲外の状態値は例外になる()
    {
        var board = Board;
        var s = Start(board);

        // 丸めて段階を返すと、壊れた状態のまま画面が出て原因が分からなくなる
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => Display.Map(s with { Arousal = 101 }, board, Tuning),
                "覚醒度が範囲外でも段階を返している");

            Assert.Throws<ArgumentOutOfRangeException>(
                () => Display.Map(s with { Vigor = -1 }, board, Tuning),
                "元気が範囲外でも段階を返している");
        });
    }
}
