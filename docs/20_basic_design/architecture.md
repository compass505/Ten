# モジュール分割

種別: リファレンス（規約・事実）— 理由は ADR に置く
更新トリガー: 要件が増えたとき / 依存方向を変えたとき
状態: 2026-09-02 起草 / 2026-09-04 更新。**フェーズ 2 の 4 本すべて起草済み**（screens.md / balance.md / data_model.md）

制約の出典: [ADR-0002](../10_requirements/decisions/ADR-0002-test-harness.md)（決定論の 4 条件） /
[ADR-0001](../10_requirements/decisions/ADR-0001-tech-stack.md)（Unity + Android） /
[ADR-0009](../10_requirements/decisions/ADR-0009-time-structure.md)（固定 tick） /
[ADR-0008](../10_requirements/decisions/ADR-0008-randomness-scope.md) /
[ADR-0011](../10_requirements/decisions/ADR-0011-per-play-randomness.md)（乱数）

## 分割の原則

[ADR-0002](../10_requirements/decisions/ADR-0002-test-harness.md) が
「純粋ロジックが環境に触らない」形を要求している。したがって 3 層に分ける。

| 層 | 性質 | テスト |
| --- | --- | --- |
| **純粋層** | 乱数・時間・I/O・Unity API に一切触らない。入力と状態から次の状態を返すだけ | **harness 層の主戦場。**ここが全 Must の大半を持つ |
| **境界層** | 環境に触る唯一の場所。純粋層へは値として渡す | 差し替え可能にする（D3） |
| **表示層** | Unity の描画・入力。状態を受け取って見せるだけで、判断をしない | e2e と目視 |

**依存は上から下への一方向のみ。**表示層 → 純粋層は許すが、純粋層 → 表示層は禁止。

## モジュール一覧

### 純粋層（7 件）

| ID | 役割 | 主な要件 |
| --- | --- | --- |
| `MOD-Rng` | 用途別の決定論的乱数。`値(用途, 通番...)` の純関数。ハッシュ実装を自前で持つ | REQ-019 / 048 |
| `MOD-Board` | 盤面の生成。日付シード → 親の初期覚醒度・山札の枚数と内訳・出来事の種類と時刻。盤面仕様の版番号を持つ | REQ-019 / 021 / 040 / 041 / 043 / 048 |
| `MOD-Sim` | 夜の状態と、1 tick 進める純関数。状態（覚醒度・慣れ・元気・山札残・時刻・寝たふり・得点）の型と不変条件もここ | REQ-003 / 004 / 011〜018 / 029 / 030 / 039 / 041 / 043 / 046 / 047 / 049 / 055 / 056 / 057 |
| `MOD-Score` | 得点の判定とエッジ検出（上限到達の瞬間に 1 回だけ）。最高成績の比較 | REQ-023 / 025 / 052 |
| `MOD-End` | 終了条件の判定（6:00 到達 / 赤ちゃんの寝落ち / 山札 0）。**得点判定より後に評価する** | REQ-007 / 018 / 042 / 047 / 050 / 051 |
| `MOD-Result` | 結果テキストの生成。実況 3 文以内・プレイ回数・終わり方・版番号。盤面の答えを含めない | REQ-026 / 028 / 031 / 040 / 054 |
| `MOD-Display` | 状態 → 見た目の段階への写像。粗い固定段階、端点は正確 | REQ-006 / 008 / 016 / 044 / 045 / 058 |

### 境界層（6 件）

| ID | 役割 | 主な要件 |
| --- | --- | --- |
| `MOD-Clock` | 実時間 → 固定 tick。フォーカス喪失で停止。実フレームレートに結果を依存させない | REQ-007 / 009 / 020 |
| `MOD-Input` | タッチ → 入力イベント。tick への量子化、排他、連打の吸収 | REQ-002 / 003 / 005 / 038 / 039 |
| `MOD-Storage` | 保存と復元。破損時の復旧。原子的に書く | REQ-010 / 024 / 032 / 033 / 053 |
| `MOD-Calendar` | 端末ローカル日付（**正午 12:00 を境界**とする）。プレイ中の日付変更を無視する | REQ-019 / 022 / 036 |
| `MOD-Share` | 結果テキストのコピー / 共有 Intent | REQ-027 |
| `MOD-Power` | 無操作中の画面消灯抑止 | REQ-035 |

### 表示層（3 件）

| ID | 役割 | 主な要件 |
| --- | --- | --- |
| `MOD-View` | 一人称の寝室。首振り、親の顔 / 手 / 姿勢、窓の明るさ、部屋の照明。**明度差だけに依存しない冗長化**。操作対象は画面の下半分 | REQ-001 / 002 / 004 / 005 / 006 / 008 / 044 |
| `MOD-Tutorial` | 初回起動の導線。操作しながら覚醒度・元気・寝たふりを知る | REQ-034 |
| `MOD-Shell` | アプリのライフサイクル。プレイの開始 / 中断 / 再開 / 放棄、1 日の中の周回 | REQ-009 / 010 / 022 / 024 / 036 / 037 |

## 依存方向

```
  MOD-Shell ────────────────────────────┐
      │                                 │
      ├── MOD-View ── MOD-Display ──┐    │
      ├── MOD-Tutorial              │    │
      ├── MOD-Input ────────────────┤    │
      ├── MOD-Clock ────────────────┤    │
      ├── MOD-Calendar ──┐          │    │
      ├── MOD-Storage ───┤          │    │
      ├── MOD-Share ── MOD-Result   │    │
      └── MOD-Power      │          │    │
                         ▼          ▼    ▼
                    MOD-End ── MOD-Score ── MOD-Sim ── MOD-Board ── MOD-Rng
```

- **循環なし。**右端の `MOD-Rng` は何にも依存しない
- 純粋層（右 7 件）は Unity API を参照しない。**別アセンブリに分けて、参照を機械的に禁止する**

### アセンブリの分け方（2026-09-06 追記）

**境界層も `UnityEngine` を参照しない。**Unity API を直接叩く実装
（タッチの読み取り・端末の保存先・画面消灯抑止）は**さらに外側**に置き、
境界層は**口（インターフェース）と、環境から受け取った値だけで完結する処理**を持つ。

| アセンブリ | 中身 | `dotnet test` で走るか |
| --- | --- | --- |
| `Ten.Pure` | 純粋層 7 件 + `BoardDateRule` | **走る** |
| `Ten.Boundary` | 境界層の口と、値だけで完結する処理（時間の蓄積・保存の往復・日付の規則の呼び出し） | **走る** |
| Unity 側（未作成） | `UnityEngine` を直接叩く実装、表示層 3 件 | Unity Test Runner |

**この分け方が効いた例。**`IClock.Consume(double deltaSeconds)` は経過時間を引数で
受け取るので、`RealClock` の蓄積と上限（C-2 / C-3）は Unity なしで検証できる。
Unity 側は `Update` で `Time.deltaTime` を渡すだけになる。
`MOD-Calendar` も同じ形にした（→ [decisions_pending.md](../00_process/decisions_pending.md) G-01）。

**Unity が本当に要るのは、タッチ・描画・権限・実機の 4 つだけ。**
- `MOD-End` → `MOD-Score` の向きは REQ-051（最後の 1 枚の得点は終了判定より先）から決まる

## 要件の割り当て（DoD）

全 62 要件の割り当て先。**未割り当ては 0 件。**
REQ-060〜062 は **2026-09-06 に確定**（ADR-0015 / 0016 承認）。

| REQ | モジュール | REQ | モジュール |
| --- | --- | --- | --- |
| REQ-001 | View | REQ-030 | Sim |
| REQ-002 | Input / View | REQ-031 | Result |
| REQ-003 | Input / Sim | REQ-032 | Storage |
| REQ-004 | View / Sim | REQ-033 | Storage |
| REQ-005 | View | REQ-034 | Tutorial |
| REQ-006 | Display / View | REQ-035 | Power |
| REQ-007 | Clock / End | REQ-036 | Calendar / Shell |
| REQ-008 | Display / View | REQ-037 | Shell |
| REQ-009 | Shell / Clock | REQ-038 | Input |
| REQ-010 | Shell / Storage | REQ-039 | Sim / Input |
| REQ-011 | Sim | REQ-040 | Board / Result |
| REQ-012 | Sim | REQ-041 | Board / Sim |
| REQ-013 | Sim | REQ-042 | End |
| REQ-014 | Sim | REQ-043 | Board / Sim |
| REQ-015 | Sim | REQ-044 | Display / View |
| REQ-016 | Sim / Display | REQ-045 | Display |
| REQ-017 | Sim | REQ-046 | Sim |
| REQ-018 | Sim / End | REQ-047 | Sim / End |
| REQ-019 | Board / Rng / Calendar | REQ-048 | Rng / Board |
| REQ-020 | Sim / Rng / Clock | REQ-049 | Sim |
| REQ-021 | Board | REQ-050 | End |
| REQ-022 | Calendar / Shell | REQ-051 | End / Score |
| REQ-023 | Score | REQ-052 | Score |
| REQ-024 | Shell / Storage | REQ-053 | Storage |
| REQ-025 | Score / Storage | REQ-054 | Result |
| REQ-026 | Result | REQ-055 | Sim |
| REQ-027 | Share | REQ-056 | Sim |
| REQ-028 | Result | REQ-057 | Sim |
| REQ-029 | Sim | REQ-058 | Display |
| REQ-059 | Input / Sim | REQ-060 | Input / Sim |
| REQ-061 | Sim / Display / View | REQ-062 | Sim / Result |

非機能要件:

| NFR | モジュール |
| --- | --- |
| NFR-001 / 008 | Shell |
| NFR-002 / 003 | **全体の制約**（通信する依存を持たない。外部 SDK を組み込まない） |
| NFR-004 | Rng / Sim / Clock |
| NFR-005 / 006 | Clock / End |
| NFR-007 / 009 | View |

## 未着手

| ファイル | 内容 |
| --- | --- |
| `data_model.md` | データ構造と、永続化するもの / しないものの線引き |
| （数値の確定は [balance.md](balance.md) に移した。**全て仮の初期値**） | |
