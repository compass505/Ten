namespace Ten.Pure;

/// <summary>
/// 夜の状態（`MOD-Sim` が持つ。data_model.md 4 節と 1 対 1）。
///
/// **すべて不変。**更新は新しい値を返す。これが決定論（D1〜D4）の前提。
/// **浮動小数点を持たない。**`int` と 1/1000 固定小数だけ（NFR-004）。
///
/// 定義元: docs/30_detailed_design/types.md 3 節
/// 不変条件 I-1〜I-13 は `MOD-Sim` が常に守る。テストはそこを見る。
/// </summary>
public readonly record struct NightState(
    // 同一性
    int PlayIndex,                 // その日の何回目のプレイか（REQ-048）
    // 時刻
    int Tick,                      // 0〜5400
    // 赤ちゃん
    BabyPhase Baby,
    ActionKind? ActKind,
    int ActRemain,
    int ActStrengthMilli,          // 0〜1000。長押しの強度（ADR-0015 / REQ-060）
    int ChargeTicks,               // 溜め始めてからの tick 数。**溜めの代償を測るため**（ADR-0015）
    bool ActFired,                 // この行動が既に発火したか
    int Vigor,                     // 0〜100
    // 親
    ParentPhase Parent,
    int Arousal,                   // 0〜100
    CareKind? ActiveCare,
    int CareRemain,
    CareKind? PendingCare,         // 予告中の対処（REQ-046 / 049）
    int CareDelay,
    Habit Habit,
    HandCount Hand,
    // 時計（screens.md 4.3）
    int TIdle, int TClosed, int TSettle, int TGrace,
    // 乱数の通番（引き直さないために保存する。REQ-020 / 032）
    int PretendN, int DozeN, int ChoiceN,
    // 出来事とその後遺症
    int EventsFired,
    int RollUntil, int HungryUntil, bool PartnerHere,
    int LockUntil, ActionKind LockedKinds, int DampUntil, int DampMilli,
    // 診断（ADR-0016 / REQ-062）
    Diagnosis Diag,
    // 得点
    int Score,
    bool ScoredEdge,               // 加点済みか（REQ-052 のエッジ検出）
    bool PretendPrimed,            // 猶予中に泣いた（順 7 で解決する予約）
    int CalmBlock,                 // 起きている親が落ち着くのを止めている残り
    // 終了
    bool Dozed,                    // 寝落ちが成立した（MOD-End が読む。順 5 / 6）
    EndKind? Over
);
