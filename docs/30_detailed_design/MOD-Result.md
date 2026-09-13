# MOD-Result — 結果テキストの生成

種別: リファレンス / 層: **純粋層**
更新トリガー: 結果テキストに含めるものが増減したとき / 文言パターンを変えたとき
対応要件: REQ-026 / 028 / 031 / 040 / 054

## 公開 IF

```csharp
public static class Result {
    /// その日の結果テキスト。**盤面の答えを含めない**
    public static string Compose(BestPlay best, int playCount, int boardSpecVersion, IReadOnlyList<Beat> beats);

    /// 7 軸の割合ベクトルに最も近い診断を、カタログから選ぶ（ADR-0016）
    /// コサイン類似度。同点はカタログの並び順で先を採る（決定論）
    public static DiagnosisEntry Diagnose(Diagnosis d, Tuning tuning);

    /// 実況の素材。Sim が夜の間に積む（最大 3 件まで残す）
    public readonly record struct Beat(int Tick, BeatKind Kind);
    public enum BeatKind { FirstScore, BestPretend, HandRanLow, EventStruck, FellAsleepAt, DawnReached }

    /// 診断の濃さ（diagnosis.md 2 節）と、修飾語を付けた名前（G-03。2026-09-13 追加）
    public enum Strength { Thin, Plain, Thick }
    public static Strength StrengthOf(Diagnosis d, Tuning tuning);
    public static string Title(DiagnosisEntry entry, Strength strength);
}

/// 実況の素材を夜の間に積み、3 件に絞る（RES-02 決着。2026-09-13 追加）
public static class Commentary {
    public const int Max = 3;
    public static IReadOnlyList<Result.Beat> Observe(IReadOnlyList<Result.Beat> beats, NightState prev, NightState next, Tuning tuning);
    public static IReadOnlyList<Result.Beat> Pick(IReadOnlyList<Result.Beat> beats);
    public static string Encode(IReadOnlyList<Result.Beat> beats);
    public static IReadOnlyList<Result.Beat> Decode(string? text);
}
```

### 素材を積む条件（`Commentary.Observe`。TC-166）

| 素材 | 積む瞬間 |
| --- | --- |
| `FirstScore` | 得点が 0 → 1 |
| `BestPretend` | 親が `Settling` → `Grace`（**成功側の着地だけ**。`Feint` の満了は積まない） |
| `EventStruck` | 出来事が 1 件起きた |
| `HandRanLow` | 山札が終盤の段階に入った（0 枚は夜の終わりなので数えない） |
| `FellAsleepAt` / `DawnReached` | 夜が寝落ち / 夜明けで終わった |

**同じ種類は最初の 1 回だけ。**選ぶとき（`Pick`。TC-167）の優先順は
**終わり方 → 寝たふり → 最初の一撃 → 出来事 → 手札**。選んだ 3 件は起きた順に並べる。

## 満たすこと

| # | 性質 | 要件 |
| --- | --- | --- |
| RS-1 | 実況は **3 文以内**（「行」ではなく句点で数える） | REQ-026 |
| RS-2 | その日の**通算プレイ回数**と、**何回目のプレイの結果か**を含む | REQ-031 |
| RS-3 | **終わり方**（夜明け / 寝落ち / 山札切れ）を含む | REQ-054 |
| RS-4 | **盤面仕様の版番号**を含む | REQ-040 |
| RS-5 | **親の初期状態・山札の並び・出来事の順序を含まない** | REQ-028 |
| RS-6 | 純関数。同じ引数から常に同じ文字列 | REQ-020 |
| RS-7 | `Diagnose` は**最大軸だけで決めない。**ベクトルの形で選ぶ（実測で最大軸方式は潰れた） | REQ-062 |
| RS-8 | 合計が閾値未満なら **`DX-41` を固定で返す**（割合ベクトルが定義できない） | REQ-062 |

**RS-5 のテスト**: 生成された文字列に、`BoardSpec` の `InitialArousal` / `Hand` の内訳 /
`Events` の並びが**復元できる情報が含まれていない**こと。
（数値の直接出力だけでなく、**一意に対応する語**も禁止）

## 文言の作り

- **`Beat` は「起きたこと」であって「盤面の中身」ではない。**
  「オムツ替えが 2 枚あった」は禁止、「オムツ替えに救われた」は可
- 文言パターンは `Tuning` ではなく**リソースとして分離**する（フェーズ 3 の後半で決める）

## エラー時

| 状況 | 振る舞い |
| --- | --- |
| `beats` が空 | 終わり方だけの 1 文を返す。**例外にしない**（何もしないプレイは正常） |
| `beats` が 3 件を超える | 先頭 3 件を使う（Sim 側で既に絞っているはず） |

## 決めていないこと

| ID | 論点 |
| --- | --- |
| RES-01 | 文言パターンの持ち方（リソースファイル / 定数配列） |
| ~~RES-02~~ | ~~`Beat` の選び方~~ → **決着（2026-09-13）。**上の「素材を積む条件」と優先順。状態の前後だけを見る純関数（TC-166 / 167） |
| ~~G-03~~ | ~~「濃さ」の置き場~~ → **決着（2026-09-13）。**`DiagnosisEntry` には持たせず、`StrengthOf` / `Title` で名前に付ける（カタログの 1 件は薄くも濃くもなるため。TC-169） |
| RES-04 | カタログを埋め込むか外部リソースにするか。**50 件の文言は [diagnosis.md](../20_basic_design/diagnosis.md) 4 節が正** |
| ~~RES-03~~ | ~~実況を保存するか、入力列から再生成するか~~ → **決着（2026-09-13）。素材を保存し、共有時に文にする**（[data_model.md](../20_basic_design/data_model.md) M-04） |
