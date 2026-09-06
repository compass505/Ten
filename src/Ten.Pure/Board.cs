using System;

namespace Ten.Pure;

/// <summary>
/// MOD-Board — 日付シードから盤面を作る。
///
/// 仕様: docs/30_detailed_design/MOD-Board.md
/// **これはシグネチャだけの空実装である**（test_first.md 5.1）。
/// </summary>
public static class Board
{
    private const string NotYet = "フェーズ 5（実装）で書く（test_first.md 5.1）";

    /// <summary>盤面仕様の版（REQ-040）。**乱数実装を変えたら上げる。**</summary>
    public const int SpecVersion = 1;

    /// <summary>日付シードから、その日の盤面を作る。**プレイ回数では変わらない**（ADR-0011）。</summary>
    public static BoardSpec Generate(string seed, Tuning tuning) =>
        throw new NotImplementedException(NotYet);

    /// <summary>チュートリアル専用の固定盤面（screens.md D-09）。日付シードを使わない。</summary>
    public static BoardSpec Tutorial(Tuning tuning) =>
        throw new NotImplementedException(NotYet);
}
