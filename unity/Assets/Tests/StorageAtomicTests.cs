using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Ten.Boundary;
using UnityEngine;

namespace Ten.Tests.E2E
{
    /// <summary>
    /// TC-109 — 保存の原子性（MOD-Storage ST-1 / REQ-053）。
    ///
    /// > 一時ファイルに書き切ってから rename。**途中で落ちても前の版が壊れない。**
    ///
    /// **実ファイルでしか確かめられない。**`MemoryStorage` では
    /// 「途中で落ちる」を作れないので、TC-108 / 110〜112 とは別扱いにしてある。
    /// </summary>
    [TestFixture]
    public sealed class StorageAtomicTests
    {
        private string _dir = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Application.temporaryCachePath, "ten-atomic-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, recursive: true);
            }
        }

        [Test]
        public void TC109_書き込みの途中で落ちても前の版が残る()
        {
            var storage = new FileStorage(_dir);

            // 1 版目を書き切る
            storage.SaveToday(new TodayData("2026-09-08", 1, null));

            var before = SnapshotFiles();

            Assert.That(before, Is.Not.Empty, "前提: 1 版目が書けている");

            // 2 版目の書き込みを途中で止める（一時ファイルが残った状態を作る）
            File.WriteAllText(Path.Combine(_dir, "today.tmp"), "途中まで書かれた壊れたデータ");

            var loaded = storage.LoadToday();

            // Unity 同梱の NUnit には Assert.Multiple が無いので分けて書く
            Assert.That(storage.LastStatus, Is.EqualTo(LoadStatus.Ok),
                "**書きかけの一時ファイルを読んでいる**（ST-1 / REQ-053）。" +
                "rename で差し替えるまで、前の版が正でなければならない");

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded!.Value.PlayCount, Is.EqualTo(1),
                "**前の版が壊れている**（REQ-053）");
        }

        [Test]
        public void TC109_書き切れば新しい版に差し替わる()
        {
            var storage = new FileStorage(_dir);

            storage.SaveToday(new TodayData("2026-09-08", 1, null));
            storage.SaveToday(new TodayData("2026-09-08", 2, null));

            Assert.That(storage.LoadToday()!.Value.PlayCount, Is.EqualTo(2),
                "**書き切ったのに差し替わっていない**（ST-1）");
        }

        [Test]
        public void TC109_一時ファイルを残したままにしない()
        {
            var storage = new FileStorage(_dir);

            storage.SaveToday(new TodayData("2026-09-08", 1, null));

            var temps = SnapshotFiles()
                .Where(f => f.EndsWith(".tmp", StringComparison.Ordinal))
                .ToArray();

            Assert.That(temps, Is.Empty,
                $"**一時ファイルが残っている**（{string.Join(" / ", temps)}）。" +
                "残ると、次の起動でどちらが正か分からなくなる");
        }

        private string[] SnapshotFiles() =>
            Directory.GetFiles(_dir).Select(Path.GetFileName).ToArray()!;
    }
}
