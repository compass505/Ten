using System;
using System.Collections.Generic;
using Ten.Pure;

namespace Ten.Tests.Harness;

/// <summary>
/// ランダム入力列の生成（docs/40_test/harness.md 5 節。TC-020 / 068 / 069）。
///
/// **その生成も決定論にする。**生成に使う乱数も <see cref="Rng"/> を通すので、
/// 同じ (seed, index) からは必ず同じ列が出る。落ちた列は
/// <see cref="FailureReport"/> がファイルに残す。
/// </summary>
public static class TraceGenerator
{
    /// <summary>
    /// **テスト専用の乱数用途。**
    ///
    /// harness.md 5 節が「用途を `ParentChoice` 等と混ぜない（テスト専用の用途を
    /// テスト側に持つ）」と定めている。`RngPurpose`（src 側）に足すと、
    /// **製品の列挙にテストの都合が混ざる。**そこで定義されていない値を使う。
    ///
    /// ADR-0008 が「用途を後から足すのは安全」と言えるのは用途ごとに
    /// ストリームが独立しているからで、この値も同じ理屈で既存の値を動かさない。
    /// 9000 番台は製品側が使わない（`RngPurpose` は 0 から連番）。
    /// </summary>
    private const RngPurpose GenPurpose = (RngPurpose)9000;

    private static readonly ActionKind[] Actions =
        [ActionKind.Cry, ActionKind.Fuss, ActionKind.Kick];

    /// <summary>
    /// 入力列を 1 本生成する。
    /// </summary>
    /// <param name="header">seed / play / spec / tuning。生成の乱数は seed と index から引く</param>
    /// <param name="index">同じ seed から何本目か。**通番に入るので、本ごとに違う列になる**</param>
    /// <param name="tickCount">夜の長さ（tick）</param>
    /// <param name="changeCount">変化の数（行数）。**列の密度を決める。期待値ではない**</param>
    public static ParsedTrace Generate(TraceHeader header, int index, int tickCount, int changeCount)
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tickCount);
        ArgumentOutOfRangeException.ThrowIfNegative(changeCount);

        var entries = new List<(int Tick, TickInput Input)>();

        // 通番。1 本の中で引くたびに進める。index を混ぜて本ごとに列を変える
        var ordinal = index * 1_000_000;

        var tick = 0;
        ActionKind? held = null;

        for (var i = 0; i < changeCount && tick < tickCount; i++)
        {
            // 次の変化までの間隔。0 tick を空けないので、同じ tick に 2 行入らない
            tick += 1 + Rng.Range(header.Seed, GenPurpose, ordinal++, Math.Max(1, tickCount / Math.Max(1, changeCount) * 2));

            if (tick >= tickCount)
            {
                break;
            }

            if (held is not null)
            {
                // 押しているなら離す。押しっぱなしのまま別の行動は押せない（REQ-003）
                held = null;
                entries.Add((tick, new TickInput(null, false)));
                continue;
            }

            // 目を開閉するか、行動を押し始めるか
            var eyes = Rng.Chance(header.Seed, GenPurpose, ordinal++, 300);

            if (eyes)
            {
                entries.Add((tick, new TickInput(null, true)));
            }
            else
            {
                held = Actions[Rng.Range(header.Seed, GenPurpose, ordinal++, Actions.Length)];
                entries.Add((tick, new TickInput(held, false)));
            }
        }

        // 押しっぱなしのまま終わらせない（TraceFile.Parse が拒む形にしない）
        if (held is not null && tick + 1 < tickCount)
        {
            entries.Add((tick + 1, new TickInput(null, false)));
        }
        else if (held is not null)
        {
            entries.RemoveAt(entries.Count - 1);
        }

        return new ParsedTrace(header, new InputTrace(header.Seed, header.Play, entries));
    }
}
