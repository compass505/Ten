using NUnit.Framework;
using Ten.Boundary;

namespace Ten.Tests.Harness;

/// <summary>
/// TC-116〜118 — 共有と画面消灯の抑止（docs/40_test/cases/TC-boundary.md）。
///
/// 出典: MOD-Share SH-1〜SH-4 / MOD-Power PW-1〜PW-4 / REQ-009 / 027 / 035
///
/// **Unity は要らない。**どちらも判断は規則に切り出してあり、
/// 端末に触るのは Unity プロジェクト側の薄い実体だけ。
///
/// **TC-115 / 119（通信 0 件・権限 0 件）はここに無い。**
/// あれはビルド成果物を検査するので [BuildInspectionTests](BuildInspectionTests.cs)。
/// </summary>
[TestFixture]
public sealed class ShareAndPowerTests
{
    // ------------------------------------------------------------------
    // TC-116: 共有先が無ければコピーにフォールバックする
    // ------------------------------------------------------------------

    [Test]
    public void TC116_共有先が無ければコピーにフォールバックする()
    {
        Assert.That(ShareRule.Decide(shareAvailable: false, clipboardAvailable: true),
            Is.EqualTo(ShareAction.Copy),
            "**共有先が無いときに何もしていない**（REQ-027 / MOD-Share のエラー時）。" +
            "結果を渡せないと、夫婦で見せ合うという目的（ADR-0006）に届かない");
    }

    [Test]
    public void TC116_共有先があればそちらを使う()
    {
        Assert.That(ShareRule.Decide(shareAvailable: true, clipboardAvailable: true),
            Is.EqualTo(ShareAction.Intent),
            "**共有先があるのにコピーで済ませている**（REQ-027 / SH-2）");
    }

    [Test]
    public void TC116_どちらも使えなければ何もしないが例外は投げない()
    {
        ShareAction action = default;

        Assert.DoesNotThrow(
            () => action = ShareRule.Decide(shareAvailable: false, clipboardAvailable: false),
            "**共有もコピーもできないときに例外を投げている**（MOD-Share のエラー時）。" +
            "結果画面が壊れてはいけない");

        Assert.That(action, Is.EqualTo(ShareAction.None));
    }

    [Test]
    public void TC116_共有もコピーも通信を行わない()
    {
        // SH-1 / NFR-002。**OS の共有機構に文字列を渡すだけ。**
        // 口の形がそれを保証している（送り先を引数に取らない）
        var members = typeof(IShare).GetMethods();

        foreach (var m in members)
        {
            foreach (var p in m.GetParameters())
            {
                Assert.That(p.ParameterType, Is.EqualTo(typeof(string)),
                    $"**`{m.Name}` が文字列以外（{p.ParameterType.Name} {p.Name}）を受け取っている。**" +
                    "宛先や URL を受け取る形にすると、送り先をアプリ側で決めることになる（SH-2 / NFR-002）");
            }
        }
    }

    // ------------------------------------------------------------------
    // TC-117 / 118: 画面消灯の抑止
    // ------------------------------------------------------------------

    [Test]
    public void TC117_夜の進行中は画面を点けたままにする()
    {
        Assert.That(PowerRule.ShouldKeepAwake(isNightScreen: true, isPaused: false, isOver: false),
            Is.True,
            "**夜の進行中に画面が消える**（REQ-035 / PW-1）。" +
            "寝たふり中は入力が無いので、放置すると盲目区間が消灯で壊れる");
    }

    [Test]
    public void TC118_中断に入ったら抑止を解除する()
    {
        Assert.That(PowerRule.ShouldKeepAwake(isNightScreen: true, isPaused: true, isOver: false),
            Is.False,
            "**中断中も画面を点けたままにしている**（REQ-009 / PW-2）。中断中に電池を食う");
    }

    [Test]
    public void TC117_夜の画面以外では抑止しない()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PowerRule.ShouldKeepAwake(isNightScreen: false, isPaused: false, isOver: false),
                Is.False, "**夜以外でも画面を点けたままにしている**（PW-3）");

            Assert.That(PowerRule.ShouldKeepAwake(isNightScreen: true, isPaused: false, isOver: true),
                Is.False, "**夜が終わっているのに抑止が続いている**（PW-3）");
        });
    }

    [Test]
    public void TC117_抑止の切り替えは口を通す()
    {
        // 差し替えられること（ADR-0002 D3）。実体は Unity 側にあるので、
        // ここで見るのは口の振る舞いだけ
        var power = new NullPower();

        power.KeepAwake(true);
        Assert.That(power.IsAwake, Is.True);

        power.KeepAwake(false);
        Assert.Multiple(() =>
        {
            Assert.That(power.IsAwake, Is.False);
            Assert.That(power.Calls, Is.EqualTo(2));
        });
    }
}
