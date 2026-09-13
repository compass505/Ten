# トレーサビリティ

要件がテストで担保されているかを一覧で見る表。要件を足したら必ずここを更新する。

> 要件は **2026-09-02 に確定した**（REQ-001〜058 / NFR-001〜009）。
> **REQ-059〜062 も確定**（2026-09-06。ADR-0013 / 0015 / 0016 承認）。**暫定の要件は 0 件。**
> **2026-09-05: TC を発番した**（TC-001〜163）。
> **2026-09-06: ADR-0017 の承認により TC-164 を追加**（判別不能性）。ケースは [cases/](cases/) にある。
> **2026-09-13: TC-166〜177 を追加**（遊べる形に繋いだときに切り出した規則。[cases/TC-phase6.md](cases/TC-phase6.md)）。
> **下の表の「テストコード」列は 2026-09-05 のまま更新していない。**実装が終わった現在、TC-001〜177 のうち
> dotnet で走るものはすべてコード化済み（`dotnet test` 222 件）。Unity と実機のものは TC-view-shell.md。
> **テストコードはまだ無い。**フェーズ 4 の DoD「全て落ちる」はコードを書いてから。

非機能要件の対応:

| NFR | TC |
| --- | --- |
| NFR-001 | TC-145 |
| NFR-002 | TC-115 / 146 |
| NFR-003 | TC-119 / 147 |
| NFR-004 | TC-005 / 006 / 021 / 148 / **165** |
| NFR-005 | TC-022 / 098 |
| NFR-006 | TC-063 / 149 |
| NFR-007 | TC-128 |
| NFR-008 | TC-150 |
| NFR-009 | TC-151 |

| REQ | 優先度 | TC | テストコード | 状態 |
| --- | --- | --- | --- | --- |
| [REQ-001](../10_requirements/requirements.md) | Must | TC-120 | 未作成 | 起票済み |
| [REQ-002](../10_requirements/requirements.md) | Must | TC-105 / 120 | 未作成 | 起票済み |
| [REQ-003](../10_requirements/requirements.md) | Must | TC-023 / 025 | 未作成 | 起票済み |
| [REQ-004](../10_requirements/requirements.md) | Must | TC-093 / 123 / 124 / **170** | 未作成 | 起票済み。TC-170 は閉眼中の見せ方（MOD-Present） |
| [REQ-005](../10_requirements/requirements.md) | Must | TC-104 / 107 / 129 | 未作成 | 起票済み |
| [REQ-006](../10_requirements/requirements.md) | Must | TC-096 / 121 / 122 / **173** | 未作成 | 起票済み |
| [REQ-007](../10_requirements/requirements.md) | Must | TC-063 / 077 | 未作成 | 起票済み |
| [REQ-008](../10_requirements/requirements.md) | Must | TC-092 / 093 | 未作成 | 起票済み |
| [REQ-009](../10_requirements/requirements.md) | Must | TC-099 / 118 / 137 | 未作成 | 起票済み |
| [REQ-010](../10_requirements/requirements.md) | Must | TC-108 / 138 / **176** | 未作成 | 起票済み。TC-176 は実ファイルでの往復 |
| [REQ-011](../10_requirements/requirements.md) | Must | TC-030 | 未作成 | 起票済み |
| [REQ-012](../10_requirements/requirements.md) | Must | TC-031 / 032 | 未作成 | 起票済み |
| [REQ-013](../10_requirements/requirements.md) | Must | TC-045 / **173** | 未作成 | 起票済み |
| [REQ-014](../10_requirements/requirements.md) | Must | TC-028 / 040 / 041 / 042 | 未作成 | 起票済み |
| [REQ-015](../10_requirements/requirements.md) | Must | TC-050 / 051 / 057 / 058 | 未作成 | 起票済み |
| [REQ-016](../10_requirements/requirements.md) | Must | TC-052 / 125 / **164** / **170** / **171** | 未作成 | 起票済み。TC-164 は ADR-0017 の判別不能性。TC-170 / 171 は見せ方の側（ADR-0022） |
| [REQ-017](../10_requirements/requirements.md) | Must | TC-053 | 未作成 | 起票済み |
| [REQ-018](../10_requirements/requirements.md) | Must | TC-054 | 未作成 | 起票済み |
| [REQ-019](../10_requirements/requirements.md) | Must | TC-001 / 010 / 113 | 未作成 | 起票済み |
| [REQ-020](../10_requirements/requirements.md) | Must | TC-021 / 089 / 108 | 未作成 | 起票済み |
| [REQ-021](../10_requirements/requirements.md) | Must | TC-010 | 未作成 | 起票済み |
| [REQ-022](../10_requirements/requirements.md) | Must | TC-158 | 未作成 | 起票済み |
| [REQ-023](../10_requirements/requirements.md) | Must | TC-072 / 073 | 未作成 | 起票済み |
| [REQ-024](../10_requirements/requirements.md) | Must | TC-157 | 未作成 | 起票済み |
| [REQ-025](../10_requirements/requirements.md) | Must | TC-074 / 075 / **177** | 未作成 | 起票済み |
| [REQ-026](../10_requirements/requirements.md) | Must | TC-084 / **166** / **167** | 未作成 | 起票済み。TC-166 / 167 は実況の素材の積み方と選び方 |
| [REQ-027](../10_requirements/requirements.md) | Must | TC-115 / 116 | 未作成 | 起票済み |
| [REQ-028](../10_requirements/requirements.md) | Should | TC-088 | 未作成 | 起票済み |
| [REQ-029](../10_requirements/requirements.md) | Must | TC-033 / 034 | 未作成 | 起票済み |
| [REQ-030](../10_requirements/requirements.md) | Must | TC-037 / 038 / **172** | 未作成 | 起票済み |
| [REQ-031](../10_requirements/requirements.md) | Must | TC-085 / 133 | 未作成 | 起票済み |
| [REQ-032](../10_requirements/requirements.md) | Must | TC-108 / **168** / **176** / **177** | 未作成 | 起票済み |
| [REQ-033](../10_requirements/requirements.md) | Must | TC-110 / 111 / 112 / 143 / 144 / **168** / **177** | 未作成 | 起票済み |
| [REQ-034](../10_requirements/requirements.md) | Must | TC-015 / 016 / 131 / 132 / 134 | 未作成 | 起票済み |
| [REQ-035](../10_requirements/requirements.md) | Must | TC-117 | 未作成 | 起票済み |
| [REQ-036](../10_requirements/requirements.md) | Must | TC-114 / 141 | 未作成 | 起票済み |
| [REQ-037](../10_requirements/requirements.md) | Should | TC-139 | 未作成 | 起票済み |
| [REQ-038](../10_requirements/requirements.md) | Must | TC-024 / 025 | 未作成 | 起票済み |
| [REQ-039](../10_requirements/requirements.md) | Must | TC-029 / 103 | 未作成 | 起票済み |
| [REQ-040](../10_requirements/requirements.md) | Should | TC-087 | 未作成 | 起票済み |
| [REQ-041](../10_requirements/requirements.md) | Must | TC-011 / 046 | 未作成 | 起票済み |
| [REQ-042](../10_requirements/requirements.md) | Must | TC-078 / 079 | 未作成 | 起票済み |
| [REQ-043](../10_requirements/requirements.md) | Must | TC-012 / 013 / 065 / 067 | 未作成 | 起票済み |
| [REQ-044](../10_requirements/requirements.md) | Must | TC-126 / 127 / **173** / **174** | 未作成 | 起票済み |
| [REQ-045](../10_requirements/requirements.md) | Must | TC-090 / 091 / 092 / **174** | 未作成 | 起票済み |
| [REQ-046](../10_requirements/requirements.md) | Must | TC-036 / 047 | 未作成 | 起票済み |
| [REQ-047](../10_requirements/requirements.md) | Must | TC-043 / 062 / 063 | 未作成 | 起票済み |
| [REQ-048](../10_requirements/requirements.md) | Must | TC-017 | 未作成 | 起票済み |
| [REQ-049](../10_requirements/requirements.md) | Must | TC-035 / 036 | 未作成 | 起票済み |
| [REQ-050](../10_requirements/requirements.md) | Must | TC-048 / 082 | 未作成 | 起票済み |
| [REQ-051](../10_requirements/requirements.md) | Must | **TC-080 / 142** | 未作成 | 起票済み |
| [REQ-052](../10_requirements/requirements.md) | Must | TC-014 / 070 / 071 / 072 | 未作成 | 起票済み |
| [REQ-053](../10_requirements/requirements.md) | Must | TC-109 | 未作成 | 起票済み |
| [REQ-054](../10_requirements/requirements.md) | Must | TC-073 / 081 / 086 | 未作成 | 起票済み |
| [REQ-055](../10_requirements/requirements.md) | Must | TC-049 / 066 / 095 / **175** | 未作成 | 起票済み |
| [REQ-056](../10_requirements/requirements.md) | Must | TC-069 | 未作成 | 起票済み |
| [REQ-057](../10_requirements/requirements.md) | Must | TC-068 | 未作成 | 起票済み |
| [REQ-058](../10_requirements/requirements.md) | Must | TC-091 | 未作成 | 起票済み |
| [REQ-059](../10_requirements/requirements.md) | Must | TC-026 / 027 | 未作成 | 起票済み |
| [REQ-060](../10_requirements/requirements.md) | Must | TC-152 / 153 | 未作成 | 確定（ADR-0015 承認 2026-09-06） |
| [REQ-061](../10_requirements/requirements.md) | Must | TC-154 / 155 / 156 | 未作成 | 確定（ADR-0015 承認 2026-09-06） |
| [REQ-062](../10_requirements/requirements.md) | Must | TC-159 / 160 / 161 / 162 / 163 / **169** | 未作成 | 確定（ADR-0016 承認 2026-09-06）。TC-169 は濃さ（G-03） |

## 穴チェック（2026-09-08 実測）

| | 結果 |
| --- | --- |
| Must なのに TC が無い要件 | **0 件** |
| ケース表にあってコードが無い TC | **0 件**（159 / 159） |
| テストコードはあるが対応する REQ が無い | 無し |

**突き合わせ方**（`docs/40_test/cases/` の表の TC 番号と、
`tests/` + `unity/Assets/Tests/` のコードに現れる TC 番号を機械的に比較する）:

```bash
python3 - <<'EOF'
import re, pathlib
docs = {m.group(1) for p in pathlib.Path('docs/40_test/cases').glob('*.md')
        for m in re.finditer(r'\|\s*(TC-\d{3})\s*\|', p.read_text(encoding='utf-8'))}
code = {'TC-' + m.group(1) for d in ('tests', 'unity/Assets/Tests')
        for p in pathlib.Path(d).rglob('*.cs')
        for m in re.finditer(r'TC-?(\d{3})', p.read_text(encoding='utf-8'))}
print("未コード化:", sorted(docs - code) or "なし")
EOF
```

## 実装前に green になるテスト（DoD の例外）

**フェーズ 4 の DoD「全て落ちる」を適用しないもの。**理由は
[test_first.md](../00_process/test_first.md) 5.2。

| TC | なぜ green になるか |
| --- | --- |
| TC-006 | `src/` を走査して禁止語が**無いこと**を見る静的検査。空実装の時点で条件を満たす。見張っているのは「実装が環境に触っていないこと」であって「実装があること」ではない |
| TC-024 | 「首の向きが状態列に影響しない」を、`TickInput` が首の向きを**持たないこと**で機械的に保証している（types.md 4 節 / data_model.md 5 節）。型の形を見る検査なので、実装の有無に関係なく成り立つ |
| TC-156 | 同じ形。「予兆は顔を見ているときだけ分かる」（REQ-061）を、`NightState` が予兆を**持たないこと**で保証している。状態に入れると首の向きに関係なく出せてしまう。**表示側の検証は e2e（TC-121）** |
| TC-116 の「通信を行わない」 | `IShare` の口が**文字列しか受け取らないこと**を型で見る。宛先や URL を受け取る形にすると送り先をアプリ側で決めることになる（SH-2 / NFR-002）。型の形を見る検査 |
| TC-117 の「抑止の切り替えは口を通す」 | `NullPower` は**テスト用の差し替えそのもの**なので実装済み。TC-102 の `StepClock` と同じ |
| TC-102 の 2 件 | `StepClock` は**テスト用の差し替えそのもの**（ADR-0002 D2）で、道具なので実装済み。検証しているのは「差し替えが効くこと」と「本番と同じ振る舞いをすること」 |
| TC-113 の「規則が環境に触っていない」 | `BoardDateRule.cs` を走査して `DateTime.Now` / `UnityEngine` が**無いこと**を見る静的検査（CA-6）。TC-006 と同じ形 |
| TC-114 の「盤面は再生しても書き換わらない」 | `NightState` が seed も日付も**持たないこと**で、プレイ中に盤面が変わりえないことを保証している（CA-3 / REQ-036） |
| （TC 無し）[tests/harness/HarnessSelfTests.cs](../../tests/harness/HarnessSelfTests.cs) の `TraceFileTests` 18 件 | **ハーネス自身のテスト。**検証対象が製品（`src/`）ではなく、入力列の読み書きと失敗報告という**道具**。道具は既に実装されているので green が正しい。道具が壊れているとその先の全テストが信用できなくなるため、テストは要る |

**ここに足すときは理由を書く。**書けないなら、それは DoD の例外ではなく空振りしているテスト。

## テストコードの現況（2026-09-06）

| 置き場 | 対応 TC | 状態 |
| --- | --- | --- |
| [tests/unit/](../../tests/unit/) | TC-001〜007 / 084〜097 / **113** / 165 | **33 件中 31 件が赤 / 2 件が green**（上の例外） |
| [tests/harness/](../../tests/harness/) | TC-010〜018 / 020〜083 / 098〜122 / 127 / 130〜144 / 147 / 152〜164 | **164 件中 138 件が赤 / 26 件が green**。green は道具自身の 18 件と型・静的検査・差し替えの 8 件 |
| [unity/Assets/Tests/](../../unity/Assets/Tests/) | TC-109 / 120〜126 / 128 / 129 / 145〜147 / 150 / 151 | **16 件すべて赤**（PlayMode で実行確認済み） |

`export PATH="$HOME/.dotnet:$PATH"` のうえ、`tests/unit` と `tests/harness` でそれぞれ `dotnet test`。

**純粋層の TC はすべてコード化した。**MOD-Rng / Board / Sim / Score / End / Result / Display と、
ADR-0015 / 0016 / 0017 で足した TC-152〜156 / 159〜164。

## Unity 側（PlayMode）のテスト

**描いて見るしかないもの・実機でしか測れないもの・実ファイルが要るものだけ**を
[unity/Assets/Tests/](../../unity/Assets/Tests/) に置いた。**16 件すべて赤**（2026-09-08 実行）。

| TC | 中身 |
| --- | --- |
| TC-120 / 122〜126 / 128 / 129 | 画素を見る。`ScreenProbe` 経由で撮って測る |
| TC-109 | 保存の原子性。**実ファイルでないと「途中で落ちる」を作れない** |
| TC-145〜147 / 150 / 151 | 実機。**エディタでは落とす**（黙って通すと「測っていないのに緑」になる） |

**規則で見られるものは Unity 側に置かない。**
TC-122 の区間の重なり、TC-127 の冗長化、TC-130 の -1 の扱いは
[tests/harness/ViewRuleTests.cs](../../tests/harness/ViewRuleTests.cs) にあり、`dotnet test` で走る。

## まだ書けていない TC と、その理由

| 範囲 | 対象 | 書けない理由 |
| --- | --- | --- |
| ~~TC-098〜102~~ | ~~Clock~~ | **解決（2026-09-06）。**`Consume(deltaSeconds)` が経過時間を引数で受け取るので、Unity なしで検証できた。コード化済み |
| ~~TC-108〜112~~ | ~~Storage~~ | **解決（2026-09-06）。**`IStorage` の口に対して `MemoryStorage` で検証できた（D3）。**端末の保存先を触る `FileStorage` だけが Unity / 実機（TC-109）** |
| ~~TC-103〜107~~ | ~~Input~~ | **解決（2026-09-08）。**接触を `PointerSample` として値で受け取る形にした。Unity 側は `Input.touches` を詰め替えるだけの殻になる。コード化済み |
| ~~TC-113 / 114~~ | ~~Calendar~~ | **解決（2026-09-06）。**規則を `BoardDateRule` として純粋層に出し、時刻を引数で受ける形にした（G-01）。コード化済み |
| ~~TC-116〜118~~ | ~~Share / Power~~ | **解決（2026-09-08）。**判断を `ShareRule` / `PowerRule` に切り出し、端末に触る実体だけを Unity 側に残した。コード化済み |
| **TC-115 / 119 / 147** | ビルド検査 | **コード化済みだが、ビルド成果物が要る。**生成された `AndroidManifest.xml` を探して権限と通信の宣言を見る。**1 度 Android ビルドを通すまで赤のまま**（成果物を見ずに「権限 0 件」とは言えないので、これは正しい赤） |
| TC-109 | Storage | **原子的書き込み。**実際に落として前の版が残るかを見るので実機・実ファイル |
| TC-120〜151 | View / Tutorial / Shell / NFR | e2e と実機。Unity が要る |

**Unity が本当に要るのは、タッチ・描画・権限・実機の 4 つだけ**（architecture.md）。
境界層は `UnityEngine` を参照しない形にしたので、時間の蓄積・保存の往復・日付の規則は
`dotnet test` で走る。

TC-113 / 114 は IF の問題だったので、[MOD-Calendar](../30_detailed_design/MOD-Calendar.md) の
公開 IF を変えて解決した（2026-09-06。本人承認）。
**規則（`BoardDateRule`）を純粋層に出し、端末時計を読む役だけを `ICalendar` に残した。**
