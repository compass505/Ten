# いま人間が決めること

種別: リファレンス（事実）— 判断そのものはここに書かない
更新トリガー: ADR が承認 / 差し戻しされたとき / 新しい未決が出たとき
状態: **2026-09-12 更新。**ADR-0013〜0017 は承認済み。
**承認待ちは 0 件。**フェーズ 3 完了 + フェーズ 4 のテストケース発番まで完了し、
**ここに挙がっているものだけが、次に進むための障害になっている**

> このファイルは「戻ってきたとき何を決めればいいか」を 1 枚で見るためのもの。
> 決めた結果は ADR / 要件 / 設計側に書く。**ここには残さない。**

## A. 承認が要る — **0 件**

**2026-09-13 に ADR-0018 / 0019 を確定した**（本人から全面委任「決断は全て任せる」）。
ADR-0005 原則 4「Claude が自分で Accepted にしない」の例外として、各 ADR のステータス行に委任であることを残した。
**実機で母のモデルを見て違和感が出たら、覆す ADR を起こす。**

### 承認済み（参考）

**2026-09-06 に ADR-0015 / 0016 / 0017 を承認した。**

| ADR | 決めたこと | 確定したもの |
| --- | --- | --- |
| [ADR-0015](../10_requirements/decisions/ADR-0015-input-intensity-and-timing.md) | 行動の入力を「強度」と「タイミング」の 2 軸にする | **REQ-060 / 061** / `TickInput` の形 / [MOD-Input](../30_detailed_design/MOD-Input.md) INP-01 / [MOD-Sim](../30_detailed_design/MOD-Sim.md) SIM-01・02 / TC-152〜156 |
| [ADR-0016](../10_requirements/decisions/ADR-0016-diagnosis-parameters.md) | 一晩を 7 つのパラメータで測り、カタログから近い診断を選ぶ | **REQ-062** / [diagnosis.md](../20_basic_design/diagnosis.md) のカタログ 50 件 / `NightState` に 7 値 / TC-159〜163 |
| [ADR-0017](../10_requirements/decisions/ADR-0017-parent-moves-on-failure.md) | 寝たふりに**失敗しても親が動く**（盲目区間に手触りを戻す） | [screens.md](../20_basic_design/screens.md) の **D-12 / D-05 / 遷移図** / 新状態 `ST-P-Feint` / [setting.md](../20_basic_design/setting.md) 8 節 / **TC-164（新規）** |

**ADR-0017 が未解決のまま受容した [ISS-20](../10_requirements/open_issues.md) は、
2026-09-13 に [ADR-0022](../10_requirements/decisions/ADR-0022-single-carry-motion.md) で決着した**（成否で同じ 1 本の動き）。

**残る作業のうち、承認に依存するものは無い。**
[diagnosis.md](../20_basic_design/diagnosis.md) のカタログ 50 件は
**文言を書き直す**（本人判断 2026-09-06「もう少し長文がいい」）。
決定そのもの（7 軸 + 近傍マッチ）は実測で裏が取れているので、書き直しは文言だけ。

## B. 聞けるときに聞く — 2 件（作業は止まらない）

| ID | 中身 | 止まるもの |
| --- | --- | --- |
| [ISS-11](../10_requirements/open_issues.md) | 配偶者の端末が iOS か Android か | **iOS なら REQ-021（夫婦で同じ盤面）が成立しない。**[ADR-0001](../10_requirements/decisions/ADR-0001-tech-stack.md) の前提が崩れる |
| [ISS-12](../10_requirements/open_issues.md) | 本人・配偶者が「育児をネタにしたゲーム」を遊びたいか | [ADR-0006](../10_requirements/decisions/ADR-0006-concept.md) の前提。**直接聞くと誘導になる** |

**どちらも実装を止めない**が、ISS-11 は**実機テストの前に**判明していないと手戻りが大きい。

## C. 実機で触るまで判定できない — 3 件

**紙の上で議論しても答えが出ない。**プロトタイプか実機ビルドが要る。

| ID | 中身 | 何で測るか |
| --- | --- | --- |
| [O-09](../20_basic_design/screens.md) | 盲目でいる時間（最短 7 秒）が緊張か退屈か | `scratch/mvp/` を実機で |
| [SET-04](../20_basic_design/setting.md) | 抱っこ中の視点移動が 3D 酔い（NFR-009）を起こすか | Unity ビルドで |
| [SHL-01](../30_detailed_design/MOD-Shell.md) | コールドスタート 3 秒（NFR-008）に Unity の初期化が収まるか | 実機で 5 回計測 |

## D. 環境

| | 状態 |
| --- | --- |
| **.NET SDK 8.0.424** | **導入済み**（`~/.dotnet`。Microsoft 公式 `dotnet-install.sh`、ユーザー権限）。**PATH は未設定**（下記） |
| **Unity Hub** | **導入済み**（2026-09-07。`/Applications/Unity Hub.app`） |
| **Unity 6000.0.83f1（LTS）** | **導入済み**（`/Applications/Unity/Hub/Editor/`）。版は [ADR-0001](../10_requirements/decisions/ADR-0001-tech-stack.md) に固定 |
| **Android Build Support** | **導入済み**（2026-09-07）。SDK 1.0G / NDK 2.4G / OpenJDK 235M。エディタ全体で 14G |
| **Unity のライセンス** | **認証済み**（2026-09-08。本人が実施） |
| **`unity/` プロジェクト** | **コンパイル通過**（2026-09-08）。`src/` の 2 パッケージを読み込んで `rc=0` |

**環境は揃った。人の作業は残っていない。**

### 二重ビルドが成立している（2026-09-08 実測）

| 検証 | 結果 |
| --- | --- |
| Unity が `src/` をコンパイルできる | **通る**（`rc=0`） |
| `dotnet test` が同じソースで走る | **通る**（unit 33 件 / harness 109 件） |
| `noEngineReferences` が `using UnityEngine` を弾く | **弾く**（`error CS0246`。実測で確認） |

**この 3 つが揃って、[harness.md](../40_test/harness.md) 1 節の
「`dotnet test` が通らなくなったら純粋層が環境に触り始めた証拠」が成立する。**

**純粋層のテストは Unity 不要で、.NET SDK だけで走る**
（[harness.md](../40_test/harness.md) 1 節）。フェーズ 4 の DoD
「テストコードが存在し、**全て落ちる**」は、これで確認できる。

**Unity が要るのは 3 箇所だけ。**view / shell 層のテスト（Unity Test Runner）、
e2e、C 節の実機判定（O-09 / SET-04 / SHL-01）。**実装フェーズで view 層に入るまでは不要。**

```bash
export PATH="$HOME/.dotnet:$PATH"
```

## D2. テストを書いていて出た設計の穴 — 3 件（2026-09-06）

**いずれも後戻りコストが 1 日を超えないので ADR にしない**（documentation.md 4 節）。
ただし**こちらで決め打つと仕様事故になる**ので、判断が要るものはここに置く。

| # | 穴 | 影響 |
| --- | --- | --- |
| ~~G-01~~ | ~~`ICalendar` が時刻を注入できない~~ | **解決（2026-09-06。本人承認）。**規則を `BoardDateRule` として純粋層に出し、時刻を引数で受ける形にした。端末時計を読む役だけが `ICalendar` に残る。TC-113 / 114 はコード化済み |
| ~~G-02~~ | ~~`DiagnosisEntry` の定義がどこにも無い~~ | **解決（2026-09-06）。**diagnosis.md 4 節の表から起こして [types.md](../30_detailed_design/types.md) 3 節に載せた。実体は [src/Ten.Pure/Result.cs](../../src/Ten.Pure/Result.cs) |
| ~~G-03~~ | ~~「濃さ」（うっすら / どっぷり）の置き場が無い~~ | **解決（2026-09-13）。**エントリに持たせず、`Result.StrengthOf` / `Result.Title` で名前に付ける（[MOD-Result](../30_detailed_design/MOD-Result.md)。TC-169） |

## E2. 遊べる形に繋ぐときに決めたこと — **6 件（2026-09-13 に確定。委任）**

**2026-09-13。**Unity で「ホーム → 夜 → 結果 → 共有」とチュートリアルを繋いだとき、
決めないと動かせなかったもの。**どれも後戻りは 1 日以内**なので ADR にせず、仮に決めて動かしている。
中身と理由は [presentation.md](../20_basic_design/presentation.md) 6 節。

| ID | 仮に決めたこと | 違ったら直すもの |
| --- | --- | --- |
| PRE-01 | **チュートリアルの間だけ**、押し場所の名前（泣く / ぐずる / ばたつく / 目を閉じる）を画面下に出す | `TenApp.DrawTutorial` |
| PRE-02 | 共有する文字列の先頭に `Ten 9月13日` を付ける | `TenApp.ShareText` |
| PRE-03 | 天井灯は山札の**終盤（1〜2 枚）**で点く（setting.md 7 節の「3 枚以下」と 4.3 節が食い違っていた） | `PresentRule` |
| PRE-04 | **予告中の手の形を札種で 4 通り**にする。**Codex の r57 は `Reach` を 1 本しか頼んでいない**ので追加依頼が要る | Codex への指示書 |
| PRE-05 | 結果画面は「このプレイ」、共有は「その日の最高」 | `TenApp.DrawResult` |
| PRE-06 | 中断から戻っても自動で再開せず、`つづける` を押すまで止めておく | `TenApp.DrawNight` |

## E. こちらで決めてよいもの（決定待ちではない）

各詳細設計ファイルの「決めていないこと」（`RNG-01` / `BRD-01` / `STO-01` 等）は
**実装しながら決める技術的な選択**で、後戻りコストが 1 日を超えない。
**判断が要るものが出たらここに繰り上げる。**

## 決まっているもの（確認不要）

| | |
| --- | --- |
| コンセプト / ゲームの中身 | ADR-0006 / 0007（**リスク受容あり**） |
| 要件 REQ-001〜058 / NFR-001〜009 | 2026-09-02 確定 |
| 要件 REQ-059〜062 | 2026-09-06 確定（ADR-0013 / 0015 / 0016）。**暫定 0 件** |
| 技術スタック | ADR-0001（Unity + Android） |
| 乱数 / 時間 / 状態の見せ方 / プレイごとの運 | ADR-0008 / 0009 / 0010 / 0011 |
| テストとバランス調整の衝突（ISS-10） | ADR-0012 |
| 基本設計 6 本 | architecture / screens / balance / data_model / setting / diagnosis |
| 詳細設計 17 モジュール + 型（MOD-Present は 2026-09-13 追加） | [30_detailed_design/](../30_detailed_design/) |
| **寝たふりは閉眼中にだけ成立する** | ADR-0013 Accepted（2026-09-06）。REQ-059 確定 |
| **閉眼中は窓の明るさ以外分からない** | ADR-0014 Accepted（2026-09-06）。ISS-18 決着 |
| **入力は強度 × タイミングの 2 軸** | ADR-0015 Accepted（2026-09-06）。REQ-060 / 061 確定 |
| **診断は 7 軸の近傍マッチ** | ADR-0016 Accepted（2026-09-06）。REQ-062 確定 |
| **寝たふり失敗時も親が動く** | ADR-0017 Accepted（2026-09-06）。ISS-20 は ADR-0022 で決着 |
| **母は座ったまま、うとうと** | ADR-0021 Accepted（2026-09-13） |
| **抱き上げは成否で同じ 1 本（ISS-20）** | ADR-0022 Accepted（2026-09-13） |
| ハーネスの実装仕様（ISS-06） | [40_test/harness.md](../40_test/harness.md) |
| テストケース TC-001〜165 | [40_test/cases/](../40_test/cases/)。**全 Must 要件に対応済み（未割当 0 件）** |
| 空実装の線引き（フェーズ 4 で `src/` に何を置けるか） | [test_first.md](test_first.md) 5.1（2026-09-06）。**本体は `throw` の 1 行だけ** |
| `Rng.Hash` を internal にしてテストから見せる | [MOD-Rng](../30_detailed_design/MOD-Rng.md)（2026-09-06）。参照ベクタの hash 節 140 件を検証するため |
