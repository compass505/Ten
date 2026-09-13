using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Ten.Pure;

/// <summary>
/// 実況の素材（<see cref="Result.Beat"/>）を夜の間に積む。
///
/// 仕様: docs/30_detailed_design/MOD-Result.md（RES-02 決着）
///
/// **状態の前後だけを見る純関数。**同じ状態列から常に同じ素材が出る（REQ-020）。
/// **盤面の中身は見ない**（REQ-028）。見るのは「起きたこと」だけ。
/// </summary>
public static class Commentary
{
    /// <summary>結果に残す件数の上限（REQ-026 / RS-1）。</summary>
    public const int Max = 3;

    /// <summary>
    /// 1 tick の前後を見て、起きたことを積む。**同じ種類は最初の 1 回だけ**。
    /// 積んだ結果は新しいリストで返す（引数を書き換えない）。
    /// </summary>
    public static IReadOnlyList<Result.Beat> Observe(
        IReadOnlyList<Result.Beat> beats, NightState prev, NightState next, Tuning tuning)
    {
        var list = new List<Result.Beat>(beats ?? Array.Empty<Result.Beat>());

        void Add(Result.BeatKind kind)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Kind == kind)
                {
                    return;
                }
            }

            list.Add(new Result.Beat(next.Tick, kind));
        }

        if (prev.Score == 0 && next.Score > 0)
        {
            Add(Result.BeatKind.FirstScore);
        }

        // **成功側の着地だけ。**Feint の満了は Sleeping に戻るので拾わない
        if (prev.Parent == ParentPhase.Settling && next.Parent == ParentPhase.Grace)
        {
            Add(Result.BeatKind.BestPretend);
        }

        if (next.EventsFired > prev.EventsFired)
        {
            Add(Result.BeatKind.EventStruck);
        }

        // 終盤の段階に入った瞬間（balance.md 8 節）。0 枚は夜の終わりなので数えない
        if (prev.Hand.Total >= tuning.HandStage1 && next.Hand.Total < tuning.HandStage1 && next.Hand.Total > 0)
        {
            Add(Result.BeatKind.HandRanLow);
        }

        if (prev.Over is null && next.Over == EndKind.FellAsleep)
        {
            Add(Result.BeatKind.FellAsleepAt);
        }

        if (prev.Over is null && next.Over == EndKind.Dawn)
        {
            Add(Result.BeatKind.DawnReached);
        }

        return list;
    }

    /// <summary>
    /// 結果に出す 3 件を選ぶ。**優先順は 終わり方 → 寝たふり → 最初の一撃 → 出来事 → 手札**。
    /// 選んだあとは起きた順（tick 昇順）に並べる。
    /// </summary>
    public static IReadOnlyList<Result.Beat> Pick(IReadOnlyList<Result.Beat> beats)
    {
        if (beats is null || beats.Count == 0)
        {
            return Array.Empty<Result.Beat>();
        }

        var sorted = new List<Result.Beat>(beats);

        // 優先順 → tick → 種類。**入力の並び順に依存させない**（REQ-020）
        sorted.Sort((a, b) =>
        {
            var p = Priority(a.Kind).CompareTo(Priority(b.Kind));

            if (p != 0)
            {
                return p;
            }

            var t = a.Tick.CompareTo(b.Tick);

            return t != 0 ? t : a.Kind.CompareTo(b.Kind);
        });

        var take = sorted.GetRange(0, sorted.Count > Max ? Max : sorted.Count);

        take.Sort((a, b) => a.Tick != b.Tick ? a.Tick.CompareTo(b.Tick) : Priority(a.Kind).CompareTo(Priority(b.Kind)));

        return take;
    }

    private static int Priority(Result.BeatKind kind) => kind switch
    {
        Result.BeatKind.FellAsleepAt => 0,
        Result.BeatKind.DawnReached => 0,
        Result.BeatKind.BestPretend => 1,
        Result.BeatKind.FirstScore => 2,
        Result.BeatKind.EventStruck => 3,
        Result.BeatKind.HandRanLow => 4,
        _ => 5,
    };

    /// <summary>保存用の文字列にする（`BestPlay.Commentary` / 進行中のプレイ）。形は `種類@tick;…`。</summary>
    public static string Encode(IReadOnlyList<Result.Beat> beats)
    {
        if (beats is null || beats.Count == 0)
        {
            return string.Empty;
        }

        var text = new StringBuilder();

        for (var i = 0; i < beats.Count; i++)
        {
            if (i > 0)
            {
                text.Append(';');
            }

            text.Append(beats[i].Kind).Append('@').Append(beats[i].Tick.ToString(CultureInfo.InvariantCulture));
        }

        return text.ToString();
    }

    /// <summary><see cref="Encode"/> の逆。**読めない部分は捨てる。例外を投げない**（REQ-033）。</summary>
    public static IReadOnlyList<Result.Beat> Decode(string? text)
    {
        var list = new List<Result.Beat>();

        if (string.IsNullOrEmpty(text))
        {
            return list;
        }

        foreach (var part in text!.Split(';'))
        {
            var at = part.IndexOf('@');

            if (at <= 0)
            {
                continue;
            }

            if (Enum.TryParse<Result.BeatKind>(part.Substring(0, at), out var kind)
                && Enum.IsDefined(typeof(Result.BeatKind), kind)
                && int.TryParse(part.Substring(at + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var tick))
            {
                list.Add(new Result.Beat(tick, kind));
            }
        }

        return list;
    }
}
