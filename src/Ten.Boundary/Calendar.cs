using System;
using Ten.Pure;

namespace Ten.Boundary;

/// <summary>
/// MOD-Calendar — 端末ローカル日付。
///
/// 仕様: docs/30_detailed_design/MOD-Calendar.md
///
/// **規則そのものは持たない。**正午境界の規則は
/// <see cref="BoardDateRule"/>（純粋層）にあり、ここは**端末時計を読む役だけ。**
/// → decisions_pending.md G-01（2026-09-06）
/// </summary>
public interface ICalendar
{
    /// <summary>正午 12:00 を境界とする「その日」。`yyyy-MM-dd`。</summary>
    string BoardDate { get; }
}

/// <summary>
/// 本番。端末時計を読んで <see cref="BoardDateRule.From"/> に渡す。
///
/// **時刻の取得だけをコンストラクタで受ける。**Unity / .NET どちらでも同じ形になり、
/// 「端末の時刻が取れない」場合の分岐（MOD-Calendar のエラー時）もここに閉じる。
///
/// **これはシグネチャだけの空実装である**（test_first.md 5.1）。
/// </summary>
public sealed class DeviceCalendar : ICalendar
{

    private readonly Func<DateTimeOffset> _localNow;

    public DeviceCalendar(Func<DateTimeOffset> localNow, string? lastKnown = null)
    {
        _localNow = localNow ?? throw new ArgumentNullException(nameof(localNow));
        LastKnown = lastKnown;
    }

    /// <summary>端末の時刻が取れないときに使う、前回保存した日付。</summary>
    public string? LastKnown { get; }

    public string BoardDate
    {
        get { try { return BoardDateRule.From(_localNow()); } catch { return LastKnown ?? "1970-01-01"; } }
    }
}

/// <summary>テスト用（CA-5 / ADR-0002 D3）。固定の日付を返す。</summary>
public sealed class FixedCalendar : ICalendar
{
    public FixedCalendar(string boardDate) => BoardDate = boardDate;

    public string BoardDate { get; }
}
