# MOD-Display — 状態から見た目の段階への写像

種別: リファレンス / 層: **純粋層**
更新トリガー: 段階の数や境界を変えたとき / 見せる状態が増減したとき
対応要件: REQ-006 / 008 / 016 / 044 / 045 / 058

出典: [ADR-0010](../10_requirements/decisions/ADR-0010-no-numbers.md) /
[ADR-0014](../10_requirements/decisions/ADR-0014-closed-eyes-information.md)（**Proposed**） /
[setting.md](../20_basic_design/setting.md)（各段階の見た目） /
[balance.md](../20_basic_design/balance.md) 8 節（境界値）

## 公開 IF

```csharp
public static class Display {
    /// 状態 → 見た目の段階。**純関数。毎 tick 導出する（保存しない）**
    public static Stages Map(NightState s, BoardSpec board, Tuning tuning);

    public readonly record struct Stages(
        int Arousal,      // 0〜3（4 段階）。**見えないときは -1**
        int Vigor,        // 0〜2（3 段階）。自分の体なので常に見える
        int Hand,         // 0〜2（3 段階）。見えないときは -1
        int TimeLeft,     // 0〜4（5 段階）。見えないときは -1
        bool ArousalExact,   // 端点（上限に達している）
        bool HandExact,      // 端点（空）
        bool DawnImminent    // 端点（夜明け直前）
    );
}
```

## 満たすこと

| # | 性質 | 要件 |
| --- | --- | --- |
| D-1 | **段階と実際の値の対応は固定。**同じ値からは常に同じ段階 | REQ-045 |
| D-2 | 段階は粗い。**段階の中のどこにいるかは分からない** | REQ-045 / 058 |
| D-3 | **端点は正確。**山札が空・覚醒度が上限・夜明け直前は、区間ではなく確定して分かる | REQ-045 |
| D-4 | `Baby == EyesClosed` のとき、**`Arousal` / `Hand` は必ず -1**（窓の明るさだけ） | REQ-004 / 008 / ADR-0014 |
| D-5 | `Parent == Up`（視界からいない）のとき、`Arousal` は -1 | screens.md 4.2 |
| D-6 | 出来事「寝返り」の間、`Arousal` は -1 | REQ-055 |
| D-7 | 純関数。**保存しない**（`data_model.md` 5 節） | REQ-020 |

**視線による遮蔽（D-11）はここでは扱わない。**`Map` は「見えうる段階」を返し、
**どこを見ているかによる遮蔽は `MOD-View` が行う。**
純粋層はカメラの向きを知らない（architecture.md の依存方向）。

## 段階の境界

**値は [balance.md](../20_basic_design/balance.md) 8 節。ここには複製しない。**
見た目の具体（まぶた・口・呼吸の周期・姿勢）は
[setting.md](../20_basic_design/setting.md) 4 節。

## エラー時

| 状況 | 振る舞い |
| --- | --- |
| 状態値が範囲外 | `InvalidOperationException`。**丸めて段階を返さない**（不変条件が破れている証拠） |

## 決めていないこと

| ID | 論点 |
| --- | --- |
| DSP-01 | `Vigor` を視界の揺れで表す（setting.md 8 節）が、これを段階として返すか、連続値のまま `MOD-View` に渡すか |
| DSP-02 | 「明度差だけに依存しない冗長化」（REQ-044）を、段階の番号だけで `MOD-View` に伝えられるか。**冗長化のチャンネル（輪郭・姿勢・周期）を別々に返す必要があるかもしれない** |
