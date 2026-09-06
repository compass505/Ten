using System;

namespace Ten.Pure;

/// <summary>
/// 「その日」を決める規則（REQ-019）。**正午 12:00 が境界で、12:00 未満は前日として扱う。**
///
/// 仕様: docs/30_detailed_design/MOD-Calendar.md（CA-1 / CA-2）
///
/// **なぜ純粋層にあるか。**規則を `ICalendar` の中に閉じ込めると、
/// 端末時計を直接読む形になり、**正午境界をテストから作れない**
/// （11:59:59 と 12:00:00 を与える手段が無い）。
/// 決定論の条件 D2「時間は引数で進む」（test_first.md 2 節）にも反する。
///
/// そこで**規則だけをここに出し、端末時計を読む役は境界層の `ICalendar` に残す。**
/// → docs/00_process/decisions_pending.md G-01（2026-09-06）
///
/// **これはシグネチャだけの空実装である**（test_first.md 5.1）。
/// </summary>
public static class BoardDateRule
{
    private const string NotYet = "フェーズ 5（実装）で書く（test_first.md 5.1）";

    /// <summary>
    /// 端末ローカルの現在時刻から「その日」を返す。形式は `yyyy-MM-dd`。
    ///
    /// **夜に日付境界が来ないようにするための正午境界**（CA-1）。
    /// 0:00〜11:59 は前日、12:00〜23:59 は当日。
    /// </summary>
    /// <param name="localNow">端末ローカルの現在時刻。**引数で受ける**（D2）</param>
    public static string From(DateTimeOffset localNow) =>
        throw new NotImplementedException(NotYet);
}
