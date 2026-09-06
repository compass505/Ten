using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Ten.Pure;

namespace Ten.Tests.Harness;

/// <summary>
/// **ハーネス自身のテスト。**TC-xxx ではない。
///
/// 検証対象は製品（`src/`）ではなく、入力列の読み書きと失敗報告という**道具**。
/// 道具が壊れていると、そこから先の全テストが信用できなくなる。
///
/// **フェーズ 4 の DoD「全て落ちる」は適用しない**（道具は実装済みなので green が正しい）。
/// → docs/40_test/traceability.md の「実装前に green になるテスト」
/// </summary>
[TestFixture]
public sealed class TraceFileTests
{
    /// <summary>docs/40_test/harness.md 3 節に載っている例そのもの。</summary>
    private const string Sample =
        "# seed=2026-09-06 play=1 spec=1 tuning=a3f91c\n" +
        "12   +cry\n" +
        "81   -\n" +
        "140  eyes\n" +
        "203  +kick\n" +
        "219  -\n" +
        "1180 eyes\n";

    private static TraceHeader Header => new("2026-09-06", 1, 1, "a3f91c");

    [Test]
    public void harness仕様の例をそのまま読める()
    {
        var parsed = TraceFile.Parse(Sample);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Header.Seed, Is.EqualTo("2026-09-06"));
            Assert.That(parsed.Header.Play, Is.EqualTo(1));
            Assert.That(parsed.Header.Spec, Is.EqualTo(1));
            Assert.That(parsed.Header.Tuning, Is.EqualTo("a3f91c"));
            Assert.That(parsed.Trace.Entries.Select(e => e.Tick),
                Is.EqualTo(new[] { 12, 81, 140, 203, 219, 1180 }));
            Assert.That(parsed.Trace.Entries[0].Input.Held, Is.EqualTo(ActionKind.Cry));
            Assert.That(parsed.Trace.Entries[1].Input.Held, Is.Null, "「-」で離れている");
            Assert.That(parsed.Trace.Entries[2].Input.ToggleEyes, Is.True);
            Assert.That(parsed.Trace.Entries[3].Input.Held, Is.EqualTo(ActionKind.Kick));
        });
    }

    [Test]
    public void 書いて読み直すと同じ列になる()
    {
        var first = TraceFile.Parse(Sample);
        var round = TraceFile.Parse(TraceFile.Write(first));

        Assert.Multiple(() =>
        {
            Assert.That(round.Header, Is.EqualTo(first.Header));
            Assert.That(round.Trace.Entries, Is.EqualTo(first.Trace.Entries));
        });
    }

    [Test]
    public void 空行とコメントを読み飛ばす()
    {
        var parsed = TraceFile.Parse(
            "# seed=s play=0 spec=1 tuning=t\n" +
            "\n" +
            "# ここはコメント\n" +
            "5 eyes\n");

        Assert.That(parsed.Trace.Entries, Has.Count.EqualTo(1));
    }

    // ------------------------------------------------------------------
    // 壊れた列は握り潰さない。黙って捨てると再現できない失敗の原因になる
    // ------------------------------------------------------------------

    [TestCase("# seed=s play=0 spec=1 tuning=t\n5 eyes\n5 eyes\n", "同じ tick", TestName = "同じtickが2行あると落ちる")]
    [TestCase("# seed=s play=0 spec=1 tuning=t\n9 eyes\n5 eyes\n", "昇順", TestName = "tickが降順だと落ちる")]
    [TestCase("# seed=s play=0 spec=1 tuning=t\n5 +sing\n", "知らない行動", TestName = "知らない行動で落ちる")]
    [TestCase("# seed=s play=0 spec=1 tuning=t\n5 wat\n", "知らない変化", TestName = "知らない変化で落ちる")]
    [TestCase("# seed=s play=0 spec=1 tuning=t\n5 -\n", "押していない", TestName = "押していないのに離すと落ちる")]
    [TestCase("# seed=s play=0 spec=1 tuning=t\n5 +cry\n", "押しっぱなし", TestName = "押しっぱなしのまま終わると落ちる")]
    [TestCase("# seed=s play=0 spec=1 tuning=t\n5 +cry\n9 +kick\n", "押したまま", TestName = "離さずに次を押すと落ちる")]
    [TestCase("5 eyes\n", "ヘッダ行が無い", TestName = "ヘッダが無いと落ちる")]
    [TestCase("# play=0 spec=1 tuning=t\n5 eyes\n", "seed", TestName = "ヘッダにseedが無いと落ちる")]
    [TestCase("# seed=s play=x spec=1 tuning=t\n5 eyes\n", "整数でない", TestName = "playが整数でないと落ちる")]
    public void 壊れた列は理由つきで落ちる(string text, string expectedInMessage)
    {
        var ex = Assert.Throws<TraceFormatException>(() => TraceFile.Parse(text));

        Assert.That(ex!.Message, Does.Contain(expectedInMessage),
            $"落ちてはいるが、理由が読み取れない。実際のメッセージ:\n{ex.Message}");
    }

    // ------------------------------------------------------------------
    // 展開。1 行 1 変化 → tick ごとの入力
    // ------------------------------------------------------------------

    [Test]
    public void 押しっぱなしは離すまで続く()
    {
        var parsed = TraceFile.Parse(Sample);
        var expanded = TraceFile.Expand(parsed.Trace, 100).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(expanded[11].Held, Is.Null, "押す前");
            Assert.That(expanded[12].Held, Is.EqualTo(ActionKind.Cry), "押した tick");
            Assert.That(expanded[50].Held, Is.EqualTo(ActionKind.Cry), "間の tick は続く");
            Assert.That(expanded[80].Held, Is.EqualTo(ActionKind.Cry), "離す直前");
            Assert.That(expanded[81].Held, Is.Null, "離した tick");
        });
    }

    [Test]
    public void 目の開閉はその1tickだけで持ち越さない()
    {
        var parsed = TraceFile.Parse(Sample);
        var expanded = TraceFile.Expand(parsed.Trace, 200).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(expanded[140].ToggleEyes, Is.True, "eyes の tick");
            Assert.That(expanded[141].ToggleEyes, Is.False,
                "**持ち越すと目が開閉し続ける。**押しっぱなしと違って 1 回きり");
        });
    }

    [Test]
    public void 展開の長さは指定どおりになる()
    {
        var parsed = TraceFile.Parse(Sample);

        Assert.That(TraceFile.Expand(parsed.Trace, 5400).Count(), Is.EqualTo(5400));
    }

    // ------------------------------------------------------------------
    // 失敗時の出力（D4）
    // ------------------------------------------------------------------

    [Test]
    public void 失敗報告に再現に要るものが全部入る()
    {
        var dir = Path.Combine(Path.GetTempPath(), "ten-harness-" + Guid.NewGuid().ToString("N"));

        try
        {
            var report = FailureReport.Build(
                "TC-031",
                "上昇量が単調非増加",
                "ar=30 で +12, ar=60 で +14",
                TraceFile.Parse(Sample),
                tick: 1180,
                traceRoot: dir,
                stamp: new DateTimeOffset(2026, 9, 6, 14, 30, 22, TimeSpan.Zero));

            Assert.Multiple(() =>
            {
                Assert.That(report, Does.Contain("TC-031"), "どのケースか");
                Assert.That(report, Does.Contain("seed=2026-09-06"), "盤面を復元できるか");
                Assert.That(report, Does.Contain("play=1"));
                Assert.That(report, Does.Contain("spec=1"));
                Assert.That(report, Does.Contain("tuning=a3f91c"),
                    "**実装が壊れたのか Tuning が変わったのかを切り分けるため**");
                Assert.That(report, Does.Contain("tick=1180"), "どこで壊れたか");
                Assert.That(report, Does.Contain("22:58"), "ゲーム内時刻");
                Assert.That(report, Does.Contain("failed-20260906-143022.trace"), "入力列の在り処");
                Assert.That(report, Does.Contain("dotnet test --filter TC-031"), "再現コマンド");
            });

            var saved = Path.Combine(dir, FailureReport.TraceDirName, "failed-20260906-143022.trace");

            Assert.That(File.Exists(saved), Is.True,
                "**入力列がファイルに残っていない。**ランダム列は保存しないと二度と再現できない");

            Assert.That(TraceFile.Parse(File.ReadAllText(saved)).Trace.Entries,
                Is.EqualTo(TraceFile.Parse(Sample).Trace.Entries),
                "保存した列を読み直せない");
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
    }

    [Test]
    public void ゲーム内時刻がharness仕様の例と一致する()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GameClock.Format(0), Is.EqualTo("21:00"), "夜の始まり（REQ-007）");
            Assert.That(GameClock.Format(1180), Is.EqualTo("22:58"), "harness.md 4 節の例");
            Assert.That(GameClock.Format(5400), Is.EqualTo("06:00"), "夜明け（REQ-007）");
        });
    }
}

/// <summary>
/// ランダム入力列の生成（harness.md 5 節）。
///
/// **`Rng` に依存するので、フェーズ 4 では赤になる。**これは道具のテストだが、
/// 製品の空実装を踏むため上の TraceFileTests と扱いが違う。
/// </summary>
[TestFixture]
public sealed class TraceGeneratorTests
{
    private static TraceHeader Header => new("2026-09-06", 1, 1, "a3f91c");

    [Test]
    public void 同じseedとindexから同じ列が出る()
    {
        var a = TraceGenerator.Generate(Header, index: 0, tickCount: 5400, changeCount: 40);
        var b = TraceGenerator.Generate(Header, index: 0, tickCount: 5400, changeCount: 40);

        Assert.That(b.Trace.Entries, Is.EqualTo(a.Trace.Entries),
            "**生成が決定論でない。**落ちた列を再現できなくなる（harness.md 5 節）");
    }

    [Test]
    public void indexが違えば違う列が出る()
    {
        var a = TraceGenerator.Generate(Header, index: 0, tickCount: 5400, changeCount: 40);
        var b = TraceGenerator.Generate(Header, index: 1, tickCount: 5400, changeCount: 40);

        Assert.That(b.Trace.Entries, Is.Not.EqualTo(a.Trace.Entries),
            "何本生成しても同じ列では、入力列を振ったことにならない");
    }

    [Test]
    public void 生成した列はそのまま読み書きできる()
    {
        var generated = TraceGenerator.Generate(Header, index: 3, tickCount: 5400, changeCount: 40);
        var round = TraceFile.Parse(TraceFile.Write(generated));

        Assert.That(round.Trace.Entries, Is.EqualTo(generated.Trace.Entries),
            "**生成した列を保存できない。**落ちたときに残せない");
    }
}
