# MOD-Power — 画面消灯の抑止

種別: リファレンス / 層: **境界層**
更新トリガー: 抑止する区間を変えたとき
対応要件: REQ-035

## 公開 IF

```csharp
public interface IPower {
    void KeepAwake(bool on);
    bool IsAwake { get; }
}

public sealed class AndroidPower : IPower { }
public sealed class NullPower : IPower { }
```

## 満たすこと

| # | 性質 | 要件 |
| --- | --- | --- |
| PW-1 | `SCR-Night` かつ `ST-N-Run` の間、無操作が続いても画面が消えない | REQ-035 |
| PW-2 | **`ST-N-Pause` に入ったら抑止を解除する**（screens.md 4.4）。中断中に電池を食わない | REQ-009 |
| PW-3 | `SCR-Night` 以外では抑止しない | REQ-035 |
| PW-4 | 権限を要求しない（`FLAG_KEEP_SCREEN_ON` は権限不要） | NFR-003 |

**PW-1 の理由**: 寝たふり中は入力が無いため、放置すると OS が画面を消す。
**盲目区間（最長で約 7 秒）が消灯で壊れないこと**が最低条件。

## エラー時

| 状況 | 振る舞い |
| --- | --- |
| 抑止に失敗 | 何もしない。**プレイは続行する** |

## 決めていないこと

なし。
