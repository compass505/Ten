using System.Collections.Generic;

namespace Ten.Pure;

/// <summary>
/// その tick に割り付いた入力。複数来たら先着 1 件だけを採り、残りは捨てる（MOD-Input IN-1）。
///
/// 定義元: docs/30_detailed_design/types.md 4 節
/// </summary>
/// <param name="Held">押しっぱなしの行動（ADR-0015 / REQ-060）。離した tick で null になる</param>
/// <param name="ToggleEyes">目を閉じる / 開ける</param>
public readonly record struct TickInput(
    ActionKind? Held,
    bool ToggleEyes
);

/// <summary>
/// ハーネスが再生する入力列。REQ-020 の「入力列」の実体。
///
/// **`Entries` は `Tick` の昇順で、同じ tick は 1 件まで。**
/// この正規化は `MOD-Input` が行い、純粋層は正規化済みしか受け取らない。
///
/// 定義元: docs/30_detailed_design/types.md 4 節
/// テキスト表現と往復する手段は tests/harness/TraceFile.cs（harness.md 3 節）。
/// </summary>
public readonly record struct InputTrace(
    string Seed,
    int PlayIndex,
    IReadOnlyList<(int Tick, TickInput Input)> Entries
);
