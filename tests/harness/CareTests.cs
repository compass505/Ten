using System.Linq;
using NUnit.Framework;
using Ten.Pure;

namespace Ten.Tests.Harness;

/// <summary>
/// TC-045〜049 — 親の対処と山札（docs/40_test/cases/TC-pure-sim.md）。
/// </summary>
[TestFixture]
public sealed class CareTests
{
    private static BoardSpec Board => SimProbe.BoardOf(SimProbe.Seeds(1).Single());

    /// <summary>対処が始まるまで泣き続ける。</summary>
    private static NightState UntilCaring(NightState s, BoardSpec board) =>
        SimProbe.RunUntil(s, board, x => x.Parent == ParentPhase.Caring,
            new TickInput(ActionKind.Cry, false), what: "Parent = Caring");

    [Test]
    public void TC045_どの対処も何かを改善し何かを悪化させる()
    {
        var board = Board;

        // 4 対処すべてを踏むまで夜を回す。踏めなかった対処があれば、それは検証漏れ
        var seen = SimProbe.AllCares.ToDictionary(c => c, _ => false);
        var s = SimProbe.Begin(board);

        for (var i = 0; i < 200 && s.Over is null && seen.ContainsValue(false); i++)
        {
            var caring = UntilCaring(s, board);
            var care = caring.ActiveCare!.Value;

            var before = caring;
            var after = SimProbe.RunUntil(caring, board,
                x => x.Parent != ParentPhase.Caring, what: "対処の終わり");

            var better = Improved(before, after);
            var worse = Worsened(before, after);

            Assert.Multiple(() =>
            {
                Assert.That(better, Is.Not.Empty,
                    $"**{care} が何も改善していない**（REQ-013）");
                Assert.That(worse, Is.Not.Empty,
                    $"**{care} に代償が無い**（REQ-013）。" +
                    "代償が無いと対処を引き出すのが純粋な得になる（balance.md 11 節の実測）");
            });

            seen[care] = true;
            s = after;
        }

        Assert.That(seen.Where(p => !p.Value).Select(p => p.Key), Is.Empty,
            "**踏めなかった対処がある。**盤面か手順を変えて 4 種すべてを通す");
    }

    /// <summary>赤ちゃんにとって良くなったもの。</summary>
    private static string[] Improved(NightState a, NightState b) =>
        new[]
        {
            b.Vigor > a.Vigor ? "元気" : null,
            b.Arousal > a.Arousal ? "覚醒度" : null,
            b.Hand.Total < a.Hand.Total ? "山札の消費" : null,
        }.OfType<string>().ToArray();

    /// <summary>赤ちゃんにとって悪くなったもの。</summary>
    private static string[] Worsened(NightState a, NightState b) =>
        new[]
        {
            b.Vigor < a.Vigor ? "元気" : null,
            b.Arousal < a.Arousal ? "覚醒度" : null,
            b.Habit.Max > a.Habit.Max ? "慣れ" : null,
            b.LockUntil > a.LockUntil ? "行動の封じ" : null,
            b.DampUntil > a.DampUntil ? "効果の減衰" : null,
        }.OfType<string>().ToArray();

    [Test]
    public void TC046_対処を1回引き出すと山札が1枚減る()
    {
        var board = Board;
        var s = SimProbe.Begin(board);

        var before = s.Hand.Total;
        var caring = UntilCaring(s, board);

        Assert.That(caring.Hand.Total, Is.EqualTo(before - 1),
            "**対処を引き出したのに山札が減らない / 減りすぎる**（REQ-041）");
    }

    [Test]
    public void TC047_山札が少ないほど対処までの遅れが大きいか等しい()
    {
        var board = Board;
        var s = SimProbe.Begin(board);

        var many = DelayUntilCare(s with { Hand = new HandCount(3, 3, 3, 3) }, board);
        var few = DelayUntilCare(s with { Hand = new HandCount(1, 0, 0, 0) }, board);

        Assert.That(few, Is.GreaterThanOrEqualTo(many),
            $"**山札が少ないほうが早く来ている**（REQ-046）。多い {many} tick / 少ない {few} tick");
    }

    private static int DelayUntilCare(NightState s, BoardSpec board)
    {
        var start = s.Tick;
        var caring = UntilCaring(s, board);

        return caring.Tick - start;
    }

    [Test]
    public void TC048_山札が0なら対処に入らない()
    {
        var board = Board;
        var s = SimProbe.Begin(board) with { Hand = new HandCount(0, 0, 0, 0) };

        var after = SimProbe.RunTicks(s, board, 300, new TickInput(ActionKind.Cry, false));

        Assert.That(after.Parent, Is.Not.EqualTo(ParentPhase.Caring),
            "**山札 0 で対処に入った**（REQ-050）");
    }

    [Test]
    public void TC049_もう一人の親が来ると山札が増える()
    {
        var board = Board;
        var s = SimProbe.Begin(board);

        var scheduled = board.Events.FirstOrDefault(e => e.Kind == NightEventKind.PartnerWakes);

        Assert.That(board.Events.Any(e => e.Kind == NightEventKind.PartnerWakes), Is.True,
            $"seed={board.Seed} の盤面に「もう一人の親」が無い。出来事を含む盤面で試す");

        var before = SimProbe.RunUntil(s, board, x => x.Tick == scheduled.Tick - 1,
            what: "出来事の直前");
        var after = SimProbe.RunTicks(before, board, 2);

        Assert.That(after.Hand.Total, Is.GreaterThan(before.Hand.Total),
            "**「もう一人の親」で山札が増えない**（REQ-055）");
    }
}
