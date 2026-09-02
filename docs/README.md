# ドキュメント地図

置き場の規約は [00_process/documentation.md](00_process/documentation.md)。

| 場所 | 種別 | 中身 | 状態 |
| --- | --- | --- | --- |
| [00_process/workflow.md](00_process/workflow.md) | 規約 | フェーズ定義・ID 体系・戻り方 | 確定 |
| [00_process/test_first.md](00_process/test_first.md) | 規約 | テストファースト / ハーネスの条件 | 確定（**ISS-10 未解決**） |
| [00_process/loop.md](00_process/loop.md) | 規約 | 実行ループ / 改善ループ | 確定 |
| [00_process/documentation.md](00_process/documentation.md) | 規約 | ドキュメントの置き場と書き方 | 確定 |
| [00_process/rationale.md](00_process/rationale.md) | **説明** | なぜこの進め方なのか。トレードオフ | — |
| [10_requirements/](10_requirements/) | 成果物 | 要件・スコープ・調査・決定 | **フェーズ 1 進行中** |
| [10_requirements/research/](10_requirements/research/) | 材料 | 調査ノート（出典つき） | 13 本 完了 / 1 本 棄却 |
| [10_requirements/decisions/](10_requirements/decisions/) | 決定 | ADR | 7 件 Accepted（うち 3 件はリスク受容あり） |
| [20_basic_design/](20_basic_design/) | 成果物 | モジュール分割・遷移 | 未着手 |
| [30_detailed_design/](30_detailed_design/) | 成果物 | 公開 IF・定数表 | 未着手 |
| [40_test/](40_test/) | 成果物 | テストケース・トレーサビリティ | 未着手 |
| [50_review/](50_review/) | 記録 | 改善ループの周回記録・課題 | Codex レビュー 18 件を記録 |
| [_templates/](_templates/) | 型 | ADR / テストケース / ループ記録 | — |
| `../.claude/skills/` | 手順 | フェーズ別スキル 5 本 | — |
| `../.claude/agents/` | 監査 | doc-auditor | — |
| `../tools/check_docs.py` | 検証 | 機械チェック | — |

## 読む順

初めて読むとき: [rationale.md](00_process/rationale.md) → [workflow.md](00_process/workflow.md) → 現在のフェーズの成果物。

作業中に参照するとき: 規約ファイルを直接。理由は読まなくてよい。
