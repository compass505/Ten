using System;
using System.Collections.Generic;

namespace Ten.Boundary;

/// <summary>
/// MOD-Share — 結果テキストのコピー / 共有。
///
/// 仕様: docs/30_detailed_design/MOD-Share.md（SH-1〜SH-4）/ REQ-027 / NFR-002 / 003
///
/// **通信を行わない。**OS の共有機構に文字列を渡すだけ（SH-1）。
/// </summary>
public interface IShare
{
    void CopyToClipboard(string text);

    /// <summary>Android の ACTION_SEND。**相手アプリは OS の選択画面で選ばせる**（SH-2）。</summary>
    void ShareIntent(string text);

    bool IsAvailable { get; }
}

/// <summary>何をしたか。</summary>
public enum ShareAction { Intent, Copy, None }

/// <summary>
/// **共有の振る舞いを決める規則。**端末にも Unity にも触らない。
///
/// **これはシグネチャだけの空実装である**（test_first.md 5.1）。
/// </summary>
public static class ShareRule
{

    /// <summary>
    /// 共有先が無い / Intent が失敗したら**コピーにフォールバックする**
    /// （MOD-Share のエラー時）。クリップボードも使えなければ何もしない。
    /// **どの場合も例外を投げない。**画面は `SCR-Result` のまま。
    /// </summary>
    public static ShareAction Decide(bool shareAvailable, bool clipboardAvailable)
    { return shareAvailable ? ShareAction.Intent : clipboardAvailable ? ShareAction.Copy : ShareAction.None; }
}

/// <summary>
/// テスト用（SH-4 / ADR-0002 D3）。**呼ばれたことだけ記録する。**
/// </summary>
public sealed class NullShare : IShare
{
    private readonly List<string> _copied = new();
    private readonly List<string> _shared = new();

    public NullShare(bool available = true) => IsAvailable = available;

    public bool IsAvailable { get; }

    public IReadOnlyList<string> Copied => _copied;

    public IReadOnlyList<string> Shared => _shared;

    public void CopyToClipboard(string text) => _copied.Add(text);

    public void ShareIntent(string text) => _shared.Add(text);
}
