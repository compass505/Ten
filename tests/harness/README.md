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
| [SimRunner.cs](SimRunner.cs) | 入力列を流し込んで状態列を取る。**`Sim` → `Score` → `NightEnd` の順序をここ 1 か所に閉じ込める**（REQ-051） | [MOD-Sim](../../docs/30_detailed_design/MOD-Sim.md) |
| [Invariants.cs](Invariants.cs) | 不変条件 I-1〜I-11 の検査 | [types.md](../../docs/30_detailed_design/types.md) 3 節 |
| [SimProbe.cs](SimProbe.cs) | 状態を作る・進める・測る道具 | — |
| [SimTests.cs](SimTests.cs) | **TC-020 / 021 / 022**（不変条件・決定論） | [TC-pure-sim.md](../../docs/40_test/cases/TC-pure-sim.md) |
| [OperationTests.cs](OperationTests.cs) | **TC-023〜029**（操作と排他） | 同上 |
| [ArousalTests.cs](ArousalTests.cs) | **TC-030〜043**（覚醒度・慣れ・元気） | 同上 |
| [CareTests.cs](CareTests.cs) | **TC-045〜049**（対処と山札） | 同上 |
| [PretendTests.cs](PretendTests.cs) | **TC-050〜060 / TC-164**（寝たふり・判別不能性） | 同上 |
| [DozeAndEventTests.cs](DozeAndEventTests.cs) | **TC-062〜067**（寝落ちと出来事） | 同上 |
| [StrategyTests.cs](StrategyTests.cs) | **TC-068 / 069**（支配戦略と得点差） | 同上 |
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

**いまは 69 件中 50 件が赤、19 件が green。**
green は道具自身のテスト 18 件と TC-024（型の形を見る検査）。
どちらも**フェーズ 4 の DoD「全て落ちる」は適用しない**
（→ [traceability.md](../../docs/40_test/traceability.md) の例外表）。

## 前提の状態の作り方

**`Sim.Begin` から作り、変えたい 1 つだけを `with` で差し替える**（[SimProbe](SimProbe.cs)）。

「覚醒度 A &lt; B（他は同一）」のような前提は入力列だけでは作れない。
かといって `NightState` を丸ごと手で組むと、**バランス値を書くことになる**（ADR-0012 違反）。
`Begin` を土台にすれば、書くのは比較したい 1 つだけで済む。

**上昇量は比較にしか使わない。**絶対値を期待値に書かない。

## 順序を 1 か所に閉じ込めている

`Sim.Advance` → `Score.Apply` → `NightEnd.Evaluate` の順は **REQ-051** で決まっていて、
**型で強制できない唯一の場所**（[handoff.md](../../docs/00_process/handoff.md) 6 節）。

そこで [SimRunner.Step](SimRunner.cs) だけがこの順を持ち、各テストは自分で組み立てない。
**入れ替えると、山札の最後の 1 枚で得た点が消える**（TC-080）。

## まだ無いもの

**TC-070 以降**（境界層・表示層）。`MOD-Storage` / `MOD-Display` / `MOD-View` / `MOD-Shell`。

`tests/e2e/` は Unity が要るので手つかず。
