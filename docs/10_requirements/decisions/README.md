# ADR（決定記録）

後戻りコストの大きい判断を残す。「なぜそう決めたか」は 3 週間で忘れる。

運用規約は [../../00_process/documentation.md](../../00_process/documentation.md) の 4 節。
テンプレートは [../../_templates/adr.md](../../_templates/adr.md)。

- 連番: `ADR-xxxx-短い英語スラッグ.md`
- 書くのは**後戻りコストが 1 日を超える決定**だけ
- 確定した ADR は書き換えない。覆すときは新規発番し、旧を `Superseded by` にする

| ID | 決定 | ステータス |
| --- | --- | --- |
| [ADR-0001](ADR-0001-tech-stack.md) | Unity + Android を採用し、物理エンジンを使わない | Accepted（**リスク受容あり**） |
| [ADR-0002](ADR-0002-test-harness.md) | テストハーネスをシード注入型の決定論的ハーネスとする | Accepted |
| [ADR-0003](ADR-0003-improvement-loop.md) | ループを実行ループと改善ループに分離する | Accepted |
| [ADR-0004](ADR-0004-documentation-policy.md) | ドキュメントを規約 / 説明 / 決定の 3 系統に限定する | Accepted |
| [ADR-0005](ADR-0005-agent-roles.md) | 書く側と検める側を分け、検める側に別モデルファミリを使う | Accepted |
| [ADR-0006](ADR-0006-concept.md) | コンセプトを「困りごとを解くアプリ」ではなく「夫婦で笑う贈り物」とする | Accepted（**リスク受容あり**） |
| [ADR-0007](ADR-0007-game-design.md) | 一人称視点の「動けない赤ちゃん」で親を起こす、日替わりシードのゲームとする | Accepted（**リスク受容あり**） |

**Proposed は未確定。** 人間が承認した時点で `Accepted` に書き換える。

ADR-0001 / 0006 / 0007 は、[Codex レビュー](../../50_review/issues.md)の指摘 18 件
（うち 9 件が「決定を覆すべき」）を**未解決のまま**、ユーザー判断で Accepted にしている。
各 ADR 末尾の「承認時に受容したリスク」を必ず読むこと。
Accepted は書き換えない。覆すときは新規発番して旧を `Superseded by` にする。
