using System;
using System.Collections.Generic;

namespace Ten.Pure;

/// <summary>
/// カタログの 1 件（diagnosis.md 4 節）。**ADR-0016 / REQ-062。**
///
/// **types.md に定義が無かった型。**`MOD-Result.Diagnose` の戻り値として名前だけ
/// 現れていたので、diagnosis.md 4 節の表（ID / 名前 / 文 / プロファイル）から起こした。
/// **「濃さ」（うっすら / どっぷり）の置き場は決まっていない**（diagnosis.md 2 節）。
/// → docs/00_process/decisions_pending.md
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
/// 仕様: docs/30_detailed_design/MOD-Result.md / docs/20_basic_design/diagnosis.md
/// **これはシグネチャだけの空実装である**（test_first.md 5.1）。
/// </summary>
public static class Result
{
    private const string NotYet = "フェーズ 5（実装）で書く（test_first.md 5.1）";

    /// <summary>実況の素材。`Sim` が夜の間に積む（最大 3 件まで残す）。</summary>
    public readonly record struct Beat(int Tick, BeatKind Kind);

    public enum BeatKind { FirstScore, BestPretend, HandRanLow, EventStruck, FellAsleepAt, DawnReached }

    /// <summary>その日の結果テキスト。**盤面の答えを含めない**（REQ-028）。</summary>
    public static string Compose(
        BestPlay best, int playCount, int boardSpecVersion, IReadOnlyList<Beat> beats) =>
        throw new NotImplementedException(NotYet);

    /// <summary>
    /// 7 軸の割合ベクトルに最も近い診断を、カタログから選ぶ（ADR-0016）。
    /// コサイン類似度。**同点はカタログの並び順で先を採る**（決定論）。
    /// </summary>
    public static DiagnosisEntry Diagnose(Diagnosis d, Tuning tuning) =>
        throw new NotImplementedException(NotYet);
}

/// <summary>
/// MOD-Display — 状態 → 見た目の段階。
///
/// 仕様: docs/30_detailed_design/MOD-Display.md
/// 出典: ADR-0010（数値を出さない）/ ADR-0014（閉眼中は窓の明るさだけ）
/// </summary>
public static class Display
{
    private const string NotYet = "フェーズ 5（実装）で書く（test_first.md 5.1）";

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
    public static Stages Map(NightState s, BoardSpec board, Tuning tuning) =>
        throw new NotImplementedException(NotYet);
}
