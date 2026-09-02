# 調査: テストハーネス

**この調査で何を決めたいのか**: 「テストファーストで、ハーネス的に」を具体的に何にするか。
ハーネスが満たすべき条件、テストの層の分け方、期待値の書き方。

調査日: 2026-08-31 / → [ADR-0002](../decisions/ADR-0002-test-harness.md)

---

## 1. 決定論的シミュレーションテスト（DST）

分散システム界隈で確立された手法。**ハーネスの原型として最も参考になる。**

### 何をやっているか

システム全体を単一スレッド上で走らせ、乱数・時間・I/O を全て外から制御する。
Antithesis のドキュメントは 4 要素に整理している:

| 要素 | 内容 |
| --- | --- |
| 仮想環境での実行 | 制御されたサンドボックス内で動かす |
| エントロピーの制御 | ランダムに見えるが完全に再現可能な入力を与える |
| 状態空間の探索 | 入力と障害パターンを振る |
| 不変条件の検査 | 常に成り立つべき性質を継続的に検証する |

### 成立条件（そのままハーネスの必要条件になる）

eatonphil の解説が列挙している非決定性の排除項目:

- グローバルな乱数を廃し、**シード付き乱数**を引数で渡す
- システム時刻ではなく**注入されたクロック**を使う
- ディスク・ネットワーク I/O を**差し替え可能なインターフェース**の背後に置く
- ブロッキング呼び出しを避ける（単一スレッド実行が成立しなくなる）
- **失敗時にシードを出力する**。同じシードで完全に同じ実行を再現できることが価値の中心

### 実例

- **FoundationDB**: 2010 年頃に DST を実用化した元祖。ディスク・ネットワーク・
  マシンクラッシュまで含めたクラスタ全体を単一スレッドでシミュレートする
- **TigerBeetle**: VOPR（Viewstamped Operation Replicator）という名前のシミュレータ。
  数ヶ月分の稼働を数分の実時間に圧縮して障害を注入する
- **Antithesis**: FoundationDB の中の人が作った、決定論的ハイパーバイザとしての外付け DST

### 2 つの実装方針

| 方針 | 内容 | 向き |
| --- | --- | --- |
| 差し替え可能な部品 | 非決定的な部分を最初から注入可能に設計する（FoundationDB 型） | **新規開発。今回はこちら** |
| 決定論的ハイパーバイザ | 既存システムを外から仮想化する（Antithesis 型） | 既存システムへの後付け |

### 効くところ / 効かないところ

効く: 並行性・状態・協調が絡むもの。分散DB、金融取引エンジン、非同期ワークフロー。
効かない: モックした振る舞いしかテストしていないので、実環境との統合は別途必要。
またコードを変えるとシードは無効化される（同じシード＝同じ実行、は同一コードでのみ成立）。

Antithesis の主張で重要な一点: **テストの本数ではない。**
狭いテストを大量に書くより、包括的なシミュレーションを少数回すほうが効くことが多い。

> 出典:
> - [What's the big deal about Deterministic Simulation Testing? — notes.eatonphil.com](https://notes.eatonphil.com/2024-08-20-deterministic-simulation-testing.html)
> - [Deterministic simulation testing — Antithesis Docs](https://antithesis.com/docs/resources/deterministic_simulation_testing/)
> - [Protocol-Aware Deterministic Simulation Testing — TigerBeetle](https://tigerbeetle.com/blog/2026-08-20-protocol-aware-dst/)
> - [awesome-deterministic-simulation-testing](https://github.com/ivanyu/awesome-deterministic-simulation-testing)

---

## 2. 期待値の書き方 3 種

| 種別 | 書くもの | 強み | 弱み |
| --- | --- | --- | --- |
| 例示ベース（example-based） | 具体的な入力 → 具体的な期待値 | 意図が読める。仕様書になる | 書いた分しか守れない |
| 性質ベース（property-based） | 常に成り立つ性質。入力は自動生成 | 想定外のエッジケースを掘り当てる | 失敗時の原因追跡が重い |
| 承認テスト（approval / golden master） | 実行結果を丸ごと保存して差分比較 | テストのない既存コードを一気に固定できる | 「なぜその値か」が残らない |

「Golden Master」「スナップショット」「特性テスト（characterization）」「承認テスト」は
**実質すべて同じ手法**の別名。呼び方としては「承認テスト」が適切とされる。
人間が結果を承認しており、承認をやり直せば期待値を更新できる、という含みがあるため。

**使い分けの結論**:
- 仕様として残したいもの → 例示ベース（`TC-xxx` はこれ）
- 不変条件・往復変換・順序非依存など → 性質ベース
- 承認テストは**新規開発では原則使わない**。テストのないコードを後から囲うための道具であり、
  最初から使うと「期待値が何だったのか」が誰にも分からなくなる

> 出典:
> - [Regression / Characterization / Approval テストの違い — understandlegacycode.com](https://understandlegacycode.com/blog/characterization-tests-or-approval-tests/)
> - [Why we should be saying 'Approval Testing' instead of 'Golden Master'](https://coding-is-like-cooking.info/2021/03/why-we-should-be-saying-approval-testing-instead-of-golden-master/)

---

## 3. エージェントと組んだテストファースト

Anthropic の Claude Code ベストプラクティスが挙げている手順:

1. テストを先に書く。**実装をこの時点で書かないと明示する**
2. テストを走らせ、**落ちることを確認する**
3. テストに満足したらコミットする
4. テストを変更せずに実装を書き、全部通るまで続ける

より根本的な原則として「**検証手段を与えろ**」がある。
検証手段がないと「それらしく見える」が唯一の停止条件になり、人間が検証ループそのものになる。
テスト・ビルドの終了コード・リンタ・出力とフィクスチャの差分・スクリーンショット比較など、
合否が返るものなら何でもよい。

失敗パターンとして名指しされているもの:
- **trust-then-verify gap**: それらしいがエッジケースを踏み抜く実装。
  → 検証できないものは出荷しない
- **over-specified CLAUDE.md**: 長すぎるルールは無視される
- レビューは実装したのと別の文脈（サブエージェント）にやらせる。
  ただし「粗を探せ」と言われたレビュアは健全なコードにも粗を報告するので、
  **正しさと要件に効くものだけを findings にする**と指示する

> 出典: [Best practices for Claude Code — code.claude.com](https://code.claude.com/docs/en/best-practices)

---

## この調査から導く結論（→ ADR-0002）

1. ハーネスの必要条件は **シード注入・クロック注入・I/O 差し替え・失敗時のシード出力**。
   これはスタックが何であっても変わらないので、先に確定してよい。
2. テストは unit / harness / e2e の 3 層。ハーネス層が主戦場。
3. `TC-xxx` は例示ベースで書く。性質ベースは不変条件に限って併用。承認テストは使わない。
4. 「落ちることの確認」をフェーズ 4 の DoD に入れる。
5. スタック依存部分（ランナー、乱数ライブラリ、入力列の記法）は ADR-0001 の後。
