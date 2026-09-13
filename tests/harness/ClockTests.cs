using System.Linq;
using NUnit.Framework;
using Ten.Boundary;

namespace Ten.Tests.Harness;

/// <summary>
/// TC-098〜102 — クロック（docs/40_test/cases/TC-boundary.md）。
///
/// 出典: MOD-Clock C-1〜C-5 / NFR-005 / REQ-009 / ADR-0002 D2
///
/// **Unity は要らない。**`Consume` が経過時間を引数で受け取るので、
/// 実時間を待たずに検証できる。Unity 側は `Update` で `Time.deltaTime` を渡すだけ。
/// </summary>
[TestFixture]
public sealed class ClockTests
{
    /// <summary>1 秒 = 20 tick（balance.md 1 節）。**比較のためだけに使う。**</summary>
    private const double SecondsPerTick = 1.0 / 20;

    [Test]
    public void TC098_端数が持ち越されtickが落ちない()
    {
        var clock = new RealClock();

        // 不揃いな delta を与える。合計が同じなら、消化した tick 数も同じでなければならない
        double[] uneven = [0.017, 0.033, 0.008, 0.121, 0.004, 0.055, 0.062];
        var total = uneven.Sum();

        var stepped = uneven.Sum(clock.Consume);

        clock.Reset();
        var atOnce = clock.Consume(total);

        Assert.That(stepped, Is.EqualTo(atOnce),
            $"**端数が落ちている**（NFR-005 / C-2）。" +
            $"刻んで与えると {stepped} tick、まとめて与えると {atOnce} tick。" +
            "20 Hz 未満のフレームで tick が消える");
    }

    [Test]
    public void TC098_同じ合計なら刻み方によらず同じtick数になる()
    {
        var fine = new RealClock();
        var coarse = new RealClock();

        var fineTicks = Enumerable.Range(0, 100).Sum(_ => fine.Consume(SecondsPerTick / 5));
        var coarseTicks = Enumerable.Range(0, 20).Sum(_ => coarse.Consume(SecondsPerTick));

        Assert.That(fineTicks, Is.EqualTo(coarseTicks),
            $"**フレームレートで結果が変わる**（NFR-005 / C-1）。" +
            $"細かく {fineTicks} tick / 粗く {coarseTicks} tick");
    }

    [Test]
    public void TC099_停止中は常に0を返す()
    {
        var clock = new RealClock();
        clock.Pause();

        Assert.Multiple(() =>
        {
            Assert.That(clock.IsPaused, Is.True);

            foreach (var delta in new[] { 0.05, 1.0, 60.0 })
            {
                Assert.That(clock.Consume(delta), Is.Zero,
                    $"**停止中に {delta} 秒で tick が進んだ**（REQ-009 / C-4）");
            }
        });
    }

    [Test]
    public void TC100_1フレームで消化するtick数に上限がある()
    {
        var clock = new RealClock();

        // 長い停止からの復帰。上限が無いと、知らないうちに夜が終わる（C-3）
        var ticks = clock.Consume(600.0);

        Assert.That(ticks, Is.LessThanOrEqualTo(RealClock.MaxTicksPerFrame),
            $"**10 分の経過で {ticks} tick 進んだ**（REQ-009 / C-3）。" +
            "Pause 漏れの保険が効いていない");
    }

    [Test]
    public void TC100_上限を超えた分は捨てられ後から湧かない()
    {
        var clock = new RealClock();

        clock.Consume(600.0);

        var next = clock.Consume(0);

        Assert.That(next, Is.Zero,
            "**捨てたはずの時間が次のフレームで湧いている**（C-3）。" +
            "超過分は持ち越さずに捨てる");
    }

    [Test]
    public void TC101_負の経過時間では進まない()
    {
        var clock = new RealClock();

        Assert.That(clock.Consume(-1.0), Is.Zero,
            "**時刻が巻き戻ったときに tick が進んだ**。負の時間を進めない");

        Assert.That(clock.Consume(SecondsPerTick), Is.GreaterThan(0),
            "**負の値を与えたあと、正常な経過でも進まなくなった**");
    }

    [Test]
    public void TC102_StepClockに差し替えると実時間を待たずに進む()
    {
        IClock clock = new StepClock();

        ((StepClock)clock).Push(5400);

        Assert.That(clock.Consume(0), Is.EqualTo(5400),
            "**差し替えたクロックが効いていない**（ADR-0002 D2 / C-5）。" +
            "実時間を待つとハーネスが成立しない");
    }

    [Test]
    public void TC102_StepClockも停止中は0を返す()
    {
        var clock = new StepClock();
        clock.Push(100);
        clock.Pause();

        Assert.That(clock.Consume(0), Is.Zero,
            "**テスト用クロックが本番と違う振る舞いをする**。" +
            "差し替えで結果が変わるなら、差し替える意味が無い");
    }
}
