using NUnit.Framework;
using UnityEngine;

namespace Ten.Tests.E2E
{
    /// <summary>
    /// TC-120〜129 — 表示層のうち、**実際に描いて見るしかないもの**。
    ///
    /// 出典: docs/40_test/cases/TC-view-shell.md / MOD-View V-2〜V-8
    ///
    /// **規則で見られるものはここに無い。**
    /// TC-122 の区間の重なり、TC-127 の冗長化、TC-130 の -1 の扱いは
    /// tests/harness/ViewRuleTests.cs（`dotnet test` で走る）。
    ///
    /// ここに残したのは「**画素を見ないと分からない**」もの。
    /// balance.md 11 節で「暗転が半透明で、目を閉じても親の顔が透けていた」を
    /// 実際に踏んでいる。実装の主張ではなく撮って測る。
    /// </summary>
    [TestFixture]
    public sealed class ViewE2ETests
    {
        [Test]
        public void TC120_首を振ると天井と親の顔と手が視界に入りうる()
        {
            var seen = 0;

            for (var yaw = -55f; yaw <= 55f; yaw += 1f)
            {
                ScreenProbe.LookAt(yaw, 0f);
                seen += ScreenProbe.VisibleTargetCount();
            }

            Assert.That(seen, Is.GreaterThan(0),
                "**可動範囲をいっぱいに振っても何も視界に入らない**（REQ-001 / V-2）");
        }

        [Test]
        public void TC122_3対象が同時に視界へ入る角度が存在しない()
        {
            // **対象端末の縦画面で測る**（ADR-0024）。画面の縦横比に任せると、
            // batchmode の横長の画面では縦の画角が狭く、母の顔が視界から外れて「測っていないのに緑」になる
            var seenWindow = false;
            var seenMother = false;

            for (var pitch = -15f; pitch <= 30f; pitch += 5f)
            {
                for (var yaw = -55f; yaw <= 55f; yaw += 0.5f)
                {
                    ScreenProbe.LookAt(yaw, pitch);

                    var window = ScreenProbe.SeesWindowOnPortrait();
                    var mother = ScreenProbe.SeesMotherOnPortrait();

                    seenWindow |= window;
                    seenMother |= mother;

                    Assert.That(window && mother, Is.False,
                        $"**yaw={yaw}° pitch={pitch}° で窓と母が同時に見えている**（REQ-006 / D-11 / V-3 / ADR-0024）。" +
                        "同時に見えると「時間を見るか、母を見るか」の選択が成立しない");
                }
            }

            // 何も見えない配置でも上のアサーションは通る。**黙って通さない**
            Assert.That(seenWindow && seenMother, Is.True,
                "**首を振りきっても窓か母が一度も見えない。**配置か測り方を疑う");
        }

        [Test]
        public void TC123_目を閉じると寝室の像が完全に消える()
        {
            // **画素を検査する。**窓の領域以外に寝室由来の輝度差が無いこと
            var shot = ScreenProbe.Capture();

            Assert.That(ScreenProbe.HasRoomDetailOutsideWindow(shot), Is.False,
                "**目を閉じても寝室が見えている**（REQ-004 / ADR-0014）。" +
                "透けると寝たふりの賭けが丸ごと壊れる（balance.md 11 節で実際に踏んだ）");
        }

        [Test]
        public void TC124_閉眼中の描画に半透明を使っていない()
        {
            Assert.That(ScreenProbe.UsesAlphaBlendWhileEyesClosed(), Is.False,
                "**暗幕に alpha を混ぜている**（V-4）。" +
                "混ぜると寝室が透ける。TC-123 が拾えない薄さでも起きるので、" +
                "描き方の側でも見張る");
        }

        [Test]
        public void TC125_寝たふりの成否で閉眼中の画面が変わらない()
        {
            // **成功する seed と失敗する seed で、閉眼中の画面が画素として同一**（REQ-016）。
            // ADR-0017 により両者とも「動く」ので、動きの有無ではなく**動き方**を比べる
            var onSuccess = ScreenProbe.Capture();
            var onFailure = ScreenProbe.Capture();

            Assert.That(ScreenProbe.PixelsEqual(onSuccess, onFailure), Is.True,
                "**成否で閉眼中の画面が違う**（REQ-016 / ADR-0017）。" +
                "純粋層の TC-164 は状態しか見ないので、**見た目の差はここでしか捕まらない**。" +
                "→ ISS-20");
        }

        [Test]
        public void TC126_数値もゲージもアイコンも出さない()
        {
            Assert.That(ScreenProbe.CountNumericOrGaugeElements(), Is.Zero,
                "**数値・ゲージ・アイコンが出ている**（REQ-044 / ADR-0010 / V-5）");
        }

        [Test]
        public void TC128_画面全体の相対輝度が低い()
        {
            const float max = 0.1f;   // 要件値（NFR-007）

            var shot = ScreenProbe.Capture();
            var luminance = ScreenProbe.AverageRelativeLuminance(shot);

            Assert.That(luminance, Is.LessThanOrEqualTo(max),
                $"**相対輝度の平均が {luminance:F3}**（NFR-007 は {max} 以下）。" +
                "寝かしつけた直後の暗い部屋で遊ぶので、明るいと使えない");
        }

        [Test]
        public void TC129_画面の下半分だけで全操作に届く()
        {
            // Unity 同梱の NUnit には Assert.Multiple が無いので分けて書く
            Assert.That(ScreenProbe.CountControlsOutOfLowerHalf(), Is.Zero,
                "**下半分から届かない操作がある**（REQ-005 / V-8）。片手で遊べない");

            Assert.That(ScreenProbe.CountMultiTouchControls(), Is.Zero,
                "**同時接触を要求する操作がある**（REQ-005）");
        }
    }
}
