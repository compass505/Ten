# Ten — 作業ルール

**現在のフェーズ: 2. 基本設計**

「何を作るか」は 2026-09-02 に確定した（ADR-0006 / 0007）。技術スタックも確定（ADR-0001）。
要件は **2026-09-02 に確定した**（REQ-001〜058 / NFR-001〜009）。
基本設計に入れる。**実装フェーズに入る前にコードを書かない。**

## プロセス

要件定義（調査）→ 基本設計 → 詳細設計 → テスト作成 → 実装 → 修正改善。
定義と完了条件は [docs/00_process/workflow.md](docs/00_process/workflow.md)。

先のフェーズの作業を勝手に始めない。実装フェーズに入る前にコードを書かない
（`scratch/` での技術検証プロトタイプは例外）。

## 未確定事項（勝手に決めない）

| 項目 | 状態 |
| --- | --- |
| アプリの内容（コンセプト） | **確定** → ADR-0006 Accepted（**リスク受容あり**） |
| ゲームの中身 | **確定** → ADR-0007 Accepted（**リスク受容あり**） |
| 要件（`REQ-xxx`） | **確定** → `docs/10_requirements/requirements.md`（REQ-001〜040 / NFR-001〜009） |
| 乱数の適用範囲（ISU-14） | **確定** → ADR-0008 Accepted |
| 1 プレイの時間構造（ISU-13） | **確定** → ADR-0009 Accepted（**決め手は仮説**） |
| 技術スタック | **確定**（Unity + Android） → ADR-0001 Accepted（**リスク受容あり**） |
| テストハーネスの形 | **確定** → ADR-0002 Accepted |
| 改善ループの回し方 | **確定** → ADR-0003 Accepted |
| ドキュメント体系 | **確定** → ADR-0004 Accepted |
| 役割分担（誰が検めるか） | **確定** → ADR-0005 Accepted |

これらを聞かれずに決め打ちした場合、それは仕様ではなく事故。
判断が必要になったら止めて確認する。

**ADR-0001 / 0006 / 0007 は、Codex レビューの指摘 18 件（うち 9 件が「決定を覆すべき」）を
未解決のまま受容して確定させている。**各 ADR 末尾の「承認時に受容したリスク」と
[docs/50_review/issues.md](docs/50_review/issues.md) を先に読むこと。
特に **ISS-10（ADR-0002 のテスト書き換え禁止 × ゲームバランス調整の衝突）は、
テスト作成フェーズに入る前に決着させる必要がある。**

## スキル（手癖で始めない）

該当するものがあれば必ず読んでから始める。一覧は [.claude/skills/README.md](.claude/skills/README.md)。

| 状況 | スキル |
| --- | --- |
| 何を作るか固める | `concept` |
| 調べる | `research` |
| 決定を残す / 覆す | `adr` |
| 確定させる前に叩く | `codex-critic` |
| 仕組みを改善する / 振り返る | `loop` |

ドキュメントの監査は `doc-auditor` エージェント、機械チェックは
`python3 tools/check_docs.py`。

**書く側と検める側を分ける（ADR-0005）。** 自分が書いたものを自分で承認しない。

## テストファースト（絶対ルール）

1. テストを書く前に `src/` を書かない。
2. テスト作成フェーズの終了時点で、**全テストが落ちている**のが正しい状態。
3. 実装フェーズでテストを書き換えて通すのは禁止。テストが間違っていると判断したら、
   実装で辻褄を合わせずに止めて、要件・テストケース側を直す（ADR に理由を残す）。
4. ロジックは環境に依存させない。`Math.random()` / `Date.now()` を直接呼ばず、
   乱数はシード、時間は引数で受け取る。→ 同じ入力列で同じ結果（決定論性）。
   これはハーネスが成立するための前提条件。

詳細は [docs/00_process/test_first.md](docs/00_process/test_first.md)、
理由は [docs/00_process/rationale.md](docs/00_process/rationale.md)。

## ドキュメント

- 要件・テストには ID を振る（`REQ-001` / `TC-001`）。ID の意味は後から変えない。
- 要件を足したり変えたりしたら
  [docs/40_test/traceability.md](docs/40_test/traceability.md) を更新する。
- 後戻りコストが 1 日を超える判断は `docs/10_requirements/decisions/` に ADR として残す。
- どこに何を書くかは [docs/00_process/documentation.md](docs/00_process/documentation.md)。
  規約と理由を同じファイルに混ぜない。テンプレートは `docs/_templates/`。
- 仕組み（プロセス・ハーネス・ループ）を変えたら
  [docs/50_review/loop_log.md](docs/50_review/loop_log.md) に周回記録を残す。

## 置き場所

- `scratch/` を本番コードから import しない。
- 一時ファイル・実験結果は `scratch/` へ。`src/` と `docs/` を散らかさない。
