using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

namespace Ten.Tests.Harness;

/// <summary>
/// TC-115 / 119 / 147 — ビルド成果物の検査。
///
/// 出典: NFR-002（通信を行わない）/ NFR-003（要求権限 0 件）
///
/// **これはコードのテストではない。**Unity が吐いた
/// `AndroidManifest.xml` と、同梱される依存を検査する。
///
/// **ビルドが無いと落ちる。**それが正しい（成果物を見ずに「権限 0 件」とは言えない）。
/// ビルドの作り方は unity/README.md。
/// </summary>
[TestFixture]
public sealed class BuildInspectionTests
{
    /// <summary>Unity のビルド生成物が置かれる場所。</summary>
    private static string UnityDir => Path.Combine(RepoRoot(), "unity");

    /// <summary>
    /// 生成された `AndroidManifest.xml` を**探す**。
    ///
    /// **パスを決め打ちしない。**Unity の版でビルド中間物の置き場が変わるため、
    /// 決め打ちにすると「権限が無いから通った」のか「そもそも別の場所を見ていた」のかが
    /// 区別できなくなる（**空振りするテストになる**）。
    /// </summary>
    private static string[] FindManifests()
    {
        if (!Directory.Exists(UnityDir))
        {
            return [];
        }

        return Directory.GetFiles(UnityDir, "AndroidManifest.xml", SearchOption.AllDirectories)
            // パッケージに同梱された雛形は成果物ではない
            .Where(p => !p.Contains(Path.Combine("Library", "PackageCache"), StringComparison.Ordinal))
            // **`Assets/` の下にあるものは入力であって成果物ではない**（ADR-0020）。
            // 権限を 1 件も残さないために `tools:node="remove"` を書いた追加マニフェストが
            // ここにあり、それ自体を「権限を要求している」と数えてしまう。
            // このテストが見るのは、マージが終わったあとの**吐かれたマニフェスト**
            .Where(p => !p.Contains(Path.Combine("unity", "Assets"), StringComparison.Ordinal))
            .ToArray();
    }

    [Test]
    public void TC119_要求権限が0件()
    {
        var ns = XNamespace.Get("http://schemas.android.com/apk/res/android");

        foreach (var path in RequireManifests())
        {
            var tools = XNamespace.Get("http://schemas.android.com/tools");

            var permissions = XDocument.Load(path).Root!.Descendants("uses-permission")
                // **`tools:node="remove"` は「要求」ではなく「消す指示」**（ADR-0020）。
                // マージャはこれを見て消す。数えると、消すために書いた 1 行を
                // 「権限を 1 件要求している」と読んでしまう
                .Where(e => (string?)e.Attribute(tools + "node") is not ("remove" or "removeAll"))
                .Select(e => (string?)e.Attribute(ns + "name") ?? "(名前なし)")
                .ToArray();

            Assert.That(permissions, Is.Empty,
                $"**{Rel(path)} に要求権限が {permissions.Length} 件ある**（NFR-003）。\n  " +
                string.Join("\n  ", permissions) +
                "\n通信も外部アクセスも要らない設計なので、1 件でも付いたら理由を確かめる");
        }
    }

    [Test]
    public void TC115_通信に使う権限も宣言も無い()
    {
        var text = string.Join("\n", RequireManifests().Select(File.ReadAllText));

        string[] networkMarkers =
        [
            "android.permission.INTERNET",
            "android.permission.ACCESS_NETWORK_STATE",
            "usesCleartextTraffic",
            "networkSecurityConfig",
        ];

        foreach (var marker in networkMarkers)
        {
            Assert.That(text, Does.Not.Contain(marker),
                $"**マニフェストに `{marker}` がある**（NFR-002）。" +
                "機内モードで全機能が動くことが要件");
        }
    }

    [Test]
    public void TC147_通信を行う外部SDKが同梱されていない()
    {
        // Unity の解析・広告・診断まわりは既定で入りうる。**入っていたら外す。**
        var manifests = RequireManifests();
        var roots = manifests.Select(m => Path.GetDirectoryName(m)!).Distinct().ToArray();

        string[] forbidden =
        [
            "com.google.android.gms",
            "com.google.firebase",
            "com.unity3d.ads",
            "com.unity3d.services",
            "UnityAnalytics",
        ];

        // マニフェストと同じ階層のビルド定義を見る
        var files = roots
            .SelectMany(r => Directory.GetFiles(Path.GetFullPath(Path.Combine(r, "..", "..")), "*",
                SearchOption.AllDirectories))
            .Where(f => f.EndsWith(".gradle", StringComparison.Ordinal)
                     || f.EndsWith(".xml", StringComparison.Ordinal)
                     || f.EndsWith(".properties", StringComparison.Ordinal))
            .Distinct()
            .ToArray();

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);

            foreach (var marker in forbidden)
            {
                Assert.That(text, Does.Not.Contain(marker),
                    $"**{Rel(file)} に `{marker}` がある**（NFR-002 / 003）。" +
                    "通信する SDK が同梱されると、機内モードでの動作と権限 0 件が崩れる");
            }
        }
    }

    /// <summary>マニフェストが 1 つも無ければ落とす。**無いまま通してはいけない。**</summary>
    private static string[] RequireManifests()
    {
        var found = FindManifests();

        Assert.That(found, Is.Not.Empty,
            "**生成された AndroidManifest.xml が 1 つも無い。**\n" +
            "成果物を見ずに「権限 0 件」「通信 0 件」とは言えない。\n" +
            "先に 1 度 Android ビルドを通す（手順は unity/README.md）");

        return found;
    }

    private static string Rel(string path) =>
        Path.GetRelativePath(RepoRoot(), path);

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "docs", "00_process", "workflow.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("リポジトリのルートが見つからない");
    }
}
