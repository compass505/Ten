# MOD-Share — 結果テキストのコピー / 共有

種別: リファレンス / 層: **境界層**
更新トリガー: 共有の手段を変えたとき
対応要件: REQ-027 / NFR-002 / 003

## 公開 IF

```csharp
public interface IShare {
    void CopyToClipboard(string text);
    /// Android の ACTION_SEND。相手アプリは選ばせる
    void ShareIntent(string text);
    bool IsAvailable { get; }
}

public sealed class AndroidShare : IShare { }
public sealed class NullShare : IShare { }       // テスト。呼ばれたことだけ記録する
```

## 満たすこと

| # | 性質 | 要件 |
| --- | --- | --- |
| SH-1 | **通信を行わない。**OS の共有機構に文字列を渡すだけ | NFR-002 |
| SH-2 | 送り先をアプリ側で決めない。**相手は OS の選択画面で選ぶ** | REQ-027 |
| SH-3 | 権限を要求しない | NFR-003 |
| SH-4 | `NullShare` に差し替えられる | ADR-0002 D3 |

## エラー時

| 状況 | 振る舞い |
| --- | --- |
| 共有先が無い / Intent が失敗 | **コピーにフォールバックする。**画面は `SCR-Result` のまま |
| クリップボードが使えない | 何もしない。例外を投げない |

## 決めていないこと

なし。
