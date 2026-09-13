# MOD-Storage — 保存と復元

種別: リファレンス / 層: **境界層**
更新トリガー: 保存対象が増減したとき / 保存形式を変えたとき（**schema の版が上がる**）
対応要件: REQ-010 / 024 / 032 / 033 / 053

出典: [data_model.md](../20_basic_design/data_model.md)

## 公開 IF

```csharp
public interface IStorage {                      // D3: I/O 差し替え
    DeviceData?  LoadDevice();
    TodayData?   LoadToday();
    SavedRun?    LoadRun();
    void SaveDevice(DeviceData d);
    void SaveToday(TodayData t);
    void SaveRun(SavedRun r);
    void ClearRun();
    void ClearAll();
    /// 読めたか / 壊れていたか
    LoadStatus LastStatus { get; }
}

public enum LoadStatus { Ok, Missing, Corrupt, VersionMismatch }

public sealed class FileStorage : IStorage { }
public sealed class MemoryStorage : IStorage { }  // テスト
```

## 満たすこと

| # | 性質 | 要件 |
| --- | --- | --- |
| ST-1 | **原子的に書く。**一時ファイルに書き切ってから rename。途中で落ちても前の版が壊れない | REQ-053 |
| ST-2 | 3 つの塊（`Device` / `Today` / `Run`）を**別々に読み書きする。**壊れたときに捨てる範囲を分ける | REQ-033 |
| ST-3 | 範囲外の値を含むデータは `Corrupt` として扱う（[balance.md](../20_basic_design/balance.md) 2 節の範囲） | REQ-033 |
| ST-4 | `schemaVersion` が未知（未来の版）なら `VersionMismatch` | REQ-033 |
| ST-5 | 保存した `NightState` を復元して同じ入力列を与えると、**中断しなかった場合と状態列が一致する** | REQ-020 / 010 |
| ST-6 | `MemoryStorage` に差し替えられる | ADR-0002 D3 |

**ST-5 がこのモジュールの一番重いテスト。**任意の tick で保存 → 復元 → 続きを再生して、
通しで再生した状態列と差分 0 であることを、複数シード × 複数の中断位置で確認する。

## エラー時

| 状況 | 振る舞い |
| --- | --- |
| 読めない / 壊れている | **例外を投げず `LastStatus` で返す。**起動できなくならないことが REQ-033 |
| 書けない（容量不足等） | 例外を投げず失敗を返す。**プレイは続行する**（保存できないだけで夜は壊れない） |

## 決めていないこと

| ID | 論点 |
| --- | --- |
| STO-01 | 形式（JSON / バイナリ / PlayerPrefs）。**NFR-008 のコールドスタート 3 秒に収まること**が条件 |
| STO-02 | 検査値の作り方（チェックサム） |
