using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Ten.Pure;

namespace Ten.Tests.Unit;

/// <summary>
/// TC-166〜169 — 実況の素材と診断の濃さ（docs/40_test/cases/TC-phase6.md）。
///
/// 出典: MOD-Result RES-02（素材の選び方）/ diagnosis.md 2 節（濃さ）/ REQ-026 / 062
///
/// **バランス値を書かない**（ADR-0012）。濃さの境界は `Tuning` から読む。
/// </summary>
[TestFixture]
public sealed class CommentaryTests
{
    private static readonly Tuning Tuning = new();

    private static NightState Start()
    {
        var board = Board.Generate("2026-09-13", Tuning);

        return Sim.Begin(board, 1, Tuning);
    }

    // ------------------------------------------------------------------
    // TC-166: 同じ種類は最初の 1 回だけ積まれる
    // ------------------------------------------------------------------

    [Test]
    public void TC166_最初の一撃は得点が0から1になった瞬間に1回だけ積まれる()
    {
        var before = Start();
        var scored = before with { Tick = 120, Score = 1 };
        var again = scored with { Tick = 400, Score = 2 };

        var beats = Commentary.Observe(Array.Empty<Result.Beat>(), before, scored, Tuning);
        beats = Commentary.Observe(beats, scored, again, Tuning);

        Assert.That(beats.Count(b => b.Kind == Result.BeatKind.FirstScore), Is.EqualTo(1),
            "**最初の一撃が 2 回以上積まれている / 積まれていない**（RES-02）");
        Assert.That(beats.Single(b => b.Kind == Result.BeatKind.FirstScore).Tick, Is.EqualTo(120),
            "**積んだ tick が一撃の瞬間ではない**");
    }

    [Test]
    public void TC166_何も起きていないtickでは積まれない()
    {
        var s = Start();
        var next = s with { Tick = 1 };

        Assert.That(Commentary.Observe(Array.Empty<Result.Beat>(), s, next, Tuning), Is.Empty,
            "**何も起きていないのに実況の素材が積まれた**（RES-02）");
    }

    [Test]
    public void TC166_寝たふりが着地した瞬間と夜の終わりが積まれる()
    {
        var s = Start();
        var carried = s with { Tick = 900, Parent = ParentPhase.Settling };
        var landed = carried with { Tick = 901, Parent = ParentPhase.Grace };
        var ended = landed with { Tick = 902, Over = EndKind.Dawn };

        var beats = Commentary.Observe(Array.Empty<Result.Beat>(), carried, landed, Tuning);
        beats = Commentary.Observe(beats, landed, ended, Tuning);

        Assert.Multiple(() =>
        {
            Assert.That(beats.Any(b => b.Kind == Result.BeatKind.BestPretend), Is.True,
                "**寝たふりの着地が積まれていない**");
            Assert.That(beats.Any(b => b.Kind == Result.BeatKind.DawnReached), Is.True,
                "**夜明けで終わったことが積まれていない**（REQ-054）");
        });
    }

    [Test]
    public void TC166_失敗側の満了では寝たふりが通ったと積まない()
    {
        // Feint の満了は Sleeping に戻る。**Settling → Grace だけが「通った」**
        var s = Start();
        var feint = s with { Tick = 900, Parent = ParentPhase.Feint };
        var back = feint with { Tick = 901, Parent = ParentPhase.Sleeping };

        Assert.That(Commentary.Observe(Array.Empty<Result.Beat>(), feint, back, Tuning)
                .Any(b => b.Kind == Result.BeatKind.BestPretend), Is.False,
            "**失敗した寝たふりを「通った」と実況している**");
    }

    // ------------------------------------------------------------------
    // TC-167: 3 件以内に絞り、起きた順に並べる
    // ------------------------------------------------------------------

    [Test]
    public void TC167_3件以内に絞り終わり方を残して起きた順に並べる()
    {
        IReadOnlyList<Result.Beat> all =
        [
            new(100, Result.BeatKind.EventStruck),
            new(300, Result.BeatKind.HandRanLow),
            new(500, Result.BeatKind.FirstScore),
            new(900, Result.BeatKind.BestPretend),
            new(5400, Result.BeatKind.DawnReached),
        ];

        var picked = Commentary.Pick(all);

        Assert.Multiple(() =>
        {
            Assert.That(picked.Count, Is.LessThanOrEqualTo(Commentary.Max), "**3 件を超えた**（REQ-026）");
            Assert.That(picked.Any(b => b.Kind == Result.BeatKind.DawnReached), Is.True,
                "**終わり方が落ちた。**夜の締めが実況に残らない");
            Assert.That(picked.Select(b => b.Tick), Is.Ordered, "**起きた順に並んでいない**");
        });
    }

    [Test]
    public void TC167_同じ素材からは常に同じ3件が選ばれる()
    {
        IReadOnlyList<Result.Beat> all =
        [
            new(10, Result.BeatKind.HandRanLow),
            new(20, Result.BeatKind.EventStruck),
            new(30, Result.BeatKind.FirstScore),
            new(40, Result.BeatKind.FellAsleepAt),
        ];

        Assert.That(Commentary.Pick(all), Is.EqualTo(Commentary.Pick(all.Reverse().ToList())),
            "**並び順で選ばれるものが変わる**（決定論 / REQ-020）");
    }

    // ------------------------------------------------------------------
    // TC-168: 保存用の文字列と往復する
    // ------------------------------------------------------------------

    [Test]
    public void TC168_保存用の文字列と往復する()
    {
        IReadOnlyList<Result.Beat> beats =
        [
            new(12, Result.BeatKind.FirstScore),
            new(3456, Result.BeatKind.FellAsleepAt),
        ];

        Assert.That(Commentary.Decode(Commentary.Encode(beats)), Is.EqualTo(beats),
            "**保存して読み直すと実況の素材が変わる**（data_model.md 3 節）");
    }

    [Test]
    public void TC168_壊れた文字列を読んでも例外を投げない()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => Commentary.Decode("壊れた;;;x:y"), Throws.Nothing, "REQ-033");
            Assert.That(Commentary.Decode(null), Is.Empty);
            Assert.That(Commentary.Decode(string.Empty), Is.Empty);
        });
    }

    // ------------------------------------------------------------------
    // TC-169: 診断の濃さ（G-03）
    // ------------------------------------------------------------------

    [Test]
    public void TC169_合計が小さいとうっすら大きいとどっぷり()
    {
        var thin = new Diagnosis(Tuning.DiagnosisThinTotal - 1, 0, 0, 0, 0, 0, 0);
        var plain = new Diagnosis(Tuning.DiagnosisThinTotal, 0, 0, 0, 0, 0, 0);
        var thick = new Diagnosis(Tuning.DiagnosisThickTotal, 0, 0, 0, 0, 0, 0);

        Assert.Multiple(() =>
        {
            Assert.That(Result.StrengthOf(thin, Tuning), Is.EqualTo(Result.Strength.Thin));
            Assert.That(Result.StrengthOf(plain, Tuning), Is.EqualTo(Result.Strength.Plain));
            Assert.That(Result.StrengthOf(thick, Tuning), Is.EqualTo(Result.Strength.Thick));
        });
    }

    [Test]
    public void TC169_修飾語は名前に付き並は付かない()
    {
        var entry = Result.Diagnose(new Diagnosis(50, 10, 0, 10, 10, 10, 0), Tuning);

        Assert.Multiple(() =>
        {
            Assert.That(Result.Title(entry, Result.Strength.Plain), Is.EqualTo(entry.Name),
                "**並なのに修飾語が付いた**（diagnosis.md 2 節）");
            Assert.That(Result.Title(entry, Result.Strength.Thin), Does.Contain(entry.Name).And.Contain("うっすら"));
            Assert.That(Result.Title(entry, Result.Strength.Thick), Does.Contain(entry.Name).And.Contain("どっぷり"));
        });
    }

    [Test]
    public void TC169_何も起きなかった夜には修飾語を付けない()
    {
        var nothing = Result.Diagnose(default, Tuning);

        Assert.That(Result.Title(nothing, Result.Strength.Thin), Is.EqualTo(nothing.Name),
            "**「うっすら何も起きなかった」になっている**（DX-41 は濃さを持たない）");
    }
}
