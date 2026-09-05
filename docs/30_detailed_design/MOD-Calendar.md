# MOD-Calendar — 端末ローカル日付

種別: リファレンス / 層: **境界層**
更新トリガー: 日付境界を変えたとき
対応要件: REQ-019 / 022 / 036

## 公開 IF

```csharp
public interface ICalendar {
    /// 正午 12:00 を境界とする「その日」。12:00 未満は前日として扱う
    string BoardDate { get; }
}

public sealed class DeviceCalendar : ICalendar { }
public sealed class FixedCalendar : ICalendar { }   // テスト
```

## 満たすこと

| # | 性質 | 要件 |
| --- | --- | --- |
| CA-1 | **正午 12:00 が境界。**夜に日付境界が来ない | REQ-019 |
| CA-2 | 形式は `yyyy-MM-dd`。タイムゾーンは端末ローカル | REQ-019 |
| CA-3 | **プレイ中の日付変更を無視する。**盤面は開始時に確定させる | REQ-036 |
| CA-4 | 過去の日を指定して遊ぶ入口をアプリが持たない | REQ-022 |
| CA-5 | `FixedCalendar` に差し替えられる | ADR-0002 D3 |

**CA-1 の境界テスト**: 11:59:59 と 12:00:00 で `BoardDate` が変わること。
**端末の時計操作は防がない**（REQ-022。通信を持たない以上、原理的に不可）。

## エラー時

| 状況 | 振る舞い |
| --- | --- |
| 端末の時刻が取れない | 例外にせず、**前回保存した `BoardDate` を使う**。取れなければ起動を止めない |

## 決めていないこと

なし。
