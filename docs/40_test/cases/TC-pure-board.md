# TC-001〜019 — 乱数と盤面

対象: [MOD-Rng](../../30_detailed_design/MOD-Rng.md) / [MOD-Board](../../30_detailed_design/MOD-Board.md)
種別は全て unit。

| TC | 対象 REQ | 前提 | 手順 | 期待値 |
| --- | --- | --- | --- | --- |
| TC-001 | REQ-019 / NFR-004 | 任意の seed | 同じ (seed, 用途, 通番) で 1000 回引く | **全て同じ値** |
| TC-002 | REQ-019 | seed A / B（1 文字違い） | 同じ用途・通番で引く | 値が異なる（1000 組で一致率 < 1%） |
| TC-003 | ADR-0008 | 任意の seed | 用途だけを変えて引く | **他の用途の値が動かない**（全用途 × 通番 0〜99 を記録して比較） |
| TC-004 | ADR-0008 | 任意の seed | 通番を 1 つ飛ばす（0,1,3…） | **飛ばした後の値が、飛ばさない場合と一致** |
| TC-005 | NFR-004 | 任意の seed | `Range(…, 6)` を 10 万回 | 各値の出現が期待値 ±3% |
| TC-006 | NFR-004 | — | `Rng` の実装を静的に検査 | `System.Random` / `UnityEngine` を参照していない |
| TC-007 | — | `ordinal = -1` / `exclusiveMax = 0` / 空 seed | 呼ぶ | **例外を投げる**（握り潰さない） |
| TC-010 | REQ-019 / 021 | 任意の seed | `Board.Generate` を 100 回 | **全て同一の `BoardSpec`** |
| TC-011 | REQ-041 / 050 | 100 個の seed | 盤面を生成 | `Hand.Total` が設定範囲内、かつ**各札種が 1 枚以上** |
| TC-012 | REQ-043 | 100 個の seed | 盤面を生成 | `Events` が Tick 昇順、件数が範囲内、**同じ tick に 2 件無い** |
| TC-013 | REQ-043 | 100 個の seed | 盤面を生成 | 全ての `Event.Tick` が `0 < Tick < 5400` |
| TC-014 | REQ-052 | 100 個の seed | 盤面を生成 | `InitialArousal` が 0 以上、かつ**上限未満**（開始時に得点済みにしない） |
| TC-015 | REQ-034 / D-09 | — | `Board.Tutorial()` を呼ぶ | seed を取らず、常に同一。`SpecVersion` が日付シードの盤面と**異なる** |
| TC-016 | REQ-034 | — | `Board.Tutorial()` の盤面で、寝たふりを狙う入力列を再生 | **1 回目の寝たふりが必ず成功する** |
| TC-017 | REQ-048 | 同じ seed、`playIndex` 0 と 1 | 盤面を生成 | **`BoardSpec` が完全に一致**（盤面はプレイ回数で変わらない） |
| TC-018 | — | `HandMin > HandMax` の `Tuning` | `Generate` を呼ぶ | **例外を投げる**（壊れた盤面を返さない） |
