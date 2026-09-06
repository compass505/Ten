# 詳細設計

> フェーズ 3。**2026-09-05 着手。**基本設計 5 本（architecture / screens / balance /
> data_model / setting）を前提にしている。
> **ADR-0013〜0017 はすべて 2026-09-06 に承認済み。暫定の箇所は無い。**
> ADR-0015 により `ActStrengthMilli` と寝入りばなが確定し、
> ADR-0017 により `ParentPhase` に **`Feint`** が加わった（`Settling` の失敗側の双子）。

## 進捗

| 層 | ファイル | 状態 |
| --- | --- | --- |
| 共通 | [types.md](types.md) | 起草済み |
| 純粋 | [MOD-Rng](MOD-Rng.md) / [MOD-Board](MOD-Board.md) / [MOD-Sim](MOD-Sim.md) / [MOD-Score](MOD-Score.md) / [MOD-End](MOD-End.md) / [MOD-Result](MOD-Result.md) / [MOD-Display](MOD-Display.md) | **7 件 起草済み** |
| 境界 | [MOD-Clock](MOD-Clock.md) / [Input](MOD-Input.md) / [Storage](MOD-Storage.md) / [Calendar](MOD-Calendar.md) / [Share](MOD-Share.md) / [Power](MOD-Power.md) | **6 件 起草済み** |
| 表示 | [MOD-View](MOD-View.md) / [Tutorial](MOD-Tutorial.md) / [Shell](MOD-Shell.md) | **3 件 起草済み** |

## DoD の状態

**全 16 モジュールの公開 IF が確定した。**
残るのは各ファイルの「決めていないこと」と、[ADR-0013 / 0014 / 0015 の承認](../10_requirements/decisions/README.md)。

## 置くもの

モジュールごとに 1 ファイル（`MOD-Xxx.md`）。全モジュールが共有する型は
[types.md](types.md) にまとめる（同じ型を 16 ファイルに複製しないため）。

- 公開インターフェース（関数シグネチャ・型）
- 状態の持ち方と不変条件
- エラー時の振る舞い
- 調整値・定数の表（コードに直書きしない）

## DoD

- 各モジュールの公開 IF が確定していて、テストコードがこれだけを見て書ける
- 内部実装の詳細は書かない（書きたくなったら分割の粒度を疑う）
