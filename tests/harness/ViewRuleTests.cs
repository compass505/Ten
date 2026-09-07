using System;
using System.Linq;
using NUnit.Framework;
using Ten.Boundary;

namespace Ten.Tests.Harness;

/// <summary>
/// TC-122 / 127 / 130 — 表示層のうち、**規則で見られるもの**。
///
/// 出典: MOD-View V-3 / V-6 / エラー時 / REQ-006 / 044 / D-11
///
/// **描いて見るしかないもの（TC-120 / 121 / 123〜126 / 128 / 129）はここに無い。**
/// あれは Unity の e2e。→ unity/Assets/Tests/
/// </summary>
[TestFixture]
public sealed class ViewRuleTests
{
    /// <summary>
    /// テスト用の配置。**製品の値ではない**（setting.md 3 節にある）。
    /// 重なっていない 3 区間。
    /// </summary>
    private static GazeBand[] Bands =>
    [
        new(GazeTarget.Window, -55f, -41f),
        new(GazeTarget.ParentFace, -1f, 39f),
        new(GazeTarget.ParentHand, 40f, 55f),
    ];

    // ------------------------------------------------------------------
    // TC-122: 3 対象が同時に視界へ入る角度が存在しない（V-3 / D-11）
    // ------------------------------------------------------------------

    [Test]
    public void TC122_3対象が同時に見える角度が存在しない()
    {
        Assert.That(GazeRule.HasOverlap(Bands), Is.False,
            "**2 つ以上の対象が同時に見える角度がある**（REQ-006 / D-11 / V-3）。" +
            "同時に見えると「見ることが選択になる」が成立せず、" +
            "一人称を選んだ意味（ADR-0007）が消える");
    }

    [Test]
    public void TC122_重なる配置は重なりとして検出される()
    {
        // 検出そのものが働いているか。**通るだけのテストにしない**
        GazeBand[] overlapping =
        [
            new(GazeTarget.Window, -55f, -41f),
            new(GazeTarget.ParentFace, -45f, 10f),   // 窓と重なっている
        ];

        Assert.That(GazeRule.HasOverlap(overlapping), Is.True,
            "**重なっているのに検出できない。**この検査が空振りしている");
    }

    [Test]
    public void TC121_向けた対象だけが読める()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GazeRule.At(-48f, Bands), Is.EqualTo(GazeTarget.Window));
            Assert.That(GazeRule.At(20f, Bands), Is.EqualTo(GazeTarget.ParentFace));
            Assert.That(GazeRule.At(50f, Bands), Is.EqualTo(GazeTarget.ParentHand));
            Assert.That(GazeRule.At(-20f, Bands), Is.EqualTo(GazeTarget.Ceiling),
                "**どれでもない向きで何かが読めている**（D-11）。初期視線は天井で、何も分からない");
        });
    }

    // ------------------------------------------------------------------
    // TC-127: 明度差を消しても段階が区別できる（V-6 / REQ-044）
    // ------------------------------------------------------------------

    [Test]
    public void TC127_明度を無視しても段階が区別できる()
    {
        foreach (var target in new[] { GazeTarget.Window, GazeTarget.ParentFace, GazeTarget.ParentHand })
        {
            var count = LookRule.StageCount(target);

            Assert.That(count, Is.GreaterThan(1), $"{target} の段階が 1 つしかない");

            // **明度を落とした見え方**が、段階ごとに全部違うこと
            var withoutBrightness = Enumerable.Range(0, count)
                .Select(i => LookRule.Of(target, i))
                .Select(l => l!.Value with { Brightness = 0 })
                .ToArray();

            Assert.That(withoutBrightness.Distinct().Count(), Is.EqualTo(count),
                $"**{target} は明度差を消すと段階が区別できない**（REQ-044 / V-6）。" +
                "輪郭・姿勢・周期のどれかで冗長化する。" +
                "暗所で見るゲーム（NFR-007）なので、明度だけに頼ると読めない");
        }
    }

    [Test]
    public void TC127_段階ごとに明度以外の差が必ずある()
    {
        foreach (var target in new[] { GazeTarget.Window, GazeTarget.ParentFace, GazeTarget.ParentHand })
        {
            var count = LookRule.StageCount(target);

            for (var i = 1; i < count; i++)
            {
                var a = LookRule.Of(target, i - 1)!.Value;
                var b = LookRule.Of(target, i)!.Value;

                var differsBeyondBrightness =
                    a.Silhouette != b.Silhouette || a.Posture != b.Posture || a.CycleMilli != b.CycleMilli;

                Assert.That(differsBeyondBrightness, Is.True,
                    $"**{target} の段階 {i - 1} と {i} が明度でしか違わない**（REQ-044 / V-6）");
            }
        }
    }

    // ------------------------------------------------------------------
    // TC-130: 見えないものを推測して埋めない（MOD-View のエラー時）
    // ------------------------------------------------------------------

    [Test]
    public void TC130_見えない段階には何も出さない()
    {
        foreach (var target in Enum.GetValues<GazeTarget>())
        {
            Assert.That(LookRule.Of(target, -1), Is.Null,
                $"**{target} が -1（見えない）でも見せ方を返している**（TC-130 / REQ-004）。" +
                "推測して埋めると、閉眼中に親の状態が漏れる");
        }
    }

    [Test]
    public void TC130_範囲外の段階は例外になる()
    {
        // -1 は「見えない」で意味を持つが、それ以外の範囲外は呼び出し側のバグ
        Assert.Throws<ArgumentOutOfRangeException>(
            () => LookRule.Of(GazeTarget.ParentFace, 999),
            "**範囲外の段階を丸めて返している。**壊れた状態のまま画面が出る");
    }
}
