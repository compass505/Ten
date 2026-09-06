namespace Ten.Pure;

/// <summary>親の対処。山札の札種でもある（REQ-041）。</summary>
public enum CareKind { PatPat, Milk, Hold, DiaperChange }

/// <summary>赤ちゃんの状態（screens.md 4.1）。</summary>
public enum BabyPhase { Open, Charging, Acting, EyesClosed }

/// <summary>
/// 親の状態（screens.md 4.2）。
///
/// **<see cref="Feint"/> は <see cref="Settling"/> の失敗側の双子**（ADR-0017）。
/// 観測上、両者は区別できてはならない。違うのは満了後の行き先だけ
/// （<see cref="Grace"/> か <see cref="Sleeping"/> か）。→ types.md I-13 / TC-164
/// </summary>
public enum ParentPhase { Sleeping, Caring, Settling, Feint, Grace, Up }

/// <summary>夜の終わり方（REQ-054）。**判定順もこの並び**（screens.md 4.5 順 10）。</summary>
public enum EndKind { Dawn, FellAsleep, HandEmpty }

/// <summary>その夜の出来事（REQ-043 / 055）。</summary>
public enum NightEventKind { DiaperSoiled, PartnerWakes, PhoneRings, ParentRolls, GetsHungry }
