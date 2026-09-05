# MOD-Score — 得点とエッジ検出

種別: リファレンス / 層: **純粋層**
更新トリガー: 得点の定義を変えたとき / 最高成績の比較規則を変えたとき
対応要件: REQ-023 / 025 / 052

## 公開 IF

```csharp
public static class Score {
    /// 覚醒度が上限に達した瞬間に 1 回だけ加点する。**MOD-End より先に呼ぶ**（REQ-051）
    public static NightState Apply(NightState s, Tuning tuning);

    /// その日の最高成績を選ぶ。同点なら**先に遊んだほう**を残す（REQ-025）
    public static bool IsBetter(BestPlay candidate, BestPlay? current);
}
```

## 満たすこと

| # | 性質 | 要件 |
| --- | --- | --- |
| SC-1 | 覚醒度が上限に達した**瞬間に 1 回だけ**加点。滞在中は加点しない | REQ-052 |
| SC-2 | 再加点には覚醒度が `tuning.Rearm` まで下がる必要がある | REQ-052 |
| SC-3 | 得点は「親を完全に覚醒させた回数」であり、終わり方で加減しない | REQ-023 / 054 |
| SC-4 | `IsBetter` は同点で `false`（先に遊んだほうが残る） | REQ-025 |
| SC-5 | **`MOD-End` より先に評価される。**最後の 1 枚による上昇も得点になる | REQ-051 |

**SC-1 の反例テスト**: 覚醒度 100 のまま 100 tick 滞在して、得点が 1 のままであること。
**SC-2 の反例テスト**: 100 → 41 → 100 で得点が 1 のまま、100 → 40 → 100 で 2 になること。

## エラー時

| 状況 | 振る舞い |
| --- | --- |
| `s.Over != null` | `s` をそのまま返す |
| `tuning.Rearm >= tuning.ArousalMax` | `ArgumentException`（**再加点が不可能な設定を弾く**） |

## 決めていないこと

| ID | 論点 |
| --- | --- |
| SCR-01 | `BestPlay` の比較に得点以外（終わり方・所要時間）を入れるか。**今は入れない**（REQ-054 が「終わり方は加減しない」と言っている） |
