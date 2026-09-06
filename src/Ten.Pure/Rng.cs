using System;

namespace Ten.Pure;

/// <summary>
/// MOD-Rng — 用途別の決定論的乱数。
///
/// 仕様: docs/30_detailed_design/MOD-Rng.md / docs/40_test/harness.md 2 節
/// 出典: ADR-0008（用途別の独立ストリーム）
///
/// **これはシグネチャだけの空実装である。**フェーズ 4（テスト作成）の DoD
/// 「全テストが落ちる」を確認するために、フェーズ 3 で確定した公開 IF を
/// 機械可読にしただけのもの。**本体は throw の 1 行しか書かない**
/// （docs/00_process/test_first.md 5 節の線引き）。
///
/// 中身を書くのはフェーズ 5。そのとき、テストは 1 行も変えない。
/// </summary>
public static class Rng
{
    private const string NotYet =
        "フェーズ 5（実装）で書く。いまはシグネチャだけの空実装（test_first.md 5 節）";

    /// <summary>
    /// 値は [0, 1) の固定小数（0〜999）。**double を返さない**（端末差を作らない）。
    /// </summary>
    public static int Milli(string seed, RngPurpose purpose, int ordinal) =>
        throw new NotImplementedException(NotYet);

    /// <summary>
    /// 0 &lt;= 結果 &lt; exclusiveMax。剰余バイアスを除いた一様整数。
    /// </summary>
    public static int Range(string seed, RngPurpose purpose, int ordinal, int exclusiveMax) =>
        throw new NotImplementedException(NotYet);

    /// <summary>
    /// probMilli/1000 の確率で true。
    /// </summary>
    public static bool Chance(string seed, RngPurpose purpose, int ordinal, int probMilli) =>
        throw new NotImplementedException(NotYet);

    /// <summary>
    /// FNV-1a（32bit）+ 最終撹拌。入力は <c>seed|purposeInt|ordinal</c> の ASCII。
    ///
    /// **公開しない。**MOD-Rng の公開 IF は Milli / Range / Chance の 3 つだけ
    /// （MOD-Rng.md）。ここを internal にしているのは、参照ベクタ
    /// （tests/vectors/rng.json の hash 節 140 件）が 32 ビット全部を固定しており、
    /// Milli（上位 10 ビット相当）と Range（下位数ビット相当）だけでは
    /// 撹拌の中間ビットが未検証のまま残るため（TC-165 / NFR-004）。
    /// </summary>
    internal static uint Hash(string seed, RngPurpose purpose, int ordinal) =>
        throw new NotImplementedException(NotYet);
}
