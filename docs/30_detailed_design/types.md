# 共有する型

種別: リファレンス（規約・事実）— 理由は ADR に置く
更新トリガー: 状態が増減したとき / 乱数の用途が増えたとき / 保存形式を変えたとき
状態: **2026-09-05 起草 / 2026-09-06 更新。ADR-0013〜0017 の承認により全て確定。暫定の箇所は無い。**
`ActStrengthMilli`（ADR-0015）と `ParentPhase.Feint`（ADR-0017）はこの日に入った

出典: [architecture.md](../20_basic_design/architecture.md)（層と依存方向） /
[screens.md](../20_basic_design/screens.md)（状態と遷移） /
[data_model.md](../20_basic_design/data_model.md)（保存対象） /
[ADR-0008](../10_requirements/decisions/ADR-0008-randomness-scope.md)（乱数の用途）

## 0. 全体の規約

言語は C#（[ADR-0001](../10_requirements/decisions/ADR-0001-tech-stack.md)）。

- **純粋層の型は `UnityEngine` を参照しない。**別アセンブリに分け、参照を機械的に禁止する
- 状態は**すべて不変（`readonly record struct` / `readonly struct`）**。
  更新は新しい値を返す。これが決定論（D1〜D4）の前提
- **浮動小数点を状態に持たない。**`int` と固定小数（1/1000 単位の `int`）だけを使う。
  端末間で結果がずれる経路を塞ぐ（NFR-004）
- `null` を状態に使うのは「無い」が意味を持つ場所だけ（`CareKind?` など）

### `src/` が使える C# の範囲（2026-09-08。実測で確定）

**`src/` は `dotnet`（net8.0）と Unity（.NET Standard 2.1）の両方でコンパイルされる**
（[architecture.md](../20_basic_design/architecture.md) のローカルパッケージ参照）。
**狭いほうに合わせる。**

| | 決めたこと |
| --- | --- |
| 言語版 | **C# 10。**Unity 6 の既定は C# 9 で `record struct` も file-scoped namespace も通らなかったので、各アセンブリの `csc.rsp` に `-langversion:10` を置いた |
| `IsExternalInit` | **アセンブリごとに shim が要る。**`record` の `init` が必要とする型が .NET Standard 2.1 に無い。`#if !NET5_0_OR_GREATER` で囲み、net8.0 側では定義しない |
| **使えないもの** | **C# 11 以降の構文**（primary constructor 等）。**.NET 8 で足された BCL API**（`ArgumentOutOfRangeException.ThrowIfNegative` / `ArgumentNullException.ThrowIfNull` 等）。素の `if` + `throw` で書く |

**`tests/` は net8.0 だけで走るので、この制限を受けない。**
制限がかかるのは `src/` の 2 アセンブリだけ。

**Unity 側が `using UnityEngine` を弾くことは実測で確認した**（2026-09-08）。
`.asmdef` の `noEngineReferences: true` により、書いた時点で
`error CS0246: The type or namespace name 'UnityEngine' could not be found` になる。
**TC-006 の静的検査と合わせて二重に見張っている。**

## 1. 列挙

```csharp
/// 赤ちゃんの行動。目を閉じる/開けるは行動ではなく眼の操作（screens.md D-03）
public enum ActionKind { Cry, Fuss, Kick }

/// 親の対処。山札の札種でもある（REQ-041）
public enum CareKind { PatPat, Milk, Hold, DiaperChange }

/// 赤ちゃんの状態（screens.md 4.1）
public enum BabyPhase { Open, Charging, Acting, EyesClosed }

/// 親の状態（screens.md 4.2）
/// **`Feint` は `Settling` の失敗側の双子**（ADR-0017）。観測上、両者は区別できてはならない
public enum ParentPhase { Sleeping, Caring, Settling, Feint, Grace, Up }

/// 夜の終わり方（REQ-054）。判定順もこの並び（screens.md 4.5 順 10）
public enum EndKind { Dawn, FellAsleep, HandEmpty }

/// その夜の出来事（REQ-043 / 055）
public enum NightEventKind { DiaperSoiled, PartnerWakes, PhoneRings, ParentRolls, GetsHungry }

/// 乱数の用途（ADR-0008 の一覧 + DozeOff）
public enum RngPurpose {
    ParentInitial,   // 親の初期覚醒度。通番なし
    ParentHand,      // 山札の内訳。通番＝配る順
    NightEvent,      // 出来事。通番＝出来事の順
    SleepPretend,    // 寝たふりの成否。通番＝その夜で何回目の寝たふりか
    FallAsleep,      // 寝たふり中の寝落ち。通番＝同上
    ParentChoice,    // 親がどの対処を選ぶか。通番＝その夜で何回目の対処か
    DozeOff,         // 待機中の寝落ち。通番＝その夜で何回目の刻み判定か（screens.md D-05）
    Diagnosis,       // 診断パラメータの 3 つ目がどこに乗るか。通番＝tick（ADR-0016）
}
```

**`DozeOff` は ADR-0008 の一覧に無かった用途。**REQ-047 に対応するために足した。
ADR-0008 は「用途を後から足すのは安全（既存の値が動かない）」と明記している。

## 2. 盤面（その日固定。REQ-019 / 048）

```csharp
public readonly record struct HandCount(int PatPat, int Milk, int Hold, int DiaperChange) {
    public int Total => PatPat + Milk + Hold + DiaperChange;
    public int Of(CareKind k);
    public HandCount Minus(CareKind k);   // 0 を下回らない
    public HandCount Plus(CareKind k, int n);
}

public readonly record struct ScheduledEvent(int Tick, NightEventKind Kind);

/// 日付シードから決まる、その日の盤面。プレイ回数では変わらない（ADR-0011）
public readonly record struct BoardSpec(
    int SpecVersion,                       // 盤面仕様の版（REQ-040）
    string Seed,                           // 端末ローカル日付（正午境界）から作る
    int InitialArousal,
    HandCount Hand,
    IReadOnlyList<ScheduledEvent> Events
);
```

- **`Seed` は文字列。**日付を `yyyy-MM-dd` に正規化したもの。
  チュートリアル専用盤面（screens.md D-09）は別の接頭辞を付け、`SpecVersion` も別枠にする
- `Events` は `Tick` の昇順。同じ tick に 2 件は置かない

## 3. 夜の状態（`MOD-Sim` が持つ。data_model.md 4 節と 1 対 1）

```csharp
public readonly record struct NightState(
    // 同一性
    int PlayIndex,                 // その日の何回目のプレイか（REQ-048）
    // 時刻
    int Tick,                      // 0〜5400
    // 赤ちゃん
    BabyPhase Baby,
    ActionKind? ActKind,
    int ActRemain,
    int ActStrengthMilli,          // 0〜1000。長押しの強度（ADR-0015 / REQ-060）
    bool ActFired,                 // この行動が既に発火したか
    int Vigor,                     // 0〜100
    // 親
    ParentPhase Parent,
    int Arousal,                   // 0〜100
    CareKind? ActiveCare,
    int CareRemain,
    CareKind? PendingCare,         // 予告中の対処（REQ-046 / 049）
    int CareDelay,
    Habit Habit,
    HandCount Hand,
    // 時計（screens.md 4.3）
    int TIdle, int TClosed, int TSettle, int TGrace,
    // 乱数の通番（引き直さないために保存する。REQ-020 / 032）
    int PretendN, int DozeN, int ChoiceN,
    // 出来事とその後遺症
    int EventsFired,
    int RollUntil, int HungryUntil, bool PartnerHere,
    int LockUntil, ActionKind LockedKinds, int DampUntil, int DampMilli,
    // 診断（ADR-0016 / REQ-062）
    Diagnosis Diag,
    // 得点
    int Score,
    bool ScoredEdge,               // 加点済みか（REQ-052 のエッジ検出）
    bool PretendPrimed,            // 猶予中に泣いた（順 7 で解決する予約）
    int CalmBlock,                 // 起きている親が落ち着くのを止めている残り
    // 終了
    EndKind? Over
);

/// 一晩の診断パラメータ。値は 1/10 単位の整数（diagnosis.md 1 節）
public readonly record struct Diagnosis(
    int Irritation, int Fatigue, int Futility, int SleepLoss,
    int Anxiety, int Jerked, int Fondness
) {
    public int Total { get; }
    /// 合計で割った割合。合計 0 のときは既定値を返す
    public IReadOnlyList<int> Ratios { get; }
}

/// カタログの 1 件（diagnosis.md 4 節）。`MOD-Result.Diagnose` の戻り値
/// **`Profile` の並びは Diagnosis と同じ 7 軸。**「濃さ」は持たない（下の注記）
public readonly record struct DiagnosisEntry(
    string Id, string Name, string Text, IReadOnlyList<int> Profile
);

public readonly record struct Habit(int PatPat, int Milk, int Hold, int DiaperChange) {
    public int Of(CareKind k);
    public int Max { get; }
    public Habit Plus(CareKind k, int n);   // 0〜100 に丸める
    public Habit Decay(int n);
}
```

### 不変条件（`MOD-Sim` が常に守る。テストはここを見る）

| # | 不変条件 |
| --- | --- |
| I-1 | `0 <= Arousal <= 100`、`0 <= Vigor <= 100`、`Habit` の各値が `0〜100` |
| I-2 | `0 <= Tick <= 5400`。`Over != null` のとき `Tick` は進まない |
| I-3 | `Baby == Acting` ⟺ `ActKind != null && ActRemain > 0` |
| I-4 | `Baby == Charging` ⟺ `ActKind != null && ActRemain == 0` |
| I-5 | `Parent == Caring` ⟺ `ActiveCare != null && CareRemain > 0` |
| I-6 | `PendingCare != null` のとき `Parent` は `Sleeping` か `Up` |
| I-7 | `Hand` の各値 `>= 0`。`Hand.Total == 0` のとき `Parent != Caring` なら `Over == HandEmpty` |
| I-8 | `Baby == EyesClosed` のとき `TClosed > 0`、それ以外で `TClosed == 0` |
| I-9 | `Parent == Settling` **または `Feint`** のとき `Baby == EyesClosed`（開眼したら `Caring` へ落ちる。D-06 / ADR-0017） |
| I-10 | `ScoredEdge == true` ⟺ 直近で `Arousal` が 100 に達してから 40 まで下がっていない |
| I-11 | `PretendN`・`DozeN`・`ChoiceN` は単調非減少 |
| I-12 | `Diag` の各値は**単調非減少**（減ることがない） |
| I-13 | **`Settling` と `Feint` は観測上区別できない**（ADR-0017 / REQ-016）。`TSettle` の進み方と満了長、`Hand` を消費しないこと、開眼したときの遷移先（`Caring` / `Hand` −1）、`DozeN` を進めないことが**すべて一致する。**違うのは満了後の行き先だけ（`Grace` / `Sleeping`） |

**I-13 は「どこにも差が出ない」という形の不変条件で、他とは検証の向きが逆。**
差が 1 つでもあれば、そこから寝たふりの成否が逆算できる（→ TC-164）。

**テストは I-1〜I-11 を性質ベースで、それ以外を例示ベースで検証する**
（[ADR-0002](../10_requirements/decisions/ADR-0002-test-harness.md) の規約 3）。

## 4. 入力（1 tick 1 件。screens.md 4.5 順 1）

```csharp
/// その tick に割り付いた入力。複数来たら先着 1 件だけを採り、残りは捨てる
public readonly record struct TickInput(
    ActionKind? Held,      // 押しっぱなしの行動（ADR-0015 / REQ-060）
    bool ToggleEyes        // 目を閉じる / 開ける
);

/// ハーネスが再生する入力列。REQ-020 の「入力列」の実体
public readonly record struct InputTrace(
    string Seed,
    int PlayIndex,
    IReadOnlyList<(int Tick, TickInput Input)> Entries
);
```

- **`Entries` は `Tick` の昇順で、同じ tick は 1 件まで。**
  この正規化を `MOD-Input` が行い、純粋層は正規化済みしか受け取らない
- `InputTrace` はテキストで保存でき、失敗時にそのまま出力する（D4）。
  読み書きは [tests/harness/TraceFile.cs](../../tests/harness/TraceFile.cs)
- **`InputTrace` は `spec` と `tuning` を持たない。**
  [harness.md](../40_test/harness.md) 3 節のヘッダは 4 項目だが、
  この 2 つは**再生に要らず、再現の切り分けにだけ使う**
  （「実装が壊れた」のか「Tuning が変わった」のかの判別）。
  そのためハーネス側の `TraceHeader` が持つ。純粋層に持ち込まない

## 5. 調整値

**数値は型に埋め込まず、`Tuning` として外から渡す**
（[README](README.md) の「調整値・定数の表（コードに直書きしない）」）。

```csharp
public readonly record struct Tuning( /* balance.md の全項目 */ );
```

**値の実体は [balance.md](../20_basic_design/balance.md)。ここには複製しない**
（documentation.md 8 節: 頻繁に変わる数値をハードコードした説明を書かない）。
[ADR-0012](../10_requirements/decisions/ADR-0012-balance-vs-tests.md) により、
**テストの期待値に `Tuning` の具体値を書かない。**

## 6. 保存（`MOD-Storage` が扱う）

```csharp
public readonly record struct DeviceData(int SchemaVersion, int BoardSpecVersion, bool TutorialDone);
public readonly record struct BestPlay(int Score, int PlayIndex, EndKind EndKind,
                                      string Commentary, Diagnosis Diag, string DiagId);
public readonly record struct TodayData(string BoardDate, int PlayCount, BestPlay? Best);
public readonly record struct SavedRun(string BoardDate, NightState State);
```

`NightState` をそのまま保存する。**別の保存用の型を作らない**
（2 つの型がずれると、復元後に状態列が変わって REQ-020 が壊れる）。

## 7. 決めていないこと

| ID | 論点 |
| --- | --- |
| T-01 | `record struct` のサイズ。`NightState` が大きいので、値渡しのコストを実測して決める |
| T-02 | 固定小数の単位（1/1000）で足りるか。慣れと強度の丸め誤差が蓄積しないか |
| T-03 | `Tuning` をどこから読むか（`ScriptableObject` / JSON / 定数クラス） |
| T-04 | `LockedKinds` を `ActionKind` のビットフラグにするか、配列にするか |
| T-05 | **診断の「濃さ」（うっすら / どっぷり）をどこに持たせるか。**[diagnosis.md](../20_basic_design/diagnosis.md) 2 節が合計値の大きさで修飾語を付けると定めているが、`Diagnose` は `DiagnosisEntry` しか返さない。**カタログの 1 件は濃さを持たない**（同じ診断が薄くも濃くもなる）ので、エントリに混ぜると意味がずれる → [decisions_pending.md](../00_process/decisions_pending.md) G-03 |
