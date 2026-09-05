# MOD-Clock — 実時間 → 固定 tick

種別: リファレンス / 層: **境界層**
更新トリガー: tick レートを変えたとき / 中断の契機を変えたとき
対応要件: REQ-007 / 009 / 020 / NFR-005 / 006

出典: [ADR-0009](../10_requirements/decisions/ADR-0009-time-structure.md)（固定 tick）

## 公開 IF

```csharp
public interface IClock {                        // D2: クロック注入
    /// 前回からの経過を受け取り、消化すべき tick 数を返す。**端数は内部に残す**
    int Consume(double deltaSeconds);
    void Reset();
    bool IsPaused { get; }
    void Pause();      // フォーカス喪失（REQ-009）
    void Resume();
}

public sealed class RealClock : IClock { }       // 本番
public sealed class StepClock : IClock { }       // テスト。任意の tick 数を直接与える
```

## 満たすこと

| # | 性質 | 要件 |
| --- | --- | --- |
| C-1 | **実フレームレートに結果を依存させない。**更新間隔を変えても同じ入力列なら同じ状態列 | NFR-005 |
| C-2 | 端数を持ち越す。20 Hz 未満のフレームでも tick を落とさない | NFR-005 |
| C-3 | **1 フレームで消化する tick 数に上限を置く**（既定 10）。長い停止からの復帰で一気に進めない | REQ-009 |
| C-4 | `Pause()` 中は `Consume` が常に 0 を返す | REQ-009 |
| C-5 | `StepClock` に差し替えられる（テストで実時間を待たない） | ADR-0002 D2 |

**C-3 の理由**: フォーカス喪失を取り逃した場合でも、復帰時に数百 tick を一度に進めると
プレイヤーの知らないうちに夜が終わる。上限を置いて、超過分は捨てる（`Pause` 漏れの保険）。

## エラー時

| 状況 | 振る舞い |
| --- | --- |
| `deltaSeconds < 0`（端末の時刻が巻き戻った） | 0 を返す。**負の時間を進めない** |
| `deltaSeconds` が異常に大きい | C-3 の上限で頭打ち |

## 決めていないこと

| ID | 論点 |
| --- | --- |
| CLK-01 | Unity のどのループに載せるか（`Update` / `FixedUpdate`）。`FixedUpdate` は Unity 側の時間刻みに縛られるので、**`Update` + 自前の蓄積**を第一候補にする |
