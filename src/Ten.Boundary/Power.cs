using System;

namespace Ten.Boundary;

/// <summary>
/// MOD-Power — 画面消灯の抑止。
///
/// 仕様: docs/30_detailed_design/MOD-Power.md（PW-1〜PW-4）/ REQ-035
/// </summary>
public interface IPower
{
    void KeepAwake(bool on);

    bool IsAwake { get; }
}

/// <summary>
/// **抑止するかどうかの規則。**Unity にも端末にも触らない。
///
/// `Screen`（表示層の型）を持ち込まずに済むよう、**真偽値で受け取る。**
/// 呼び出し側（`MOD-Shell`）が `Current == Screen.Night` を判定して渡す。
///
/// **これはシグネチャだけの空実装である**（test_first.md 5.1）。
/// </summary>
public static class PowerRule
{

    /// <summary>
    /// 画面を点けたままにするか。
    ///
    /// **`SCR-Night` かつ `ST-N-Run` の間だけ true**（PW-1 / PW-3）。
    /// 中断中は false（PW-2。電池を食わない）。夜が終わっていても false。
    ///
    /// 寝たふり中は入力が無いので、放置すると OS が画面を消す。
    /// **盲目区間が消灯で壊れないことが最低条件**（MOD-Power の PW-1 の理由）。
    /// </summary>
    /// <param name="isNightScreen">いま `SCR-Night` にいるか</param>
    /// <param name="isPaused">`ST-N-Pause` に入っているか（フォーカス喪失など。REQ-009）</param>
    /// <param name="isOver">夜が終わっているか</param>
    public static bool ShouldKeepAwake(bool isNightScreen, bool isPaused, bool isOver)
    { return isNightScreen && !isPaused && !isOver; }
}

/// <summary>
/// 本番。`FLAG_KEEP_SCREEN_ON` を立てる。**権限は要らない**（PW-4 / NFR-003）。
///
/// Unity API を叩くので、**実体は Unity プロジェクト側に置く。**
/// ここに置くのは口だけ。
/// </summary>
public sealed class NullPower : IPower
{
    /// <summary>テスト用（ADR-0002 D3）。呼ばれたことだけ覚える。</summary>
    public bool IsAwake { get; private set; }

    /// <summary>何回切り替えられたか。**無駄な呼び出しを見つけるため。**</summary>
    public int Calls { get; private set; }

    public void KeepAwake(bool on)
    {
        IsAwake = on;
        Calls++;
    }
}
