using System;
using System.IO;

namespace Ten.Tests.Unit;

/// <summary>
/// テストからリポジトリ内のファイルを引くための道具。
/// 実行ディレクトリはビルド構成で変わるので、絶対パスを書かない。
/// </summary>
internal static class RepoPaths
{
    /// <summary>リポジトリのルート。目印は `docs/00_process/workflow.md`。</summary>
    internal static string Root { get; } = FindRoot();

    /// <summary>参照ベクタ（harness.md 2 節）。テスト出力にコピーされたもの。</summary>
    internal static string RngVectors =>
        Path.Combine(AppContext.BaseDirectory, "vectors", "rng.json");

    /// <summary>純粋層の実装。フェーズ 5 までは存在しない。</summary>
    internal static string PureSource => Path.Combine(Root, "src", "Ten.Pure");

    private static string FindRoot()
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

        throw new InvalidOperationException(
            $"リポジトリのルートが見つからない（起点: {AppContext.BaseDirectory}）。" +
            "目印にしている docs/00_process/workflow.md を動かした場合は RepoPaths を直す。");
    }
}
