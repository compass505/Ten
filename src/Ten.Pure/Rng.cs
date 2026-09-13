using System;

namespace Ten.Pure;

/// <summary>
/// MOD-Rng — 用途別の決定論的乱数。
///
/// 仕様: docs/30_detailed_design/MOD-Rng.md / docs/40_test/harness.md 2 節
/// 出典: ADR-0008（用途別の独立ストリーム）
///
/// **実装をこちらで持つ**（R-5）。環境の乱数・時計・GUID に触らない。
/// 入力文字列は <c>seed|purposeInt|ordinal</c> の ASCII のみ（harness.md 2 節）。
/// </summary>
public static class Rng
{
    // FNV-1a（32bit）の定数。harness.md 2 節。
    private const uint FnvOffsetBasis = 2166136261u;
    private const uint FnvPrime = 16777619u;

    // 最終撹拌。
    private const int MixShift1 = 15;
    private const uint MixMultiplier = 2246822507u;
    private const int MixShift2 = 13;

    /// <summary>棄却したときに用途をずらす幅（harness.md 2 節「purpose + 1000」）。</summary>
    private const int RejectPurposeOffset = 1000;

    private const long TwoPow32 = 4294967296L;

    /// <summary>
    /// 値は [0, 1) の固定小数（0〜999）。**double を返さない**（端末差を作らない）。
    /// </summary>
    public static int Milli(string seed, RngPurpose purpose, int ordinal)
    {
        var h = Hash(seed, purpose, ordinal);

        // h / 2^32 * 1000 の切り捨て。浮動小数を通さない（types.md 0 節）。
        return (int)((ulong)h * 1000UL / (ulong)TwoPow32);
    }

    /// <summary>
    /// 0 &lt;= 結果 &lt; exclusiveMax。剰余バイアスを除いた一様整数。
    /// </summary>
    public static int Range(string seed, RngPurpose purpose, int ordinal, int exclusiveMax)
    {
        if (exclusiveMax <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(exclusiveMax), exclusiveMax, "exclusiveMax は 1 以上（MOD-Rng）");
        }

        // 棄却法: floor(2^32 / max) * max 以上を捨てる（harness.md 2 節）。
        var limit = (ulong)(TwoPow32 / exclusiveMax * exclusiveMax);

        var h = (ulong)Hash(seed, purpose, ordinal);

        // 捨てるときは purpose + 1000 と ordinal + 試行回数 で引き直す。
        for (var attempt = 1; h >= limit; attempt++)
        {
            h = (ulong)HashCore(seed, (int)purpose + RejectPurposeOffset, ordinal + attempt);
        }

        return (int)(h % (ulong)exclusiveMax);
    }

    /// <summary>
    /// probMilli/1000 の確率で true。
    /// </summary>
    public static bool Chance(string seed, RngPurpose purpose, int ordinal, int probMilli) =>
        Milli(seed, purpose, ordinal) < probMilli;

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
        HashCore(seed, (int)purpose, ordinal);

    private static uint HashCore(string seed, int purposeInt, int ordinal)
    {
        if (string.IsNullOrEmpty(seed))
        {
            throw new ArgumentOutOfRangeException(nameof(seed), seed, "seed が空（MOD-Rng）");
        }

        if (ordinal < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ordinal), ordinal, "ordinal は 0 以上（MOD-Rng）");
        }

        var h = FnvOffsetBasis;

        h = FeedAscii(h, seed);
        h = Feed(h, (byte)'|');
        h = FeedInt(h, purposeInt);
        h = Feed(h, (byte)'|');
        h = FeedInt(h, ordinal);

        // 最終撹拌。
        h ^= h >> MixShift1;
        h = unchecked(h * MixMultiplier);
        h ^= h >> MixShift2;

        return h;
    }

    private static uint Feed(uint h, byte c) => unchecked((h ^ c) * FnvPrime);

    private static uint FeedAscii(uint h, string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (c > 0x7F)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(text), text, "入力は ASCII に限る（harness.md 2 節）");
            }

            h = Feed(h, (byte)c);
        }

        return h;
    }

    /// <summary>整数を 10 進の ASCII として食わせる。負号も ASCII の 1 バイト。</summary>
    private static uint FeedInt(uint h, int value)
    {
        if (value < 0)
        {
            h = Feed(h, (byte)'-');
        }

        // 桁を上位から取り出す。文字列を作らない（割り当てを避ける）。
        var magnitude = value < 0 ? -(long)value : value;
        var scale = 1L;

        while (magnitude / scale >= 10)
        {
            scale *= 10;
        }

        while (scale > 0)
        {
            var digit = magnitude / scale % 10;
            h = Feed(h, (byte)('0' + digit));
            scale /= 10;
        }

        return h;
    }
}
