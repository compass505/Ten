# 引き継ぎ（セッションをまたぐ文脈）

種別: リファレンス（事実）— 判断そのものは ADR / 設計側に置く
更新トリガー: フェーズが進んだとき / 会話でだけ決まったことが出たとき（**その場で書く**） /
測って分かった事実が出たとき
状態: **2026-09-13 更新。**フェーズ 6。**アプリとして一通り遊べる形に繋いだ**（見た目は仮。絵は Codex 待ち）。
**ADR の承認待ちは 0 件**（ADR-0018 / 0019 / 0023 と PRE-01〜06 を、本人の全面委任で確定）。**支配戦略を崩し、初期視線を直した**

> **これは何か。**エージェントは会話履歴を持たない
> （[documentation.md](documentation.md) 1 節）。**このプロジェクトで一番失われやすいのは、
> 会話の中でだけ決まって、まだどのファイルにも落ちていないこと。**ここはそれを拾う場所。
>
> **ファイル一覧はここに書かない。**それは [docs/README.md](../README.md)。
> **決めることの一覧もここに書かない。**それは [decisions_pending.md](decisions_pending.md)。

## 1. 最初に読む 4 本

| 順 | 何のために |
| --- | --- |
| [CLAUDE.md](../../CLAUDE.md) | 手癖で始めないため。**実装フェーズ前にコードを書かない**が最重要 |
| [game_overview.md](../10_requirements/game_overview.md) | 何を作っているか |
| [decisions_pending.md](decisions_pending.md) | いま止まっているものは何か |
| **このファイルの 3 節** | **どこにも書かれていない決定が無いか** |

## 2. 現在地

**フェーズ 5（実装）が終わった**（2026-09-12）。**2026-09-13 に、絵以外をゲームとして繋いだ。**

| 繋いだもの（2026-09-13） | 実体 |
| --- | --- |
| 画面の流れ（起動 → チュートリアル / ホーム → 夜 → 結果 → ホーム、復旧、中断、戻る操作） | `unity/Assets/Scripts/TenApp.cs` |
| 保存と再開（フォーカス喪失で保存、続きから / やり直す、最高成績、日付の切り替え） | 同上 + `FileStorage`（**入れ子の値が書き戻せない欠陥を直した**。TC-176 / 177） |
| 共有（Android の共有シート / それ以外はコピー）と画面消灯の抑止 | `DeviceServices.cs` |
| 一晩の進行（**tick の無いフレームで接触を取りこぼしていた**のを直した） | `NightSession.cs`（旧 `NightDriver.cs` を置き換え） |
| 状態 → 見せ方（母の姿勢・手・小物・電気・窓・出来事・視界の揺れと沈み） | `src/Ten.Boundary/Present.cs`（[MOD-Present](../30_detailed_design/MOD-Present.md)）+ `RoomRig.cs` の仮の姿 |
| 実況の素材と診断の濃さ | `src/Ten.Pure/Commentary.cs` / `Result.StrengthOf`・`Title` |
| アセットの差し込み口・画面の文言・チュートリアルの流れ | [presentation.md](../20_basic_design/presentation.md) |

残っているのは**実機で測る 5 件**と、**Codex の絵を差し込むこと**（presentation.md 2 節の手順）。
**課題の台帳は [issues.md](../50_review/issues.md) の「2026-09-13 フェーズ 6」**（ISS-21〜31。対応済み / 見送りの判定つき）。

| フェーズ | 実体 |
| --- | --- |
| 0 仕組み | 完了 |
| 1 要件定義 | 完了。REQ-001〜058 / NFR-001〜009 が 2026-09-02 確定。**REQ-059〜062 も 2026-09-06 確定。暫定 0 件** |
| 2 基本設計 | **完了。**architecture / screens / balance / data_model / setting / diagnosis の 6 本 |
| 3 詳細設計 | **完了。**16 モジュール + types.md。公開 IF 確定（2026-09-13 に MOD-Present を追加） |
| 4 テスト作成 | **完了。**TC-001〜165 を発番し、全件をコード化した |
| 5 実装 | **完了。**純粋層 / 境界層 / 表示層 / Android ビルド |
| 6 修正改善 | **1 周目済み**（2026-09-13。REQ-056・初期視線・stripping）。**ここからは実機で遊んで測る** |

### いま緑になっているもの

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test tests/unit/Ten.Tests.Unit.csproj        # 44 / 44
dotnet test tests/harness/Ten.Tests.Harness.csproj  # 180 / 180
```

| | 結果 |
| --- | --- |
| `tests/unit` | **44 green / 0 赤**（2026-09-13 に TC-166〜169 を追加） |
| `tests/harness` | **180 green / 0 赤**（2026-09-13 に TC-170〜179 を追加、TC-069 に方針 2 つ） |
| `unity/Assets/Tests`（PlayMode） | **11 green / 5 赤** |

**PlayMode の 5 赤は Android 実機でしか測れない**（TC-145 / 146 / 147 / 150 / 151）。
`Application.isEditor` を見て、エディタでは**わざと落とす**ようにテスト側が書かれている
（「黙って通すと『測っていないのに緑』になる」）。**実機に挿すまで赤のままが正しい。**

### 動かし方

```bash
# 純粋層・境界層（Unity 不要。秒で回る）
export PATH="$HOME/.dotnet:$PATH"
dotnet test tests/unit/Ten.Tests.Unit.csproj
dotnet test tests/harness/Ten.Tests.Harness.csproj

# Unity PlayMode
"/Applications/Unity/Hub/Editor/6000.0.83f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -runTests -testPlatform PlayMode \
  -projectPath unity -testResults /tmp/playmode.xml -logFile -

# Android ビルド（TC-115 / 119 / 147 の前提。**成果物を見ないと通らない**）
"/Applications/Unity/Hub/Editor/6000.0.83f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -projectPath unity -buildTarget Android \
  -executeMethod Ten.Editor.TenBuild.Android -logFile -
```

- **Unity のライセンスは有効**（2026-09-12 に batchmode 起動を確認）。Android モジュール・SDK・NDK も入っている
- **.NET SDK 8.0.424 は `~/.dotnet`。**PATH をシェルの設定に入れていないので毎回 export が要る

### 実装で判明したこと（次の人が最初に踏む）

| | |
| --- | --- |
| **テストケースの欠陥 8 件** | 実装をどう書いても通らないものがあった。直した理由は [ADR-0020](../10_requirements/decisions/ADR-0020-test-defects-found-in-implementation.md)。**自分で書いて自分で直したので、別の目で検めるまでリスクを持つ**（ADR-0005） |
| **バランス値が動いた** | 寝入りばなを `ST-P-Grace` に統合、`doze_off` を弱く遅く、対処の代償を 4 札すべてに。[balance.md](../20_basic_design/balance.md) 15 節 |
| **IL2CPP は「1 つの長い式」で落ちる** | `Tuning`（97 項目）の自動生成 `GetHashCode` と、カタログ 50 件の配列初期化子が、C++ の入れ子 256 段を超えて **Android ビルドを止めた。**項目の多い `record struct` は等価比較とハッシュを手で書く |
| **得点が 0〜1 に縮んで見えたのは、測った方針のせい**（2026-09-13 に測った） | 起きている親に撃ち続ける方針しか無かった。待つ方針を足すと**寝入りばな狙いが 48/48 で独走**していた（REQ-056 違反）。規則 + 5 値で崩した（最大単独勝率 33%）。[balance.md](../20_basic_design/balance.md) 16 節 / [ADR-0023](../10_requirements/decisions/ADR-0023-strategy-test-missing-policies.md) |
| **`FileStorage` が進行中のプレイを読み戻せなかった**（2026-09-13） | `NightState` の入れ子（山札・慣れ・診断）を `ToString()` で書いていた。**TC-108 は `MemoryStorage` なので気づけなかった。**最高成績も保存していなかった。主コンストラクタの引数順で書くように直した（TC-176 / 177） |
| **tick の無いフレームで接触が消えていた**（2026-09-13） | 60fps に対して 20 tick/秒なので、**3 回に 2 回のタップが無視されていた。**押す / 離すを 1 tick に 1 件ずつ流す列に溜める（`NightSession`） |
| **夜の初期視線が母の顔になっていた**（2026-09-13 に**直した**） | 首の向きが指の絶対位置で決まっていた。**相対ドラッグ + 注入した初期視線**にした（MOD-Input IN-8 / TC-178 / 179）。**Mac ビルドで目視はまだ** |
| **`FileStorage` は反射で保存する**（2026-09-13） | IL2CPP のコード削除で主コンストラクタが消えうるので、`unity/Assets/link.xml` で 2 アセンブリを丸ごと残した。**実機で「続きから」を 1 度確かめる** |
| **`scratch/` から `src/*.csproj` を参照してビルドすると Unity が落ちる**（2026-09-13） | `src/Ten.Pure/obj/Release/` に `AssemblyInfo.cs` ができ、Unity が `src/` をパッケージとして読むとき二重定義になる。**`scratch/` はソースを直接コンパイルする**（`scratch/balance/Probe.csproj`） |

## 3. 会話でだけ決まっていること（**最重要**）

**ここが空になっているのが正しい状態。**残っているものは、書き落としである。

| # | 決まったこと | 誰が | 現状 |
| --- | --- | --- | --- |
| H-02 | **診断の文言はもう少し長くする**（一行では短い） | 本人（2026-09-06） | [diagnosis.md](../20_basic_design/diagnosis.md) 3 節に反映済み。**50 件は 2026-09-12 に 2 文ずつで書き下ろし済み**（diagnosis.md 4 節）。3 節の「2〜4 文」の上側に寄せるかは、実機で結果画面を見てから |
| H-03 | **モデル（3D モデル・アセット）の作成は Codex 側で行う。**このリポジトリの作業範囲に含めない | 本人（2026-09-06） | **ここにしか書かれていない。**Unity が要るのは view / shell 層のテスト・e2e・実機判定で、**アセット制作はその前段として Codex が持つ**。[ADR-0005](../10_requirements/decisions/ADR-0005-agent-roles.md)（役割分担）に反映するかは未定 |
| H-04 | **親の姿と、モデルの合格条件を決めた** | 本人（2026-09-12） | **ADR に落とした**（[ADR-0018](../10_requirements/decisions/ADR-0018-parent-is-mother.md) / [ADR-0019](../10_requirements/decisions/ADR-0019-parent-model-acceptance.md)）。**2026-09-13 に Accepted（委任）。3 節から消してよい。**採用したモデルシートは `scratch/visual/parent/parent-model-sheet-v4-e2-long-cute-30s.png` と `parent-hand-sheet-v2.png`。Codex への指示書は `scratch/visual/parent/ASTRA-PROMPT-r56.md` |
| H-05 | **実在の人物（俳優）に顔を寄せる案は採らない** | 2026-09-12 | 肖像の問題と、[setting.md](../20_basic_design/setting.md) 2 節の制約の両方。**ADR-0018 の確定内容 4 として残した**（性別の確定とは独立に維持する） |
| H-06 | **ゲームに要る見た目の制作物を 4 包み（母 / 寝室と小物 / 父 / アプリの外側）に分けて Codex の Astra に作らせる。**作り込みは「必要最低限」。生活感の小物は置かない（SET-02）、父は表情なしのシルエット（SET-03）。**母は本人が制作中。父は時間的に間に合うか分からないので後回し**（指示があるまで着手しない） | 本人（2026-09-13） | 指示書は `scratch/visual/ASTRA-COMMON.md` と各包みの `ASTRA-PROMPT-*.md`。SET-02 / 03 は setting.md 10 節に反映済み。**PRE-04（予告の手 4 形）の追加依頼は `scratch/visual/ASTRA-PROMPT-hands-r58.md`** |
| H-09 | **母は `scratch/visual/parent/r56-approved` で A〜D 合格（Blender 上）。**Codex 側で本人が承認した条件変更: 顔の基準位置 **(−0.30, 0.80, +0.40) m**（setting.md 3 節は 1.15 m / Z +0.15）、**垂直 FOV 70°**、オムツ替え終盤だけ布団へ向くカメラ演出。`ASTRA-PROMPT-r57.md` は「残りを足す依頼」に書き直した（Carry・予告の手 4 形・TurnAway・Sniff・ループ整形。hands-r58 を統合） | 本人（Codex 上で承認。2026-09-13）/ Claude（2026-09-14 に指示書へ反映） | **setting.md 3 節・presentation.md 2.1 のクリップ名・RoomRig の画角（水平 30°）と未整合。**Unity への取り込み時に直す。**2026-09-14: room-r1 / ui-r1 が届いた。ui は取り込み済み（presentation.md 2.3）。**母と寝室は**取り込みを止めている** → H-10 |
| H-10 | **実寸で置くと、母の顔と手が同時に視界へ入る**（2026-09-14 に測った）。**2026-09-14 に母と寝室を実寸で取り込んだ**（presentation.md 2.0）。**PlayMode の TC-122 は緑だが、batchmode の画面が縦長でないため顔が視界から外れているだけ。**縦画面（`TenShots` のログ）では yaw 20〜55° で顔と手が同時に入る | Claude（委任で決着） | **2026-09-14 に [ADR-0024](../10_requirements/decisions/ADR-0024-window-vs-mother-exclusive.md) で決着**（窓 ⇔ 母を排他、顔と手の同時表示は許す。TC-122 は縦画面で測る形に直して緑）。**本人が戻ったら見せる。**以下は経緯: Blender で r56-approved の頭・右手の範囲を実測し、Unity の視錐台で首を振った: **yaw 21°〜55° で顔と手が同時に入る**（窓と母は重ならない）。**e2e の TC-122 は「1 つまで」を要求しているので赤になる。**setting.md 3 節の表も顔 −1〜39° / 手 29〜55° で元から重なっている。D-11 の文言は「3 つは同時に入らない」。決め方の候補: (a) 「窓 ⇔ 母」の排他だけを要件とし TC-122 を ADR で直す / (b) Codex に手の静止位置を顔から離させる / (c) 画角を狭める。計測は `scratch/view-geometry/`（`probe_mother.py` を Blender で `room-r1.blend` に当て、`frustum.py` で首を振る）で再現できる |
| H-07 | **寸法の正は setting.md。**`RoomRig.cs` の抽象配置（対象を 4 m 先に ±45°）と「ベビーベッドの柵」（setting.md 2 節は床の布団）は、アセット取り込み時に直す。画角は Unity 実装の**水平 30°**。床は**フローリング**。（母の姿勢は ADR-0021、抱き上げは ADR-0022 に落とした） | Claude（本人から委任。2026-09-13） | **取り込み時の注意:** PlayMode の TC-122（視界の判定）は差し替えで結果が変わりうる。**テストを書き換えず**、視界判定用の代理物を残す方針 |

| H-08 | **「残りのタスクを洗い出して全て終わらせて、決断は全て任せる」**（ADR・PRE・INP-02 などの判断を Claude に全面委任） | 本人（2026-09-13） | 委任で確定したものは各ステータス行に「委任」と書いた。**ADR-0005 原則 4 の例外。**次に本人が戻ったら、[issues.md](../50_review/issues.md) の ISS-31 を見せる |

## 4. 測って初めて分かったこと

**`scratch/mvp/` の実測（24 シード × 複数方針）で、紙の上の設計が壊れていた箇所。**
理由と数値は [balance.md](../20_basic_design/balance.md) 11〜14 節。ここは索引。

| 何が壊れていたか | どう直したか |
| --- | --- |
| **覚醒度が 100 に到達しない**（上昇量が覚醒度に比例して減るため） | 上昇量に `max(0.25, …)` の床を入れた。**入れないと REQ-011 が達成不能** |
| **親が夜の 96%（5163/5400 tick）を `ST-P-Up` で過ごす** | 覚醒度の戻りと `ST-P-Up` の長さを調整 |
| **対処を引き出すのが純粋な得** だった（REQ-013 の代償が未実装） | 4 つの対処すべてに封じ・減衰・覚醒度減を付けた |
| **寝たふりが一度も使う価値を持たない**（16/16 で死ぬ） | 成功率に下限を入れ、**とどめ**の位置づけにした |
| **長押しだけでは最弱**（連打 3.42 に対して 2.88） | 長押し × 寝入りばなの合わせ技が最強（4.58）。**ADR-0015 が成立する根拠はこれ** |
| **最大軸だけでは診断が潰れる**（7 方針中 3 つが「いらだち」最大） | ベクトルの**形**で近傍マッチする。**ADR-0016 の決め手はこれ** |
| **猶予 1 秒 に対して最大まで溜めるのに 3.5 秒** かかり、背中スイッチが最弱でしか撃てない | **背中スイッチは溜めない**（押した時点で最大強度）。ADR-0015 の「結果」に追記済み |

## 5. 踏んだ罠

**同じ罠に二度はまらないための記録。**手順ではなく、疑い方。

| 罠 | 症状 | 次にどうするか |
| --- | --- | --- |
| **パッチが黙って当たらない**（`str.replace` が一致しない） | 3 回の計測で赤ちゃんの行動が覚醒度を **0** しか上げていなかった。パラメータを 2 倍にしても結果が**まったく変わらない**ことで気づいた | **数値が動かないときは、まず計測系を疑う。**書き換え前に対象の実テキストを読む。未一致は警告を出す |
| **暗転が半透明だった**（`opacity .985`） | 目を閉じても親の顔が透けて見え、REQ-004 と「賭け」が丸ごと壊れていた | 「見えない」は**実際に見えないこと**を目視で確かめる |
| **コーチ文が親の状態を漏らしていた** | 閉眼中に「まだ運ばれている…」と表示 = 寝たふり成功の通知。REQ-016 違反 | 閉眼中に表示するものは**親の状態から独立**していなければならない |
| **用語が二重に使われていた**（「窓」= 寝室の窓 / タイミングの猶予） | 本人に「まどってなに？？」と聞かれるまで気づかなかった | 猶予は「**寝入りばな**」と書く。[setting.md](../20_basic_design/setting.md) 冒頭に注記 |
| **`head -N` で切った** | ファイルが伸びた後の行数で切り、Android バックキーの表が消えた | 行数で切らない |
| **バックグラウンドに回さず Codex を起動した** | プロセスが消え、状態が 29 分間 `running` のまま | `--background` を付ける |

## 6. 崩してはいけない前提

**これを崩すと広範囲が壊れる。**理由は各出典に。

| 前提 | 崩れると |
| --- | --- |
| **純粋層が `UnityEngine` を参照しない** | `dotnet test` が通らなくなる。[harness.md](../40_test/harness.md) 1 節が言うとおり、**それ自体が独立性のテスト**になっている |
| **状態に浮動小数点を持たない**（int と 1/1000 固定小数） | 端末間で結果がずれる（NFR-004）。[types.md](../30_detailed_design/types.md) 0 節 |
| **状態はすべて不変**（`readonly record struct`） | 決定論（D1〜D4）の前提が消える |
| **`MOD-Sim` の公開 IF は `Begin` / `Advance` の 2 つだけ** | `Sim → Score → NightEnd` の順序を呼び出し側が守る設計（REQ-051）。**型で強制できない唯一の場所**なので TC-142 / TC-080 で見張っている |
| **`src/` に判断を含むコードを書かない**（フェーズ 4 の間） | 空実装の線引きが崩れ、なし崩しで実装が始まる。[test_first.md](test_first.md) 5.1。**本体は `throw` の 1 行だけ** |
| **`ST-P-Settle`（成功）と `ST-P-Feint`（失敗）が観測上まったく同じ** | 動きの有無・長さ・山札の減り・`doze_off` の停止のどれか 1 つでも食い違えば、**そこから寝たふりの成否が逆算できる**（REQ-016 違反）。[ADR-0017](../10_requirements/decisions/ADR-0017-parent-moves-on-failure.md)。TC-164 で見張る |
| **バランス値をテストの期待値に書かない** | [ADR-0012](../10_requirements/decisions/ADR-0012-balance-vs-tests.md)。調整のたびにテストが落ちる |
| **乱数は用途ごとに独立**（`Hash(seed, purpose, ordinal)`） | [ADR-0008](../10_requirements/decisions/ADR-0008-randomness-scope.md)。**用途を後から足すのは安全**（既存の値が動かない） |
| **`scratch/` を `src/` から import しない** | `CLAUDE.md`。プロトタイプは捨てるもの |

## 7. 次の一手（依存順）

**2026-09-06 に、この図の上半分（承認と SDK）は片付いた。**

```
[済] 承認 3 件（ADR-0015 / 0016 / 0017）──▶ REQ-060/061/062 確定 ──▶ TC-152〜164 確定
[済] .NET SDK 8.0.424 を ~/.dotnet に導入
                              │
                              ▼
テストプロジェクトの雛形を作る（tests/unit・tests/harness の .csproj）
                              │
                              ▼
純粋層のテストを書く（TC-001〜007 の乱数から）──▶ dotnet test で全部落ちるのを確認
                              │
                     （フェーズ 4 DoD 達成）
                              ▼
                           実装へ
```

**並行して進められること（テストコードに依存しない）。**

- 診断カタログ 50 件の**文言を書き直す**（長文化。H-02）
- 実機で確かめるしかない 3 件（O-09 / SET-04 / SHL-01）は
  [decisions_pending.md](decisions_pending.md) C 節。
  （ISS-20 は 2026-09-13 に ADR-0022 で決着した）
- 聞けるときに聞く 2 件（ISS-11 の端末 / ISS-12）

## 8. この文書の使い方

**新しいセッションを始めるとき**: 1 節の 4 本を読む。3 節が空でなければ、そこから拾う。

**セッションを終えるとき**: **会話でだけ決まったことを 3 節に書く。**
そのまま終わると、次のセッションはそれを知らない。
ファイルに落とせたなら、3 節からは消す（**二重管理にしない**）。
