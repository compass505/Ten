# MOD-Shell — アプリのライフサイクルと画面遷移

種別: リファレンス / 層: **表示層**
更新トリガー: 画面が増減したとき / 遷移を変えたとき
対応要件: REQ-009 / 010 / 022 / 024 / 036 / 037 / NFR-001 / 008

出典: [screens.md](../20_basic_design/screens.md) 1〜3 節（画面と遷移）

## 公開 IF

```csharp
public interface IShell {
    Screen Current { get; }
    void Boot();                       // SCR-Boot: 保存の読み込みと日付判定
    void StartPlay();                  // T-06: 新規（プレイ回数 +1）
    void ResumePlay();                 // T-07: 復元（回数は増やさない）
    void RestartPlay();                // T-08: 放棄してやり直す（放棄も回数に数える）
    void AbandonPlay();                // T-11: 中断中に放棄 → SCR-Home
    void FinishPlay(EndKind kind);     // T-10: 夜が終わった → SCR-Result
    void CloseResult();                // T-12: 最高成績を判定してから SCR-Home
    void OnFocusLost();                // REQ-009: 時間を止めて保存
    void OnBackPressed();              // screens.md 3 節
}

public enum Screen { Boot, Tutorial, Home, Night, Result, Recover }
```

## 満たすこと

| # | 性質 | 要件 |
| --- | --- | --- |
| SL-1 | [screens.md](../20_basic_design/screens.md) の遷移表 T-01〜T-13 と 1 対 1 | — |
| SL-2 | 戻る操作の行き先が全画面で定義されている（3 節） | — |
| SL-3 | **フォーカス喪失で時間を止め、その tick の状態を保存する** | REQ-009 / 010 |
| SL-4 | プロセス破棄後も、次回起動で続きから再開できる | REQ-010 |
| SL-5 | 放棄したプレイもプレイ回数に数える | REQ-037 |
| SL-6 | **再開を自動化しない。**`SCR-Home` で明示的に選ばせる（D-10） | REQ-010 / 037 |
| SL-7 | 日付をまたいで再開したプレイは完走できるが、前の日の最高成績には入らない（D-08） | REQ-025 / 036 |
| SL-8 | **コールドスタートから操作受付まで 3 秒以内** | NFR-008 |
| SL-9 | 各 tick で `Sim.Advance` → `Score.Apply` → `NightEnd.Evaluate` の順に呼ぶ | REQ-051 |

**SL-9 が唯一「順序を型で守れない」場所。**呼び出し順を間違えると REQ-051 が壊れる。
**この順序をテストで直接検証する**（[MOD-End](MOD-End.md) E-1 の反例）。

## エラー時

| 状況 | 振る舞い |
| --- | --- |
| `Run` だけ壊れている | `Run` を捨てて `SCR-Home`。その日の記録は残す |
| `Today` が壊れている | `Today` + `Run` を捨てて `SCR-Recover` → `SCR-Home` |
| `Device` / schema が壊れている | 全部捨てて `SCR-Recover` → `SCR-Tutorial` |
| 盤面仕様の版が違う | `Today` + `Run` を捨てて `SCR-Home`（**破損ではないので Recover を通さない**） |

## 決めていないこと

| ID | 論点 |
| --- | --- |
| SHL-01 | `SCR-Boot` の 3 秒（NFR-008）に、Unity の初期化がどれだけ乗るか。**実機で測るまで分からない** |
