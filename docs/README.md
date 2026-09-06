# ドキュメント地図

置き場の規約は [00_process/documentation.md](00_process/documentation.md)。

| 場所 | 種別 | 中身 | 状態 |
| --- | --- | --- | --- |
| [00_process/workflow.md](00_process/workflow.md) | 規約 | フェーズ定義・ID 体系・戻り方 | 確定 |
| [00_process/test_first.md](00_process/test_first.md) | 規約 | テストファースト / ハーネスの条件 | 確定（ISS-10 は ADR-0012 で決着） |
| [00_process/loop.md](00_process/loop.md) | 規約 | 実行ループ / 改善ループ | 確定 |
| [00_process/documentation.md](00_process/documentation.md) | 規約 | ドキュメントの置き場と書き方 | 確定 |
| [00_process/rationale.md](00_process/rationale.md) | **説明** | なぜこの進め方なのか。トレードオフ | — |
| [00_process/decisions_pending.md](00_process/decisions_pending.md) | リファレンス | **いま人間が決めること（1 枚）** | **承認 3 件が保留中**（ADR-0015 / 0016 / 0017） |
| [00_process/handoff.md](00_process/handoff.md) | リファレンス | **セッションをまたぐ文脈。**現在地 / 会話でだけ決まったこと / 測って分かったこと / 踏んだ罠 | 2026-09-06 起草 |
| [10_requirements/](10_requirements/) | 成果物 | 要件・スコープ・調査・決定 | **フェーズ 1 完了**（2026-09-02 確定） |
| [10_requirements/game_overview.md](10_requirements/game_overview.md) | リファレンス | **どんなゲームかの全体像**（散らばった決定の集約） | 2026-09-02 |
| [10_requirements/research/](10_requirements/research/) | 材料 | 調査ノート（出典つき） | 13 本 完了 / 1 本 棄却 |
| [10_requirements/decisions/](10_requirements/decisions/) | 決定 | ADR | **14 件 Accepted**（うち 3 件はリスク受容あり）+ **ADR-0015 / 0016 / 0017 が承認待ち** |
| [20_basic_design/](20_basic_design/) | 成果物 | モジュール分割・遷移・数値・データ・設定・診断 | **完了**（architecture / screens / balance / data_model / setting / diagnosis） |
| [30_detailed_design/](30_detailed_design/) | 成果物 | 公開 IF・型 | **完了**（types + 16 モジュール。純粋層 7 / 境界 6 / 表示 3） |
| [40_test/](40_test/) | 成果物 | テストケース・トレーサビリティ・ハーネス仕様 | **TC-001〜163 発番済み。テストコードは未着手**（.NET SDK が無い） |
| [50_review/](50_review/) | 記録 | 改善ループの周回記録・課題 | Codex レビュー 18 + 28 + 24 件を記録 |
| [_templates/](_templates/) | 型 | ADR / テストケース / ループ記録 | — |
| `../.claude/skills/` | 手順 | フェーズ別スキル 5 本 | — |
| `../.claude/agents/` | 監査 | doc-auditor | — |
| `../tools/check_docs.py` | 検証 | 機械チェック | — |

## 読む順

初めて読むとき: **[handoff.md](00_process/handoff.md)（いまどこにいるか）** →
[game_overview.md](10_requirements/game_overview.md)（何を作っているか） →
[rationale.md](00_process/rationale.md) → [workflow.md](00_process/workflow.md) → 現在のフェーズの成果物。

作業中に参照するとき: 規約ファイルを直接。理由は読まなくてよい。
