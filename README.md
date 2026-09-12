# Ten

**現在のフェーズ: 4. テスト作成（ケース発番済み / コードはこれから）**

> **戻ってきたら [docs/00_process/handoff.md](docs/00_process/handoff.md) を先に読む。**
> **ADR の承認待ちは 2 件**（ADR-0018 / 0019。親の 3D モデル制作が止まっている）。
> **.NET SDK 8.0.424 導入済み**（`~/.dotnet`）。残りはテストコードを書くことだけ。

何を作るかは **ADR-0006 / 0007 で確定**（2026-09-02）。技術スタックは **ADR-0001**。
要件は **2026-09-02 に確定した**（REQ-001〜058 / NFR-001〜009）。
**REQ-059〜062 も 2026-09-06 に確定した。**
Codex レビューを 2 周通し、A 判定 12 件のうち 10 件を反映済み。
**どんなゲームかは [docs/10_requirements/game_overview.md](docs/10_requirements/game_overview.md)。**
ドキュメントの地図は [docs/README.md](docs/README.md)。
進め方の理由は [docs/00_process/rationale.md](docs/00_process/rationale.md)。

| 項目 | 状態 |
| --- | --- |
| コンセプト | **確定** → [ADR-0006](docs/10_requirements/decisions/ADR-0006-concept.md) |
| ゲームの中身 | **確定** → [ADR-0007](docs/10_requirements/decisions/ADR-0007-game-design.md) |
| 要件 | **確定**（2026-09-02） → [requirements.md](docs/10_requirements/requirements.md) |
| 乱数の適用範囲 | **確定**（用途別ストリーム） → [ADR-0008](docs/10_requirements/decisions/ADR-0008-randomness-scope.md) |
| 1 プレイの時間構造 | **確定**（決め手は仮説） → [ADR-0009](docs/10_requirements/decisions/ADR-0009-time-structure.md) |
| 技術スタック | **確定**（Unity + Android） → [ADR-0001](docs/10_requirements/decisions/ADR-0001-tech-stack.md) |
| テストファースト / ハーネス | **確定** → [ADR-0002](docs/10_requirements/decisions/ADR-0002-test-harness.md) |
| 改善ループ | **確定** → [ADR-0003](docs/10_requirements/decisions/ADR-0003-improvement-loop.md) |
| ドキュメント体系 | **確定** → [ADR-0004](docs/10_requirements/decisions/ADR-0004-documentation-policy.md) |
| 状態の見せ方 | **確定** → [ADR-0010](docs/10_requirements/decisions/ADR-0010-no-numbers.md) |
| テストとバランス調整の衝突 | **確定** → [ADR-0012](docs/10_requirements/decisions/ADR-0012-balance-vs-tests.md) |
| 寝たふりの成立条件 | **確定** → [ADR-0013](docs/10_requirements/decisions/ADR-0013-pretend-requires-closed-eyes.md) |
| 閉眼中に何が分かるか | **確定** → [ADR-0014](docs/10_requirements/decisions/ADR-0014-closed-eyes-information.md) |
| 入力の強度とタイミング | **確定** → [ADR-0015](docs/10_requirements/decisions/ADR-0015-input-intensity-and-timing.md) |
| 一晩の診断（7 軸の近傍マッチ） | **確定** → [ADR-0016](docs/10_requirements/decisions/ADR-0016-diagnosis-parameters.md) |
| 寝たふり失敗時に親が動くか | **確定**（**リスク受容あり**） → [ADR-0017](docs/10_requirements/decisions/ADR-0017-parent-moves-on-failure.md) |
| 親の性別と姿 | **承認待ち**（母に確定する案） → [ADR-0018](docs/10_requirements/decisions/ADR-0018-parent-is-mother.md) |
| 親モデルの合格条件・作り込み度 | **承認待ち**（ゲーム内の見え方で判定する案） → [ADR-0019](docs/10_requirements/decisions/ADR-0019-parent-model-acceptance.md) |

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

**フェーズ 2 / 3 は完了。**基本設計 6 本と詳細設計 16 モジュールが揃い、
全 Must 要件が割り当て済みで、画面と状態遷移は閉じている（行き止まり 0 件）。
**フェーズ 4 は TC-001〜164 の発番まで終わっている。**

**残っているのは 2 つ。**

1. **テストコードを書く。**フェーズ 4 の DoD は「テストコードが存在し、**全て落ちる**」。
   .NET SDK は導入済みなので、`export PATH="$HOME/.dotnet:$PATH"` で `dotnet test` が走る。
   純粋層は Unity 不要（[harness.md](docs/40_test/harness.md) 1 節）
2. **`scratch/mvp/` を実機で触る**（screens.md O-09）。
   **盲目でいる 7 秒が苦痛かどうかは、触るまで判定できない**

ADR-0001 / 0006 / 0007 は、[Codex レビュー](docs/50_review/issues.md)の指摘 18 件を
**未解決のまま受容して**確定させている。各 ADR 末尾の「承認時に受容したリスク」を先に読むこと。
ISS-10（テスト書き換え禁止 × バランス調整）は ADR-0012 で決着した。
**ADR-0017 も ISS-20（判別不能性を実際に作れるか）を未解決のまま受容している。**
