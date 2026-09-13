# MOD-Board — 盤面の生成

種別: リファレンス / 層: **純粋層**
更新トリガー: 盤面の構成要素が増えたとき（**盤面仕様の版が上がる**）
対応要件: REQ-019 / 021 / 040 / 041 / 043 / 048

## 公開 IF

```csharp
public static class Board {
    /// 日付シードから、その日の盤面を作る。プレイ回数では変わらない（ADR-0011）
    public static BoardSpec Generate(string seed, Tuning tuning);

    /// チュートリアル専用の固定盤面（screens.md D-09）。日付シードを使わない
    public static BoardSpec Tutorial(Tuning tuning);

    public const int SpecVersion = 1;
}
```

## 満たすこと

| # | 性質 | 根拠 |
| --- | --- | --- |
| B-1 | 同じ seed から常に同じ `BoardSpec` | REQ-019 / 021 |
| B-2 | `Hand.Total` が `tuning.HandMin`〜`HandMax` の範囲。**各札種が 1 枚以上** | REQ-041 / 050 |
| B-3 | `Events` は `Tick` 昇順、件数が `tuning.EventMin`〜`EventMax`、**同じ tick に 2 件無い** | REQ-043 |
| B-4 | `Events` の `Tick` は `0 < Tick < 5400`（夜明けちょうどに置かない） | REQ-043 |
| B-5 | `InitialArousal` が `0 <= x <= 100`、かつ **100 未満**（開始時に得点済みにしない） | REQ-052 |
| B-6 | `Tutorial()` は seed を取らず、**寝たふりの好機が必ず含まれる** | REQ-034 / D-09 |
| B-7 | `Tutorial()` の `SpecVersion` は日付シードの盤面と**混ざらない値** | D-09 |

## 使う乱数の通番

| 用途 | 通番 |
| --- | --- |
| `ParentInitial` | 0（その日 1 回） |
| `ParentHand` | 0 = 総枚数、1〜 = 追加分を配る順 |
| `NightEvent` | 0 = 件数、`2i+1` = i 件目の時刻、`2i+2` = i 件目の種類 |

**この割り当てを変えると盤面仕様の版が上がる**（REQ-040 / ADR-0008）。

## エラー時

| 状況 | 振る舞い |
| --- | --- |
| `Tuning` の範囲が不正（`HandMin > HandMax` 等） | `ArgumentException`。**盤面を壊して返さない** |

## 決めていないこと

| ID | 論点 |
| --- | --- |
| BRD-01 | 出来事の時刻を「夜を N 等分した区間から 1 件ずつ」にするか、完全にランダムにするか。前者は偏りが出ない代わりに予測されやすい |
