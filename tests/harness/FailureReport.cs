using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Ten.Tests.Harness;

/// <summary>
/// 落ちたテストの出力（決定論の条件 D4 / docs/40_test/harness.md 4 節）。
///
/// <code>
/// FAILED TC-031  期待: 上昇量が単調非増加  実際: ar=30 で +12, ar=60 で +14
///   seed=2026-09-06 play=1 spec=1 tuning=a3f91c
///   tick=1180  (ゲーム内 22:58)
///   入力列: tests/traces/failed-20260906-143022.trace
///   再現:   dotnet test --filter TC-031 -- Trace=failed-20260906-143022.trace
/// </code>
///
/// **D1〜D3 が守られていても、D4 が無いと落ちたテストはデバッグできない**
/// （test_first.md 2 節）。ここを手抜きすると 4 つで 1 セットが崩れる。
/// </summary>
public static class FailureReport
{
    /// <summary>落ちた入力列の置き場（harness.md 4 節）。</summary>
    public const string TraceDirName = "traces";

    /// <summary>
    /// 報告文を組み立て、**入力列をファイルに書き出す。**
    ///
    /// ランダム入力列（TC-020）は、落ちた列を保存しないと二度と再現できない
    /// （harness.md 4 節）。**通ったときは保存しない**（保存すると承認テストになる。
    /// ADR-0002 が禁じている）。
    /// </summary>
    /// <param name="testCaseId">TC 番号。再現コマンドの --filter に入る</param>
    /// <param name="expected">期待したこと（性質で書く。バランス値を書かない）</param>
    /// <param name="actual">実際に起きたこと</param>
    /// <param name="trace">再現に要る入力列</param>
    /// <param name="tick">落ちた tick</param>
    /// <param name="traceRoot">入力列を書き出す先の親。省略時はテストの実行ディレクトリ</param>
    /// <param name="stamp">ファイル名に使う時刻。テストから固定できるように引数で受ける（D2）</param>
    public static string Build(
        string testCaseId,
        string expected,
        string actual,
        ParsedTrace trace,
        int tick,
        string? traceRoot = null,
        DateTimeOffset? stamp = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(testCaseId);
        ArgumentNullException.ThrowIfNull(trace);
        ArgumentOutOfRangeException.ThrowIfNegative(tick);

        var fileName = TraceFileName(stamp ?? DateTimeOffset.Now);
        var dir = Path.Combine(traceRoot ?? AppContext.BaseDirectory, TraceDirName);

        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, fileName), TraceFile.Write(trace));

        var sb = new StringBuilder();

        sb.Append("FAILED ").Append(testCaseId)
          .Append("  期待: ").Append(expected)
          .Append("  実際: ").Append(actual).Append('\n');

        sb.Append("  ").Append(trace.Header.ToLine().TrimStart('#', ' ')).Append('\n');

        sb.Append("  tick=").Append(tick.ToString(CultureInfo.InvariantCulture))
          .Append("  (ゲーム内 ").Append(GameClock.Format(tick)).Append(")\n");

        sb.Append("  入力列: ").Append(TraceDirName).Append('/').Append(fileName).Append('\n');

        sb.Append("  再現:   dotnet test --filter ").Append(testCaseId)
          .Append(" -- Trace=").Append(fileName).Append('\n');

        return sb.ToString();
    }

    /// <summary>`failed-20260906-143022.trace`。</summary>
    private static string TraceFileName(DateTimeOffset stamp) =>
        $"failed-{stamp:yyyyMMdd-HHmmss}.trace";
}

/// <summary>
/// tick をゲーム内時刻にするだけの道具。**表示専用。**
///
/// **ここの数値をテストの期待値に書かない**（ADR-0012 / test_first.md 4.1）。
/// 1 夜 = 5400 tick は ADR-0009 の「3〜5 分」から選んだバランス値で、
/// 変わりうる。変わったら報告文の見え方が変わるだけで、合否は変わらない。
///
/// 出典: docs/20_basic_design/balance.md 1 節（21:00〜6:00 / 1 tick = ゲーム内 6 秒）
/// </summary>
public static class GameClock
{
    private const int StartHour = 21;          // REQ-007（夜の始まり）
    private const int GameSecondsPerTick = 6;  // balance.md 1 節

    /// <summary>`22:58` の形。</summary>
    public static string Format(int tick)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tick);

        var minutes = tick * GameSecondsPerTick / 60;
        var hour = (StartHour + minutes / 60) % 24;

        return $"{hour:D2}:{minutes % 60:D2}";
    }
}
