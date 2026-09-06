# harness

アプリの中身を UI 抜きで駆動する層と、それを使ったテスト。

```
入力列 + 初期状態 + シード  →  ハーネス  →  観測結果
```

**主戦場はここ**（[test_first.md](../../docs/00_process/test_first.md) 3 節）。
要件 `REQ-xxx` の大半をここで担保する。
**依存してよいのは公開 IF だけ。**`src` の internal に触らない。

## 中身（2026-09-06）

| ファイル | 役割 | 出典 |
| --- | --- | --- |
| [TraceFile.cs](TraceFile.cs) | 入力列のテキスト ⇔ `InputTrace`。tick ごとへの展開 | [harness.md](../../docs/40_test/harness.md) 3 節 |
| [TraceHeader.cs](TraceHeader.cs) | `# seed=... play=... spec=... tuning=...` | 同 3 節 |
| [FailureReport.cs](FailureReport.cs) | 落ちたときの出力と、入力列の保存（**D4**） | 同 4 節 |
| [TraceGenerator.cs](TraceGenerator.cs) | ランダム入力列。**生成も決定論** | 同 5 節 |
| [HarnessSelfTests.cs](HarnessSelfTests.cs) | **道具自身のテスト**（TC-xxx ではない） | — |

## 満たすこと

- 同じ入力を与えたら必ず同じ結果になる（決定論性）
- 失敗時に、再現に必要な情報（シード・入力列・状態）を出力する
- 実時間を待たない（時間は引数で進める）

## 入力列の記法

**1 行 1 変化。**間の tick は直前の状態が続く（押しっぱなしを表現するため）。

```
# seed=2026-09-06 play=1 spec=1 tuning=a3f91c
12   +cry
81   -
140  eyes
```

**壊れた行は握り潰さず例外にする。**黙って捨てると、再現できない失敗の原因になる。
拒む形は [HarnessSelfTests.cs](HarnessSelfTests.cs) の `壊れた列は理由つきで落ちる` が全部並べてある。

## 走らせ方

```bash
export PATH="$HOME/.dotnet:$PATH"
cd tests/harness && dotnet test
```

**いまは 21 件中 18 件が green、3 件が赤。**
赤 3 件は `TraceGenerator`（`Rng` の空実装を踏む）。
green 18 件は道具自身のテストで、**フェーズ 4 の DoD「全て落ちる」は適用しない**
（→ [traceability.md](../../docs/40_test/traceability.md)）。

## まだ無いもの

**`Sim` を実際に駆動する再生系。**`Sim.Begin` / `Advance` を呼んで状態列を取る部分は、
`BoardSpec` / `Tuning` / `NightState` の型が要るのでこれから。
TC-020 以降がそこに乗る。
