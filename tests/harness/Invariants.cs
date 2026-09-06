using System;
using System.Collections.Generic;
using Ten.Pure;

namespace Ten.Tests.Harness;

/// <summary>
/// <see cref="NightState"/> の不変条件 I-1〜I-11（docs/30_detailed_design/types.md 3 節）。
///
/// **全 TC の前提として常に検査する**（TC-020）。どれか 1 つでも破れた状態は、
/// そこから先の判定に意味が無くなる。
///
/// **バランス値を書かない**（ADR-0012）。ここに出てくる数値は types.md が
/// 不変条件そのものとして定めた範囲（0〜100 / 0〜5400）だけ。
/// </summary>
public static class Invariants
{
    /// <summary>破れている不変条件の説明。破れていなければ null。</summary>
    public static string? FirstViolation(NightState s)
    {
        // I-1
        if (s.Arousal is < 0 or > 100)
        {
            return $"I-1: Arousal が範囲外（{s.Arousal}）";
        }

        if (s.Vigor is < 0 or > 100)
        {
            return $"I-1: Vigor が範囲外（{s.Vigor}）";
        }

        foreach (var (name, value) in HabitValues(s.Habit))
        {
            if (value is < 0 or > 100)
            {
                return $"I-1: Habit.{name} が範囲外（{value}）";
            }
        }

        // I-2
        if (s.Tick is < 0 or > 5400)
        {
            return $"I-2: Tick が範囲外（{s.Tick}）";
        }

        // I-3 / I-4
        var acting = s.ActKind is not null && s.ActRemain > 0;

        if ((s.Baby == BabyPhase.Acting) != acting)
        {
            return $"I-3: Baby={s.Baby} と ActKind={s.ActKind}/ActRemain={s.ActRemain} が食い違う";
        }

        var charging = s.ActKind is not null && s.ActRemain == 0;

        if ((s.Baby == BabyPhase.Charging) != charging)
        {
            return $"I-4: Baby={s.Baby} と ActKind={s.ActKind}/ActRemain={s.ActRemain} が食い違う";
        }

        // I-5
        var caring = s.ActiveCare is not null && s.CareRemain > 0;

        if ((s.Parent == ParentPhase.Caring) != caring)
        {
            return $"I-5: Parent={s.Parent} と ActiveCare={s.ActiveCare}/CareRemain={s.CareRemain} が食い違う";
        }

        // I-6
        if (s.PendingCare is not null && s.Parent is not (ParentPhase.Sleeping or ParentPhase.Up))
        {
            return $"I-6: 予告中なのに Parent={s.Parent}";
        }

        // I-7
        foreach (var (name, value) in HandValues(s.Hand))
        {
            if (value < 0)
            {
                return $"I-7: Hand.{name} が負（{value}）";
            }
        }

        // I-8
        if (s.Baby == BabyPhase.EyesClosed)
        {
            if (s.TClosed <= 0)
            {
                return $"I-8: 閉眼中なのに TClosed={s.TClosed}";
            }
        }
        else if (s.TClosed != 0)
        {
            return $"I-8: 開眼中なのに TClosed={s.TClosed}";
        }

        // I-9（ADR-0017 で Feint に拡張）
        if (s.Parent is ParentPhase.Settling or ParentPhase.Feint && s.Baby != BabyPhase.EyesClosed)
        {
            return $"I-9: Parent={s.Parent} なのに Baby={s.Baby}（閉眼していない）";
        }

        return null;
    }

    /// <summary>I-11: 3 本の通番が単調非減少であること。**列全体を見ないと判定できない。**</summary>
    public static string? FirstOrdinalRegression(IReadOnlyList<NightState> states)
    {
        ArgumentNullException.ThrowIfNull(states);

        for (var i = 1; i < states.Count; i++)
        {
            var (prev, now) = (states[i - 1], states[i]);

            if (now.PretendN < prev.PretendN)
            {
                return $"I-11: tick={now.Tick} で PretendN が戻った（{prev.PretendN} → {now.PretendN}）";
            }

            if (now.DozeN < prev.DozeN)
            {
                return $"I-11: tick={now.Tick} で DozeN が戻った（{prev.DozeN} → {now.DozeN}）";
            }

            if (now.ChoiceN < prev.ChoiceN)
            {
                return $"I-11: tick={now.Tick} で ChoiceN が戻った（{prev.ChoiceN} → {now.ChoiceN}）";
            }
        }

        return null;
    }

    private static IEnumerable<(string Name, int Value)> HabitValues(Habit h)
    {
        yield return (nameof(h.PatPat), h.PatPat);
        yield return (nameof(h.Milk), h.Milk);
        yield return (nameof(h.Hold), h.Hold);
        yield return (nameof(h.DiaperChange), h.DiaperChange);
    }

    private static IEnumerable<(string Name, int Value)> HandValues(HandCount h)
    {
        yield return (nameof(h.PatPat), h.PatPat);
        yield return (nameof(h.Milk), h.Milk);
        yield return (nameof(h.Hold), h.Hold);
        yield return (nameof(h.DiaperChange), h.DiaperChange);
    }
}
