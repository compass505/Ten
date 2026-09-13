// balance.md 16 節の計測。**使い捨て。src/ から参照しない。**
// dotnet run -c Release -- [Key=Value ...]   例: Rearm=60 ArousalDecayUpTicks=3
using System.Reflection;
using Ten.Pure;

var Idle = new TickInput(null, false);
var Eyes = new TickInput(null, true);
var tuning = new Tuning();
foreach (var arg in args)
{
    var kv = arg.Split('=');
    var prop = typeof(Tuning).GetProperty(kv[0]) ?? throw new Exception("no " + kv[0]);
    object boxed = tuning;
    prop.SetValue(boxed, int.Parse(kv[1]));
    tuning = (Tuning)boxed;
}

const int SeedCount = 48;

var seeds = Enumerable.Range(0, SeedCount)
    .Select(i => new DateTime(2026, 1, 1).AddDays(i).ToString("yyyy-MM-dd")).ToArray();

var policies = new (string Name, Func<NightState, BoardSpec, NightState> Move)[]
{
    ("連打", (s, b) => Act(s, b, ActionKind.Cry, 1)),
    ("溜め", (s, b) => Full(s, b, ActionKind.Cry)),
    ("寝たふり", Pretend),
    ("混ぜる", (s, b) => s.Arousal < 50 ? Full(s, b, ActionKind.Cry) : Pretend(s, b)),
    ("放置", (s, b) => Run(s, b, 60, Idle)),
    ("溜め+待つ", (s, b) => s.Parent == ParentPhase.Up ? Run(s, b, 30, Idle) : Full(s, b, ActionKind.Cry)),
    ("連打+待つ", (s, b) => s.Parent == ParentPhase.Up ? Run(s, b, 30, Idle) : Act(s, b, ActionKind.Cry, 1)),
    ("寝入りばな狙い", Sniper),
    ("3種順+待つ", Rotate),
    ("下準備+寝入りばな", (s, b) => s.Parent == ParentPhase.Sleeping && s.Arousal < 60 && s.PendingCare is null
        ? Act(s, b, ActionKind.Fuss, 1) : Sniper(s, b)),
};

(double[] Avg, double Rate, string Winner) Measure()
{
    var sc = new int[policies.Length, SeedCount];
    for (var p = 0; p < policies.Length; p++)
    for (var i = 0; i < SeedCount; i++)
    {
        var board = Board.Generate(seeds[i], tuning);
        var s = Sim.Begin(board, 1, tuning);
        while (s.Over is null)
        {
            s = policies[p].Move(s, board);
            s = s.Over is null ? Step(s, Idle, board) : s;
        }
        sc[p, i] = s.Score;
    }
    var w = new int[policies.Length];
    for (var i = 0; i < SeedCount; i++)
    {
        var best = Enumerable.Range(0, policies.Length).Max(p => sc[p, i]);
        var ws = Enumerable.Range(0, policies.Length).Where(p => sc[p, i] == best).ToArray();
        if (ws.Length == 1) w[ws[0]]++;
    }
    var top = Enumerable.Range(0, policies.Length).MaxBy(p => w[p]);
    var avg = Enumerable.Range(0, policies.Length).Select(p => Enumerable.Range(0, SeedCount).Average(i => sc[p, i])).ToArray();
    return (avg, (double)w[top] / SeedCount, policies[top].Name);
}

// SWEEP=1: 格子を回して 1 行ずつ出す（方針ごとの表は出さない）
if (Environment.GetEnvironmentVariable("SWEEP") == "1")
{
    var baseTuning = tuning;
    Console.WriteLine("GraceBase\tGraceMul\tCryBase\tRearm\tDecayUp\t狙い\t他の最良\t最大単独勝率\t勝者");
    foreach (var gb in new[] { 30, 45, 60 })
    foreach (var gm in new[] { 1100, 1400, 1800 })
    foreach (var cb in new[] { 30, 45 })
    foreach (var ra in new[] { 40, 55 })
    foreach (var du in new[] { 6, 12 })
    {
        tuning = baseTuning with { GraceBase = gb, GraceMultiplierMilli = gm, CryBase = cb, Rearm = ra, ArousalDecayUpTicks = du };
        (double[] avg, double rate, string winner) r;
        try { r = Measure(); } catch { continue; }
        var (avg, rate, winner) = r;
        var snipe = avg[7];
        var other = avg.Where((_, i) => i != 7).Max();
        Console.WriteLine($"{gb}\t{gm}\t{cb}\t{ra}\t{du}\t{snipe:F2}\t{other:F2}\t{rate:P0}\t{winner}");
    }
    return;
}


var scores = new int[policies.Length, SeedCount];
var ends = new Dictionary<string, int>[policies.Length];
for (var p = 0; p < policies.Length; p++)
{
    ends[p] = new();
    for (var i = 0; i < SeedCount; i++)
    {
        var board = Board.Generate(seeds[i], tuning);
        var s = Sim.Begin(board, 1, tuning);
        while (s.Over is null)
        {
            s = policies[p].Move(s, board);
            s = s.Over is null ? Step(s, Idle, board) : s;
        }
        scores[p, i] = s.Score;
        if (Environment.GetEnvironmentVariable("DETAIL") == policies[p].Name && i < 6)
            Console.WriteLine($"  {seeds[i]} score={s.Score} hand={s.Hand.Total}/{board.Hand.Total} habit={s.Habit.Max} vigor={s.Vigor} choice={s.ChoiceN} over={s.Over}@{s.Tick}");
        var key = s.Over.ToString()!;
        ends[p][key] = ends[p].GetValueOrDefault(key) + 1;
    }
}

var wins = new int[policies.Length];
for (var i = 0; i < SeedCount; i++)
{
    var best = Enumerable.Range(0, policies.Length).Max(p => scores[p, i]);
    var w = Enumerable.Range(0, policies.Length).Where(p => scores[p, i] == best).ToArray();
    if (w.Length == 1) wins[w[0]]++;
}

Console.WriteLine($"args: {string.Join(' ', args)}");
Console.WriteLine("方針\t平均\t最小\t最大\t単独勝ち\t終わり方");
for (var p = 0; p < policies.Length; p++)
{
    var row = Enumerable.Range(0, SeedCount).Select(i => scores[p, i]).ToArray();
    Console.WriteLine($"{policies[p].Name}\t{row.Average():F2}\t{row.Min()}\t{row.Max()}\t{wins[p]}\t" +
        string.Join(" ", ends[p].Select(kv => $"{kv.Key}={kv.Value}")));
}

NightState Step(NightState s, TickInput input, BoardSpec board)
{
    if (s.Over is not null) return s;
    var a = Sim.Advance(s, input, board, tuning);
    var sc = Score.Apply(a, tuning);
    var over = NightEnd.Evaluate(sc, tuning);
    return over is null ? sc : sc with { Over = over };
}

NightState Run(NightState s, BoardSpec b, int n, TickInput input)
{
    for (var i = 0; i < n && s.Over is null; i++) s = Step(s, input, b);
    return s;
}

NightState Settle(NightState s, BoardSpec b)
{
    for (var i = 0; i < 5400 && s.Over is null && s.Baby == BabyPhase.Acting; i++) s = Step(s, Idle, b);
    return s;
}

NightState Act(NightState s, BoardSpec b, ActionKind k, int hold)
{
    s = Run(s, b, hold, new TickInput(k, false));
    return Settle(Step(s, Idle, b), b);
}

NightState Full(NightState s, BoardSpec b, ActionKind k)
{
    var held = new TickInput(k, false);
    for (var i = 0; i < 5400 && s.Over is null && s.ActStrengthMilli < 1000; i++) s = Step(s, held, b);
    return Settle(Step(s, Idle, b), b);
}

NightState Pretend(NightState s, BoardSpec b)
{
    if (s.Baby != BabyPhase.EyesClosed) s = Step(s, Eyes, b);
    var before = s.PretendN;
    for (var i = 0; i < 600 && s.Over is null && s.PretendN == before; i++) s = Step(s, Idle, b);
    for (var i = 0; i < 600 && s.Over is null && s.Parent is ParentPhase.Settling or ParentPhase.Feint; i++) s = Step(s, Idle, b);
    if (s.Over is not null) return s;
    s = Step(s, Eyes, b);
    return s.Over is null ? Act(s, b, ActionKind.Cry, 1) : s;
}

// 親の周期（DrowsyPeriodTicks）を読んで、寝入りばなに溜めた一撃を当てる
NightState Sniper(NightState s, BoardSpec b)
{
    if (s.Parent == ParentPhase.Up) return Run(s, b, 30, Idle);
    if (s.Parent != ParentPhase.Sleeping || s.PendingCare is not null) return Act(s, b, ActionKind.Cry, 1);
    var period = tuning.DrowsyPeriodTicks;
    var next = (s.Tick / period + 1) * period;
    var start = next - tuning.ChargeTicks + 2;
    if (s.Tick < start) return Run(s, b, start - s.Tick, Idle);
    return Full(s, b, ActionKind.Cry);
}

NightState Rotate(NightState s, BoardSpec b)
{
    if (s.Parent == ParentPhase.Up) return Run(s, b, 30, Idle);
    var k = (ActionKind)(s.Tick / 7 % 3);
    return Full(s, b, k);
}

static partial class P { }
