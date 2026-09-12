using System;
using System.Linq;
using NUnit.Framework;
using Ten.Pure;

namespace Ten.Tests.Harness;

/// <summary>
/// TC-050〜060 — 寝たふり（docs/40_test/cases/TC-pure-sim.md）。
///
/// 出典: ADR-0013（閉眼中にだけ成立する）/ ADR-0014（閉眼中に分かるのは窓の明るさだけ）/
/// **ADR-0017（失敗しても親は動く）**
/// </summary>
[TestFixture]
public sealed class PretendTests
{
    private static BoardSpec BoardOf(string seed) => SimProbe.BoardOf(seed);

    private static BoardSpec Board => BoardOf(SimProbe.Seeds(1).Single());

    /// <summary>寝たふりの判定が 1 回走るまで、目を閉じて待つ。</summary>
    private static NightState UntilJudged(NightState s, BoardSpec board)
    {
        var closed = SimProbe.CloseEyes(s, board);
        var before = closed.PretendN;

        return SimProbe.RunUntil(closed, board, x => x.PretendN > before || x.Over is not null,
            what: "寝たふりの判定");
    }

    [Test]
    public void TC050_目を開けたまま待っても寝たふりは成立しない()
    {
        var board = Board;
        var s = SimProbe.RunTicks(SimProbe.Begin(board), board, 600);

        Assert.Multiple(() =>
        {
            Assert.That(s.Parent, Is.Not.EqualTo(ParentPhase.Settling),
                "**開眼のまま寝たふりが成立した**（REQ-015 / D-02 / ADR-0013）");
            Assert.That(s.Parent, Is.Not.EqualTo(ParentPhase.Feint),
                "失敗側にも入ってはいけない。判定そのものが走らない");
            Assert.That(s.PretendN, Is.Zero, "判定が走っている");
        });
    }

    [Test]
    public void TC051_閉眼して閾値まで待つと判定が1回だけ走る()
    {
        var board = Board;
        var judged = UntilJudged(SimProbe.Begin(board), board);

        Assert.That(judged.PretendN, Is.EqualTo(1),
            "**判定が走らない、または一度に複数回走っている**（REQ-015）");
    }

    [Test]
    public void TC052_成功しても失敗しても観測できるものに差が出ない()
    {
        // 成功する夜と失敗する夜を、シードを振って両方見つける
        NightState? success = null;
        NightState? failure = null;

        // **山札の枚数は日替わり**（REQ-019）なので、盤面をまたいで枚数そのものは比べられない。
        // 比べるのは「その盤面の初期枚数から減っているか」（ADR-0020）
        var successSpent = 0;
        var failureSpent = 0;

        foreach (var seed in SimProbe.Seeds(40))
        {
            var board = BoardOf(seed);
            var judged = UntilJudged(SimProbe.Begin(board), board);

            if (judged.Parent == ParentPhase.Settling && success is null)
            {
                success = judged;
                successSpent = board.Hand.Total - judged.Hand.Total;
            }
            else if (judged.Parent == ParentPhase.Feint && failure is null)
            {
                failure = judged;
                failureSpent = board.Hand.Total - judged.Hand.Total;
            }

            if (success is not null && failure is not null)
            {
                break;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.Not.Null, "成功する夜が見つからない（REQ-017 の下限 > 0 が怪しい）");
            Assert.That(failure, Is.Not.Null,
                "**失敗する夜が見つからない。**失敗が起きないなら賭けが成立しない");
        });

        var a = success!.Value;
        var b = failure!.Value;

        // 観測できるもの: 閉眼中に分かるのは窓の明るさだけ（ADR-0014）。
        // 山札・元気・行動の可否は開眼すれば見えるので、ここも一致していなければならない
        Assert.Multiple(() =>
        {
            Assert.That(failureSpent, Is.EqualTo(successSpent),
                "**山札の減り方が違う。**そこから成否が逆算できる（ADR-0017）");
            Assert.That(b.TSettle, Is.EqualTo(a.TSettle),
                "**盲目区間の長さが違う。**長さが成否の通知になる");
            Assert.That(b.DozeN, Is.EqualTo(a.DozeN),
                "**寝落ち判定の回数が違う。**寝落ちの起こりやすさが成否を漏らす");
            Assert.That(b.Baby, Is.EqualTo(a.Baby), "どちらも閉眼のまま（I-9）");
        });
    }

    [Test]
    public void TC053_寝たふりの成功率はn回目ほど下がり下限を持つ()
    {
        const int seeds = 60;      // 統計を取るためのシード数。判定基準であって balance ではない
        const int maxN = 6;        // TC-053 が定めた n = 1..6

        var rates = new double[maxN];

        for (var n = 1; n <= maxN; n++)
        {
            var success = 0;

            foreach (var seed in SimProbe.Seeds(seeds))
            {
                var board = BoardOf(seed);
                var s = SimProbe.Begin(board);

                // n 回目の判定まで、閉眼と開眼を繰り返す
                for (var i = 0; i < n && s.Over is null; i++)
                {
                    s = UntilJudged(s, board);

                    if (i < n - 1)
                    {
                        s = SimProbe.OpenEyes(s, board);
                        s = SimProbe.RunTicks(s, board, 1);
                    }
                }

                if (s.Parent == ParentPhase.Settling)
                {
                    success++;
                }
            }

            rates[n - 1] = (double)success / seeds;
        }

        for (var i = 1; i < maxN; i++)
        {
            Assert.That(rates[i], Is.LessThanOrEqualTo(rates[i - 1] + 0.001),
                $"**成功率が上がった**（REQ-017）。n={i} で {rates[i - 1]:P1}、n={i + 1} で {rates[i]:P1}");
        }

        Assert.That(rates[maxN - 1], Is.GreaterThan(0),
            "**成功率の下限が 0 になっている**（REQ-017）。" +
            "0 まで落ちると、そこから先は寝たふりを狙う意味が消える");
    }

    [Test]
    public void TC054_寝たふり中に寝落ちするとその時点で夜が終わる()
    {
        NightState? fellAsleep = null;

        foreach (var seed in SimProbe.Seeds(60))
        {
            var board = BoardOf(seed);
            var judged = UntilJudged(SimProbe.Begin(board), board);

            if (judged.Over == EndKind.FellAsleep)
            {
                fellAsleep = judged;
                break;
            }
        }

        Assert.That(fellAsleep, Is.Not.Null,
            "**寝たふり中に寝落ちする夜が 60 シードで 1 つも無い**（REQ-018）");

        Assert.That(fellAsleep!.Value.Over, Is.EqualTo(EndKind.FellAsleep));
    }

    [Test]
    public void TC055_親が寝ていないときは判定が走らない()
    {
        var board = Board;

        // **`ST-P-Up` は「完全に覚醒してベッドを出ている」状態**（screens.md 4.2）。
        // 覚醒度を初期値のままにすると REQ-030 で 1 tick でベッドへ戻り、前提が崩れる（ADR-0020）
        foreach (var phase in new[] { ParentPhase.Caring, ParentPhase.Up })
        {
            var s = SimProbe.Begin(board) with { Parent = phase, Arousal = phase == ParentPhase.Up ? 100 : 0 };
            var closed = SimProbe.CloseEyes(s, board);
            var later = SimProbe.RunTicks(closed, board, 300);

            Assert.That(later.PretendN, Is.Zero,
                $"**Parent = {phase} でも判定が走った**（D-04）。走る唯一の状態は Sleeping");
        }
    }

    [Test]
    public void TC056_着地前に目を開けると気づかれて山札が1枚減る()
    {
        var (board, settling) = FindPhase(ParentPhase.Settling);

        var before = settling.Hand.Total;
        var opened = SimProbe.OpenEyes(settling, board);
        var after = SimProbe.RunTicks(opened, board, 1);

        Assert.Multiple(() =>
        {
            Assert.That(after.Parent, Is.EqualTo(ParentPhase.Caring),
                "**着地前の開眼で気づかれていない**（REQ-015 / D-06）");
            Assert.That(after.Hand.Total, Is.EqualTo(before - 1),
                "**早く開けすぎた代償が山札に出ていない**（D-06）");
        });
    }

    /// <summary>
    /// TC-164 — 成功側（`Settling`）と失敗側（`Feint`）の判別不能性（ADR-0017 / REQ-016）。
    ///
    /// 見るのは 4 点（docs/40_test/cases/TC-view-shell.md）。
    /// **1 つでも食い違えば、そこから寝たふりの成否が逆算できる。**
    /// </summary>
    [Test]
    public void TC164_成功側と失敗側で観測できるものが完全に一致する()
    {
        var (settleBoard, settling) = FindPhase(ParentPhase.Settling);
        var (feintBoard, feint) = FindPhase(ParentPhase.Feint);

        // (1) 区間の長さ
        var settleLen = PhaseLength(settleBoard, settling, ParentPhase.Settling);
        var feintLen = PhaseLength(feintBoard, feint, ParentPhase.Feint);

        Assert.That(feintLen, Is.EqualTo(settleLen),
            $"**(1) 区間の長さが違う**（成功 {settleLen} tick / 失敗 {feintLen} tick）。" +
            "長さそのものが成否の通知になる");

        // (2) 途中で開眼したときの結果 (3) 山札の消費
        var (settleParent, settleSpent) = OpenMidway(settleBoard, settling);
        var (feintParent, feintSpent) = OpenMidway(feintBoard, feint);

        Assert.Multiple(() =>
        {
            Assert.That(feintParent, Is.EqualTo(settleParent),
                $"**(2) 開眼したときの遷移先が違う**（成功 {settleParent} / 失敗 {feintParent}）");
            Assert.That(feintSpent, Is.EqualTo(settleSpent),
                $"**(3) 山札の減り方が違う**（成功 {settleSpent} 枚 / 失敗 {feintSpent} 枚）。" +
                "山札の減り方から逆算できる");
        });

        // (4) DozeOff が両方で止まる → TC-060 が 3 状態すべてを見ている
    }

    private static int PhaseLength(BoardSpec board, NightState s, ParentPhase phase)
    {
        var start = s.Tick;
        var done = SimProbe.RunUntil(s, board, x => x.Parent != phase, what: $"{phase} の満了");

        return done.Tick - start;
    }

    /// <summary>区間の途中で開眼したときの (遷移先, 減った山札の枚数)。</summary>
    private static (ParentPhase Parent, int Spent) OpenMidway(BoardSpec board, NightState s)
    {
        var before = s.Hand.Total;
        var after = SimProbe.RunTicks(SimProbe.OpenEyes(s, board), board, 1);

        return (after.Parent, before - after.Hand.Total);
    }

    [Test]
    public void TC057_猶予中に泣くとその夜のどの行動より大きく上がる()
    {
        var (board, grace) = FindPhase(ParentPhase.Grace);

        var special = SimProbe.ArousalGain(SimProbe.OpenEyes(grace, board), board, ActionKind.Cry, 1);

        // 同じ状態から、猶予に乗っていない場合の上昇量と比べる
        var plain = grace with { Parent = ParentPhase.Sleeping, TGrace = 0 };

        foreach (var kind in SimProbe.AllActions)
        {
            var normal = SimProbe.ArousalGain(plain, board, kind, 1);

            Assert.That(special, Is.GreaterThan(normal),
                $"**猶予中の一撃が {kind} より大きくない**（REQ-015）。" +
                $"猶予 +{special} / 通常 +{normal}");
        }
    }

    [Test]
    public void TC058_猶予中に泣かなければ親は寝たままに戻る()
    {
        var (board, grace) = FindPhase(ParentPhase.Grace);

        var after = SimProbe.RunUntil(grace, board,
            x => x.Parent != ParentPhase.Grace, what: "猶予の満了");

        Assert.That(after.Parent, Is.EqualTo(ParentPhase.Sleeping), "REQ-015");
    }

    [Test]
    public void TC059_着地までの長さは固定tickで満了する()
    {
        var board = Board;

        // 2 つの異なる夜で、Settling に入ってから満了するまでの tick 数が一致する
        var lengths = new System.Collections.Generic.List<int>();

        foreach (var seed in SimProbe.Seeds(40))
        {
            var b = BoardOf(seed);
            var judged = UntilJudged(SimProbe.Begin(b), b);

            if (judged.Parent != ParentPhase.Settling)
            {
                continue;
            }

            var start = judged.Tick;
            var done = SimProbe.RunUntil(judged, b,
                x => x.Parent != ParentPhase.Settling, what: "着地");

            lengths.Add(done.Tick - start);

            if (lengths.Count >= 3)
            {
                break;
            }
        }

        Assert.That(lengths, Has.Count.GreaterThanOrEqualTo(2), "成功する夜が足りない");
        Assert.That(lengths.Distinct().Count(), Is.EqualTo(1),
            $"**着地までの長さがばらついている**（D-07）。実測 {string.Join(" / ", lengths)} tick。" +
            "演出の終了で遷移させると、フレームレートや中断で状態列が変わる（NFR-004 / 005）");
    }

    [Test]
    public void TC060_盲目区間と猶予中は寝落ち判定が走らない()
    {
        foreach (var phase in new[] { ParentPhase.Settling, ParentPhase.Feint, ParentPhase.Grace })
        {
            var (board, s) = FindPhase(phase);
            var before = s.DozeN;
            var later = SimProbe.RunUntil(s, board, x => x.Parent != phase, what: $"{phase} の満了");

            Assert.That(later.DozeN, Is.EqualTo(before),
                $"**{phase} 中に DozeOff が走った**（D-05）。" +
                "Settling で止まり Feint で走ると、寝落ちの有無から寝たふりの成否が逆算できる（ADR-0017）");
        }
    }

    /// <summary>
    /// その親の状態に入る夜を、シードを振って探す。
    /// **盤面も一緒に返す。**状態だけ返すと、どの盤面で作った状態か分からなくなる
    /// （`NightState` は seed を持たない）。
    /// </summary>
    private static (BoardSpec Board, NightState State) FindPhase(ParentPhase phase)
    {
        foreach (var seed in SimProbe.Seeds(60))
        {
            var board = BoardOf(seed);
            var s = SimProbe.Begin(board);

            for (var i = 0; i < 8 && s.Over is null; i++)
            {
                s = UntilJudged(s, board);

                if (s.Parent == phase)
                {
                    return (board, s);
                }

                if (phase == ParentPhase.Grace && s.Parent == ParentPhase.Settling)
                {
                    var landed = SimProbe.RunUntil(s, board,
                        x => x.Parent != ParentPhase.Settling, what: "着地");

                    if (landed.Parent == ParentPhase.Grace)
                    {
                        return (board, landed);
                    }

                    s = landed;
                }

                s = SimProbe.RunTicks(SimProbe.OpenEyes(s, board), board, 1);
            }
        }

        throw new InvalidOperationException(
            $"Parent = {phase} に入る夜が 60 シードで見つからない。前提が作れていない");
    }
}
