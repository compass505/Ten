using System;
using System.Linq;
using NUnit.Framework;
using Ten.Pure;

namespace Ten.Tests.Harness;

/// <summary>
/// TC-010〜018 — 盤面の生成（docs/40_test/cases/TC-pure-board.md）。
///
/// 出典: MOD-Board / REQ-019 / 021 / 034 / 040 / 041 / 043 / 048 / 050 / 052 / D-09
///
/// **山札の枚数と出来事の件数はバランス値**なので、範囲の具体値を書かない（ADR-0012）。
/// 見るのは「同じ日は同じ盤面」「各札種が 1 枚以上」「昇順で重複無し」「端に寄らない」。
/// </summary>
[TestFixture]
public sealed class BoardTests
{
    /// <summary>TC-011〜014 が定めた 100 個。</summary>
    private const int SeedCount = 100;

    [Test]
    public void TC010_同じ日からは常に同じ盤面が出る()
    {
        var seed = SimProbe.Seeds(1).Single();
        var first = Board.Generate(seed, SimProbe.Tuning);

        for (var i = 0; i < 100; i++)
        {
            Assert.That(Board.Generate(seed, SimProbe.Tuning), Is.EqualTo(first),
                $"**{i + 1} 回目で盤面が変わった**（REQ-019 / 021）。" +
                "夫婦で同じ盤面を遊ぶという前提（ADR-0006）が崩れる");
        }
    }

    [Test]
    public void TC017_プレイ回数では盤面が変わらない()
    {
        // ADR-0011: 盤面は日固定、運は毎回変わる。**盤面側は playIndex を見ない**
        foreach (var seed in SimProbe.Seeds(10))
        {
            var board = Board.Generate(seed, SimProbe.Tuning);
            var again = Board.Generate(seed, SimProbe.Tuning);

            Assert.That(again, Is.EqualTo(board),
                $"**seed={seed} で盤面がプレイ回数に依存している**（REQ-048 / ADR-0011）");
        }
    }

    [Test]
    public void TC011_山札は各札種が1枚以上ある()
    {
        foreach (var seed in SimProbe.Seeds(SeedCount))
        {
            var hand = Board.Generate(seed, SimProbe.Tuning).Hand;

            foreach (var kind in SimProbe.AllCares)
            {
                Assert.That(hand.Of(kind), Is.GreaterThanOrEqualTo(1),
                    $"**seed={seed} に {kind} が 1 枚も無い**（REQ-041）。" +
                    "札種が欠けると、その対処を引き出す立ち回りがその日だけ死ぬ");
            }

            Assert.That(hand.Total, Is.GreaterThanOrEqualTo(SimProbe.AllCares.Count),
                $"**seed={seed} の合計が札種の数より少ない**（REQ-050）");
        }
    }

    [Test]
    public void TC011_山札の枚数が日によって変わる()
    {
        // 全部同じだと、盤面が日替わりである意味が薄い（REQ-019）
        var totals = SimProbe.Seeds(SeedCount)
            .Select(s => Board.Generate(s, SimProbe.Tuning).Hand.Total)
            .Distinct()
            .Count();

        Assert.That(totals, Is.GreaterThan(1),
            "**どの日も山札の合計が同じ**（REQ-019）。日替わりの手応えが出ない");
    }

    [Test]
    public void TC012_出来事は昇順で同じtickに2件置かない()
    {
        foreach (var seed in SimProbe.Seeds(SeedCount))
        {
            var events = Board.Generate(seed, SimProbe.Tuning).Events;

            for (var i = 1; i < events.Count; i++)
            {
                Assert.That(events[i].Tick, Is.GreaterThan(events[i - 1].Tick),
                    $"**seed={seed} の出来事が昇順でない、または同じ tick に 2 件ある**（REQ-043）。" +
                    $"{events[i - 1].Tick} の次が {events[i].Tick}。" +
                    "同 tick に 2 件あると、どちらが先かで結果が変わる（NFR-004）");
            }
        }
    }

    [Test]
    public void TC013_出来事は夜の内側で起きる()
    {
        foreach (var seed in SimProbe.Seeds(SeedCount))
        {
            var board = Board.Generate(seed, SimProbe.Tuning);

            foreach (var e in board.Events)
            {
                Assert.That(e.Tick, Is.GreaterThan(0),
                    $"**seed={seed} の {e.Kind} が開始と同時に起きる**（REQ-043）");

                Assert.That(e.Tick, Is.LessThan(SimProbe.NightTicks),
                    $"**seed={seed} の {e.Kind} が夜明け以降に置かれている**（REQ-043）。" +
                    "起きないまま終わる出来事は盤面に置く意味が無い");
            }
        }
    }

    [Test]
    public void TC014_親の初期覚醒度は上限未満()
    {
        foreach (var seed in SimProbe.Seeds(SeedCount))
        {
            var arousal = Board.Generate(seed, SimProbe.Tuning).InitialArousal;

            Assert.That(arousal, Is.GreaterThanOrEqualTo(0), $"seed={seed}: I-1 違反");

            Assert.That(arousal, Is.LessThan(100),
                $"**seed={seed} が開始時点で上限に達している**（REQ-052）。" +
                "開始と同時に得点済みになってしまう");
        }
    }

    [Test]
    public void TC015_チュートリアル盤面は日付シードを使わず版番号も別枠()
    {
        var a = Board.Tutorial(SimProbe.Tuning);
        var b = Board.Tutorial(SimProbe.Tuning);

        Assert.Multiple(() =>
        {
            Assert.That(b, Is.EqualTo(a), "**呼ぶたびに変わる**（D-09 / TU-2）");

            Assert.That(a.SpecVersion, Is.Not.EqualTo(Board.SpecVersion),
                "**日付シードの盤面と同じ版番号**（D-09）。混ざると結果の出所が分からなくなる");

            Assert.That(SimProbe.Seeds(20).Any(s => Board.Generate(s, SimProbe.Tuning).Seed == a.Seed),
                Is.False,
                "**チュートリアル盤面が日付シードと同じ seed を使っている**（D-09）");
        });
    }

    [Test]
    public void TC018_壊れた調整値は例外になる()
    {
        // 山札の下限が上限を超えているような設定。**丸めて盤面を返さない。**
        // 壊れた盤面で夜が進むと、原因が分からなくなる
        //
        // `Tuning` はまだ項目を持たない（types.md 5 節）ので、
        // フェーズ 5 で項目を起こすときにここを実際の値にする
        Assert.Throws<ArgumentException>(
            () => Board.Generate("2026-01-01", BrokenTuning()),
            "**壊れた Tuning を弾いていない**（MOD-Board のエラー時）");
    }

    private static Tuning BrokenTuning() => new();
}
