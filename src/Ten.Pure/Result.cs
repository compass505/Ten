using System;
using System.Collections.Generic;
using System.Text;

namespace Ten.Pure;

/// <summary>
/// カタログの 1 件（diagnosis.md 4 節）。**ADR-0016 / REQ-062。**
/// </summary>
/// <param name="Id">`DX-01` の形。**合計が閾値未満のときは `DX-41` を固定で返す**</param>
/// <param name="Profile">7 次元の重み。並びは Diagnosis と同じ</param>
public readonly record struct DiagnosisEntry(
    string Id,
    string Name,
    string Text,
    IReadOnlyList<int> Profile
);

/// <summary>
/// MOD-Result — 結果テキストと診断。
///
/// 仕様: docs/30_detailed_design/MOD-Result.md（RS-1〜RS-8）/ docs/20_basic_design/diagnosis.md
/// </summary>
public static class Result
{
    /// <summary>実況の素材。`Sim` が夜の間に積む（最大 3 件まで残す）。</summary>
    public readonly record struct Beat(int Tick, BeatKind Kind);

    public enum BeatKind { FirstScore, BestPretend, HandRanLow, EventStruck, FellAsleepAt, DawnReached }

    /// <summary>実況に使う素材の上限（MOD-Result のエラー時: 超えたら先頭 3 件）。</summary>
    private const int MaxBeats = 3;

    /// <summary>
    /// その日の結果テキスト。**盤面の答えを含めない**（REQ-028 / RS-5）。
    ///
    /// **出す数字は 4 つだけ**（通算回数 / 何回目 / 得点 / 盤面仕様の版）。
    /// 盤面から来る数値（初期覚醒度・山札の内訳・出来事の時刻）は一切載せない。
    /// </summary>
    public static string Compose(
        BestPlay best, int playCount, int boardSpecVersion, IReadOnlyList<Beat> beats)
    {
        var text = new StringBuilder();

        text.Append("今夜は通算").Append(playCount).Append("回のうち、")
            .Append(best.PlayIndex).Append("回目がいちばんよかった。");

        text.Append(EndPhrase(best.EndKind)).Append('、')
            .Append("起こせたのは").Append(best.Score).Append("回")
            .Append(BeatPhrase(beats)).Append("。");

        text.Append("（盤面 v").Append(boardSpecVersion).Append('）');

        return text.ToString();
    }

    /// <summary>終わり方（REQ-054 / RS-3）。**3 通りが区別できる形で入る。**</summary>
    private static string EndPhrase(EndKind kind) => kind switch
    {
        EndKind.Dawn => "朝まで持ちこたえられて",
        EndKind.FellAsleep => "こちらが先に寝てしまって",
        EndKind.HandEmpty => "親の手がすっかり尽きて",
        _ => "夜が終わって",
    };

    /// <summary>
    /// 実況の素材を 1 句にする。**「起きたこと」であって「盤面の中身」ではない**（RS-5）。
    /// 句点を増やさないので、文の数は常に 3 以内（RS-1）。
    /// </summary>
    private static string BeatPhrase(IReadOnlyList<Beat> beats)
    {
        if (beats is null || beats.Count == 0)
        {
            return string.Empty;
        }

        var take = beats.Count > MaxBeats ? MaxBeats : beats.Count;
        var phrase = new StringBuilder();

        for (var i = 0; i < take; i++)
        {
            phrase.Append(i == 0 ? "（" : "、").Append(BeatWord(beats[i].Kind));
        }

        return phrase.Append('）').ToString();
    }

    private static string BeatWord(BeatKind kind) => kind switch
    {
        BeatKind.FirstScore => "はじめの一撃が効いた",
        BeatKind.BestPretend => "寝たふりが通った",
        BeatKind.HandRanLow => "手札が心もとなくなった",
        BeatKind.EventStruck => "思わぬ邪魔が入った",
        BeatKind.FellAsleepAt => "まぶたが重くなった",
        BeatKind.DawnReached => "窓が白んできた",
        _ => "夜が動いた",
    };

    /// <summary>
    /// 7 軸の割合ベクトルに最も近い診断を、カタログから選ぶ（ADR-0016 / RS-7）。
    /// コサイン類似度。**同点はカタログの並び順で先を採る**（決定論）。
    /// </summary>
    public static DiagnosisEntry Diagnose(Diagnosis d, Tuning tuning)
    {
        tuning.Validate();

        // **合計が閾値未満なら固定で返す。**割合ベクトルが定義できない（RS-8）
        if (d.Total < tuning.DiagnosisMinTotal)
        {
            return DiagnosisCatalog.Fallback;
        }

        var ratios = d.Ratios;
        var catalog = DiagnosisCatalog.All;

        var bestIndex = 0;
        var bestNum = -1L;
        var bestDen = 1L;

        for (var i = 0; i < catalog.Count; i++)
        {
            var profile = catalog[i].Profile;

            long dot = 0;
            long normP = 0;

            for (var a = 0; a < Diagnosis.Axes; a++)
            {
                dot += (long)ratios[a] * profile[a];
                normP += (long)profile[a] * profile[a];
            }

            if (dot <= 0 || normP <= 0)
            {
                continue;
            }

            // cos = dot / (|r| * |p|)。|r| は全候補で共通なので、dot^2 / normP の大小で比べれば足りる
            var num = dot * dot;

            if (num * bestDen > bestNum * normP)
            {
                bestNum = num;
                bestDen = normP;
                bestIndex = i;
            }
        }

        return catalog[bestIndex];
    }
}

/// <summary>
/// MOD-Display — 状態 → 見た目の段階。
///
/// 仕様: docs/30_detailed_design/MOD-Display.md（D-1〜D-7）
/// 出典: ADR-0010（数値を出さない）/ ADR-0014（閉眼中は窓の明るさだけ）
///
/// **視線による遮蔽はここでは扱わない**（`MOD-View` の担当）。
/// 純粋層はカメラの向きを知らない。
/// </summary>
public static class Display
{
    /// <summary>
    /// 段階。**見えないものは -1。**推測して埋めない（TC-130）。
    /// </summary>
    public readonly record struct Stages(
        int Arousal,          // 0〜3（4 段階）。見えないときは -1
        int Vigor,            // 0〜2（3 段階）。自分の体なので常に見える
        int Hand,             // 0〜2（3 段階）。見えないときは -1
        int TimeLeft,         // 0〜4（5 段階）。見えないときは -1
        bool ArousalExact,    // 端点（上限に達している）
        bool HandExact,       // 端点（空）
        bool DawnImminent     // 端点（夜明け直前）
    );

    /// <summary>状態 → 段階。**純関数。毎 tick 導出する（保存しない）。**</summary>
    public static Stages Map(NightState s, BoardSpec board, Tuning tuning)
    {
        tuning.Validate();

        // **丸めて段階を返さない**（不変条件が破れている証拠）
        Require(nameof(s.Arousal), s.Arousal, 0, tuning.ArousalMax);
        Require(nameof(s.Vigor), s.Vigor, 0, tuning.VigorMax);
        Require(nameof(s.Tick), s.Tick, 0, tuning.NightTicks);

        var closed = s.Baby == BabyPhase.EyesClosed;

        // D-4 / D-5 / D-6: 閉眼中・親が視界から消えている間・寝返りの間は覚醒度が読めない
        var faceVisible = !closed && s.Parent != ParentPhase.Up && s.RollUntil <= s.Tick;

        var arousal = faceVisible ? ArousalStage(s.Arousal, tuning) : -1;
        var hand = closed ? -1 : HandStage(s.Hand.Total, tuning);

        // 窓の明るさは閉眼中も分かる（REQ-008 / D-12）
        var timeLeft = TimeStage(s.Tick, tuning);

        return new Stages(
            arousal,
            VigorStage(s.Vigor, tuning),
            hand,
            timeLeft,
            faceVisible && s.Arousal >= tuning.ArousalMax,
            !closed && s.Hand.Total == 0,
            tuning.NightTicks - s.Tick <= tuning.DawnImminentTicks);
    }

    private static int ArousalStage(int v, Tuning t) =>
        v < t.ArousalStage1 ? 0 : v < t.ArousalStage2 ? 1 : v < t.ArousalStage3 ? 2 : 3;

    private static int VigorStage(int v, Tuning t) =>
        v < t.VigorStage1 ? 0 : v < t.VigorStage2 ? 1 : 2;

    private static int HandStage(int total, Tuning t) =>
        total < t.HandStage1 ? 0 : total < t.HandStage2 ? 1 : 2;

    private static int TimeStage(int tick, Tuning t)
    {
        var stage = tick / t.TimeStageTicks;
        var last = t.NightTicks / t.TimeStageTicks - 1;

        if (last < 0)
        {
            last = 0;
        }

        return stage > last ? last : stage;
    }

    private static void Require(string what, int value, int lo, int hi)
    {
        if (value < lo || value > hi)
        {
            throw new ArgumentOutOfRangeException(
                what, value, $"{what} が範囲外（{lo}〜{hi}）。不変条件が破れている（MOD-Display）");
        }
    }
}
