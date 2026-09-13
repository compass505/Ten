using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Ten.Boundary;
using Ten.Pure;

namespace Ten.Tests.Harness;

/// <summary>
/// TC-170〜175 — 状態 → 見せ方（MOD-Present PR-1〜PR-7）。
///
/// 出典: setting.md 4〜8 節 / REQ-006 / 016 / 030 / 044 / 055 / ADR-0017 / ADR-0022
///
/// **REQ-016 は画面側でも漏れうる。**TC-164 は状態しか見ず、TC-125 は Unity で撮る。
/// その間にある「状態 → 何を描くか」の写像をここで見る。
/// </summary>
[TestFixture]
public sealed class PresentTests
{
    private static Tuning T => SimProbe.Tuning;

    private static Presentation Of(NightState s, BoardSpec board) =>
        PresentRule.Of(s, Display.Map(s, board, T), board, T);

    /// <summary>
    /// 夜の中で実際に通る状態を集める。**寝たふり・対処・起床が全部混ざる入力**で走らせる。
    /// </summary>
    private static IEnumerable<(NightState State, BoardSpec Board)> Visited(int seeds = 6)
    {
        foreach (var seed in SimProbe.Seeds(seeds))
        {
            var board = SimProbe.BoardOf(seed);
            var s = SimProbe.Begin(board);

            for (var tick = 0; tick < SimProbe.NightTicks && s.Over is null; tick++)
            {
                // 400 tick 周期: 泣きを溜めて離す → 待つ → 目を閉じて待つ → 開ける
                var phase = tick % 400;
                var input = phase switch
                {
                    < 30 => new TickInput(ActionKind.Cry, false),
                    150 => new TickInput(null, true),
                    330 => new TickInput(null, true),
                    _ => new TickInput(null, false),
                };

                s = SimProbe.Step(s, input, board);

                yield return (s, board);
            }
        }
    }

    // ------------------------------------------------------------------
    // TC-170: 閉眼中の見せ方は、親の状態の中身から独立（REQ-016 / ADR-0014）
    // ------------------------------------------------------------------

    [Test]
    public void TC170_閉眼中の見せ方は覚醒度も山札も出来事も映さない()
    {
        var checkedCount = 0;

        foreach (var (s, board) in Visited().Where(v => v.State.Baby == BabyPhase.EyesClosed).Take(4000))
        {
            var baseline = Of(s, board);

            var variants = new[]
            {
                s with { Arousal = s.Arousal >= 50 ? 5 : 95 },
                s with { Hand = new HandCount(1, 0, 0, 0) },
                s with { RollUntil = s.Tick + 50, HungryUntil = s.Tick + 50, PartnerHere = !s.PartnerHere },
                s with { PendingCare = CareKind.Milk, CareDelay = 5 },
                s with { Vigor = s.Vigor >= 50 ? 1 : 99 },
            };

            foreach (var v in variants)
            {
                Assert.That(Of(v, board), Is.EqualTo(baseline),
                    $"**閉眼中なのに見せ方が親の状態で変わる**（REQ-004 / 016 / ADR-0014）。" +
                    $"tick={s.Tick} Parent={s.Parent}\n元: {baseline}\n変: {Of(v, board)}");
            }

            checkedCount++;
        }

        Assert.That(checkedCount, Is.GreaterThan(100), "**閉眼中の状態がほとんど通っていない。**測れていない");
    }

    // ------------------------------------------------------------------
    // TC-171: Settling と Feint が同じ（ADR-0017 / 0022）
    // ------------------------------------------------------------------

    [Test]
    public void TC171_寝たふりの成否で見せ方が同じ()
    {
        var carried = Visited(10)
            .Where(v => v.State.Parent is ParentPhase.Settling or ParentPhase.Feint)
            .ToList();

        Assert.That(carried, Is.Not.Empty, "**運ばれている状態が一度も通っていない。**測れていない");

        foreach (var (s, board) in carried)
        {
            var twin = s with { Parent = s.Parent == ParentPhase.Settling ? ParentPhase.Feint : ParentPhase.Settling };

            Assert.That(Of(twin, board), Is.EqualTo(Of(s, board)),
                $"**成功と失敗で見せ方が違う**（REQ-016 / ADR-0022）。tick={s.Tick}");
        }

        var p = Of(carried[0].State, carried[0].Board);

        Assert.Multiple(() =>
        {
            Assert.That(p.Body, Is.EqualTo(BodyClip.Carry), "**運ばれているのに抱き上げの動きが出ない**（ADR-0022）");
            Assert.That(p.CameraLifted, Is.True, "**運ばれているのに視点が動かない**（D-12）");
        });
    }

    // ------------------------------------------------------------------
    // TC-172: 親が起きて部屋を出る（REQ-030 / setting.md 4.4）
    // ------------------------------------------------------------------

    [Test]
    public void TC172_親が起きたら姿が消え電気が点く()
    {
        var board = SimProbe.BoardOf(SimProbe.Seeds(1).Single());
        var up = SimProbe.Begin(board) with { Parent = ParentPhase.Up, Arousal = T.ArousalMax, Tick = 500 };

        var p = Of(up, board);

        Assert.Multiple(() =>
        {
            Assert.That(p.Body, Is.EqualTo(BodyClip.Hidden), "**起きて出ていったのに姿が残っている**（REQ-030）");
            Assert.That(p.Hand, Is.EqualTo(HandClip.Rest));
            Assert.That(p.LampOn, Is.True, "**親がいないのに電気が点かない**（setting.md 4.4）");
        });
    }

    // ------------------------------------------------------------------
    // TC-173: 対処 4 種と、予告 4 種が区別できる（REQ-006 / 013）
    // ------------------------------------------------------------------

    [Test]
    public void TC173_今している対処が4種とも区別できる()
    {
        var board = SimProbe.BoardOf(SimProbe.Seeds(1).Single());
        var s = SimProbe.Begin(board) with { Tick = 500, Parent = ParentPhase.Caring, CareRemain = 10 };

        var looks = Enum.GetValues<CareKind>()
            .Select(k => Of(s with { ActiveCare = k }, board))
            .Select(p => (p.Hand, p.BottleInFace, p.CameraLifted))
            .ToList();

        Assert.That(looks.Distinct().Count(), Is.EqualTo(4),
            "**違う対処が同じに見える**（REQ-006: 親が今している対処を画面から知る）。\n" +
            string.Join("\n", looks));
    }

    [Test]
    public void TC173_次に来る対処が手を見れば4種とも区別できる()
    {
        var board = SimProbe.BoardOf(SimProbe.Seeds(1).Single());
        var s = SimProbe.Begin(board) with { Tick = 500, CareDelay = 10 };

        var looks = Enum.GetValues<CareKind>()
            .Select(k => Of(s with { PendingCare = k }, board))
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(looks.All(p => p.Hand == HandClip.Reach), Is.True, "**予告中なのに手が伸びてこない**（REQ-046）");
            Assert.That(looks.Select(p => p.Reaching).Distinct().Count(), Is.EqualTo(4),
                "**何が来るかが手から分からない**（screens.md 4.2.1）");
        });
    }

    [Test]
    public void TC173_トントンの手つきが山札の段階で変わる()
    {
        var board = SimProbe.BoardOf(SimProbe.Seeds(1).Single());
        var s = SimProbe.Begin(board) with
        {
            Tick = 500, Parent = ParentPhase.Caring, ActiveCare = CareKind.PatPat, CareRemain = 10,
        };

        var hands = new[] { new HandCount(1, 0, 0, 0), new HandCount(2, 1, 1, 0), new HandCount(3, 3, 3, 3) }
            .Select(h => s with { Hand = h })
            .Select(v => Of(v, board).Hand)
            .ToList();

        Assert.That(hands.Distinct().Count(), Is.EqualTo(3),
            "**山札の残量が手つきに出ない**（setting.md 4.3 / REQ-044）。" + string.Join(",", hands));
    }

    // ------------------------------------------------------------------
    // TC-174: 覚醒度は姿勢で分かり、見えないときは出さない（REQ-044 / 045 / TC-130）
    // ------------------------------------------------------------------

    [Test]
    public void TC174_覚醒度の4段階が別の姿勢になる()
    {
        var board = SimProbe.BoardOf(SimProbe.Seeds(1).Single());

        // 寝入りばなの予兆に掛からない tick を使う
        var s = SimProbe.Begin(board) with { Tick = T.DrowsyPeriodTicks + 1 };

        var bodies = new[] { 0, T.ArousalStage1, T.ArousalStage2, T.ArousalStage3 }
            .Select(a => Of(s with { Arousal = a }, board).Body)
            .ToList();

        Assert.That(bodies.Distinct().Count(), Is.EqualTo(4),
            "**覚醒度の段階が姿勢で区別できない**（REQ-044 / 045）。" + string.Join(",", bodies));
    }

    [Test]
    public void TC174_寝返り中は覚醒度を表す姿勢を出さない()
    {
        var board = SimProbe.BoardOf(SimProbe.Seeds(1).Single());
        var s = SimProbe.Begin(board) with { Tick = T.DrowsyPeriodTicks + 1, RollUntil = T.DrowsyPeriodTicks + 100 };

        var bodies = new[] { 0, T.ArousalMax - 1 }.Select(a => Of(s with { Arousal = a }, board).Body).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(bodies.Distinct().Count(), Is.EqualTo(1), "**寝返り中なのに覚醒度で姿勢が変わる**（D-6）");
            Assert.That(bodies[0], Is.EqualTo(BodyClip.TurnAway));
        });
    }

    // ------------------------------------------------------------------
    // TC-175: その夜の出来事が見える（REQ-055 / setting.md 6 節）
    // ------------------------------------------------------------------

    [Test]
    public void TC175_出来事が起きたことが見える()
    {
        var board = SimProbe.BoardOf(SimProbe.Seeds(1).Single()) with
        {
            Events =
            [
                new(1000, NightEventKind.PhoneRings),
                new(2000, NightEventKind.DiaperSoiled),
            ],
        };

        var s = SimProbe.Begin(board);

        Assert.Multiple(() =>
        {
            Assert.That(Of(s with { Tick = 1000 }, board).PhoneFlash, Is.True, "**スマホが光らない**");
            Assert.That(Of(s with { Tick = 1900 }, board).PhoneFlash, Is.False, "**スマホが光り続けている**");
            Assert.That(Of(s with { Tick = 2001 }, board).Sniff, Is.True, "**オムツが汚れても鼻をひくつかせない**");
            Assert.That(Of(s with { Tick = 10, PartnerHere = true }, board).PartnerHere, Is.True, "**もう一人の親が現れない**");
            Assert.That(Of(s with { Tick = 10, HungryUntil = 200 }, board).Vignette, Is.GreaterThan(0),
                "**おなかが空いても視界が沈まない**");
        });
    }
}
