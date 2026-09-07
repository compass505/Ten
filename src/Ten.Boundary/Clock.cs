using System;

namespace Ten.Boundary;

/// <summary>
/// MOD-Clock — 実時間 → 固定 tick。
///
/// 仕様: docs/30_detailed_design/MOD-Clock.md（C-1〜C-5）
/// **D2: クロック注入**（test_first.md 2 節）。
/// </summary>
public interface IClock
{
    /// <summary>前回からの経過を受け取り、消化すべき tick 数を返す。**端数は内部に残す**。</summary>
    int Consume(double deltaSeconds);

    void Reset();

    bool IsPaused { get; }

    /// <summary>フォーカス喪失（REQ-009）。</summary>
    void Pause();

    void Resume();
}

/// <summary>
/// 本番のクロック。
///
/// **経過時間を引数で受け取るので、Unity なしで検証できる。**
/// Unity 側は `Update` で `Time.deltaTime` を渡すだけ（CLK-01）。
///
/// **これはシグネチャだけの空実装である**（test_first.md 5.1）。
/// </summary>
public sealed class RealClock : IClock
{
    private const string NotYet = "フェーズ 5（実装）で書く（test_first.md 5.1）";

    /// <summary>1 フレームで消化する tick 数の上限（C-3）。**バランス値ではなく安全弁。**</summary>
    public const int MaxTicksPerFrame = 10;

    public bool IsPaused => throw new NotImplementedException(NotYet);

    public int Consume(double deltaSeconds) => throw new NotImplementedException(NotYet);

    public void Reset() => throw new NotImplementedException(NotYet);

    public void Pause() => throw new NotImplementedException(NotYet);

    public void Resume() => throw new NotImplementedException(NotYet);
}

/// <summary>
/// テスト用。**任意の tick 数を直接与える**（C-5 / ADR-0002 D2）。
/// 実時間を待たない。
/// </summary>
public sealed class StepClock : IClock
{
    private int _pending;

    public bool IsPaused { get; private set; }

    /// <summary>次の <see cref="Consume"/> で返す tick 数を積む。</summary>
    public void Push(int ticks)
    {
        // `ArgumentOutOfRangeException.ThrowIfNegative` は .NET 8 の API で、
        // Unity（.NET Standard 2.1）には無い。**src/ は両方でコンパイルできる書き方に限る。**
        if (ticks < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ticks), ticks, "負の tick は積めない");
        }

        _pending += ticks;
    }

    public int Consume(double deltaSeconds)
    {
        if (IsPaused)
        {
            return 0;
        }

        var take = _pending;
        _pending = 0;

        return take;
    }

    public void Reset() => _pending = 0;

    public void Pause() => IsPaused = true;

    public void Resume() => IsPaused = false;
}
