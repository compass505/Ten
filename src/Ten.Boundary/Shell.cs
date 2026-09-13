using System;
using Ten.Pure;

namespace Ten.Boundary;

/// <summary>画面（screens.md 2 節）。</summary>
public enum Screen { Boot, Tutorial, Home, Night, Result, Recover }

/// <summary>画面を動かす契機（screens.md 2 節の遷移表 T-04〜T-13）。</summary>
public enum ShellEvent
{
    /// <summary>T-04: チュートリアル完了</summary>
    TutorialDone,

    /// <summary>T-05: 初期化を実行</summary>
    Recovered,

    /// <summary>T-06: 遊ぶ（新規。プレイ回数 +1）</summary>
    Play,

    /// <summary>T-07: 続きから（復元。回数は増やさない）</summary>
    Resume,

    /// <summary>T-08: やり直す（放棄。**放棄した回も数える**）</summary>
    Restart,

    /// <summary>T-09 / T-13: 共有</summary>
    Share,

    /// <summary>T-10: 夜が終わる</summary>
    Finish,

    /// <summary>T-11: 中断中に放棄</summary>
    Abandon,

    /// <summary>T-12: 結果を閉じる</summary>
    CloseResult,
}

/// <summary>戻る操作の行き先（screens.md 3 節）。</summary>
public enum BackAction
{
    /// <summary>受け付けない（行き止まりにしない代わりに、出口を作らない画面）</summary>
    Ignore,

    /// <summary>アプリを終了する</summary>
    ExitApp,

    /// <summary>中断する（`ST-N-Pause` へ）。夜からは直接出ない</summary>
    Pause,

    /// <summary>別の画面へ</summary>
    GoHome,
}

/// <summary>起動時に分かっていること（screens.md T-01〜T-03 / MOD-Shell のエラー時）。</summary>
public readonly record struct BootState(
    bool TutorialDone,
    LoadStatus DeviceStatus,
    LoadStatus TodayStatus,
    LoadStatus RunStatus,
    bool BoardSpecChanged
);

/// <summary>
/// **画面遷移の規則。**端末にも Unity にも触らない。
///
/// 仕様: docs/20_basic_design/screens.md 2 節（T-01〜T-13）と 3 節（戻る操作）/
/// docs/30_detailed_design/MOD-Shell.md（SL-1〜SL-9）
///
/// **これはシグネチャだけの空実装である**（test_first.md 5.1）。
/// </summary>
public static class ShellRule
{

    /// <summary>
    /// 起動直後の行き先（T-01〜T-03）。
    ///
    /// **盤面仕様の版が変わっただけなら `Recover` を通さない**（破損ではない。TC-144）。
    /// </summary>
    public static Screen Boot(BootState state)
    {
        if (state.DeviceStatus != LoadStatus.Ok) return Screen.Recover;
        if (state.TodayStatus == LoadStatus.Corrupt || state.RunStatus == LoadStatus.Corrupt) return state.RunStatus == LoadStatus.Corrupt ? Screen.Home : Screen.Recover;
        return state.TutorialDone ? Screen.Home : Screen.Tutorial;
    }

    /// <summary>
    /// 契機による遷移（T-04〜T-13）。**遷移表に無い組み合わせは例外。**
    /// 黙って同じ画面を返すと、未定義の遷移が起きても気づけない（SL-1）。
    /// </summary>
    public static Screen Next(Screen from, ShellEvent e)
    {
        if ((from == Screen.Tutorial && e == ShellEvent.TutorialDone) || (from == Screen.Recover && e == ShellEvent.Recovered) || (from == Screen.Home && (e == ShellEvent.Play || e == ShellEvent.Resume || e == ShellEvent.Restart)) || (from == Screen.Home && e == ShellEvent.Share) || (from == Screen.Night && e == ShellEvent.Finish) || (from == Screen.Night && e == ShellEvent.Abandon) || (from == Screen.Result && e == ShellEvent.CloseResult) || (from == Screen.Result && e == ShellEvent.Share))
            return from == Screen.Tutorial || from == Screen.Recover ? Screen.Home : e == ShellEvent.Finish ? Screen.Result : e == ShellEvent.Abandon || e == ShellEvent.CloseResult ? Screen.Home : e == ShellEvent.Share ? from : Screen.Night;
        throw new InvalidOperationException("未定義の画面遷移");
    }

    /// <summary>戻る操作の行き先（screens.md 3 節）。**全画面について定まっている**（SL-2）。</summary>
    public static BackAction Back(Screen from) { switch (from) { case Screen.Home: return BackAction.ExitApp; case Screen.Night: return BackAction.Pause; case Screen.Result: return BackAction.GoHome; default: return BackAction.Ignore; } }

    /// <summary>
    /// その契機がプレイ回数を増やすか（REQ-024 / 031 / 037 / 048）。
    ///
    /// **`Play` と `Restart` は数える。`Resume` は数えない**（SL-5 / T-06〜T-08）。
    /// </summary>
    public static bool CountsAsPlay(ShellEvent e) => e == ShellEvent.Play || e == ShellEvent.Restart;
}

/// <summary>
/// MOD-Shell — アプリのライフサイクル。
///
/// **判断は <see cref="ShellRule"/> にある。**ここは口。
/// 実体（Unity のライフサイクルに載せる部分）は Unity プロジェクト側。
/// </summary>
public interface IShell
{
    Screen Current { get; }

    void Boot();
    void StartPlay();
    void ResumePlay();
    void RestartPlay();
    void AbandonPlay();
    void FinishPlay(EndKind kind);
    void CloseResult();
    void OnFocusLost();
    void OnBackPressed();
}
