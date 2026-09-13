# ハーネスの実装仕様

種別: リファレンス（規約・事実）— 理由は ADR に置く
更新トリガー: 乱数の実装を変えたとき（**盤面仕様の版が上がる**） / 入力列の記法を変えたとき /
ランナーを変えたとき
状態: **2026-09-06 確定。**[ISS-06](../10_requirements/open_issues.md) の決着

出典: [ADR-0002](../10_requirements/decisions/ADR-0002-test-harness.md)（決定論の 4 条件・3 層） /
[ADR-0001](../10_requirements/decisions/ADR-0001-tech-stack.md)（Unity + Android） /
[test_first.md](../00_process/test_first.md)

> [ADR-0002](../10_requirements/decisions/ADR-0002-test-harness.md) は
> 「ランナー・乱数実装・入力列の記法は ADR-0001 の後に決める」と残していた。ここで決める。
> **後戻りコストがいずれも 1 日を超えないため ADR にしない**（documentation.md 4 節）。

## 1. ランナー

| | 決めたこと |
| --- | --- |
| フレームワーク | **NUnit**。Unity Test Framework が NUnit ベースなので、実質これしかない |
| 置き場 | `tests/unit/` `tests/harness/` `tests/e2e/`（ADR-0002 の 3 層） |

**純粋層のテストは Unity なしでも走る。**
[architecture.md](../20_basic_design/architecture.md) が純粋層を別アセンブリに分け、
`UnityEngine` を参照禁止にしているため、`tests/unit` と `tests/harness` は
**plain .NET のプロジェクトとしても実行できる。**

| 走らせ方 | 対象 | 使う場面 |
| --- | --- | --- |
| `dotnet test` | unit + harness | **開発中の主線。**Unity を起動せず秒で回る |
| Unity Test Runner | 全層 | 統合確認と e2e |
| 実機 | e2e + NFR | NFR-001 / 007 / 008 / 009 |

**この二重化が成立していること自体が、純粋層の独立性のテストになっている。**
`dotnet test` が通らなくなったら、純粋層が環境に触り始めた証拠。

## 2. 乱数の仕様

**実装をこちらで持つ**（ADR-0008）。Unity / .NET 標準は版と環境で再現を保証しない。

```
入力文字列  s = "{seed}|{purposeInt}|{ordinal}"      ※ ASCII のみ
h = 2166136261
各バイト c について:  h = (h XOR c) * 16777619        (32bit 環境で切り捨て)
最終撹拌:  h = h XOR (h >> 15);  h = h * 2246822507;  h = h XOR (h >> 13)
```

| 決めたこと | 理由 |
| --- | --- |
| **`purpose` は列挙の整数値を使う**（名前の文字列ではない） | 名前をリネームしただけで全結果が変わるのを防ぐ |
| **入力は ASCII に限る** | 文字エンコードの差で結果が変わる経路を塞ぐ。seed は `yyyy-MM-dd`、purpose と ordinal は整数 |
| `Milli` は `h / 2^32 * 1000` の切り捨て（0〜999） | 浮動小数を状態に持たない（[types.md](../30_detailed_design/types.md) 0 節） |
| `Range` は**棄却法**（`floor(2^32 / max) * max` 以上を捨てる） | 剰余バイアスを消す。捨てるときは `purpose + 1000` と `ordinal + 試行回数` で引き直す |

### テストベクタ

**[tests/vectors/rng.json](../../tests/vectors/rng.json) を正とする。**
C# 実装がこれと 1 件でも食い違ったら NFR-004 が成立しない。

| 種類 | 件数 |
| --- | --- |
| `hash`（生の 32bit 値） | 140 |
| `Milli`（0〜999） | 140 |
| `Range`（max 2〜100） | 96 |
| 一様性（6 面 12 万回。最大偏り 1.00%） | 1 |

**ベクタは参照実装（JavaScript）から生成した。**
2 つの言語で同じ値が出ることが、仕様が言語に依存していないことの担保になる。
→ TC-001〜007

## 3. 入力列の記法

**1 行 1 変化。**間の tick は直前の状態が続く（押しっぱなしを表現するため）。

```
# seed=2026-09-06 play=1 spec=1 tuning=a3f91c
12   +cry
81   -
140  eyes
203  +kick
219  -
1180 eyes
```

| 要素 | 意味 |
| --- | --- |
| ヘッダ | `seed` / `play`（プレイ回数）/ `spec`（盤面仕様の版）/ `tuning`（Tuning のハッシュ） |
| `<tick> +<行動>` | その行動を押し始める |
| `<tick> -` | 離す（発火する） |
| `<tick> eyes` | 目を閉じる / 開ける（トグル） |
| 空行・`#` 始まり | 無視 |

- **tick は昇順で、同じ tick に 2 行置かない**（[MOD-Input](../30_detailed_design/MOD-Input.md) IN-1）
- **`tuning` を記録する理由**: バランス値が変わると同じ入力列でも結果が変わる。
  再現しないときに「実装が壊れた」のか「Tuning が変わった」のかを切り分けられる
- **首の向きを記録しない。**状態列に影響しないため（[data_model.md](../20_basic_design/data_model.md) 5 節）

## 4. 失敗時の出力（決定論の条件 D4）

テストが落ちたとき、**その場で再現できる情報を必ず出す。**

```
FAILED TC-031  期待: 上昇量が単調非増加  実際: ar=30 で +12, ar=60 で +14
  seed=2026-09-06 play=1 spec=1 tuning=a3f91c
  tick=1180  (ゲーム内 22:58)
  入力列: tests/traces/failed-20260906-143022.trace
  再現:   dotnet test --filter TC-031 -- Trace=failed-20260906-143022.trace
```

| 必ず含めるもの | 理由 |
| --- | --- |
| seed / play / spec / tuning | 盤面と数値を復元する |
| 落ちた tick とゲーム内時刻 | どこで壊れたかを絞る |
| **入力列をファイルに書き出す** | ランダム入力列（TC-020）は、落ちた列を保存しないと二度と再現できない |
| 再現コマンド | 手で組み立てさせない |

## 5. ランダム入力列の作り方（TC-020 / 068 / 069）

性質ベースのテストは入力列を自動生成する。**その生成も決定論にする。**

- 入力列の生成に使う乱数も `Rng` を使い、**用途を `ParentChoice` 等と混ぜない**
  （テスト専用の用途をテスト側に持つ）
- 生成した列は、落ちたときだけ `tests/traces/` に保存する
- **通ったときは保存しない。**保存すると承認テストになり、ADR-0002 が禁じている

## 6. 動かし方（2026-09-06 時点）

**.NET SDK 8.0.424 を `~/.dotnet` に導入済み**（Microsoft 公式 `dotnet-install.sh`。sudo 不要）。
**PATH はシェルの設定に入れていない**ので、毎回これが要る。

```bash
export PATH="$HOME/.dotnet:$PATH"
cd tests/unit && dotnet test
```

| できること | 状態 |
| --- | --- |
| 純粋層のテストを書いて**落ちることを確認する** | **できる。**TC-001〜007 / 165 は確認済み |
| Unity プロジェクトの雛形と assembly definition | **できない**（Unity 未導入）。**view / shell 層に入るまで不要** |
| e2e / 実機の NFR 計測 | **できない**（Unity + 実機）。→ [decisions_pending.md](../00_process/decisions_pending.md) C 節 |

**モデル（3D モデル・アセット）の作成は Codex 側で行う**（本人判断 2026-09-06）。
このリポジトリの作業範囲に含めない。

**純粋層のテストは Unity なしで書き始められる**ので、.NET SDK が入れば
承認待ちの 3 件に依存しない TC（TC-001〜022 / 030〜049 / 070〜083 など）から着手できる。

## 7. 決めていないこと

| ID | 論点 |
| --- | --- |
| HAR-01 | `Tuning` のハッシュの取り方（全フィールドの連結か、明示的な版番号か） |
| HAR-02 | ランダム入力列の生成方針（完全ランダムか、方針つきか）。TC-069 の合格線と一緒に決める |
