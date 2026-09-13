namespace Ten.Pure;

/// <summary>
/// 乱数の用途（ADR-0008 の一覧 + DozeOff）。
///
/// **並び順を変えてはいけない。**MOD-Rng は列挙の整数値を入力に使うので、
/// 並べ替えると同じ日付の盤面が丸ごと変わる（NFR-004 / REQ-019）。
/// 足すのは末尾にだけ。末尾に足す限り既存の値は動かない（ADR-0008）。
///
/// 定義元: docs/30_detailed_design/types.md 1 節
/// </summary>
public enum RngPurpose
{
    /// <summary>親の初期覚醒度。通番なし</summary>
    ParentInitial,

    /// <summary>山札の内訳。通番＝配る順</summary>
    ParentHand,

    /// <summary>出来事。通番＝出来事の順</summary>
    NightEvent,

    /// <summary>寝たふりの成否。通番＝その夜で何回目の寝たふりか</summary>
    SleepPretend,

    /// <summary>寝たふり中の寝落ち。通番＝同上</summary>
    FallAsleep,

    /// <summary>親がどの対処を選ぶか。通番＝その夜で何回目の対処か</summary>
    ParentChoice,

    /// <summary>待機中の寝落ち。通番＝その夜で何回目の刻み判定か（screens.md D-05）</summary>
    DozeOff,

    /// <summary>診断パラメータの 3 つ目がどこに乗るか。通番＝tick（ADR-0016）</summary>
    Diagnosis,
}
