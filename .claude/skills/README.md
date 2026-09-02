# skills

このリポジトリ専用のスキル。フェーズごとの手順を、手癖で始めないように固定する。

構成: `.claude/skills/<name>/SKILL.md`（必要に応じて `references/` `assets/`）

## いま使えるもの

| スキル | フェーズ | 役割 |
| --- | --- | --- |
| [`concept`](concept/SKILL.md) | 1 入口 | ユーザーにインタビューして「何を作るか」を掘り出す |
| [`research`](research/SKILL.md) | 1 | 出典つきで調査し、材料として `research/` に残す |
| [`adr`](adr/SKILL.md) | 全般 | 決定を ADR に落とし、index まで更新する。Superseded 処理も |
| [`codex-critic`](codex-critic/SKILL.md) | 全般 | 確定前のドキュメントを Codex に読み取り専用で叩かせる |
| [`loop`](loop/SKILL.md) | 6 / フェーズ完了時 | 改善ループを1周まわして記録する |

## まだ作らないもの

スタック未確定（ADR-0001）のため、中身が placeholder だらけになる。
**ADR-0001 が確定してから作る。**

| スキル | フェーズ | 待っているもの |
| --- | --- | --- |
| `test-cases` | 4 前半 | 要件確定（TC の発番規則自体は先に書ける） |
| `test-code` | 4 後半 | テストランナー・ハーネスの実装形式 |
| `implement` | 5 | ランナーのコマンド・規約検証の grep パターン |

## 増やすときの判断

**その手順を2回以上繰り返したか？** 1回しかやらない手順はスキルにしない
（[documentation.md](docs/00_process/documentation.md)）。

スキルは必要なときだけ読み込まれるので、`CLAUDE.md` に書くより安い。
逆に「毎回必ず効くルール」は `CLAUDE.md` に置く。
