# MOD-Sim — 夜の状態と 1 tick

種別: リファレンス / 層: **純粋層**
更新トリガー: 状態が増減したとき / tick の処理順を変えたとき / 出来事の効果を変えたとき
対応要件: REQ-003 / 004 / 011〜018 / 029 / 030 / 039 / 041 / 043 / 046 / 047 / 049 / 055 / 056 / 057 / 059

出典: [screens.md](../20_basic_design/screens.md) 4 節（状態機械と処理順） /
[balance.md](../20_basic_design/balance.md)（数値）

> **このモジュールが全 Must の大半を持つ。**ハーネス層のテストはここに集中する。

## 公開 IF

```csharp
public static class Sim {
    /// 夜の開始状態。盤面とプレイ回数から作る
    public static NightState Begin(BoardSpec board, int playIndex, Tuning tuning);

    /// 1 tick 進める。**純関数**（引数以外の一切に触らない）
    public static NightState Advance(NightState s, TickInput input, BoardSpec board, Tuning tuning);
}
```

**`Advance` はこれ 1 本だけ。**内部の段階を公開しない。
テストは「状態 → 状態」だけを見る（[README](README.md) の DoD）。

## 処理順（[screens.md](../20_basic_design/screens.md) 4.5 と 1 対 1）

`Advance` は必ずこの順で処理する。**順序が決定論の前提**（REQ-039）。

| 順 | 処理 |
| --- | --- |
| 1 | 入力を適用（1 tick 1 件。閉眼中は `ToggleEyes` のみ受け付け、他は捨てて副作用も残さない） |
| 2 | その時刻の出来事を発生させる |
| 3 | 覚醒度に依存しない親の遷移（`Caring` 開始/終了、`Settling`→`Grace`、**`Feint`→`Sleeping`**、`Settling` / **`Feint`** 中の開眼→`Caring`、`Grace` 満了）。`Grace` で泣いた場合は**予約のみ** |
| 3.5 | **REQ-049 の適用範囲**: `Caring` 中に加え、**予告中（`PendingCare != null`）も覚醒度を上げない** |
| 4 | 時計を進める（`TIdle` / `TClosed` / `TSettle` / `TGrace`） |
| 5 | 寝たふりの判定（`TClosed` 閾値到達 **かつ** `Parent == Sleeping`。`FallAsleep(n)` → `SleepPretend(n)` → `n` を +1）。**成功なら `Settling`、失敗なら `Feint`**（ADR-0017。どちらも `TSettle` を 0 から進め始める） |
| 6 | 寝たふり中の寝落ち（`Settling` **または `Feint`** に入る tick のみ） |
| 7 | 待機中の寝落ち（`Settling` / **`Feint`** / `Grace` 中は停止。5 / 6 を引いた tick はスキップ） |
| 8 | 状態値の更新（覚醒度・元気・慣れ・時刻）。**順 3 の予約をここで適用** |
| 9 | 覚醒度に依存する親の遷移（上限到達 → `Up` / 定められた値まで低下 → `Sleeping`） |

### 順 5〜7 に `Feint` を含める理由（ADR-0017）

**`Feint` は `Settling` の失敗側の双子で、観測上まったく同じでなければならない。**
処理順のどこか 1 つでも扱いが違うと、そこから寝たふりの成否が逆算できる（REQ-016 違反）。

| もし `Feint` を外すと | 何が漏れるか |
| --- | --- |
| 順 6 で `Feint` を除く | 失敗側だけ寝落ち判定が走らない ⟹ **寝落ちしなかったこと自体が「失敗」の通知**になる |
| 順 7 で `Feint` を止めない | 失敗側だけ待機中の寝落ちが進む ⟹ **寝落ちの起こりやすさが成否を漏らす** |
| 順 3 で開眼時の遷移先を変える | 失敗側で開眼したときだけ `Hand` が減らない ⟹ **山札の減り方から逆算できる** |

**「失敗側は何もしない」が正しくない**のがこの設計の要点。
失敗側は**成功側と同じだけのことをして、行き先だけが違う。**
→ [I-13](types.md) / TC-164

**得点（順 9）と終了（順 10）は `MOD-Score` / `MOD-End` の担当。**
`Advance` はそれらを呼ばず、**呼び出し側が順に適用する**（architecture.md の依存方向）。

## 満たすこと

| # | 性質 | 要件 |
| --- | --- | --- |
| S-1 | **純関数。**同じ (s, input, board, tuning) は常に同じ結果 | REQ-020 / NFR-004 |
| S-2 | `types.md` の不変条件 I-1〜I-11 を常に満たす | — |
| S-3 | 同じ行動の上昇量は覚醒度について**単調非増加** | REQ-012 |
| S-4 | `Caring` 中と予告中は、覚醒行動が覚醒度を上げない | REQ-049 |
| S-5 | `n` 回目の寝たふりの成功率は `n` について単調非増加、**下限 > 0** | REQ-017 |
| S-6 | どの対処も赤ちゃんの状態を**少なくとも 1 つ改善し、1 つ悪化させる** | REQ-013 |
| S-7 | 眼の操作（開閉）は `TIdle` をリセットしない | REQ-047 / D-03 |
| S-8 | 閉眼中は `ToggleEyes` 以外の入力が状態を変えない（**キューにも残さない**） | REQ-059 |
| S-9 | 山札 0 で `Caring` に入らない | REQ-050 |
| S-10 | 対処までの遅れは山札の残量について単調非増加 | REQ-046 |
| S-11 | どの行動・どの対処も、**全ての状態において他より優れていることがない** | REQ-056 |
| S-12 | 同じ盤面で、入力列の違いによって得点差が生じる | REQ-057 |
| S-13 | **`Settling` と `Feint` から観測できるものが完全に一致する**（I-13） | REQ-016 / ADR-0017 |

**S-11 / S-12 は単体テストで示せない。**複数シード × 複数入力列をハーネスで回して、
得点の分布で判定する（[balance.md](../20_basic_design/balance.md) 12 節と同じ形）。

## エラー時

| 状況 | 振る舞い |
| --- | --- |
| `s.Over != null` | **何もせず `s` をそのまま返す**（終了後に進めても壊れない） |
| 不変条件が破れる入力 | `InvalidOperationException`。**丸めて続行しない。**壊れた状態で進むと再現できなくなる |
| `board` と `s` の seed が違う | 同上 |

## 決めていないこと

| ID | 論点 |
| --- | --- |
| SIM-03 | `Advance` を 1 本にするか、順ごとに分けた internal メソッドにするか。テストは 1 本しか見ない |

**SIM-01（長押しの強度）と SIM-02（寝入りばな）は決着した。**
2026-09-06 に [ADR-0015](../10_requirements/decisions/ADR-0015-input-intensity-and-timing.md) が承認され、
**どちらも入る**（REQ-060 / 061）。`ActStrengthMilli` は常時 1000 の扱いにしない。
