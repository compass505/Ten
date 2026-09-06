namespace Ten.Pure;

/// <summary>
/// 赤ちゃんの覚醒行動（REQ-003）。
///
/// **`t_idle` をリセットする 3 つ**（screens.md 4.3）。
/// 目の開閉はここに含めない。目は行動ではなく <see cref="TickInput.ToggleEyes"/>。
///
/// 定義元: docs/30_detailed_design/types.md 1 節
/// </summary>
public enum ActionKind
{
    /// <summary>泣く</summary>
    Cry,

    /// <summary>ぐずる</summary>
    Fuss,

    /// <summary>ばたつかせる</summary>
    Kick,
}
