# Ten

**現在のフェーズ: 1. 要件定義（調査）**

何を作るかは **ADR-0006 / 0007 で確定**（2026-09-02）。技術スタックは **ADR-0001**。
次は要件（`REQ-xxx`）の発番。調査 → 要件確定 → 設計 → テスト → 実装 の順で進める。
ドキュメントの地図は [docs/README.md](docs/README.md)。
進め方の理由は [docs/00_process/rationale.md](docs/00_process/rationale.md)。

| 項目 | 状態 |
| --- | --- |
| コンセプト | **確定** → [ADR-0006](docs/10_requirements/decisions/ADR-0006-concept.md) |
| ゲームの中身 | **確定** → [ADR-0007](docs/10_requirements/decisions/ADR-0007-game-design.md) |
| 要件 | 未確定 → [requirements.md](docs/10_requirements/requirements.md) |
| 技術スタック | **確定**（Unity + Android） → [ADR-0001](docs/10_requirements/decisions/ADR-0001-tech-stack.md) |
| テストファースト / ハーネス | **確定** → [ADR-0002](docs/10_requirements/decisions/ADR-0002-test-harness.md) |
| 改善ループ | **確定** → [ADR-0003](docs/10_requirements/decisions/ADR-0003-improvement-loop.md) |
| ドキュメント体系 | **確定** → [ADR-0004](docs/10_requirements/decisions/ADR-0004-documentation-policy.md) |

未解決の論点は [open_issues.md](docs/10_requirements/open_issues.md) に一覧がある。

## この土台の考え方

- **テストファースト**は品質のためというより、エージェントに赤 / 緑という
  曖昧さのない信号を渡すためにある。実装でテストを書き換えるのは最も重い違反
- **ハーネス**は乱数・時間・I/O を外から握って実行を完全再現できるようにする層。
  同時に「注入できない設計は不健全」という設計の指標を兼ねる
- **ループ**は 2 種類ある。実行ループ（green まで自走）と改善ループ（仕組みを良くする）を
  混ぜない

## ディレクトリ

```
docs/       フェーズごとの成果物（00_process 〜 50_review）+ _templates
src/        実装。スタック未確定のため空
tests/      unit / harness / e2e
tools/      検証・自動化スクリプト
scratch/    技術検証プロトタイプ。本番コードから import しない
.claude/    このリポジトリ専用のスキル・設定
```

## 次にやること

要件（`REQ-xxx`）の発番。

ADR-0001 / 0006 / 0007 は、[Codex レビュー](docs/50_review/issues.md)の指摘 18 件を
**未解決のまま受容して**確定させている。各 ADR 末尾の「承認時に受容したリスク」を先に読むこと。
特に ISS-10（テスト書き換え禁止 × バランス調整）は、テスト作成フェーズより前に決着が要る。
