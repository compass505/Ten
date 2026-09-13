using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Ten.Pure;

namespace Ten.Tests.Harness;

/// <summary>
/// TC-159〜163 — 一晩の診断（ADR-0016 / REQ-062）。
///
/// 出典: docs/20_basic_design/diagnosis.md
/// **診断は得点と別レイヤー**（順位には関わらない）。見せ合って笑うための「模様」。
/// </summary>
[TestFixture]
public sealed class DiagnosisTests
{
    /// <summary>TC-160 / 162 が定めた 24 シード。</summary>
    private const int SeedCount = 24;

    /// <summary>diagnosis.md 1 節の 7 軸。</summary>
    private const int Axes = 7;

    /// <summary>TC-162 が定めた相関の上限。</summary>
    private const double MaxCorrelation = 0.6;

    /// <summary>ADR-0016: 合計が閾値未満なら固定で返す診断。</summary>
    private const string FallbackId = "DX-41";

    /// <summary>diagnosis.md 4 節が言う「立ち回りの違う 7 方針」。</summary>
    private enum Style { Tap, Charge, Pretend, Mixed, Idle, EyesOnly, Burst }

    [Test]
    public void TC159_同じ入力列からは常に同じ診断が出る()
    {
        var board = SimProbe.BoardOf(SimProbe.Seeds(1).Single());
        var diag = PlayFor(board, Style.Mixed).Diag;

        var first = Result.Diagnose(diag, SimProbe.Tuning);

        for (var i = 0; i < 50; i++)
        {
            Assert.That(Result.Diagnose(diag, SimProbe.Tuning), Is.EqualTo(first),
                $"**{i + 1} 回目で診断が変わった**（REQ-062）。" +
                "コサイン類似度の同点はカタログの並び順で先を採る（ADR-0016）");
        }
    }

    [Test]
    public void TC160_立ち回りが違えば診断も割れる()
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var seed in SimProbe.Seeds(SeedCount))
        {
            var board = SimProbe.BoardOf(seed);

            foreach (var style in Enum.GetValues<Style>())
            {
                var entry = Result.Diagnose(PlayFor(board, style).Diag, SimProbe.Tuning);
                counts[entry.Id] = counts.GetValueOrDefault(entry.Id) + 1;
            }
        }

        var total = counts.Values.Sum();
        var top = counts.MaxBy(kv => kv.Value);
        var share = (double)top.Value / total;

        Assert.Multiple(() =>
        {
            Assert.That(counts, Has.Count.GreaterThan(1),
                "**全部同じ診断になっている**（REQ-062）。模様になっていない");

            Assert.That(share, Is.LessThan(0.5),
                $"**{top.Key} が {share:P0} を占めている**（REQ-062）。" +
                "偏ると見せ合っても会話にならない（ADR-0006）");
        });
    }

    [Test]
    public void TC161_診断に盤面の答えが含まれない()
    {
        foreach (var seed in SimProbe.Seeds(SeedCount))
        {
            var board = SimProbe.BoardOf(seed);
            var entry = Result.Diagnose(PlayFor(board, Style.Mixed).Diag, SimProbe.Tuning);
            var text = entry.Name + entry.Text;

            foreach (var value in new[]
            {
                board.InitialArousal, board.Hand.PatPat, board.Hand.Milk,
                board.Hand.Hold, board.Hand.DiaperChange,
            })
            {
                Assert.That(text, Does.Not.Contain(value.ToString()),
                    $"**盤面の値 {value} が診断に出ている**（REQ-028。seed={seed}）\n{text}");
            }

            foreach (var e in board.Events)
            {
                Assert.That(text, Does.Not.Contain(e.Kind.ToString()),
                    $"**出来事 {e.Kind} の名前が診断に出ている**（REQ-028。seed={seed}）");
            }
        }
    }

    [Test]
    public void TC162_7軸のどの2本も相関が高すぎない()
    {
        var samples = new List<int[]>();

        foreach (var seed in SimProbe.Seeds(SeedCount))
        {
            var board = SimProbe.BoardOf(seed);

            foreach (var style in Enum.GetValues<Style>())
            {
                samples.Add(AxesOf(PlayFor(board, style).Diag));
            }
        }

        for (var i = 0; i < Axes; i++)
        {
            for (var j = i + 1; j < Axes; j++)
            {
                var r = Math.Abs(Correlation(
                    samples.Select(s => (double)s[i]).ToArray(),
                    samples.Select(s => (double)s[j]).ToArray()));

                Assert.That(r, Is.LessThanOrEqualTo(MaxCorrelation),
                    $"**軸 {i} と {j} の相関が {r:F2}**（上限 {MaxCorrelation}）。" +
                    "1 軸に潰れていると、7 軸ある意味が無い（ADR-0016 が案 D を棄却した理由）");
            }
        }
    }

    [Test]
    public void TC163_ほぼ何も起きなかった夜は固定の診断が返る()
    {
        // 合計が閾値未満だと割合ベクトルが定義できないので、DX-41 を固定で返す（ADR-0016）
        var empty = new Diagnosis(0, 0, 0, 0, 0, 0, 0);

        Assert.That(Result.Diagnose(empty, SimProbe.Tuning).Id, Is.EqualTo(FallbackId),
            $"**合計 0 の夜に {FallbackId} が返らない**（ADR-0016）。" +
            "割合ベクトルが定義できない入力で近傍マッチを走らせてはいけない");
    }

    // ------------------------------------------------------------------

    private static int[] AxesOf(Diagnosis d) =>
        [d.Irritation, d.Fatigue, d.Futility, d.SleepLoss, d.Anxiety, d.Jerked, d.Fondness];

    /// <summary>その方針で 1 夜を通した最終状態。</summary>
    private static NightState PlayFor(BoardSpec board, Style style)
    {
        var s = SimProbe.Begin(board);

        while (s.Over is null)
        {
            var before = s.Tick;

            s = style switch
            {
                Style.Tap => SimProbe.Act(s, board, ActionKind.Cry, 1),
                Style.Charge => SimProbe.ActFullStrength(s, board, ActionKind.Cry),
                Style.Pretend => SimProbe.RunTicks(SimProbe.CloseEyes(s, board), board, 120),
                Style.Mixed => s.Arousal < 50
                    ? SimProbe.ActFullStrength(s, board, ActionKind.Cry)
                    : SimProbe.RunTicks(SimProbe.CloseEyes(s, board), board, 120),
                Style.Idle => SimProbe.RunTicks(s, board, 120),
                Style.EyesOnly => SimProbe.RunTicks(SimProbe.Step(s, new TickInput(null, true), board), board, 30),
                Style.Burst => SimProbe.AllActions.Aggregate(s, (acc, k) => SimProbe.Act(acc, board, k, 3)),
                _ => SimProbe.RunTicks(s, board, 1),
            };

            // 進まない方針で無限ループにしない
            if (s.Over is null && s.Tick == before)
            {
                s = SimProbe.RunTicks(s, board, 1);
            }
        }

        return s;
    }

    private static double Correlation(double[] a, double[] b)
    {
        var meanA = a.Average();
        var meanB = b.Average();

        var cov = a.Zip(b, (x, y) => (x - meanA) * (y - meanB)).Sum();
        var devA = Math.Sqrt(a.Sum(x => (x - meanA) * (x - meanA)));
        var devB = Math.Sqrt(b.Sum(y => (y - meanB) * (y - meanB)));

        return devA == 0 || devB == 0 ? 0 : cov / (devA * devB);
    }
}
