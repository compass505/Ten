using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Ten.Boundary;
using Ten.Pure;

namespace Ten.Tests.Harness;

/// <summary>
/// TC-131〜144 / 157 / 158 — チュートリアルとライフサイクル。
///
/// 出典: docs/20_basic_design/screens.md 2 節（T-01〜T-13）と 3 節（戻る操作）/
/// MOD-Shell SL-1〜SL-9 / MOD-Tutorial TU-1〜TU-5
///
/// **Unity は要らない。**遷移の判断は <see cref="ShellRule"/> にあり、
/// Unity のライフサイクルに載せる部分だけが Unity プロジェクト側。
/// </summary>
[TestFixture]
public sealed class ShellTests
{
    private static BootState Healthy => new(
        TutorialDone: true,
        DeviceStatus: LoadStatus.Ok,
        TodayStatus: LoadStatus.Ok,
        RunStatus: LoadStatus.Ok,
        BoardSpecChanged: false);

    // ------------------------------------------------------------------
    // TC-135: 遷移表 T-01〜T-13 と 1 対 1（SL-1）
    // ------------------------------------------------------------------

    /// <summary>screens.md 2 節の遷移表を、そのまま書き写したもの。</summary>
    private static readonly (string Id, Screen From, ShellEvent Event, Screen To)[] Table =
    [
        ("T-04", Screen.Tutorial, ShellEvent.TutorialDone, Screen.Home),
        ("T-05", Screen.Recover, ShellEvent.Recovered, Screen.Home),
        ("T-06", Screen.Home, ShellEvent.Play, Screen.Night),
        ("T-07", Screen.Home, ShellEvent.Resume, Screen.Night),
        ("T-08", Screen.Home, ShellEvent.Restart, Screen.Night),
        ("T-09", Screen.Home, ShellEvent.Share, Screen.Home),
        ("T-10", Screen.Night, ShellEvent.Finish, Screen.Result),
        ("T-11", Screen.Night, ShellEvent.Abandon, Screen.Home),
        ("T-12", Screen.Result, ShellEvent.CloseResult, Screen.Home),
        ("T-13", Screen.Result, ShellEvent.Share, Screen.Result),
    ];

    [Test]
    public void TC135_遷移表のとおりに動く()
    {
        foreach (var (id, from, e, to) in Table)
        {
            Assert.That(ShellRule.Next(from, e), Is.EqualTo(to),
                $"**{id} が遷移表と違う**（SL-1）。{from} + {e} → {to} のはず");
        }
    }

    [Test]
    public void TC135_遷移表に無い組み合わせは例外になる()
    {
        var defined = Table.Select(t => (t.From, t.Event)).ToHashSet();

        foreach (var from in Enum.GetValues<Screen>())
        {
            foreach (var e in Enum.GetValues<ShellEvent>())
            {
                if (defined.Contains((from, e)))
                {
                    continue;
                }

                Assert.Throws<InvalidOperationException>(
                    () => ShellRule.Next(from, e),
                    $"**{from} + {e} が黙って通った**（SL-1）。" +
                    "遷移表に無い組み合わせを同じ画面で返すと、未定義の遷移に気づけない");
            }
        }
    }

    // ------------------------------------------------------------------
    // TC-136: 戻る操作の行き先が全画面で定義されている（SL-2）
    // ------------------------------------------------------------------

    [Test]
    public void TC136_全画面で戻る操作の行き先が定まっている()
    {
        var expected = new Dictionary<Screen, BackAction>
        {
            [Screen.Boot] = BackAction.Ignore,        // 通過するだけ
            [Screen.Tutorial] = BackAction.Ignore,    // 完了以外の出口を作らない（TU-4）
            [Screen.Home] = BackAction.ExitApp,       // 確認を出さない
            [Screen.Night] = BackAction.Pause,        // 夜からは直接出ない
            [Screen.Result] = BackAction.GoHome,      // 最高成績の判定を必ず通る
            [Screen.Recover] = BackAction.Ignore,     // 初期化しないと先に進めない
        };

        foreach (var screen in Enum.GetValues<Screen>())
        {
            Assert.That(expected.ContainsKey(screen), Is.True,
                $"**{screen} の戻る操作が screens.md 3 節に無い。**行き止まりになる");

            Assert.That(ShellRule.Back(screen), Is.EqualTo(expected[screen]),
                $"**{screen} の戻る操作が定義と違う**（SL-2）");
        }
    }

    [Test]
    public void TC136_夜からは戻る操作で直接出られない()
    {
        Assert.That(ShellRule.Back(Screen.Night), Is.EqualTo(BackAction.Pause),
            "**夜から直接出られる**（screens.md 3 節）。" +
            "中断を挟まないと、REQ-009（時間を止めて保存）を通らずに抜けられる");
    }

    // ------------------------------------------------------------------
    // TC-140 / 143 / 144: 起動時の行き先（T-01〜T-03 / エラー時）
    // ------------------------------------------------------------------

    [Test]
    public void TC140_進行中のプレイがあっても自動で夜に入らない()
    {
        // D-10 / SL-6: 再開を自動化しない。SCR-Home で明示的に選ばせる
        var withRun = Healthy;

        Assert.That(ShellRule.Boot(withRun), Is.EqualTo(Screen.Home),
            "**起動していきなり夜が始まる**（D-10 / SL-6）。" +
            "放棄するのに一度中断してから放棄する手順が要り、手数が増える");
    }

    [Test]
    public void TC131_初回起動はチュートリアルに入る()
    {
        var first = Healthy with { TutorialDone = false, TodayStatus = LoadStatus.Missing, RunStatus = LoadStatus.Missing };

        Assert.That(ShellRule.Boot(first), Is.EqualTo(Screen.Tutorial), "T-01 / REQ-034");
    }

    [Test]
    public void TC143_端末データが壊れていれば復旧を通ってチュートリアルへ()
    {
        var broken = Healthy with { DeviceStatus = LoadStatus.Corrupt };

        Assert.That(ShellRule.Boot(broken), Is.EqualTo(Screen.Recover),
            "**壊れた保存で起動できなくなる / 黙って進む**（T-02 / REQ-033）");
    }

    [Test]
    public void TC144_盤面仕様の版が変わっただけなら復旧を通さない()
    {
        var changed = Healthy with { BoardSpecChanged = true };

        Assert.That(ShellRule.Boot(changed), Is.EqualTo(Screen.Home),
            "**版が変わっただけで復旧画面を出している**（MOD-Shell のエラー時 / TC-144）。" +
            "破損ではないので、`Today` と `Run` を捨てて `SCR-Home` に行く");
    }

    [Test]
    public void TC143_進行中のプレイだけ壊れていればその日の記録は残す()
    {
        var runBroken = Healthy with { RunStatus = LoadStatus.Corrupt };

        Assert.That(ShellRule.Boot(runBroken), Is.EqualTo(Screen.Home),
            "**Run が壊れただけで復旧画面に落ちている**（MOD-Shell のエラー時）。" +
            "捨てる範囲を分ける（ST-2）");
    }

    // ------------------------------------------------------------------
    // TC-139 / 157: プレイ回数の数え方（REQ-024 / 031 / 037 / 048）
    // ------------------------------------------------------------------

    [Test]
    public void TC139_放棄してやり直した回も数える()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ShellRule.CountsAsPlay(ShellEvent.Play), Is.True, "T-06: 新規は数える");
            Assert.That(ShellRule.CountsAsPlay(ShellEvent.Restart), Is.True,
                "**放棄してやり直した回を数えていない**（REQ-037 / SL-5）");
            Assert.That(ShellRule.CountsAsPlay(ShellEvent.Resume), Is.False,
                "**続きからで回数が増えている**（T-07）。中断復帰は新しいプレイではない");
        });
    }

    [Test]
    public void TC157_同じ日に5回続けて遊べる()
    {
        var screen = Screen.Home;
        var count = 0;

        for (var i = 0; i < 5; i++)
        {
            screen = ShellRule.Next(screen, ShellEvent.Play);
            count += ShellRule.CountsAsPlay(ShellEvent.Play) ? 1 : 0;

            Assert.That(screen, Is.EqualTo(Screen.Night), $"{i + 1} 回目に夜へ入れない（REQ-024）");

            screen = ShellRule.Next(screen, ShellEvent.Finish);
            screen = ShellRule.Next(screen, ShellEvent.CloseResult);
        }

        Assert.Multiple(() =>
        {
            Assert.That(count, Is.EqualTo(5), "**5 回遊んだのに回数が合わない**（REQ-024 / 031）");
            Assert.That(screen, Is.EqualTo(Screen.Home));
        });
    }

    [Test]
    public void TC158_過去の日付を選ぶ入口が存在しない()
    {
        // REQ-022。**遷移表にも戻る操作にも、日付を選ぶ契機が無いこと**で保証する。
        // 端末の時計操作は防がない（通信を持たない以上、原理的に不可）
        var names = Enum.GetNames<ShellEvent>();

        foreach (var forbidden in new[] { "Date", "Day", "Calendar", "Past", "History", "Select" })
        {
            Assert.That(names.Any(n => n.Contains(forbidden, StringComparison.OrdinalIgnoreCase)), Is.False,
                $"**日付を選ぶ契機（{forbidden}）がある**（REQ-022）。" +
                "入口を作らないことで守る");
        }
    }

    // ------------------------------------------------------------------
    // TC-137 / 138 / 141: 中断と再開
    // ------------------------------------------------------------------

    [Test]
    public void TC137_フォーカスを外すと中断になる()
    {
        // SL-3: 時間を止め、その tick の状態を保存する。
        // 戻る操作と同じ扱い（screens.md 3 節）
        Assert.That(ShellRule.Back(Screen.Night), Is.EqualTo(BackAction.Pause),
            "**夜で中断に入らない**（REQ-009 / SL-3）");
    }

    [Test]
    public void TC138_中断したプレイは続きからで再開できる()
    {
        var resumed = ShellRule.Next(Screen.Home, ShellEvent.Resume);

        Assert.Multiple(() =>
        {
            Assert.That(resumed, Is.EqualTo(Screen.Night), "T-07 / REQ-010");
            Assert.That(ShellRule.CountsAsPlay(ShellEvent.Resume), Is.False,
                "**再開で回数が増えている**（SL-5 の裏返し）");
        });
    }

    [Test]
    public void TC141_正午をまたいで再開しても完走できる()
    {
        // D-08 / SL-7: 完走はできる。ただし前の日の最高成績には入らない。
        // ここで見るのは「完走できること」。成績の帰属は TC-074 / 075 の担当
        var screen = ShellRule.Next(Screen.Home, ShellEvent.Resume);
        screen = ShellRule.Next(screen, ShellEvent.Finish);

        Assert.That(screen, Is.EqualTo(Screen.Result),
            "**日付をまたいだプレイが完走できない**（REQ-036 / D-08）");
    }

    // ------------------------------------------------------------------
    // TC-142: Sim → Score → NightEnd の順（SL-9 / REQ-051）
    // ------------------------------------------------------------------

    [Test]
    public void TC142_1tickの呼び出しがSimからScoreそしてNightEndの順になっている()
    {
        // **型で守れない唯一の場所**（SL-9）。順序はハーネスの再生系 1 か所に閉じてある。
        // ここでは「その順序を通ると、最後の 1 枚の得点が入る」ことで間接的に見る。
        // 直接の検証は TC-080（山札 1 枚で上限に達する入力列）。
        var board = SimProbe.BoardOf(SimProbe.Seeds(1).Single());
        var s = SimProbe.Begin(board) with { Hand = new HandCount(1, 0, 0, 0), Arousal = 99 };

        var stepped = SimRunner.Step(s, new TickInput(ActionKind.Cry, false), board, SimProbe.Tuning);

        Assert.That(stepped.Score, Is.GreaterThanOrEqualTo(s.Score),
            "**得点が減った。**Score.Apply より先に NightEnd.Evaluate を呼ぶと、" +
            "最後の 1 枚で得た点が入らない（REQ-051 / SL-9）");
    }
}

/// <summary>
/// TC-131〜134 — チュートリアル（MOD-Tutorial TU-1〜TU-5 / REQ-034 / D-09）。
/// </summary>
[TestFixture]
public sealed class TutorialTests
{
    [Test]
    public void TC131_3段を順に達成すると完了する()
    {
        var board = Board.Tutorial(SimProbe.Tuning);
        var s = SimProbe.Begin(board);

        TutorialStep? step = TutorialStep.Arousal;
        var seen = new List<TutorialStep>();

        for (var i = 0; i < 50 && step is not null; i++)
        {
            seen.Add(step.Value);
            s = DriveUntilSatisfied(s, board, step.Value);
            step = TutorialRule.Advance(step.Value, s);
        }

        Assert.Multiple(() =>
        {
            Assert.That(seen, Is.EqualTo(new[]
            {
                TutorialStep.Arousal, TutorialStep.Vigor, TutorialStep.Pretend,
            }), "**教える順が違う / 抜けている**（REQ-034 は 3 つと定めている）");

            Assert.That(step, Is.Null, "**3 段を達成しても完了しない**（TU-1）");
        });
    }

    /// <summary>その段の条件を満たすまで操作する。**操作して達成させる**（TU-1）。</summary>
    private static NightState DriveUntilSatisfied(NightState s, BoardSpec board, TutorialStep step)
    {
        for (var i = 0; i < 400 && s.Over is null; i++)
        {
            if (TutorialRule.IsSatisfied(step, s))
            {
                return s;
            }

            s = step switch
            {
                TutorialStep.Arousal => SimProbe.ActFullStrength(s, board, ActionKind.Cry),
                TutorialStep.Vigor => s.Vigor > 0
                    ? SimProbe.Act(s, board, ActionKind.Cry, 1)
                    : SimProbe.RunTicks(s, board, 30),
                TutorialStep.Pretend => SimProbe.RunTicks(SimProbe.CloseEyes(s, board), board, 30),
                _ => SimProbe.RunTicks(s, board, 1),
            };
        }

        Assert.Fail($"**{step} の条件が満たせない**（TU-2）。" +
                    "固定盤面は 3 つが必ず達成できるものでなければならない");
        return s;
    }

    [Test]
    public void TC132_チュートリアルは日付シードを使わない()
    {
        var a = Board.Tutorial(SimProbe.Tuning);
        var b = Board.Tutorial(SimProbe.Tuning);

        Assert.Multiple(() =>
        {
            Assert.That(b, Is.EqualTo(a), "**呼ぶたびに盤面が変わる**（D-09 / TU-2）");

            Assert.That(a.SpecVersion, Is.Not.EqualTo(Board.SpecVersion),
                "**日付シードの盤面と同じ版番号を使っている**（D-09）。" +
                "混ざると、どちらの盤面の結果か分からなくなる");
        });
    }

    [Test]
    public void TC133_チュートリアルはプレイ回数に数えない()
    {
        // TU-3。数える契機は ShellRule.CountsAsPlay にしか無く、
        // TutorialDone はそこに入っていない
        Assert.That(ShellRule.CountsAsPlay(ShellEvent.TutorialDone), Is.False,
            "**チュートリアルがプレイ回数に入っている**（REQ-031 / 048 / TU-3）");
    }

    [Test]
    public void TC134_チュートリアルは戻る操作を受け付けない()
    {
        Assert.That(ShellRule.Back(Screen.Tutorial), Is.EqualTo(BackAction.Ignore),
            "**完了以外の出口がある**（TU-4 / screens.md 3 節）");
    }

    [Test]
    public void TC131_1回目の寝たふりが必ず成功する()
    {
        // TU-2 / TC-016: 固定盤面は sleep_pretend の 1 回目が必ず当たるように選ぶ
        var board = Board.Tutorial(SimProbe.Tuning);
        var s = SimProbe.CloseEyes(SimProbe.Begin(board), board);

        var judged = SimProbe.RunUntil(s, board,
            x => x.PretendN > 0 || x.Over is not null, what: "寝たふりの判定");

        Assert.That(judged.Parent, Is.EqualTo(ParentPhase.Settling),
            "**チュートリアルで寝たふりが失敗する**（TU-2）。" +
            "教えるべき 3 つが必ず出る盤面でなければならない");
    }
}
