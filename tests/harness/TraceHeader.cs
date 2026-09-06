using System;
using System.Collections.Generic;
using System.Globalization;
using Ten.Pure;

namespace Ten.Tests.Harness;

/// <summary>
/// 入力列のヘッダ（docs/40_test/harness.md 3 節）。
///
/// <c># seed=2026-09-06 play=1 spec=1 tuning=a3f91c</c>
///
/// **`tuning` を記録する理由**: バランス値が変わると同じ入力列でも結果が変わる。
/// 再現しないときに「実装が壊れた」のか「Tuning が変わった」のかを切り分けられる。
/// </summary>
/// <param name="Seed">盤面のシード（`yyyy-MM-dd`）</param>
/// <param name="Play">プレイ回数（ADR-0011。運はこれで変わる）</param>
/// <param name="Spec">盤面仕様の版（乱数実装を変えると上がる）</param>
/// <param name="Tuning">Tuning のハッシュ</param>
public sealed record TraceHeader(string Seed, int Play, int Spec, string Tuning)
{
    /// <summary>
    /// <see cref="InputTrace"/> は Seed と PlayIndex しか持たない（types.md 4 節）。
    /// Spec と Tuning は**再現の切り分けにだけ使う**ので、ハーネス側に置く。
    /// </summary>
    public static TraceHeader Parse(string line, int lineNo)
    {
        var body = line.TrimStart('#', ' ', '\t');
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var part in body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = part.IndexOf('=');

            if (eq <= 0)
            {
                throw new TraceFormatException(lineNo, line,
                    $"ヘッダの項目が「名前=値」でない: \"{part}\"");
            }

            fields[part[..eq]] = part[(eq + 1)..];
        }

        return new TraceHeader(
            Require(fields, "seed", lineNo, line),
            RequireInt(fields, "play", lineNo, line),
            RequireInt(fields, "spec", lineNo, line),
            Require(fields, "tuning", lineNo, line));
    }

    public string ToLine() =>
        $"# seed={Seed} play={Play.ToString(CultureInfo.InvariantCulture)} " +
        $"spec={Spec.ToString(CultureInfo.InvariantCulture)} tuning={Tuning}";

    private static string Require(IReadOnlyDictionary<string, string> fields, string key, int lineNo, string line) =>
        fields.TryGetValue(key, out var value) && value.Length > 0
            ? value
            : throw new TraceFormatException(lineNo, line,
                $"ヘッダに {key} が無い。seed / play / spec / tuning の 4 つが要る（harness.md 3 節）");

    private static int RequireInt(IReadOnlyDictionary<string, string> fields, string key, int lineNo, string line)
    {
        var raw = Require(fields, key, lineNo, line);

        return int.TryParse(raw, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new TraceFormatException(lineNo, line, $"ヘッダの {key} が整数でない: \"{raw}\"");
    }
}

/// <summary>ヘッダ付きの入力列。テキストと往復する単位。</summary>
public sealed record ParsedTrace(TraceHeader Header, InputTrace Trace);

/// <summary>
/// 入力列の記法に合わない。**握り潰さない**（壊れた列で再生すると再現できなくなる）。
/// </summary>
public sealed class TraceFormatException(int lineNo, string line, string reason)
    : Exception($"入力列の {lineNo} 行目が読めない: {reason}" +
                (line.Length > 0 ? $"\n  > {line}" : string.Empty))
{
    public int LineNo { get; } = lineNo;
    public string Line { get; } = line;
    public string Reason { get; } = reason;
}
