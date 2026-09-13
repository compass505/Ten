# テストケース

規約は [../../00_process/test_first.md](../../00_process/test_first.md)。
対応表は [../traceability.md](../traceability.md)。

## 採番

| 範囲 | 対象 |
| --- | --- |
| TC-001〜019 | [MOD-Rng](../../30_detailed_design/MOD-Rng.md) / [MOD-Board](../../30_detailed_design/MOD-Board.md) |
| TC-020〜069 | [MOD-Sim](../../30_detailed_design/MOD-Sim.md)（**主戦場**） |
| TC-070〜089 | [MOD-Score](../../30_detailed_design/MOD-Score.md) / [MOD-End](../../30_detailed_design/MOD-End.md) / [MOD-Result](../../30_detailed_design/MOD-Result.md) / [MOD-Display](../../30_detailed_design/MOD-Display.md) |
| TC-090〜109 | 境界層 6 モジュール |
| TC-110〜129 | 表示層 3 モジュール / NFR |
| TC-130〜165 | 表示層の続き・ADR-0015〜0017 で足したもの（各ファイル内） |
| TC-166〜177 | **フェーズ 6 で足したもの**（実況の素材・診断の濃さ / MOD-Present / 実ファイルでの往復） |

**ID の意味は後から変えない。**欠番は詰めない。

## 期待値の書き方（ADR-0012）

**[balance.md](../../20_basic_design/balance.md) の具体値を期待値に書かない。**
バランス調整のたびにテストが落ちると、実装フェーズでテストを書き換えたくなる。

書いてよいのは次の 4 つだけ。

| 形 | 例 |
| --- | --- |
| **単調性** | 覚醒度 A < B のとき、同じ行動の上昇量は A のほうが B 以上 |
| **上下限** | 覚醒度は 0 未満にも 100 超にもならない |
| **エッジ** | 上限に達した「瞬間」に 1 回だけ加点される |
| **順序** | 得点判定が終了判定より先に評価される |

「17 上がる」は書かない。「前より大きい」「上限で止まる」を書く。

## ファイル

| ファイル | 中身 |
| --- | --- |
| [TC-pure-board.md](TC-pure-board.md) | 乱数と盤面 |
| [TC-pure-sim.md](TC-pure-sim.md) | 夜の状態機械 |
| [TC-pure-out.md](TC-pure-out.md) | 得点・終了・結果テキスト・段階表示 |
| [TC-boundary.md](TC-boundary.md) | 時間・入力・保存・日付・共有・電源 |
| [TC-view-shell.md](TC-view-shell.md) | 表示・チュートリアル・ライフサイクル・NFR |
| [TC-phase6.md](TC-phase6.md) | 実況の素材・診断の濃さ・状態 → 見せ方・実ファイルでの往復 |
