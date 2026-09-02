---
name: adr
description: 決定を ADR として docs/10_requirements/decisions/ に起案・採番し、関連する index（decisions/README.md・open_issues.md・docs/README.md・README.md）まで更新する。既存の決定を覆すときの Superseded 処理も担当する。「ADR を書いて」「これを決定として残して」「ADR-0003 を覆したい」「この判断を記録して」といった依頼で使う。調査そのものは research スキルの担当。
model: opus
effort: high
allowed-tools: Read, Glob, Grep, Write, Edit, AskUserQuestion, Bash(grep:*), Bash(ls:*), Bash(python3 tools/check_docs.py)
---

# 決定を ADR に落とす

## 手順

### 0. そもそも ADR にすべきか判定する

判断基準は**後戻りコストが1日を超えるか**
（[documentation.md](docs/00_process/documentation.md) 4節）。

超えないなら ADR にしない。可逆で安い決定を ADR にすると、
決定ログがノイズで埋まり、本当に重要な数本が読まれなくなる。

ADR にしない場合の落とし先: 規約なら `00_process/`、
まだ決まらないなら `open_issues.md`。

### 1. 採番する

```bash
ls docs/10_requirements/decisions/ADR-*.md
```

最大番号の次。**欠番は詰めない。** 番号の意味は後から変えない。
ファイル名は `ADR-xxxx-english-slug.md`。

### 2. テンプレートから書く

[docs/_templates/adr.md](docs/_templates/adr.md) をコピーして埋める。

書くときの要点:

| セクション | 落とし穴 |
| --- | --- |
| 背景 | 「なぜ**今**決める必要があるのか」を書く。書けないなら決めるのが早い |
| 選択肢 | **3つ以上**。採らない案にも利点を書く。利点が無い案は当て馬 |
| 決定 | 決め手を**1つ**に絞る。理由を並べるほど、後から検証できなくなる |
| 結果 | **何を諦めたか**を必ず書く。ここが一番効く |
| 崩れる条件 | どうなったら見直すか。無いと見直しのタイミングが永遠に来ない |

### 3. ステータスは Proposed で出す

**自分で Accepted にしない。** 決めるのはユーザー。
起案して、要点を示して止まる。

```
- ステータス: **Proposed**（人間の承認待ち）
```

ユーザーが承認したら `**Accepted**（YYYY-MM-DD 承認）` に書き換える。

### 4. index を更新する

ADR を書いただけでは終わらない。参照される場所を全部更新する。

| ファイル | 更新内容 |
| --- | --- |
| `decisions/README.md` | 一覧表に行を追加 |
| `10_requirements/open_issues.md` | 対応する ISS を「解決 → ADR-xxxx」に |
| `10_requirements/research/README.md` | 元になった調査の「決定」欄 |
| `docs/README.md` | 影響する規約ファイルの状態 |
| `README.md`（ルート） | ステータス表 |
| 該当する `00_process/*.md` | 規約本体と「根拠」行 |

**規約本体に理由を書かない。** 理由は ADR にあり、規約からリンクする。

### 5. 検証する

```bash
python3 tools/check_docs.py
```

リンク切れ・ADR 形式・未承認の残りが出ないことを確認する。

## 既存の ADR を覆すとき

**既存ファイルを編集しない。** これが最も破られやすいルール。

1. 新しい番号で ADR を起案する
2. 「背景」に、旧 ADR の**どの前提が崩れたか**を書く
   （旧 ADR の「この決定が崩れる条件」に該当するかを確認する）
3. 旧 ADR のステータス行だけを `**Superseded by ADR-xxxx**（YYYY-MM-DD）` に書き換える。
   **本文は書き換えない**
4. index の一覧表を両方更新する

覆す理由が「気が変わった」なら、それは旧 ADR の検討が足りなかったということ。
新 ADR の背景にそう書く。取り繕うと、次に同じ間違いをする。
