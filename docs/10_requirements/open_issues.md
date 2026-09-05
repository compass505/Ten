# 未解決の論点

決まっていないことを消さずに置いておく場所。解決したら ADR か要件に昇格させ、
ここには「→ ADR-000x で解決」と残す。

| ID | 論点 | 状態 | 決着 |
| --- | --- | --- | --- |
| ISS-01 | 何を作るか（コンセプト） | **解決** | → [ADR-0006](decisions/ADR-0006-concept.md) Accepted（リスク受容あり） |
| ISS-09 | ゲームの中身（何をする遊びか） | **解決** | → [ADR-0007](decisions/ADR-0007-game-design.md) Accepted（リスク受容あり） |
| ISS-02 | 技術スタック | **解決** | → [ADR-0001](decisions/ADR-0001-tech-stack.md) Accepted（リスク受容あり） |
| ISS-03 | テストハーネスの形 | **解決** | → [ADR-0002](decisions/ADR-0002-test-harness.md) Accepted |
| ISS-04 | 改善ループの回し方 | **解決** | → [ADR-0003](decisions/ADR-0003-improvement-loop.md) Accepted |
| ISS-05 | ドキュメント体系 | **解決** | → [ADR-0004](decisions/ADR-0004-documentation-policy.md) Accepted |
| ISS-06 | ハーネスの実装詳細（ランナー・乱数・入力列の記法） | **未着手**（ADR-0001 承認済みのため着手可能） | — |
| ISS-10 | ADR-0002 の「テスト書き換え禁止」とゲームバランス調整の衝突（ISU-08） | **解決** | → [ADR-0012](decisions/ADR-0012-balance-vs-tests.md) Accepted |
| ISS-11 | 配偶者の端末（iOS / Android）が未確認（ISU-06） | **未解決 / 高**。ユーザー判断 2026-09-02：**聞ける時に聞く**（ネタバレなしで確認できる） | — |
| ISS-12 | 本人・配偶者が「育児をネタにしたゲーム」を遊びたいか未確認（ISU-01） | **未解決 / 高**。ユーザー判断 2026-09-02：**聞ける時に聞く**。直接聞くと誘導になるため周辺の質問で確かめる | — |
| ISS-13 | 乱数の適用範囲（「同じ盤面」の範囲。ISU-14） | **解決** | → [ADR-0008](decisions/ADR-0008-randomness-scope.md) Accepted |
| ISS-14 | 1 プレイの時間構造（ADR-0007 のリアルタイム × 調査のターン制。ISU-13） | **解決**（決め手は仮説） | → [ADR-0009](decisions/ADR-0009-time-structure.md) Accepted |
| ISS-15 | 想定利用期間と成功指標（何日遊べれば成功か。ISU-16） | **未解決 / 中**。要件に成功指標を書けていない | — |
| ISS-16 | 状態の見せ方（数値を出すか / 見た目で表すか） | **解決** | → [ADR-0010](decisions/ADR-0010-no-numbers.md) Accepted |
| ISS-17 | 何度でも遊べることと日固定の乱数の衝突（ISU-44） | **解決** | → [ADR-0011](decisions/ADR-0011-per-play-randomness.md) Accepted |
| ISS-18 | 目を閉じている間、REQ-006「親が今している対処を画面から知る」を満たす経路が無い（REQ-004 が寝室の像を失うと定めているため） | **解決**（承認待ち） | → [ADR-0014](decisions/ADR-0014-closed-eyes-information.md) Proposed |
| ISS-19 | 操作系の拡張（長押しで強度 / 寝入りばなの窓）を要件に入れるか | **解決**（承認待ち） | → [ADR-0015](decisions/ADR-0015-input-intensity-and-timing.md) Proposed |
| ISS-07 | 実行ループの自動化をどこまで上げるか | 保留（実装フェーズで判断） | — |
| ISS-08 | 誰が検めるか（役割分担） | **解決** | → [ADR-0005](decisions/ADR-0005-agent-roles.md) Accepted |

## 依存関係

```
ISS-01 コンセプト ──→ ISS-09 ゲームの中身 ──→ ISS-02 スタック ──→ ISS-06 ハーネス実装詳細
 (ADR-0006)            (ADR-0007)              (ADR-0001)          └→ ISS-07 自動化の段階

ISS-01〜05/08/09 は解決済み。要件（REQ-001〜028 / NFR-001〜009）は 2026-09-02 に発番した。
その後の Codex レビュー（28 件）を反映して REQ-029〜040 を追加し、
ADR-0008〜0011 の承認をもって、REQ-001〜058 / NFR-001〜009 が **2026-09-02 に確定した。**

ただし ISS-10/11/12 は、ADR を Accepted にする際に**未解決のまま受容した**もの。
ISS-11/12 は本人に聞けば潰せる。ISS-10 は 2026-09-02 に ADR-0012 で決着した。
```
