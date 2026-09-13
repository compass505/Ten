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
| [ADR-0008](ADR-0008-randomness-scope.md) | 乱数を「用途別の独立ストリーム」とし、寝たふり等の判定にも使う | Accepted |
| [ADR-0009](ADR-0009-time-structure.md) | 1 プレイを「一晩を実時間 3〜5 分に圧縮した連続時間」とする | Accepted（**決め手が仮説**） |
| [ADR-0010](ADR-0010-no-numbers.md) | 状態を数値で表示せず、粗い固定段階の見た目で表す | Accepted |
| [ADR-0011](ADR-0011-per-play-randomness.md) | 判定用乱数の通番にプレイ回数を含める（盤面は日固定、運は毎回変わる） | Accepted |
| [ADR-0012](ADR-0012-balance-vs-tests.md) | テストは要件が言っていることだけを検証する（バランス値を期待値に書かない） | Accepted |
| [ADR-0013](ADR-0013-pretend-requires-closed-eyes.md) | 寝たふりは「目を閉じている間」にだけ成立させる（REQ-059 を発番） | Accepted |
| [ADR-0014](ADR-0014-closed-eyes-information.md) | REQ-006 の「知ることができる」は目を開けている間について言う | Accepted |
| [ADR-0015](ADR-0015-input-intensity-and-timing.md) | 行動の入力を「強度」と「タイミング」の 2 軸にする（REQ-060 / 061 を発番） | Accepted |
| [ADR-0016](ADR-0016-diagnosis-parameters.md) | 一晩を 7 つのパラメータで測り、カタログから近い診断を選ぶ（REQ-062 を発番） | Accepted |
| [ADR-0017](ADR-0017-parent-moves-on-failure.md) | 寝たふりに失敗しても親は赤ちゃんを動かす（盲目区間に手触りを戻す） | Accepted（**リスク受容あり**） |
| [ADR-0018](ADR-0018-parent-is-mother.md) | 親を「母」として確定し、姿を 1 つに固定する | **Proposed**（承認待ち） |
| [ADR-0019](ADR-0019-parent-model-acceptance.md) | 親モデルの合格条件を「ゲーム内の見え方」に置き、作り込みをそこで止める（SET-01 決着） | **Proposed**（承認待ち） |
| [ADR-0020](ADR-0020-test-defects-found-in-implementation.md) | 実装で判明したテストケースの欠陥 8 件を、テスト側を直して解消する | Accepted |
| [ADR-0021](ADR-0021-mother-sits-and-dozes.md) | 母は「布団の横に座ったまま、うとうとしている」姿勢で夜を過ごす | Accepted |
| [ADR-0022](ADR-0022-single-carry-motion.md) | 寝たふりの成否にかかわらず、母は同じ抱き上げを 1 本の動きで行う（ISS-20 決着） | Accepted |

**Proposed は未確定。** 人間が承認した時点で `Accepted` に書き換える。
**2026-09-12 時点で Proposed は 2 件**（ADR-0018 / 0019。どちらも親の 3D モデル制作を止めている）。

ADR-0001 / 0006 / 0007 は、[Codex レビュー](../../50_review/issues.md)の指摘 18 件
（うち 9 件が「決定を覆すべき」）を**未解決のまま**、ユーザー判断で Accepted にしている。
各 ADR 末尾の「承認時に受容したリスク」を必ず読むこと。
ADR-0017 は [ISS-20](../open_issues.md)（判別不能性を実際に作れるか）を
未解決のまま Accepted にしたが、2026-09-13 に ADR-0022 で決着した。
Accepted は書き換えない。覆すときは新規発番して旧を `Superseded by` にする。

ADR-0008〜0011 は、確定前に Codex レビュー（28 件 / 24 件）を通し、
A 判定 12 件のうち 10 件を反映してから Accepted にした。
ADR-0009 は決め手が「ユーザー判断」であり、3〜5 分という数値は**未検証の仮説**である。

ADR-0018 は ADR-0007 を覆すものではない。**ADR-0007 は親の性別を決めていない。**
覆しているのは [setting.md](../../20_basic_design/setting.md) 2 節の記述であり、
後戻りコストが 1 日を超えるため ADR に上げている。

ADR-0008 / 0009 は、ADR-0007 の確定事項のうち
**乱数の適用範囲（確定事項 5）と時間の進み方**を差し替えるために起案したもの。
ADR-0007 本文は書き換えず、差し替えは新規 ADR の側に書いてある。
