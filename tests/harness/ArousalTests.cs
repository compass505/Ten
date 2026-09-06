using System.Linq;
using NUnit.Framework;
using Ten.Pure;

namespace Ten.Tests.Harness;

/// <summary>
/// TC-030〜043 — 覚醒度・慣れ・元気（docs/40_test/cases/TC-pure-sim.md）。
///
/// **上昇量の絶対値を期待値に書かない**（ADR-0012）。見るのは比較と単調性だけ。
/// </summary>
[TestFixture]
public sealed class ArousalTests
{
    private const int Hold = 1;

    private static BoardSpec Board => SimProbe.BoardOf(SimProbe.Seeds(1).Single());

    private static NightState Start(BoardSpec board) => SimProbe.Begin(board);

    // ------------------------------------------------------------------
    // 覚醒度
    // ------------------------------------------------------------------

    [Test]
    public void TC030_覚醒度は上限に到達できる()
    {
        var board = Board;
        var s = Start(board);

        // 最大強度で撃ち続ける。到達不能なら REQ-011 が成立しない
        for (var i = 0; i < 200 && s.Over is null && s.Arousal < 100; i++)
        {
            s = SimProbe.ActFullStrength(s, board, ActionKind.Cry);
            s = SimProbe.RunUntil(s, board, x => x.Vigor > 0 || x.Over is not null, what: "元気の回復");
        }

        Assert.That(s.Arousal, Is.EqualTo(100),
            "**覚醒度が上限に到達できない**（REQ-011）。" +
            "上昇量が覚醒度に比例して減ると、床が無い限り到達しない（balance.md 11 節の実測）");
    }

    [Test]
    public void TC031_覚醒度が高いほど上昇量が小さいか等しい()
    {
        var board = Board;
        var s = Start(board);

        // 10 組（TC-031 が定めた本数）
        for (var i = 0; i < 10; i++)
        {
            var low = 5 + i * 4;
            var high = low + 30;

            var gainLow = SimProbe.ArousalGain(s with { Arousal = low }, board, ActionKind.Cry, Hold);
            var gainHigh = SimProbe.ArousalGain(s with { Arousal = high }, board, ActionKind.Cry, Hold);

            Assert.That(gainLow, Is.GreaterThanOrEqualTo(gainHigh),
                $"**単調非増加が破れている**（REQ-012）。" +
                $"覚醒度 {low} で +{gainLow}、{high} で +{gainHigh}");
        }
    }

    [Test]
    public void TC032_覚醒度を5刻みで上げると上昇量の列が単調非増加になる()
    {
        var board = Board;
        var s = Start(board);

        var gains = Enumerable.Range(0, 21)
            .Select(i => i * 5)
            .Select(a => (Arousal: a, Gain: SimProbe.ArousalGain(s with { Arousal = a }, board, ActionKind.Cry, Hold)))
            .ToArray();

        for (var i = 1; i < gains.Length; i++)
        {
            Assert.That(gains[i].Gain, Is.LessThanOrEqualTo(gains[i - 1].Gain),
                $"**上昇量の列が増えた**（REQ-012）。" +
                $"覚醒度 {gains[i - 1].Arousal} で +{gains[i - 1].Gain}、" +
                $"{gains[i].Arousal} で +{gains[i].Gain}");
        }
    }

    // ------------------------------------------------------------------
    // 慣れ
    // ------------------------------------------------------------------

    [Test]
    public void TC033_慣れが付くほど上昇量が小さいか等しい()
    {
        var board = Board;
        var s = Start(board);

        foreach (var care in SimProbe.AllCares)
        {
            var fresh = s with { Habit = new Habit(0, 0, 0, 0) };
            var used = s with { Habit = HabitWith(care, 3) };

            var gainFresh = SimProbe.ArousalGain(fresh, board, ActionKind.Cry, Hold);
            var gainUsed = SimProbe.ArousalGain(used, board, ActionKind.Cry, Hold);

            Assert.That(gainUsed, Is.LessThanOrEqualTo(gainFresh),
                $"**{care} に慣れたのに上昇量が増えた**（REQ-029）。" +
                $"慣れ 0 で +{gainFresh}、慣れ 3 で +{gainUsed}");
        }
    }

    [Test]
    public void TC034_何もしないでいると慣れが戻る()
    {
        var board = Board;
        var s = Start(board) with { Habit = new Habit(60, 60, 60, 60) };

        var before = s.Habit;
        var later = SimProbe.RunUntil(
            s, board, x => x.Habit != before, what: "慣れの減衰");

        Assert.Multiple(() =>
        {
            Assert.That(later.Habit.PatPat, Is.LessThanOrEqualTo(before.PatPat), "REQ-029: 慣れは戻る");
            Assert.That(later.Habit.Milk, Is.LessThanOrEqualTo(before.Milk));
            Assert.That(later.Habit.Hold, Is.LessThanOrEqualTo(before.Hold));
            Assert.That(later.Habit.DiaperChange, Is.LessThanOrEqualTo(before.DiaperChange));
        });
    }

    // ------------------------------------------------------------------
    // 対処中・予告中は上がらない（REQ-049。順 3.5）
    // ------------------------------------------------------------------

    [Test]
    public void TC035_対処中は覚醒行動が覚醒度を上げない()
    {
        var board = Board;
        var s = SimProbe.RunUntil(
            Start(board), board, x => x.Parent == ParentPhase.Caring,
            new TickInput(ActionKind.Cry, false), what: "Parent = Caring");

        var gain = SimProbe.ArousalGain(s, board, ActionKind.Cry, Hold);

        Assert.That(gain, Is.LessThanOrEqualTo(0),
            $"**対処中に覚醒度が上がった**（+{gain}。REQ-049）");
    }

    [Test]
    public void TC036_予告中も覚醒行動が覚醒度を上げない()
    {
        var board = Board;
        var s = SimProbe.RunUntil(
            Start(board), board, x => x.PendingCare is not null,
            new TickInput(ActionKind.Cry, false), what: "PendingCare != null");

        var gain = SimProbe.ArousalGain(s, board, ActionKind.Cry, Hold);

        Assert.That(gain, Is.LessThanOrEqualTo(0),
            $"**予告中に覚醒度が上がった**（+{gain}。REQ-046 / 049。順 3.5）");
    }

    // ------------------------------------------------------------------
    // 親が起きたあと（REQ-030）
    // ------------------------------------------------------------------

    [Test]
    public void TC037_親が起きても静かにしていれば寝直す()
    {
        var board = Board;
        var up = Start(board) with { Parent = ParentPhase.Up, Arousal = 100 };

        var back = SimProbe.RunUntil(
            up, board, x => x.Parent == ParentPhase.Sleeping, what: "Parent = Sleeping への復帰");

        Assert.That(back.Parent, Is.EqualTo(ParentPhase.Sleeping), "REQ-030");
    }

    [Test]
    public void TC038_親が起きている間に騒ぐと寝直しが遅れる()
    {
        var board = Board;
        var up = Start(board) with { Parent = ParentPhase.Up, Arousal = 100 };

        var quiet = TicksUntilAsleep(up, board, noisy: false);
        var noisy = TicksUntilAsleep(up, board, noisy: true);

        Assert.That(noisy, Is.GreaterThan(quiet),
            $"**騒いでも寝直しが早まる / 変わらない**（REQ-030）。静か {quiet} tick / 騒ぐ {noisy} tick");
    }

    private static int TicksUntilAsleep(NightState s, BoardSpec board, bool noisy)
    {
        var start = s.Tick;

        for (var i = 0; i < SimProbe.NightTicks; i++)
        {
            if (s.Parent == ParentPhase.Sleeping)
            {
                return s.Tick - start;
            }

            if (s.Over is not null)
            {
                break;
            }

            s = noisy
                ? SimProbe.Act(s, board, ActionKind.Cry, Hold)
                : SimProbe.RunTicks(s, board, 1);
        }

        return int.MaxValue;
    }

    // ------------------------------------------------------------------
    // 元気（REQ-014）
    // ------------------------------------------------------------------

    [Test]
    public void TC040_覚醒行動で元気が減り下限を割らない()
    {
        var board = Board;
        var s = Start(board) with { Vigor = 100 };

        var cost = SimProbe.VigorCost(s, board, ActionKind.Cry, Hold);

        Assert.That(cost, Is.GreaterThan(0), "**覚醒行動に元気の代償が無い**（REQ-014）");

        // 撃ち続けても 0 未満にならない
        for (var i = 0; i < 100 && s.Over is null; i++)
        {
            s = SimProbe.Act(s, board, ActionKind.Cry, Hold);

            Assert.That(s.Vigor, Is.GreaterThanOrEqualTo(0), "I-1 / REQ-014: 元気が下限を割った");
        }
    }

    [Test]
    public void TC041_何もしないと元気が回復し上限を超えない()
    {
        var board = Board;
        var s = Start(board) with { Vigor = 10 };

        var later = SimProbe.RunUntil(s, board, x => x.Vigor > 10, what: "元気の回復");

        Assert.That(later.Vigor, Is.GreaterThan(10), "REQ-014");

        var full = SimProbe.RunUntil(later, board, x => x.Vigor >= 100, what: "元気の全回復");
        var beyond = SimProbe.RunTicks(full, board, 100);

        Assert.That(beyond.Vigor, Is.LessThanOrEqualTo(100), "I-1: 元気が上限を超えた");
    }

    [Test]
    public void TC042_対処を受けると元気が回復する()
    {
        var board = Board;
        var s = Start(board) with { Vigor = 10 };

        var caring = SimProbe.RunUntil(
            s, board, x => x.Parent == ParentPhase.Caring,
            new TickInput(ActionKind.Cry, false), what: "Parent = Caring");

        var before = caring.Vigor;
        var during = SimProbe.RunUntil(caring, board, x => x.Vigor > before, what: "対処による回復");

        Assert.That(during.Vigor, Is.GreaterThan(before), "REQ-014 / 013: 対処は元気を戻す");
    }

    [Test]
    public void TC043_目を閉じたり開けたりしても無操作の時計が戻らない()
    {
        var board = Board;
        var s = SimProbe.RunTicks(Start(board), board, 30);

        var before = s.TIdle;

        Assert.That(before, Is.GreaterThan(0), "前提: 無操作の時計が進んでいる");

        for (var i = 0; i < 10; i++)
        {
            s = SimProbe.Step(s, new TickInput(null, true), board);
            s = SimProbe.RunTicks(s, board, 1);
        }

        Assert.That(s.TIdle, Is.GreaterThan(before),
            "**眼の操作で TIdle が戻った**（REQ-047 / D-03）。" +
            "戻せると、目を閉じ続けるだけで寝落ちを永久に回避できる");
    }

    private static Habit HabitWith(CareKind kind, int n) => kind switch
    {
        CareKind.PatPat => new Habit(n, 0, 0, 0),
        CareKind.Milk => new Habit(0, n, 0, 0),
        CareKind.Hold => new Habit(0, 0, n, 0),
        CareKind.DiaperChange => new Habit(0, 0, 0, n),
        _ => new Habit(0, 0, 0, 0),
    };
}
