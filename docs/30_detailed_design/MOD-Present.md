# MOD-Present — 状態 → 見せ方（アセットの差し込み口）

種別: リファレンス / 層: **境界層**（`src/Ten.Boundary/Present.cs`）
更新トリガー: 見せる動き・小物が増減したとき / Codex のアニメーション名が変わったとき /
閉眼中に出すものを変えたとき
対応要件: REQ-004 / 006 / 013 / 016 / 030 / 044 / 045 / 055 / 061

出典: [setting.md](../20_basic_design/setting.md) 4〜8 節（何を見せるか） /
[presentation.md](../20_basic_design/presentation.md) 2 節（アセットへの差し込み方） /
[ADR-0014](../10_requirements/decisions/ADR-0014-closed-eyes-information.md) /
[ADR-0017](../10_requirements/decisions/ADR-0017-parent-moves-on-failure.md) /
[ADR-0022](../10_requirements/decisions/ADR-0022-single-carry-motion.md)

> **なぜ別モジュールにしたか（2026-09-13）。**`MOD-View` の中で状態から見た目を決めると、
> REQ-016（寝たふりの成否が漏れない）を守れているかは **Unity で撮るまで分からない**（TC-125）。
> 「何を描くか」を境界層の純関数に出したので、**`dotnet test` で秒で見張れる**（TC-170 / 171）。
> `MOD-View` は受け取った `Presentation` を寝室に写すだけになる。

## 公開 IF

```csharp
public static class PresentRule {
    /// 状態と段階から、そのフレームに何を見せるか。**純関数。判断をしない**
    public static Presentation Of(NightState s, Display.Stages stages, BoardSpec board, Tuning tuning);
}

public readonly record struct Presentation(
    bool EyesClosed,
    BodyClip Body, HandClip Hand, CareKind? Reaching,
    bool Sniff, bool LampOn, bool PhoneFlash, bool PartnerHere,
    int Window,          // 0〜5。5 は夜明け直前（端点）
    int Vignette,        // 0〜2。視界の周辺の沈み
    bool CameraLifted, bool BottleInFace,
    BabyMotion Baby, int BabyStrengthMilli,
    int CarryTick);

public enum BodyClip { Hidden, BreathDeep, BreathLight, BreathHalf, BreathAwake, DozeWarn, DozeDrop, TurnAway, Carry }
public enum HandClip { Rest, Reach, PatSteady, PatRough, PatStall, CareMilk, CareHold, CareDiaper }
public enum BabyMotion { Breathe, Charge, Cry, Fuss, Kick }
```

## 満たすこと

| # | 性質 | 要件 | TC |
| --- | --- | --- | --- |
| PR-1 | **閉眼中に出すのは「窓の明るさ」と「運ばれている」だけ。**覚醒度・山札・出来事・元気・予告中の対処のどれを変えても、見せ方が 1 ビットも変わらない | REQ-004 / 016 / ADR-0014 | TC-170 |
| PR-2 | **`Settling` と `Feint` は同じ見せ方**（`Body = Carry`、同じ `CarryTick`） | REQ-016 / ADR-0017 / 0022 | TC-171 |
| PR-3 | `Up` では母が映らず（`Body = Hidden`）、**天井の電気が点く** | REQ-030 / setting.md 4.4 | TC-172 |
| PR-4 | **今している対処 4 種**と、**次に来る対処 4 種**（手の予告）が区別できる。トントンの手つきが山札の段階 3 つで変わる | REQ-006 / 013 / 044 / 046 | TC-173 |
| PR-5 | **覚醒度の 4 段階が別の姿勢**になる。寝返り中は覚醒度を表す姿勢を出さない | REQ-044 / 045 / MOD-Display D-6 | TC-174 |
| PR-6 | その夜の出来事（スマホ・オムツ・もう一人の親・空腹）が見える | REQ-055 / setting.md 6 節 | TC-175 |
| PR-7 | **判断をしない。**`NightState` を返さず、ゲームの結果に影響する値を持たない | architecture.md | 構造（戻り値の型） |

## 写像（開眼中）

**優先順は上から。**表の値は `presentation.md` 2.1 の Codex アニメーション名と 1 対 1。

### 体（`Body`）

| 条件 | `Body` | setting.md |
| --- | --- | --- |
| 親が `Up` | `Hidden` | 4.4 |
| 寝返り中（`RollUntil > Tick`） | `TurnAway` | 6 節 |
| 親が `Grace`（寝入りばな） | `DozeDrop` | 4.2 / balance.md 15 節 |
| 親が `Sleeping`・予告なし・寝入りばなの直前 `DozeWarnTicks` | `DozeWarn` | 4.2 |
| 覚醒度の段階 0 / 1 / 2 / 3 | `BreathDeep` / `BreathLight` / `BreathHalf` / `BreathAwake` | 4.1 |

### 手（`Hand`）

| 条件 | `Hand` | 備考 |
| --- | --- | --- |
| 親が `Up` | `Rest` | 映らない |
| 対処中: トントン | 山札の段階 2 / 1 / 0 → `PatSteady` / `PatRough` / `PatStall` | 4.3 |
| 対処中: ミルク / 抱っこ / オムツ | `CareMilk`（`BottleInFace`）/ `CareHold`（`CameraLifted`）/ `CareDiaper` | 5 節 |
| 予告中（`PendingCare`） | `Reach`。**`Reaching` に何が来るか** | screens.md 4.2.1 |
| それ以外 | `Rest` | |

### 部屋

| 値 | 条件 |
| --- | --- |
| `LampOn` | 親が `Up`、または山札が終盤の段階（段階 0） |
| `PhoneFlash` | 出来事「スマホ」の発生から `PhoneFlashTicks` |
| `Sniff` | 出来事「オムツ」の発生から `SniffTicks` |
| `PartnerHere` | `NightState.PartnerHere` |
| `Window` | 残り時間の段階 0〜4。夜明け直前なら 5 |
| `Vignette` | 空腹中なら +1、元気の段階 0 なら +1 |
| `Baby` | 溜め中 `Charge` / 行動中 `Cry`・`Fuss`・`Kick` / それ以外 `Breathe`（強さは元気の段階） |

## 見せ方の値

**バランス値ではない**（ゲームの結果を変えない）。setting.md の秒数を tick にしたもの。

| 定数 | 値 | 出典 |
| --- | --- | --- |
| `DozeWarnTicks` | 90（4.5 秒） | setting.md 4.2「予兆 4.5 秒」 |
| `SniffTicks` | 40（2 秒） | setting.md 6 節 |
| `PhoneFlashTicks` | 16（0.8 秒） | setting.md 6 節「一瞬だけ」 |

## エラー時

| 状況 | 振る舞い |
| --- | --- |
| 段階が -1（見えない） | 覚醒度の姿勢を出さない（`Hidden`）。**推測して埋めない**（TC-130 と同じ線） |
| 盤面に出来事が無い | `PhoneFlash` / `Sniff` は常に false |

## 決めていないこと

| ID | 論点 |
| --- | --- |
| PRS-01 | **予告中の手の形を 4 種に分ける素材が r57 に無い**（`Reach` が 1 本だけ）。仮の姿では「手のひら / 小さな哺乳瓶 / 両手 / 布」で分けている → [presentation.md](../20_basic_design/presentation.md) 6 節 PRE-04 |
| PRS-02 | **着地直後の `Grace` と、自然な寝入りばなの `Grace` を同じ `DozeDrop` で見せている。**状態の型に区別が無い（balance.md 15 節で統合した）。着地直後は「まだ手が離れていない」（setting.md 4.4）とずれる |
| PRS-03 | **天井灯の点灯条件が setting.md の中で 2 通りある**（7 節「残り 3 枚以下」/ 4.3 節「終盤 1〜2 枚」）。段階表示と同じ線（段階 0 = 1〜2 枚）に揃えた → PRE-03 |
