using NUnit.Framework;
using Ten.Boundary;
using Ten.Pure;

namespace Ten.Tests.Harness;

/// <summary>
/// TC-103〜107 — 入力（docs/40_test/cases/TC-boundary.md）。
///
/// 出典: MOD-Input IN-1〜IN-7 / REQ-002 / 005 / 038 / 039 / 059
///
/// **Unity は要らない。**端末から来る接触を <see cref="PointerSample"/> として
/// 値で受け取る形にしたので、量子化・排他・丸めを `dotnet test` で検証できる。
/// Unity 側は `Input.touches` を詰め替えるだけの殻になる。
///
/// **可動角度を期待値に書かない。**
/// [test_first.md](../../docs/00_process/test_first.md) 4.1 が「首の可動角度」を
/// バランス値としているため、限界値は <see cref="LookLimits"/> で注入して
/// **「限界に丸められること」だけ**を見る（ADR-0012）。
/// </summary>
[TestFixture]
public sealed class InputTests
{
    private const float ScreenW = 1080f;
    private const float ScreenH = 2400f;

    /// <summary>テスト用の可動範囲。**製品の値ではない**（balance.md にある）。</summary>
    private static LookLimits Limits => new(YawMaxDeg: 40f, PitchMinDeg: -10f, PitchMaxDeg: 20f);

    private static PointerInputSource NewSource() => new(ScreenW, ScreenH, Limits);

    /// <summary>画面の下半分（行動の操作対象。REQ-005）。</summary>
    private static float LowerY => ScreenH * 0.25f;

    /// <summary>画面の上半分。</summary>
    private static float UpperY => ScreenH * 0.75f;

    [Test]
    public void TC103_同じtickに複数来ても先着1件だけを採る()
    {
        var src = NewSource();

        src.Feed(10, new PointerSample(0, 100f, LowerY, PointerPhase.Down));
        src.Feed(10, new PointerSample(1, 300f, LowerY, PointerPhase.Down));
        src.Feed(10, new PointerSample(2, 500f, LowerY, PointerPhase.Down));

        var first = src.Sample(10);

        Assert.That(first.Held, Is.Not.Null, "前提: 1 件目は行動として採られる");

        // 捨てたものが次の tick に出てこない（IN-2 / REQ-059）
        var next = src.Sample(11);

        Assert.That(next.Held, Is.EqualTo(first.Held),
            "**捨てたはずの 2 件目・3 件目が次の tick に出てきた**（IN-1 / IN-2）。" +
            "キューに残すと、離したあとに知らない行動が発火する");
    }

    [Test]
    public void TC104_画面上半分のタッチは行動として解釈されない()
    {
        var src = NewSource();

        src.Feed(5, new PointerSample(0, 540f, UpperY, PointerPhase.Down));

        Assert.Multiple(() =>
        {
            Assert.That(src.Sample(5).Held, Is.Null,
                "**画面上半分で行動が発火した**（REQ-005 / IN-5）。上半分は首振りだけ");

            Assert.That(src.IsActionArea(UpperY), Is.False, "上半分は操作対象でない");
            Assert.That(src.IsActionArea(LowerY), Is.True, "下半分は操作対象");
        });
    }

    [Test]
    public void TC105_可動範囲を超えるドラッグは限界に丸められる()
    {
        var src = NewSource();
        var limits = Limits;

        // 範囲をはるかに超える量を振る。**「55」のような具体値は書かない**（ADR-0012）
        src.Feed(1, new PointerSample(0, ScreenW * 0.5f, UpperY, PointerPhase.Down));
        src.Feed(2, new PointerSample(0, ScreenW * 50f, ScreenH * 50f, PointerPhase.Move));

        var (yaw, pitch) = src.Look;

        Assert.Multiple(() =>
        {
            Assert.That(yaw, Is.InRange(-limits.YawMaxDeg, limits.YawMaxDeg),
                $"**左右が範囲外（{yaw}°）**（REQ-002 / IN-6）");
            Assert.That(pitch, Is.InRange(limits.PitchMinDeg, limits.PitchMaxDeg),
                $"**上下が範囲外（{pitch}°）**（REQ-002 / IN-6）");
        });

        // 逆向きも同じ
        var back = NewSource();
        back.Feed(1, new PointerSample(0, ScreenW * 0.5f, UpperY, PointerPhase.Down));
        back.Feed(2, new PointerSample(0, -ScreenW * 50f, -ScreenH * 50f, PointerPhase.Move));

        var (yaw2, pitch2) = back.Look;

        Assert.Multiple(() =>
        {
            Assert.That(yaw2, Is.InRange(-limits.YawMaxDeg, limits.YawMaxDeg));
            Assert.That(pitch2, Is.InRange(limits.PitchMinDeg, limits.PitchMaxDeg));
        });
    }

    [Test]
    public void TC105_限界を変えるとその限界で丸められる()
    {
        // 丸めが**注入した限界に従う**こと。値を焼き込んでいたらここで落ちる
        var narrow = new PointerInputSource(ScreenW, ScreenH, new LookLimits(5f, -2f, 3f));

        narrow.Feed(1, new PointerSample(0, ScreenW * 0.5f, UpperY, PointerPhase.Down));
        narrow.Feed(2, new PointerSample(0, ScreenW * 50f, ScreenH * 50f, PointerPhase.Move));

        var (yaw, pitch) = narrow.Look;

        Assert.Multiple(() =>
        {
            Assert.That(yaw, Is.InRange(-5f, 5f),
                "**可動角度がコードに焼き込まれている**（ADR-0012 / test_first.md 4.1）");
            Assert.That(pitch, Is.InRange(-2f, 3f), "同上");
        });
    }

    [Test]
    public void TC106_画面外で離すと押しっぱなしが解除される()
    {
        var src = NewSource();

        src.Feed(1, new PointerSample(0, 540f, LowerY, PointerPhase.Down));

        Assert.That(src.Sample(1).Held, Is.Not.Null, "前提: 押している");

        // 画面外で離される（Cancel）。**押しっぱなしが残り続けてはいけない**
        src.Feed(2, new PointerSample(0, -50f, -50f, PointerPhase.Cancel));

        Assert.That(src.Sample(2).Held, Is.Null,
            "**画面外で離しても押しっぱなしが残っている**（MOD-Input のエラー時）。" +
            "残ると、指を離したのに行動が続く");
    }

    [Test]
    public void TC107_2本目の指は無視される()
    {
        var src = NewSource();

        src.Feed(1, new PointerSample(0, 200f, LowerY, PointerPhase.Down));
        var withOne = src.Sample(1);

        // 2 本目。**同時接触を要求しない**（REQ-005 / IN-5）
        src.Feed(2, new PointerSample(1, 800f, LowerY, PointerPhase.Down));
        var withTwo = src.Sample(2);

        Assert.That(withTwo.Held, Is.EqualTo(withOne.Held),
            "**2 本目の指が入力を変えた**（REQ-005 / MOD-Input のエラー時）。" +
            "見るのは最初の 1 本だけ");

        // 1 本目を離せば、そこで解除される（2 本目に引き継がない）
        src.Feed(3, new PointerSample(0, 200f, LowerY, PointerPhase.Up));

        Assert.That(src.Sample(3).Held, Is.Null,
            "**1 本目を離したのに 2 本目が引き継いでいる**");
    }

    [Test]
    public void TC103_首振りは行動と直交する()
    {
        // ドラッグ中でも行動を受け付ける（IN-3 / REQ-038）。
        // 首振りは状態列に影響しないので、Held だけを見る
        var src = NewSource();

        src.Feed(1, new PointerSample(0, 540f, UpperY, PointerPhase.Down));
        src.Feed(2, new PointerSample(0, 700f, UpperY, PointerPhase.Move));

        var beforeLook = src.Look;

        src.Feed(3, new PointerSample(1, 300f, LowerY, PointerPhase.Down));

        Assert.That(src.Look, Is.EqualTo(beforeLook),
            "**行動の入力で首の向きが動いた。**首振りと行動は直交する（IN-3 / REQ-038）");
    }
}
