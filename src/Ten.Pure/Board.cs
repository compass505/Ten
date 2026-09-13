using System;
using System.Collections.Generic;

namespace Ten.Pure;

/// <summary>
/// MOD-Board — 日付シードから盤面を作る。
///
/// 仕様: docs/30_detailed_design/MOD-Board.md（B-1〜B-7）
/// 出典: ADR-0008（用途別の独立ストリーム）/ ADR-0011（盤面は日固定、運は毎回変わる）
///
/// **通番の割り当てを変えると盤面仕様の版が上がる**（REQ-040）。
/// </summary>
public static class Board
{
    /// <summary>盤面仕様の版（REQ-040）。**乱数実装か通番の割り当てを変えたら上げる。**</summary>
    public const int SpecVersion = 1;

    /// <summary>チュートリアル盤面の版。**日付シードの盤面と混ざらない値**（D-09 / B-7）。</summary>
    public const int TutorialSpecVersion = 9001;

    /// <summary>チュートリアル盤面の seed。`yyyy-MM-dd` と衝突しない形にする（D-09）。</summary>
    public const string TutorialSeed = "tutorial-fixed";

    /// <summary>札種の数。山札の配分に使う。</summary>
    private const int CareKinds = 4;

    /// <summary>出来事の種類の数。</summary>
    private const int EventKinds = 5;

    /// <summary>区間の端に寄せないための余白（tick）。</summary>
    private const int SegmentMargin = 60;

    /// <summary>日付シードから、その日の盤面を作る。**プレイ回数では変わらない**（ADR-0011）。</summary>
    public static BoardSpec Generate(string seed, Tuning tuning)
    {
        tuning.Validate();

        if (string.IsNullOrEmpty(seed))
        {
            throw new ArgumentException("seed が空", nameof(seed));
        }

        return new BoardSpec(
            SpecVersion,
            seed,
            InitialArousalOf(seed, tuning),
            HandOf(seed, tuning),
            EventsOf(seed, tuning));
    }

    /// <summary>
    /// チュートリアル専用の固定盤面（screens.md D-09）。日付シードを使わない。
    ///
    /// **寝たふりの好機が必ず含まれる**（B-6）。山札を厚くし、出来事を置かないことで、
    /// 閉眼して待てば必ず 1 回目の寝たふりに辿り着く。
    /// </summary>
    public static BoardSpec Tutorial(Tuning tuning)
    {
        tuning.Validate();

        // 教えるべき 3 つ（覚醒度・元気・寝たふり）が必ず出る固定値。**乱数を引かない。**
        var hand = new HandCount(3, 2, 2, 2);

        return new BoardSpec(
            TutorialSpecVersion,
            TutorialSeed,
            tuning.InitialArousalMin,
            hand,
            Array.Empty<ScheduledEvent>());
    }

    // ------------------------------------------------------------------
    // 親の初期覚醒度（通番 0。その日 1 回）
    // ------------------------------------------------------------------

    private static int InitialArousalOf(string seed, Tuning tuning)
    {
        var span = tuning.InitialArousalMax - tuning.InitialArousalMin + 1;

        return tuning.InitialArousalMin + Rng.Range(seed, RngPurpose.ParentInitial, 0, span);
    }

    // ------------------------------------------------------------------
    // 山札（通番 0 = 総枚数、1〜 = 追加分を配る順）
    // ------------------------------------------------------------------

    private static HandCount HandOf(string seed, Tuning tuning)
    {
        var span = tuning.HandMax - tuning.HandMin + 1;
        var total = tuning.HandMin + Rng.Range(seed, RngPurpose.ParentHand, 0, span);

        // **各札種が 1 枚以上**（REQ-041）。まず下限を配ってから、残りをシードで配分する
        var hand = new HandCount(
            tuning.HandPerKindMin, tuning.HandPerKindMin,
            tuning.HandPerKindMin, tuning.HandPerKindMin);

        var extra = total - tuning.HandPerKindMin * CareKinds;

        for (var i = 0; i < extra; i++)
        {
            var kind = (CareKind)Rng.Range(seed, RngPurpose.ParentHand, i + 1, CareKinds);
            hand = hand.Plus(kind, 1);
        }

        return hand;
    }

    // ------------------------------------------------------------------
    // 出来事（通番 0 = 件数、2i+1 = i 件目の時刻、2i+2 = i 件目の種類）
    // ------------------------------------------------------------------

    private static IReadOnlyList<ScheduledEvent> EventsOf(string seed, Tuning tuning)
    {
        var span = tuning.EventMax - tuning.EventMin + 1;
        var count = tuning.EventMin + Rng.Range(seed, RngPurpose.NightEvent, 0, span);

        // 夜を等分した区間に 1 件ずつ置く。**昇順で、同じ tick に 2 件は置かない**（B-3）
        var segment = tuning.NightTicks / tuning.EventSegments;
        var inner = segment - SegmentMargin * 2;
        var events = new ScheduledEvent[count];

        // **種類は重複させない。**同じ夜に同じ出来事が 2 度起きると、
        // 出来事が「その夜の顔つき」ではなく単なる乱数になる（REQ-043 / 055）
        var pool = new NightEventKind[EventKinds];

        for (var i = 0; i < EventKinds; i++)
        {
            pool[i] = (NightEventKind)i;
        }

        for (var i = 0; i < count; i++)
        {
            var offset = inner > 0 ? Rng.Range(seed, RngPurpose.NightEvent, 2 * i + 1, inner) : 0;
            var tick = segment * i + SegmentMargin + offset;

            var pick = i + Rng.Range(seed, RngPurpose.NightEvent, 2 * i + 2, EventKinds - i);
            var kind = pool[pick];

            pool[pick] = pool[i];
            pool[i] = kind;

            events[i] = new ScheduledEvent(tick, kind);
        }

        return events;
    }
}
