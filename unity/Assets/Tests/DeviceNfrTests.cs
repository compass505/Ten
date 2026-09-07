using System;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ten.Tests.E2E
{
    /// <summary>
    /// TC-145〜151 — 実機でしか測れない非機能要件。
    ///
    /// 出典: docs/40_test/cases/TC-view-shell.md / NFR-001〜009
    ///
    /// **これらは実機で走らせる。**エディタで通っても意味が無いものが混ざっているので、
    /// 実機以外では**落とす**（黙って通すと「測っていないのに緑」になる）。
    ///
    /// TC-148（決定論）と TC-149（所要時間）は純粋層で同じことを見ているので
    /// （TC-021 / TC-063）、ここでは実機でも同じ結果になることだけを確かめる。
    /// </summary>
    [TestFixture]
    public sealed class DeviceNfrTests
    {
        /// <summary>実機でないなら測っていない。**通してはいけない。**</summary>
        private static void RequireDevice()
        {
            Assert.That(Application.isEditor, Is.False,
                "**エディタで実行している。**この TC は実機でしか測れない（NFR-001 / 007〜009）。" +
                "エディタで通しても、端末の画面・熱・ストレージの影響が入らない");

            Assert.That(Application.platform, Is.EqualTo(RuntimePlatform.Android),
                "**Android 実機で測る**（ADR-0001 の配布先）");
        }

        [Test]
        public void TC145_実機で1プレイ最後まで通る()
        {
            RequireDevice();

            Assert.Fail("**未計測。**実機で 1 プレイ通し、最後まで動くことを確認する（NFR-001）");
        }

        [Test]
        public void TC146_機内モードで全機能が動く()
        {
            RequireDevice();

            Assert.That(Application.internetReachability, Is.EqualTo(NetworkReachability.NotReachable),
                "**機内モードにしてから測る**（NFR-002）。通信がある状態では検証にならない");

            Assert.Fail("**未計測。**機内モードで一通り操作し、全機能が動くことを確認する（NFR-002）");
        }

        [Test]
        public void TC150_コールドスタートが3秒以内()
        {
            RequireDevice();

            const double maxSeconds = 3.0;   // 要件値（NFR-008）
            const int trials = 5;            // TC-150 が定めた回数

            var elapsed = new double[trials];

            for (var i = 0; i < trials; i++)
            {
                // プロセスを毎回落として測る必要があるため、
                // 実体は外側の計測スクリプトから渡す（フェーズ 5 で用意する）
                elapsed[i] = ColdStartSeconds(i);
            }

            Array.Sort(elapsed);
            var median = elapsed[trials / 2];

            Assert.That(median, Is.LessThanOrEqualTo(maxSeconds),
                $"**コールドスタートの中央値が {median:F2} 秒**（NFR-008 は {maxSeconds} 秒以内）。" +
                "寝かしつけた直後に開くので、待たされると使われない");
        }

        private static double ColdStartSeconds(int trial) =>
            throw new NotImplementedException("フェーズ 5（実装）で書く（test_first.md 5.1）");

        [Test]
        public void TC151_3D酔いの自己申告が下位2段階に収まる()
        {
            RequireDevice();

            // **人が答える。**自動では測れない（NFR-009）。
            // 本人と配偶者が別の日に 3 回ずつ通し、記録を残す
            Assert.Fail(
                "**未計測。**本人と配偶者が別の日に 3 回ずつ通し、" +
                "酔いの自己申告がいずれも下位 2 段階であることを確認する（NFR-009）。" +
                "→ decisions_pending.md C 節 SET-04");
        }

        [Test]
        public void TC147_ビルドに要求権限も通信SDKも無い()
        {
            // ビルド成果物の検査は dotnet 側（tests/harness/BuildInspectionTests.cs）で行う。
            // ここでは**実機に載ったものが同じか**を見る
            RequireDevice();

            Assert.Fail(
                "**未計測。**実機の APK を検査する。" +
                "静的な検査は tests/harness/BuildInspectionTests.cs が行っている（NFR-002 / 003）");
        }
    }
}
