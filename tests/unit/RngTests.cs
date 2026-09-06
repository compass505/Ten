using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Ten.Pure;

namespace Ten.Tests.Unit;

/// <summary>
/// TC-001〜007 — 乱数（MOD-Rng）。
///
/// 対象: docs/40_test/cases/TC-pure-board.md
/// 仕様: docs/30_detailed_design/MOD-Rng.md / docs/40_test/harness.md 2 節
/// 出典: ADR-0008（用途別の独立ストリーム）/ REQ-019 / REQ-048 / NFR-004
///
/// **バランス値を期待値に書かない**（ADR-0012 / test_first.md 4.1）。
/// ここに出てくる数値は、テストケースが定めた要件値と、参照ベクタの値だけ。
///
/// 失敗時にはシード・用途・通番を必ずメッセージに出す（test_first.md 2 節 D4）。
/// </summary>
[TestFixture]
public sealed class RngTests
{
    /// <summary>テストケースが「任意の seed」と言っている箇所で使う。日付形式は harness.md 2 節。</summary>
    private const string Seed = "2026-09-06";

    /// <summary>1 文字だけ違う seed（TC-002）。</summary>
    private const string SeedOneCharApart = "2026-09-05";

    private static string Where(string seed, RngPurpose purpose, int ordinal) =>
        $"seed=\"{seed}\" purpose={purpose}({(int)purpose}) ordinal={ordinal}";

    private static IEnumerable<RngPurpose> AllPurposes =>
        Enum.GetValues<RngPurpose>();

    // ------------------------------------------------------------------
    // TC-001: REQ-019 / NFR-004 — 同じ (seed, 用途, 通番) で 1000 回引くと全て同じ値
    // ------------------------------------------------------------------

    [Test]
    public void TC001_同じ入力を1000回引くと常に同じ値になる()
    {
        const int draws = 1000;

        foreach (var purpose in AllPurposes)
        {
            var first = Rng.Milli(Seed, purpose, 0);

            for (var i = 1; i < draws; i++)
            {
                var again = Rng.Milli(Seed, purpose, 0);
                Assert.That(again, Is.EqualTo(first),
                    $"{i} 回目で値が変わった。{Where(Seed, purpose, 0)} " +
                    $"（1 回目={first} / {i + 1} 回目={again}）。" +
                    "Rng が内部状態を持っている疑いがある（R-1 / NFR-004）");
            }
        }
    }

    // ------------------------------------------------------------------
    // TC-002: REQ-019 — seed が 1 文字違えば値が異なる（1000 組で一致率 < 1%）
    // ------------------------------------------------------------------

    [Test]
    public void TC002_1文字違いのseedでは値がほとんど一致しない()
    {
        const int pairs = 1000;
        const double maxMatchRate = 0.01;   // 要件値（TC-002 が定めた 1%）

        var matches = 0;

        for (var ordinal = 0; ordinal < pairs; ordinal++)
        {
            var a = Rng.Milli(Seed, RngPurpose.ParentHand, ordinal);
            var b = Rng.Milli(SeedOneCharApart, RngPurpose.ParentHand, ordinal);

            if (a == b)
            {
                matches++;
            }
        }

        var rate = (double)matches / pairs;

        Assert.That(rate, Is.LessThan(maxMatchRate),
            $"1 文字違いの seed で値が一致しすぎている（{matches}/{pairs} = {rate:P2}）。" +
            $"seed=\"{Seed}\" と \"{SeedOneCharApart}\" / purpose={RngPurpose.ParentHand}。" +
            "seed が結果に十分効いていない（REQ-019）");
    }

    // ------------------------------------------------------------------
    // TC-003: ADR-0008 — 用途は独立。他の用途を引いても値が動かない
    // ------------------------------------------------------------------

    [Test]
    public void TC003_用途を変えても他の用途の値が動かない()
    {
        const int ordinals = 100;   // TC-003 が定めた「通番 0〜99」

        // 1 周目: 用途ごとにまとめて引く
        var baseline = new Dictionary<(RngPurpose, int), int>();

        foreach (var purpose in AllPurposes)
        {
            for (var ordinal = 0; ordinal < ordinals; ordinal++)
            {
                baseline[(purpose, ordinal)] = Rng.Milli(Seed, purpose, ordinal);
            }
        }

        // 2 周目: 引く順序を変えて（通番ごとに全用途を横断して）引き直す。
        // 純関数なら結果は変わらない。変わったら共有カウンタを持っている。
        for (var ordinal = 0; ordinal < ordinals; ordinal++)
        {
            foreach (var purpose in AllPurposes)
            {
                var again = Rng.Milli(Seed, purpose, ordinal);
                Assert.That(again, Is.EqualTo(baseline[(purpose, ordinal)]),
                    $"引く順序を変えたら値が動いた。{Where(Seed, purpose, ordinal)} " +
                    $"（1 周目={baseline[(purpose, ordinal)]} / 2 周目={again}）。" +
                    "用途別ストリームが独立していない（ADR-0008）");
            }
        }
    }

    [Test]
    public void TC003_用途が違えば値の並びも違う()
    {
        const int ordinals = 100;
        const double maxMatchRate = 0.01;   // TC-002 と同じ基準を用途間にも当てる

        var purposes = AllPurposes.ToArray();

        for (var i = 0; i < purposes.Length; i++)
        {
            for (var j = i + 1; j < purposes.Length; j++)
            {
                var matches = Enumerable.Range(0, ordinals)
                    .Count(o => Rng.Milli(Seed, purposes[i], o) == Rng.Milli(Seed, purposes[j], o));

                var rate = (double)matches / ordinals;

                Assert.That(rate, Is.LessThan(maxMatchRate),
                    $"用途 {purposes[i]} と {purposes[j]} の値が一致しすぎている" +
                    $"（{matches}/{ordinals} = {rate:P2}）。seed=\"{Seed}\"。" +
                    "purpose が結果に効いていない（ADR-0008）");
            }
        }
    }

    // ------------------------------------------------------------------
    // TC-004: ADR-0008 — 通番を飛ばしても、その後の値が変わらない
    // ------------------------------------------------------------------

    [Test]
    public void TC004_通番を飛ばしても後続の値が変わらない()
    {
        const int ordinals = 100;
        const int skipped = 2;   // 0, 1, 3, 4 … と 2 を飛ばす

        var purpose = RngPurpose.NightEvent;

        // 飛ばさずに全部引く
        var full = Enumerable.Range(0, ordinals)
            .ToDictionary(o => o, o => Rng.Milli(Seed, purpose, o));

        // 1 つ飛ばして引く
        foreach (var ordinal in Enumerable.Range(0, ordinals).Where(o => o != skipped))
        {
            var value = Rng.Milli(Seed, purpose, ordinal);
            Assert.That(value, Is.EqualTo(full[ordinal]),
                $"通番 {skipped} を飛ばしたら通番 {ordinal} の値が動いた。" +
                $"{Where(Seed, purpose, ordinal)}（飛ばさない={full[ordinal]} / 飛ばした={value}）。" +
                "通番が「引いた回数」になっている（ADR-0008 は通番を引数と定めている）");
        }
    }

    // ------------------------------------------------------------------
    // TC-005: NFR-004 — Range の一様性（各値の出現が期待値 ±3%）
    // ------------------------------------------------------------------

    [Test]
    public void TC005_Rangeの出現が期待値の3パーセント以内に収まる()
    {
        const double tolerance = 0.03;   // 要件値（TC-005 が定めた ±3%）

        var vectors = RngVectorFile.Load();
        var max = vectors.UniformityCheck.Max;
        var n = vectors.UniformityCheck.N;
        var expectedEach = (double)n / max;

        Assert.That(expectedEach, Is.EqualTo((double)vectors.UniformityCheck.ExpectedEach).Within(0.5),
            "参照ベクタの n / max / expectedEach が整合していない。tests/vectors/rng.json を疑う");

        var counts = new int[max];

        for (var ordinal = 0; ordinal < n; ordinal++)
        {
            var value = Rng.Range(Seed, RngPurpose.ParentChoice, ordinal, max);

            Assert.That(value, Is.InRange(0, max - 1),
                $"Range が範囲外の値を返した（{value}）。{Where(Seed, RngPurpose.ParentChoice, ordinal)} max={max}");

            counts[value]++;
        }

        for (var value = 0; value < max; value++)
        {
            var deviation = Math.Abs(counts[value] - expectedEach) / expectedEach;

            Assert.That(deviation, Is.LessThanOrEqualTo(tolerance),
                $"値 {value} の出現が偏っている（{counts[value]} 回 / 期待 {expectedEach:F0} 回 = " +
                $"ずれ {deviation:P2}）。seed=\"{Seed}\" purpose={RngPurpose.ParentChoice} " +
                $"max={max} n={n}。剰余バイアスが残っている疑いがある（MOD-Rng の棄却法）");
        }
    }

    // ------------------------------------------------------------------
    // TC-006: NFR-004 — 実装が System.Random / UnityEngine を参照していない（静的検査）
    // ------------------------------------------------------------------

    [Test]
    public void TC006_Rngの実装が環境の乱数を参照していない()
    {
        // 禁止語。MOD-Rng「実装をこちらで持つ」（R-5）
        string[] forbidden = ["System.Random", "UnityEngine", "new Random(", "Guid.NewGuid", "DateTime."];

        var dir = RepoPaths.PureSource;

        Assert.That(Directory.Exists(dir), Is.True,
            $"純粋層の実装が見つからない: {dir}。" +
            "フェーズ 5 でここに MOD-Rng を置く（それまでこのテストは落ちているのが正しい）");

        var sources = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);

        Assert.That(sources, Is.Not.Empty, $"純粋層に .cs が 1 つも無い: {dir}");

        foreach (var file in sources)
        {
            var text = File.ReadAllText(file);

            foreach (var word in forbidden)
            {
                Assert.That(text, Does.Not.Contain(word),
                    $"{Path.GetRelativePath(RepoPaths.Root, file)} が \"{word}\" を参照している。" +
                    "純粋層は環境に触らない（NFR-004 / harness.md 1 節）。" +
                    "乱数はシード、時間は引数で受け取る");
            }
        }
    }

    // ------------------------------------------------------------------
    // TC-007: 不正な引数は例外を投げる（握り潰さない）
    // ------------------------------------------------------------------

    [Test]
    public void TC007_通番が負なら例外を投げる()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Rng.Milli(Seed, RngPurpose.ParentInitial, -1),
            "ordinal < 0 を握り潰している（MOD-Rng「呼び出し側のバグ」）");
    }

    [Test]
    public void TC007_上限が0以下なら例外を投げる()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Rng.Range(Seed, RngPurpose.ParentInitial, 0, 0),
            "exclusiveMax <= 0 を握り潰している（MOD-Rng）");

        Assert.Throws<ArgumentOutOfRangeException>(
            () => Rng.Range(Seed, RngPurpose.ParentInitial, 0, -1),
            "exclusiveMax <= 0 を握り潰している（MOD-Rng）");
    }

    [Test]
    public void TC007_seedが空なら例外を投げる()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Rng.Milli(string.Empty, RngPurpose.ParentInitial, 0),
            "空の seed を握り潰している（MOD-Rng）");
    }

    // ------------------------------------------------------------------
    // TC-165: NFR-004 — 参照ベクタに完全一致する（docs/40_test/harness.md 2 節）
    //
    // 「tests/vectors/rng.json を正とする。C# 実装がこれと 1 件でも食い違ったら
    //   NFR-004 が成立しない」
    //
    // hash 節は 32 ビット全部を固定する。Milli は上位 10 ビット相当、
    // Range は下位数ビット相当しか固定しないため、hash を見ないと
    // 最終撹拌の中間ビットが未検証のまま残る。
    // ------------------------------------------------------------------

    [Test]
    public void TC165_参照ベクタのHashと完全に一致する()
    {
        var vectors = RngVectorFile.Load();

        Assert.That(vectors.Hash, Is.Not.Empty, "hash の参照ベクタが空");

        foreach (var v in vectors.Hash)
        {
            var purpose = (RngPurpose)v.Purpose;
            var actual = Rng.Hash(v.Seed, purpose, v.Ordinal);

            Assert.That(actual, Is.EqualTo(v.Expected),
                $"参照ベクタと食い違った。{Where(v.Seed, purpose, v.Ordinal)} " +
                $"（期待={v.Expected} / 実際={actual}）。" +
                "FNV-1a または最終撹拌の実装が harness.md 2 節と違う。NFR-004 が成立しない");
        }
    }

    [Test]
    public void TC165_参照ベクタのMilliと完全に一致する()
    {
        var vectors = RngVectorFile.Load();

        Assert.That(vectors.Milli, Is.Not.Empty, "milli の参照ベクタが空");

        foreach (var v in vectors.Milli)
        {
            var purpose = (RngPurpose)v.Purpose;
            var actual = Rng.Milli(v.Seed, purpose, v.Ordinal);

            Assert.That(actual, Is.EqualTo(v.Expected),
                $"参照ベクタと食い違った。{Where(v.Seed, purpose, v.Ordinal)} " +
                $"（期待={v.Expected} / 実際={actual}）。NFR-004 が成立しない");
        }
    }

    [Test]
    public void TC165_参照ベクタのRangeと完全に一致する()
    {
        var vectors = RngVectorFile.Load();

        Assert.That(vectors.Range, Is.Not.Empty, "range の参照ベクタが空");

        foreach (var v in vectors.Range)
        {
            var purpose = (RngPurpose)v.Purpose;
            var actual = Rng.Range(v.Seed, purpose, v.Ordinal, v.Max);

            Assert.That(actual, Is.EqualTo(v.Expected),
                $"参照ベクタと食い違った。{Where(v.Seed, purpose, v.Ordinal)} max={v.Max} " +
                $"（期待={v.Expected} / 実際={actual}）。NFR-004 が成立しない");
        }
    }
}
