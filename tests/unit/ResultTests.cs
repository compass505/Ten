using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Ten.Pure;

namespace Ten.Tests.Unit;

/// <summary>
/// TC-084〜089 — 結果テキスト（docs/40_test/cases/TC-pure-out.md）。
///
/// 出典: MOD-Result / REQ-026 / 028 / 031 / 040 / 054
/// **TC-088 が一番重い。**盤面の答えが漏れると、翌日以降の「同じ盤面」が壊れる。
/// </summary>
[TestFixture]
public sealed class ResultTests
{
    private static readonly Tuning Tuning = new();

    private static BestPlay Best(int score = 2, int playIndex = 3, EndKind end = EndKind.Dawn) =>
        new(score, playIndex, end, string.Empty, default, "DX-41");

    private static IReadOnlyList<Result.Beat> Beats =>
        [new(100, Result.BeatKind.FirstScore), new(2000, Result.BeatKind.BestPretend)];

    [Test]
    public void TC084_結果テキストは3文以内()
    {
        var text = Result.Compose(Best(), playCount: 5, boardSpecVersion: 1, Beats);

        var sentences = text.Split('。', StringSplitOptions.RemoveEmptyEntries).Length;

        Assert.That(sentences, Is.LessThanOrEqualTo(3),
            $"**{sentences} 文ある**（REQ-026）。実況は 3 文以内。\n{text}");
    }

    [Test]
    public void TC085_通算回数と最高を出した回が両方含まれる()
    {
        var text = Result.Compose(Best(playIndex: 3), playCount: 5, boardSpecVersion: 1, Beats);

        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain("5"), "**通算 5 回が出ていない**（REQ-031）");
            Assert.That(text, Does.Contain("3"), "**最高が 3 回目だと分からない**（REQ-031）");
        });
    }

    [Test]
    public void TC086_終わり方が区別できる形で含まれる()
    {
        var texts = Enum.GetValues<EndKind>()
            .ToDictionary(k => k, k => Result.Compose(Best(end: k), 5, 1, Beats));

        Assert.That(texts.Values.Distinct().Count(), Is.EqualTo(texts.Count),
            "**終わり方が違うのに同じ文になる**（REQ-054）。\n" +
            string.Join("\n", texts.Select(kv => $"{kv.Key}: {kv.Value}")));
    }

    [Test]
    public void TC087_盤面仕様の版番号が含まれる()
    {
        var text = Result.Compose(Best(), 5, boardSpecVersion: 7, Beats);

        Assert.That(text, Does.Contain("7"),
            "**版番号が出ていない**（REQ-040）。版が違う結果を比べても意味が無い");
    }

    [Test]
    public void TC088_盤面の答えが復元できない()
    {
        // 100 個の盤面で、結果テキストから InitialArousal / Hand の内訳 / Events の並びが
        // 読み取れないことを見る（REQ-028）。**数値だけでなく、一意に対応する語も含めない**
        foreach (var seed in Seeds(100))
        {
            var board = Board.Generate(seed, Tuning);
            var text = Result.Compose(Best(), 5, board.SpecVersion, Beats);

            AssertDoesNotLeak(text, board.InitialArousal, "InitialArousal", seed);
            AssertDoesNotLeak(text, board.Hand.PatPat, "Hand.PatPat", seed);
            AssertDoesNotLeak(text, board.Hand.Milk, "Hand.Milk", seed);
            AssertDoesNotLeak(text, board.Hand.Hold, "Hand.Hold", seed);
            AssertDoesNotLeak(text, board.Hand.DiaperChange, "Hand.DiaperChange", seed);

            foreach (var e in board.Events)
            {
                Assert.That(text, Does.Not.Contain(e.Kind.ToString()),
                    $"**出来事 {e.Kind} の名前が結果に出ている**（REQ-028。seed={seed}）");
                AssertDoesNotLeak(text, e.Tick, $"Event.Tick({e.Kind})", seed);
            }
        }
    }

    private static void AssertDoesNotLeak(string text, int value, string what, string seed)
    {
        // 「一意に対応する語」まで見るのは実装を見ないと書けないので、
        // ここでは**数値そのものが出ていないこと**を見る。
        // 語の側は TC-161（診断）と合わせて、実装時にレビューで見る
        Assert.That(text, Does.Not.Contain(value.ToString()),
            $"**{what} の値 {value} が結果テキストに出ている**（REQ-028。seed={seed}）\n{text}");
    }

    [Test]
    public void TC089_同じ引数からは同じ文字列が出る()
    {
        var first = Result.Compose(Best(), 5, 1, Beats);

        for (var i = 0; i < 100; i++)
        {
            Assert.That(Result.Compose(Best(), 5, 1, Beats), Is.EqualTo(first),
                $"**{i + 1} 回目で文字列が変わった**（REQ-020）。実況に環境や乱数が混ざっている");
        }
    }

    private static IEnumerable<string> Seeds(int count) =>
        Enumerable.Range(0, count).Select(i =>
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(i).ToString("yyyy-MM-dd"));
}
