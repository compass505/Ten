# MOD-Calendar — 端末ローカル日付

種別: リファレンス / 層: **境界層**
更新トリガー: 日付境界を変えたとき
対応要件: REQ-019 / 022 / 036

## 公開 IF

```csharp
// 純粋層。**規則そのもの。時刻を引数で受ける**（D2）
public static class BoardDateRule {
    /// 正午 12:00 を境界とする「その日」。12:00 未満は前日として扱う。形式は yyyy-MM-dd
    public static string From(DateTimeOffset localNow);
}

// 境界層。**端末時計を読む役だけを持つ**
public interface ICalendar {
    string BoardDate { get; }
}

public sealed class DeviceCalendar : ICalendar { }  // BoardDateRule.From(端末の現在時刻)
public sealed class FixedCalendar : ICalendar { }   // テスト
```

### 規則と時計を分ける理由（2026-09-06）

**もとの IF は `ICalendar.BoardDate` プロパティだけで、規則が端末時計と一体だった。**
その形だと **11:59:59 と 12:00:00 を与える手段が無く、CA-1 の境界テスト
（TC-113 / 114）が書けない。**決定論の条件 D2「時間は引数で進む」
（[test_first.md](../00_process/test_first.md) 2 節）にも反する。

**規則を純粋層に出し、端末時計を読む役だけを境界層に残した。**
これで境界の検証は `dotnet test` だけで走る。

→ [decisions_pending.md](../00_process/decisions_pending.md) G-01

## 満たすこと

| # | 性質 | 要件 |
| --- | --- | --- |
| CA-1 | **正午 12:00 が境界。**夜に日付境界が来ない | REQ-019 |
| CA-2 | 形式は `yyyy-MM-dd`。タイムゾーンは端末ローカル | REQ-019 |
| CA-3 | **プレイ中の日付変更を無視する。**盤面は開始時に確定させる | REQ-036 |
| CA-4 | 過去の日を指定して遊ぶ入口をアプリが持たない | REQ-022 |
| CA-5 | `FixedCalendar` に差し替えられる | ADR-0002 D3 |
| CA-6 | **`BoardDateRule` が `UnityEngine` にも端末時計にも触らない。**引数の時刻だけで決まる | NFR-004 / D2 |

**CA-1 の境界テスト**: 11:59:59 と 12:00:00 で `BoardDate` が変わること。
**端末の時計操作は防がない**（REQ-022。通信を持たない以上、原理的に不可）。

## エラー時

| 状況 | 振る舞い |
| --- | --- |
| 端末の時刻が取れない | 例外にせず、**前回保存した `BoardDate` を使う**。取れなければ起動を止めない |

## 決めていないこと

なし。

> **2026-09-06 に公開 IF を変えた。**`BoardDateRule` を純粋層に出した（上記）。
> `ICalendar` の役割は端末時計を読むことだけになった。
