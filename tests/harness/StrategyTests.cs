using System;
using System.Linq;
using NUnit.Framework;
using Ten.Pure;

namespace Ten.Tests.Harness;

/// <summary>
/// TC-068 / 069 — 支配戦略と得点差（docs/40_test/cases/TC-pure-sim.md）。
///
/// **単体テストで示せない。**複数シード × 複数方針を回して、得点の分布で判定する
/// （MOD-Sim の S-11 / S-12。balance.md 12 節と同じ形）。
/// </summary>
[TestFixture]
public sealed class StrategyTests
{
    /// <summary>TC-068 / 069 が定めた 24 シード。</summary>
    private const int SeedCount = 24;

    /// <summary>
    /// **TC-069 の合格線。**
    ///
    /// TC-pure-sim.md が「要件が数値を持たないため、フェーズ 4 で決めてテストに固定する。
    /// これは balance ではなく**テストの判定基準**なので ADR-0012 の対象外」と定めている。
    ///
    /// **0.8 を選んだ理由。**REQ-056 は「どの行動・どの対処も、全ての状態において
    /// 他より優れていることがない」。24 シードで最良方針が 20 勝（83%）を超えたら、
    /// 「とりあえずこれを選べばよい」が成立していると見なす。
    /// 1.0 にすると 1 敗しただけで通ってしまい、何も見張らないテストになる。
    /// </summary>
    private const double MaxWinRate = 0.8;

    /// <summary>方針。**入力列の作り方の違い**であって、バランス値ではない。</summary>
    private enum Policy
    {
        /// <summary>短く連打する</summary>
        Tap,

        /// <summary>最大まで溜めて撃つ（ADR-0015 / REQ-060）</summary>
        Charge,

        /// <summary>寝たふりを狙う（ADR-0013）</summary>
        Pretend,

        /// <summary>溜めと寝たふりを混ぜる</summary>
        Mixed,

        /// <summary>何もしない</summary>
        Idle,

        /// <summary>
        /// **親の寝入りばなの周期を読んで、溜めた一撃だけを当てる**（REQ-061 の予兆は顔から読める）。
        /// 2026-09-13 に追加。これが無いと、48 シードすべてで独走する方針を見張れていなかった（ADR-0023）
        /// </summary>
        GraceSnipe,

        /// <summary>
        /// **溜めて撃ち、親が起きている間は待つ。**GraceSnipe と同じ「起きている親を待つ」を持つ比較対象。
        /// Tap / Charge は起きている親にも撃ち続けるので再加点できず、得点が 1 に張り付く。
        /// それだけと比べると、待つ方針はどれでも独走に見えてしまう（ADR-0023）
        /// </summary>
        ChargeWait,
    }

    [Test]
    public void TC068_同じ盤面でも方針によって得点が変わる()
    {
        foreach (var seed in SimProbe.Seeds(SeedCount))
        {
            var board = SimProbe.BoardOf(seed);
            var scores = Enum.GetValues<Policy>().Select(p => Play(board, p)).ToArray();

            Assert.That(scores.Distinct().Count(), Is.GreaterThan(1),
                $"**seed={seed} で、どの方針でも得点が同じ**（REQ-057）。" +
                $"得点 {string.Join(" / ", scores)}。立ち回りに意味が無い");
        }
    }

    [Test]
    public void TC069_全シードに勝ち続ける方針が無い()
    {
        var policies = Enum.GetValues<Policy>();
        var wins = policies.ToDictionary(p => p, _ => 0);

        foreach (var seed in SimProbe.Seeds(SeedCount))
        {
            var board = SimProbe.BoardOf(seed);
            var scores = policies.ToDictionary(p => p, p => Play(board, p));
            var best = scores.Values.Max();

            // 同点は全員勝ち。単独で勝てているかを見たいので、独走のときだけ数える
            var winners = scores.Where(kv => kv.Value == best).Select(kv => kv.Key).ToArray();

            if (winners.Length == 1)
            {
                wins[winners[0]]++;
            }
        }

        var top = wins.MaxBy(kv => kv.Value);
        var rate = (double)top.Value / SeedCount;

        Assert.That(rate, Is.LessThanOrEqualTo(MaxWinRate),
            $"**{top.Key} が {top.Value}/{SeedCount}（{rate:P0}）で独走している**（REQ-056）。" +
            $"合格線は {MaxWinRate:P0}。" +
            $"内訳: {string.Join(" / ", wins.Select(kv => $"{kv.Key}={kv.Value}"))}");
    }

    /// <summary>方針どおりに 1 夜を通し、得点を返す。</summary>
    private static int Play(BoardSpec board, Policy policy)
    {
        var s = SimProbe.Begin(board);

        while (s.Over is null)
        {
            s = policy switch
            {
                Policy.Tap => SimProbe.Act(s, board, ActionKind.Cry, hold: 1),
                Policy.Charge => SimProbe.ActFullStrength(s, board, ActionKind.Cry),
                Policy.Pretend => TryPretend(s, board),
                Policy.Mixed => s.Arousal < 50
                    ? SimProbe.ActFullStrength(s, board, ActionKind.Cry)
                    : TryPretend(s, board),
                Policy.Idle => SimProbe.RunTicks(s, board, 60),
                Policy.GraceSnipe => Snipe(s, board),
                Policy.ChargeWait => s.Parent == ParentPhase.Up
                    ? SimProbe.RunTicks(s, board, 30)
                    : SimProbe.ActFullStrength(s, board, ActionKind.Cry),
                _ => SimProbe.RunTicks(s, board, 1),
            };

            // 進んでいなければ無限ループになる。1 tick は必ず進める
            s = s.Over is null ? SimProbe.RunTicks(s, board, 1) : s;
        }

        return s.Score;
    }

    /// <summary>
    /// 親が起きている間は待ち、寝ていれば次の寝入りばなに満タンの泣きが届くよう溜め始める。
    /// **周期と溜めの長さは「いつ押すか」を決める手順としてだけ読む**（期待値には使わない。ADR-0012）。
    /// </summary>
    private static NightState Snipe(NightState s, BoardSpec board)
    {
        if (s.Parent != ParentPhase.Sleeping || s.PendingCare is not null)
        {
            return SimProbe.RunTicks(s, board, 30);
        }

        var period = SimProbe.Tuning.DrowsyPeriodTicks;
        var start = (s.Tick / period + 1) * period - SimProbe.Tuning.ChargeTicks + 2;

        return s.Tick < start
            ? SimProbe.RunTicks(s, board, start - s.Tick)
            : SimProbe.ActFullStrength(s, board, ActionKind.Cry);
    }

    /// <summary>目を閉じて判定を待ち、猶予に乗ったら泣く。</summary>
    private static NightState TryPretend(NightState s, BoardSpec board)
    {
        var closed = SimProbe.CloseEyes(s, board);
        var before = closed.PretendN;

        for (var i = 0; i < 600 && closed.Over is null; i++)
        {
            closed = SimProbe.Step(closed, new TickInput(null, false), board);

            if (closed.PretendN > before)
            {
                break;
            }
        }

        if (closed.Over is not null)
        {
            return closed;
        }

        // 着地を待ってから開眼して泣く（早く開けると気づかれる。D-06）
        for (var i = 0; i < 600 && closed.Over is null && closed.Parent is ParentPhase.Settling or ParentPhase.Feint; i++)
        {
            closed = SimProbe.Step(closed, new TickInput(null, false), board);
        }

        var opened = SimProbe.OpenEyes(closed, board);

        return opened.Over is null ? SimProbe.Act(opened, board, ActionKind.Cry, hold: 1) : opened;
    }
}
