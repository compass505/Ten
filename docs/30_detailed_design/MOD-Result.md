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
}
```

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
| RES-02 | `Beat` の選び方。夜の間に積んだものから「面白い 3 件」をどう選ぶか。**選択規則も決定論でなければならない** |
| RES-04 | カタログを埋め込むか外部リソースにするか。**50 件の文言は [diagnosis.md](../20_basic_design/diagnosis.md) 4 節が正** |
| RES-03 | 実況を保存するか、入力列から再生成するか（[data_model.md](../20_basic_design/data_model.md) M-04。**今は保存する側**） |
