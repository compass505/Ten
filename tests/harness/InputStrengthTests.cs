using System.Linq;
using NUnit.Framework;
using Ten.Pure;

namespace Ten.Tests.Harness;

/// <summary>
/// TC-152〜156 — 入力の強度とタイミング（ADR-0015 / REQ-060 / 061）。
///
/// **溜める時間や倍率はバランス値**なので期待値に書かない（ADR-0012）。
/// 見るのは単調性と、代償があることだけ。
/// </summary>
[TestFixture]
public sealed class InputStrengthTests
{
    private static BoardSpec Board => SimProbe.BoardOf(SimProbe.Seeds(1).Single());

    [Test]
    public void TC152_長く引っ張るほど上昇量も消費も対処の確率も増える()
    {
        var board = Board;
        var s = SimProbe.Begin(board);

        // 押している長さを増やしていく。**長さそのものは期待値ではない**
        var holds = new[] { 1, 5, 10, 20, 40 };

        var samples = holds.Select(h => (
            Hold: h,
            Gain: SimProbe.ArousalGain(s, board, ActionKind.Cry, h),
            Cost: SimProbe.VigorCost(s, board, ActionKind.Cry, h),
            CareRate: CareRate(board, ActionKind.Cry, h))).ToArray();

        for (var i = 1; i < samples.Length; i++)
        {
            var (prev, now) = (samples[i - 1], samples[i]);

            Assert.Multiple(() =>
            {
                Assert.That(now.Gain, Is.GreaterThanOrEqualTo(prev.Gain),
                    $"**上昇量が単調でない**（REQ-060）。{prev.Hold} tick で +{prev.Gain}、" +
                    $"{now.Hold} tick で +{now.Gain}");

                Assert.That(now.Cost, Is.GreaterThanOrEqualTo(prev.Cost),
                    $"**元気の消費が単調でない**（REQ-060）。{prev.Hold} tick で {prev.Cost}、" +
                    $"{now.Hold} tick で {now.Cost}");

                Assert.That(now.CareRate, Is.GreaterThanOrEqualTo(prev.CareRate),
                    $"**対処の確率が単調でない**（REQ-060）。{prev.Hold} tick で {prev.CareRate:P0}、" +
                    $"{now.Hold} tick で {now.CareRate:P0}");
            });
        }

        Assert.That(samples[^1].Gain, Is.GreaterThan(samples[0].Gain),
            "**長く引っ張っても上昇量が変わらない。**それでは強度の軸が機能していない");
    }

    /// <summary>その長さで撃ったとき、対処を引き出せたシードの割合。</summary>
    private static double CareRate(BoardSpec board, ActionKind kind, int hold)
    {
        const int seeds = 24;
        var drew = 0;

        foreach (var seed in SimProbe.Seeds(seeds))
        {
            var b = SimProbe.BoardOf(seed);
            var s = SimProbe.Begin(b);
            var after = SimProbe.Act(s, b, kind, hold);

            if (after.Parent == ParentPhase.Caring || after.PendingCare is not null)
            {
                drew++;
            }
        }

        return (double)drew / seeds;
    }

    [Test]
    public void TC153_溜め続けることに代償がある()
    {
        var board = Board;
        var s = SimProbe.Begin(board);

        // (1) 溜めている間に元気が減る
        var charging = SimProbe.RunTicks(s, board, 30, new TickInput(ActionKind.Cry, false));

        Assert.That(charging.Vigor, Is.LessThan(s.Vigor),
            "**溜めている間に元気が減らない**（ADR-0015）。" +
            "代償が無いと常に最大まで溜めるのが正解になり、判断が消える");

        // (2) 対処が始まると溜めが飛ぶ
        var caring = SimProbe.RunUntil(s, board, x => x.Parent == ParentPhase.Caring,
            new TickInput(ActionKind.Cry, false), what: "Parent = Caring");

        Assert.That(caring.ActStrengthMilli, Is.Zero,
            "**対処が始まっても溜めが残っている**（ADR-0015）。" +
            "残るなら、対処を待ってから撃つのが常に得になる");
    }

    [Test]
    public void TC154_寝入りばなの内と外で上昇量が違う()
    {
        var board = Board;

        var inside = FindDrowsyWindow(board);
        var outside = inside with { Parent = ParentPhase.Sleeping, TGrace = 0, TSettle = 0 };

        var gainInside = SimProbe.ArousalGain(inside, board, ActionKind.Cry, 1);
        var gainOutside = SimProbe.ArousalGain(outside, board, ActionKind.Cry, 1);

        Assert.That(gainInside, Is.GreaterThan(gainOutside),
            $"**寝入りばなに当てても伸びない**（REQ-061）。内 +{gainInside} / 外 +{gainOutside}");
    }

    [Test]
    public void TC155_予兆から寝入りばなまでに最大まで溜める余裕がある()
    {
        var board = Board;

        var chargeTicks = TicksToFullCharge(board);
        var leadTicks = TicksFromOmenToWindow(board);

        Assert.That(leadTicks, Is.GreaterThanOrEqualTo(chargeTicks),
            $"**予兆を見てから溜めていては間に合わない**（REQ-061）。" +
            $"予兆から {leadTicks} tick、最大まで {chargeTicks} tick。" +
            "間に合わないと、長押しと寝入りばなを組み合わせられない（balance.md 13 節の実測）");
    }

    /// <summary>最大強度まで溜めるのに要る tick 数。**測って出す。値は書かない。**</summary>
    private static int TicksToFullCharge(BoardSpec board)
    {
        var s = SimProbe.Begin(board);
        var held = new TickInput(ActionKind.Cry, false);
        var ticks = 0;

        while (s.ActStrengthMilli < 1000 && ticks < SimProbe.NightTicks && s.Over is null)
        {
            s = SimProbe.Step(s, held, board);
            ticks++;
        }

        return ticks;
    }

    /// <summary>予兆が出てから寝入りばなに入るまでの tick 数。</summary>
    private static int TicksFromOmenToWindow(BoardSpec board)
    {
        var s = SimProbe.Begin(board);

        // 予兆は「親が深い眠りに落ちる短い時間帯」の手前に出る。
        // 純粋層では TGrace / TSettle ではなく、親の状態遷移の予告として観測する
        var omen = SimProbe.RunUntil(s, board, x => x.PendingCare is null && x.Parent == ParentPhase.Sleeping,
            what: "予兆");

        var window = FindDrowsyWindow(board);

        return window.Tick - omen.Tick;
    }

    /// <summary>寝入りばなに入っている状態を探す。</summary>
    private static NightState FindDrowsyWindow(BoardSpec board) =>
        SimProbe.RunUntil(SimProbe.Begin(board), board,
            x => x.Parent == ParentPhase.Grace, what: "寝入りばな");

    [Test]
    public void TC156_顔を見ていないときは予兆が表示されない()
    {
        // 予兆は「親の顔を見ているときだけ分かる」（REQ-061）。
        // 首の向きは純粋層に渡らない（types.md 4 節）ので、**予兆は状態ではなく表示の側にある。**
        // 純粋層で言えるのは「NightState に予兆のフラグが無い」ことまで。
        var members = typeof(NightState).GetProperties()
            .Select(p => p.Name)
            .Where(n => n.Contains("Omen", System.StringComparison.OrdinalIgnoreCase)
                     || n.Contains("Drowsy", System.StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.That(members, Is.Empty,
            "**予兆が状態に入っている。**状態に入れると首の向きに関係なく出せてしまい、" +
            "「顔を見ているときだけ」が成立しない（REQ-061）。" +
            $"見つかった: {string.Join(" / ", members)}");
    }
}
