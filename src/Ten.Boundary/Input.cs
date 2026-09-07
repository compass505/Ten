using System;
using Ten.Pure;

namespace Ten.Boundary;

/// <summary>接触の段階。端末から来る生の情報。</summary>
public enum PointerPhase { Down, Move, Up, Cancel }

/// <summary>
/// 端末から来た接触 1 件。**値として受け取る。**
///
/// こうしておくと、量子化・排他・丸め（IN-1〜IN-6）が Unity なしで検証できる。
/// Unity 側は `Input.touches` を読んでこれに詰め替えるだけ。
/// </summary>
/// <param name="X">画面座標。原点は左下（Unity のタッチと同じ）</param>
public readonly record struct PointerSample(int FingerId, float X, float Y, PointerPhase Phase);

/// <summary>
/// 首の可動範囲（IN-6 / REQ-002 / setting.md 3 節）。
///
/// **値は balance.md 側にある。**
/// [test_first.md](../../docs/00_process/test_first.md) 4.1 が
/// 「首の可動角度」を**バランス値**として挙げているので、
/// **ここに既定値を書かず、外から渡す**（ADR-0012）。
/// </summary>
public readonly record struct LookLimits(float YawMaxDeg, float PitchMinDeg, float PitchMaxDeg);

/// <summary>
/// MOD-Input — タッチ → 入力イベント。
///
/// 仕様: docs/30_detailed_design/MOD-Input.md（IN-1〜IN-7）
/// </summary>
public interface IInputSource
{
    /// <summary>この tick に割り付いた入力。**1 tick 1 件に正規化済み**（IN-1）。</summary>
    TickInput Sample(int tick);

    /// <summary>首の向き。**表示層だけが使う。状態列に影響しない**（data_model.md 5 節）。</summary>
    (float YawDeg, float PitchDeg) Look { get; }
}

/// <summary>
/// 接触の列から入力を組み立てる。**Unity を参照しない。**
///
/// Unity 側の `TouchInput` は、`Input.touches` を <see cref="PointerSample"/> に
/// 詰め替えて <see cref="Feed"/> に渡すだけの薄い殻になる。
/// 量子化・排他・丸めの判断はすべてここにある。
///
/// **これはシグネチャだけの空実装である**（test_first.md 5.1）。
/// </summary>
public sealed class PointerInputSource : IInputSource
{
    private const string NotYet = "フェーズ 5（実装）で書く（test_first.md 5.1）";

    public PointerInputSource(float screenWidth, float screenHeight, LookLimits limits)
    {
        ScreenWidth = screenWidth;
        ScreenHeight = screenHeight;
        Limits = limits;
    }

    public float ScreenWidth { get; }
    public float ScreenHeight { get; }
    public LookLimits Limits { get; }

    public (float YawDeg, float PitchDeg) Look => throw new NotImplementedException(NotYet);

    /// <summary>端末から来た接触を積む。**同じ tick に複数来てもよい**（捨てるのはこちらの仕事）。</summary>
    public void Feed(int tick, PointerSample sample) => throw new NotImplementedException(NotYet);

    /// <summary>
    /// その tick の入力。**先着 1 件だけを採り、残りは捨てる。**
    /// 捨てたものはキューに残さず、次の tick にも出さない（IN-1 / IN-2 / REQ-059）。
    /// </summary>
    public TickInput Sample(int tick) => throw new NotImplementedException(NotYet);

    /// <summary>画面の下半分か（IN-5 / REQ-005）。行動の操作対象はここだけ。</summary>
    public bool IsActionArea(float y) => throw new NotImplementedException(NotYet);
}

/// <summary>
/// 入力列を再生する（IN-7 / ADR-0002 D3）。ハーネスが使う。
///
/// **これはシグネチャだけの空実装である**（test_first.md 5.1）。
/// </summary>
public sealed class TraceInput : IInputSource
{
    private const string NotYet = "フェーズ 5（実装）で書く（test_first.md 5.1）";

    public TraceInput(InputTrace trace) => Trace = trace;

    public InputTrace Trace { get; }

    /// <summary>**首の向きは入力列に記録しない**（harness.md 3 節）。常に初期値。</summary>
    public (float YawDeg, float PitchDeg) Look => (0f, 0f);

    public TickInput Sample(int tick) => throw new NotImplementedException(NotYet);
}
