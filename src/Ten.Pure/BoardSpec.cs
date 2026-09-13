using System;
using System.Collections.Generic;

namespace Ten.Pure;

/// <summary>
/// 山札の内訳（REQ-041）。
///
/// 定義元: docs/30_detailed_design/types.md 2 節
/// </summary>
public readonly record struct HandCount(int PatPat, int Milk, int Hold, int DiaperChange)
{
    public int Total => PatPat + Milk + Hold + DiaperChange;

    public int Of(CareKind k) => k switch
    {
        CareKind.PatPat => PatPat,
        CareKind.Milk => Milk,
        CareKind.Hold => Hold,
        CareKind.DiaperChange => DiaperChange,
        _ => 0,
    };

    /// <summary>0 を下回らない。</summary>
    public HandCount Minus(CareKind k) => Plus(k, -1);

    public HandCount Plus(CareKind k, int n) => k switch
    {
        CareKind.PatPat => this with { PatPat = AtLeastZero(PatPat + n) },
        CareKind.Milk => this with { Milk = AtLeastZero(Milk + n) },
        CareKind.Hold => this with { Hold = AtLeastZero(Hold + n) },
        CareKind.DiaperChange => this with { DiaperChange = AtLeastZero(DiaperChange + n) },
        _ => this,
    };

    private static int AtLeastZero(int v) => v < 0 ? 0 : v;
}

/// <summary>その夜に起きる出来事とその時刻。</summary>
public readonly record struct ScheduledEvent(int Tick, NightEventKind Kind);

/// <summary>
/// 日付シードから決まる、その日の盤面。**プレイ回数では変わらない**（ADR-0011）。
///
/// 定義元: docs/30_detailed_design/types.md 2 節
/// </summary>
/// <param name="SpecVersion">盤面仕様の版（REQ-040）</param>
/// <param name="Seed">端末ローカル日付（正午境界）から作る `yyyy-MM-dd`</param>
/// <param name="Events">`Tick` の昇順。同じ tick に 2 件は置かない</param>
public readonly record struct BoardSpec(
    int SpecVersion,
    string Seed,
    int InitialArousal,
    HandCount Hand,
    IReadOnlyList<ScheduledEvent> Events
)
{
    /// <summary>
    /// **`Events` を中身で比べる。**既定の生成では配列の参照比較になり、
    /// 同じ seed から作った 2 つの盤面が「違うもの」と判定される（B-1 / REQ-019）。
    /// </summary>
    public bool Equals(BoardSpec other)
    {
        if (SpecVersion != other.SpecVersion
            || !string.Equals(Seed, other.Seed, StringComparison.Ordinal)
            || InitialArousal != other.InitialArousal
            || !Hand.Equals(other.Hand))
        {
            return false;
        }

        var a = Events;
        var b = other.Events;

        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a is null || b is null || a.Count != b.Count)
        {
            return false;
        }

        for (var i = 0; i < a.Count; i++)
        {
            if (!a[i].Equals(b[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hash = SpecVersion * 31;

        hash = hash * 31 + (Seed is null ? 0 : Seed.GetHashCode());
        hash = hash * 31 + InitialArousal;
        hash = hash * 31 + Hand.GetHashCode();
        hash = hash * 31 + (Events is null ? 0 : Events.Count);

        return hash;
    }
}

/// <summary>
/// 対処への慣れ（REQ-029）。
///
/// 定義元: docs/30_detailed_design/types.md 3 節
/// </summary>
public readonly record struct Habit(int PatPat, int Milk, int Hold, int DiaperChange)
{
    public int Of(CareKind k) => k switch
    {
        CareKind.PatPat => PatPat,
        CareKind.Milk => Milk,
        CareKind.Hold => Hold,
        CareKind.DiaperChange => DiaperChange,
        _ => 0,
    };

    public int Max
    {
        get
        {
            var m = PatPat;

            if (Milk > m)
            {
                m = Milk;
            }

            if (Hold > m)
            {
                m = Hold;
            }

            if (DiaperChange > m)
            {
                m = DiaperChange;
            }

            return m;
        }
    }

    /// <summary>0〜100 に丸める。</summary>
    public Habit Plus(CareKind k, int n) => k switch
    {
        CareKind.PatPat => this with { PatPat = Clamp(PatPat + n) },
        CareKind.Milk => this with { Milk = Clamp(Milk + n) },
        CareKind.Hold => this with { Hold = Clamp(Hold + n) },
        CareKind.DiaperChange => this with { DiaperChange = Clamp(DiaperChange + n) },
        _ => this,
    };

    public Habit Decay(int n) => new(Clamp(PatPat - n), Clamp(Milk - n), Clamp(Hold - n), Clamp(DiaperChange - n));

    private static int Clamp(int v) => v < 0 ? 0 : v > 100 ? 100 : v;
}

/// <summary>
/// 一晩の診断パラメータ。値は 1/10 単位の整数（diagnosis.md 1 節 / ADR-0016 / REQ-062）。
///
/// **軸の並びは固定**（いらだち / 疲労 / 徒労 / 寝不足 / 不安 / 翻弄 / いとおしさ）。
/// 並べ替えるとカタログのプロファイルと対応が外れる。
/// </summary>
public readonly record struct Diagnosis(
    int Irritation, int Fatigue, int Futility, int SleepLoss,
    int Anxiety, int Jerked, int Fondness)
{
    /// <summary>軸の数。カタログのプロファイルもこの長さ。</summary>
    public const int Axes = 7;

    public int Total => Irritation + Fatigue + Futility + SleepLoss + Anxiety + Jerked + Fondness;

    /// <summary>合計で割った割合（1/1000）。合計 0 のときは既定値（すべて 0）を返す。</summary>
    public IReadOnlyList<int> Ratios
    {
        get
        {
            var raw = Values;
            var total = Total;

            if (total <= 0)
            {
                return new[] { 0, 0, 0, 0, 0, 0, 0 };
            }

            var ratios = new int[Axes];

            for (var i = 0; i < Axes; i++)
            {
                ratios[i] = raw[i] * 1000 / total;
            }

            return ratios;
        }
    }

    /// <summary>7 軸を並び順どおりに取り出す。</summary>
    public IReadOnlyList<int> Values =>
        new[] { Irritation, Fatigue, Futility, SleepLoss, Anxiety, Jerked, Fondness };

    /// <summary>軸を 1 つ増やす（<paramref name="axis"/> は 0〜6）。**減ることはない**（I-12）。</summary>
    internal Diagnosis Add(int axis, int amount)
    {
        if (amount <= 0)
        {
            return this;
        }

        return axis switch
        {
            0 => this with { Irritation = Irritation + amount },
            1 => this with { Fatigue = Fatigue + amount },
            2 => this with { Futility = Futility + amount },
            3 => this with { SleepLoss = SleepLoss + amount },
            4 => this with { Anxiety = Anxiety + amount },
            5 => this with { Jerked = Jerked + amount },
            6 => this with { Fondness = Fondness + amount },
            _ => this,
        };
    }
}

/// <summary>
/// その日の最高成績（REQ-025）。
///
/// 定義元: docs/30_detailed_design/types.md 6 節
/// </summary>
public readonly record struct BestPlay(
    int Score, int PlayIndex, EndKind EndKind,
    string Commentary, Diagnosis Diag, string DiagId
);
