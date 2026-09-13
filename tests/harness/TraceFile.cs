using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Ten.Pure;

namespace Ten.Tests.Harness;

/// <summary>
/// 入力列のテキスト表現（docs/40_test/harness.md 3 節）と <see cref="InputTrace"/> の往復。
///
/// <code>
/// # seed=2026-09-06 play=1 spec=1 tuning=a3f91c
/// 12   +cry
/// 81   -
/// 140  eyes
/// 203  +kick
/// 219  -
/// 1180 eyes
/// </code>
///
/// **1 行 1 変化。**間の tick は直前の状態が続く（押しっぱなしを表現するため）。
/// **首の向きは記録しない**（状態列に影響しないため。data_model.md 5 節）。
/// </summary>
public static class TraceFile
{
    private const string HeaderPrefix = "#";

    /// <summary>離す（発火する）を表すトークン。</summary>
    private const string ReleaseToken = "-";

    /// <summary>目を閉じる / 開ける（トグル）を表すトークン。</summary>
    private const string EyesToken = "eyes";

    private static readonly IReadOnlyDictionary<string, ActionKind> ActionTokens =
        new Dictionary<string, ActionKind>(StringComparer.Ordinal)
        {
            ["cry"] = ActionKind.Cry,
            ["fuss"] = ActionKind.Fuss,
            ["kick"] = ActionKind.Kick,
        };

    private static readonly IReadOnlyDictionary<ActionKind, string> ActionNames =
        ActionTokens.ToDictionary(p => p.Value, p => p.Key);

    // ------------------------------------------------------------------
    // 読む
    // ------------------------------------------------------------------

    /// <summary>
    /// テキストを入力列に変換する。**壊れた行は握り潰さず例外にする。**
    /// 黙って捨てると、再現できない失敗の原因になる。
    /// </summary>
    /// <exception cref="TraceFormatException">記法に合わない行がある</exception>
    public static ParsedTrace Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        TraceHeader? header = null;
        var entries = new List<(int Tick, TickInput Input)>();
        var lastTick = -1;

        // 直前までの状態。1 行 1 変化なので、変化していないものは持ち越す
        ActionKind? held = null;

        var lines = text.Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var lineNo = i + 1;
            var line = lines[i].Trim('\r', ' ', '\t');

            if (line.Length == 0)
            {
                continue;
            }

            if (line.StartsWith(HeaderPrefix, StringComparison.Ordinal))
            {
                // ヘッダ行は最初の 1 本だけを採る。以降の # 始まりはコメント
                header ??= TraceHeader.Parse(line, lineNo);
                continue;
            }

            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
            {
                throw new TraceFormatException(lineNo, line,
                    $"「<tick> <変化>」の 2 語でなければならない（{parts.Length} 語あった）");
            }

            if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var tick))
            {
                throw new TraceFormatException(lineNo, line, $"tick が非負の整数でない: \"{parts[0]}\"");
            }

            // MOD-Input IN-1: tick は昇順で、同じ tick に 2 行置かない
            if (tick == lastTick)
            {
                throw new TraceFormatException(lineNo, line,
                    $"同じ tick が 2 行ある（tick={tick}）。1 tick 1 件（MOD-Input IN-1）");
            }

            if (tick < lastTick)
            {
                throw new TraceFormatException(lineNo, line,
                    $"tick が昇順でない（tick={tick} の前に tick={lastTick} がある）");
            }

            var token = parts[1];
            bool toggleEyes;

            if (token == ReleaseToken)
            {
                if (held is null)
                {
                    throw new TraceFormatException(lineNo, line,
                        "押していないものを離している。対応する「+<行動>」が無い");
                }

                held = null;
                toggleEyes = false;
            }
            else if (token == EyesToken)
            {
                toggleEyes = true;
            }
            else if (token.StartsWith('+'))
            {
                var name = token[1..];

                if (!ActionTokens.TryGetValue(name, out var kind))
                {
                    throw new TraceFormatException(lineNo, line,
                        $"知らない行動: \"{name}\"（使えるのは {string.Join(" / ", ActionTokens.Keys)}）");
                }

                if (held is not null)
                {
                    throw new TraceFormatException(lineNo, line,
                        $"{ActionNames[held.Value]} を押したまま {name} を押している。" +
                        "離す「-」を挟む（REQ-003: 行動の実行中は別の行動を受け付けない）");
                }

                held = kind;
                toggleEyes = false;
            }
            else
            {
                throw new TraceFormatException(lineNo, line,
                    $"知らない変化: \"{token}\"（使えるのは +<行動> / {ReleaseToken} / {EyesToken}）");
            }

            entries.Add((tick, new TickInput(held, toggleEyes)));
            lastTick = tick;
        }

        if (header is null)
        {
            throw new TraceFormatException(0, string.Empty,
                "ヘッダ行が無い。「# seed=... play=... spec=... tuning=...」が要る（harness.md 3 節）");
        }

        if (held is not null)
        {
            throw new TraceFormatException(lines.Length, string.Empty,
                $"{ActionNames[held.Value]} が押しっぱなしのまま終わっている。離す「-」で閉じる");
        }

        return new ParsedTrace(
            header,
            new InputTrace(header.Seed, header.Play, entries));
    }

    // ------------------------------------------------------------------
    // 書く
    // ------------------------------------------------------------------

    /// <summary>
    /// 入力列をテキストにする。**落ちたときだけ保存する**（harness.md 5 節）。
    /// 通ったときに保存すると承認テストになり、ADR-0002 が禁じている。
    /// </summary>
    public static string Write(ParsedTrace trace)
    {
        ArgumentNullException.ThrowIfNull(trace);

        var sb = new StringBuilder();
        sb.Append(trace.Header.ToLine()).Append('\n');

        ActionKind? previousHeld = null;

        foreach (var (tick, input) in trace.Trace.Entries)
        {
            string token;

            if (input.ToggleEyes)
            {
                token = EyesToken;
            }
            else if (input.Held is null)
            {
                token = ReleaseToken;
            }
            else
            {
                token = "+" + ActionNames[input.Held.Value];
            }

            sb.Append(tick.ToString(CultureInfo.InvariantCulture).PadRight(5))
              .Append(token)
              .Append('\n');

            previousHeld = input.Held;
        }

        _ = previousHeld;
        return sb.ToString();
    }

    // ------------------------------------------------------------------
    // 展開
    // ------------------------------------------------------------------

    /// <summary>
    /// 1 行 1 変化の記法を、tick ごとの入力に展開する。
    /// **間の tick は直前の状態が続く**（押しっぱなし）。
    /// </summary>
    /// <param name="trace">入力列</param>
    /// <param name="tickCount">展開する長さ。1 夜なら balance.md の 5400</param>
    public static IEnumerable<TickInput> Expand(InputTrace trace, int tickCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tickCount);

        var current = new TickInput(null, false);
        var next = 0;

        for (var tick = 0; tick < tickCount; tick++)
        {
            if (next < trace.Entries.Count && trace.Entries[next].Tick == tick)
            {
                current = trace.Entries[next].Input;
                next++;
            }
            else
            {
                // ToggleEyes は「その tick の 1 回きり」。押しっぱなしと違って持ち越さない
                current = current with { ToggleEyes = false };
            }

            yield return current;
        }
    }
}
