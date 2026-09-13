using System;
using System.Collections.Generic;
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
    /// <summary>下半分を横に割る数（泣く / ぐずる / ばたつかせる / 目の開閉）。</summary>
    private const int ZoneCount = 4;

    private readonly Dictionary<int, TickInput> _inputs = new();

    private int? _finger;
    private ActionKind? _held;
    private (float YawDeg, float PitchDeg) _look;

    /// <summary>ドラッグを始めた位置と、そのときの向き。**首振りは相対量で動く**（INP-02）。</summary>
    private (float X, float Y) _dragFrom;
    private (float YawDeg, float PitchDeg) _lookFrom;

    /// <param name="initialLook">
    /// 最初のドラッグまでの向き（screens.md 4.2.1「初期視線は天井」）。
    /// **角度はバランス値なので外から渡す**（ADR-0012）。
    /// </param>
    public PointerInputSource(
        float screenWidth, float screenHeight, LookLimits limits,
        (float YawDeg, float PitchDeg) initialLook = default)
    {
        ScreenWidth = screenWidth;
        ScreenHeight = screenHeight;
        Limits = limits;
        _look = Clamp(initialLook.YawDeg, initialLook.PitchDeg);
    }

    public float ScreenWidth { get; }
    public float ScreenHeight { get; }
    public LookLimits Limits { get; }

    public (float YawDeg, float PitchDeg) Look => _look;

    /// <summary>端末から来た接触を積む。**同じ tick に複数来てもよい**（捨てるのはこちらの仕事）。</summary>
    public void Feed(int tick, PointerSample sample)
    {
        // IN-1: その tick はもう埋まっている。**先着 1 件だけを採り、残りは捨てる**
        if (_inputs.ContainsKey(tick))
        {
            return;
        }

        switch (sample.Phase)
        {
            case PointerPhase.Down:
                // 既に 1 本が触れている間は、2 本目を採らない（REQ-005: 同時接触を要求しない）
                if (_finger.HasValue)
                {
                    return;
                }

                _finger = sample.FingerId;
                _dragFrom = (sample.X, sample.Y);
                _lookFrom = _look;

                // **上半分は首振りだけ**（IN-5 / REQ-005）
                if (!IsActionArea(sample.Y))
                {
                    return;
                }

                _inputs[tick] = Press(sample.X);
                return;

            case PointerPhase.Move:
                if (_finger != sample.FingerId)
                {
                    return;
                }

                // 首振りのドラッグは**画面全体で受ける**（balance.md 9 節）
                if (_held is null)
                {
                    _look = Dragged(sample.X, sample.Y);
                }

                return;

            case PointerPhase.Up:
            case PointerPhase.Cancel:
                if (_finger != sample.FingerId)
                {
                    return;
                }

                _finger = null;
                _held = null;

                // 離した tick は「何も押していない」＝発火（ADR-0015）
                _inputs[tick] = new TickInput(null, false);
                return;
        }
    }

    /// <summary>
    /// その tick の入力。**先着 1 件だけを採り、残りは捨てる。**
    /// 捨てたものはキューに残さず、次の tick にも出さない（IN-1 / IN-2 / REQ-059）。
    ///
    /// 押しっぱなしは**毎 tick 同じ値が出る**（溜めが育つ。ADR-0015）。
    /// </summary>
    public TickInput Sample(int tick)
    {
        if (_inputs.TryGetValue(tick, out var explicitInput))
        {
            return explicitInput;
        }

        return _held is null ? new TickInput(null, false) : new TickInput(_held, false);
    }

    /// <summary>画面の下半分か（IN-5 / REQ-005）。行動の操作対象はここだけ。</summary>
    public bool IsActionArea(float y) => y < ScreenHeight / 2f;

    /// <summary>
    /// 下半分のどこを押したかで、何をするかが決まる。
    ///
    /// **画面に印を出さない**（REQ-044 / ADR-0010）。位置だけで覚える。
    /// 左から 泣く / ぐずる / ばたつかせる / 目の開閉。
    /// </summary>
    private TickInput Press(float x)
    {
        var zone = (int)(x / ScreenWidth * ZoneCount);

        if (zone < 0)
        {
            zone = 0;
        }

        if (zone >= ZoneCount)
        {
            zone = ZoneCount - 1;
        }

        if (zone == ZoneCount - 1)
        {
            // 目の開閉はトグル。**押しっぱなしにならない**（行動ではない。D-03）
            _held = null;

            return new TickInput(null, true);
        }

        _held = (ActionKind)zone;

        return new TickInput(_held, false);
    }

    /// <summary>
    /// ドラッグした量を首の向きに足す。**画面の幅いっぱいで可動範囲の端から端まで**（INP-02）。
    /// 指の絶対位置を使わないので、触れた瞬間に跳ばず、持ち替えても向きが残る。
    /// </summary>
    private (float YawDeg, float PitchDeg) Dragged(float x, float y)
    {
        var yaw = _lookFrom.YawDeg + (x - _dragFrom.X) / ScreenWidth * Limits.YawMaxDeg * 2f;
        var pitch = _lookFrom.PitchDeg + (y - _dragFrom.Y) / ScreenHeight * (Limits.PitchMaxDeg - Limits.PitchMinDeg);

        return Clamp(yaw, pitch);
    }

    /// <summary>**可動範囲で丸める**（IN-6 / REQ-002）。</summary>
    private (float YawDeg, float PitchDeg) Clamp(float yaw, float pitch)
    {

        if (yaw < -Limits.YawMaxDeg)
        {
            yaw = -Limits.YawMaxDeg;
        }

        if (yaw > Limits.YawMaxDeg)
        {
            yaw = Limits.YawMaxDeg;
        }

        if (pitch < Limits.PitchMinDeg)
        {
            pitch = Limits.PitchMinDeg;
        }

        if (pitch > Limits.PitchMaxDeg)
        {
            pitch = Limits.PitchMaxDeg;
        }

        return (yaw, pitch);
    }
}

/// <summary>
/// 入力列を再生する（IN-7 / ADR-0002 D3）。ハーネスが使う。
///
/// **これはシグネチャだけの空実装である**（test_first.md 5.1）。
/// </summary>
public sealed class TraceInput : IInputSource
{

    public TraceInput(InputTrace trace) => Trace = trace;

    public InputTrace Trace { get; }

    /// <summary>**首の向きは入力列に記録しない**（harness.md 3 節）。常に初期値。</summary>
    public (float YawDeg, float PitchDeg) Look => (0f, 0f);

    public TickInput Sample(int tick) { foreach (var e in Trace.Entries) if (e.Tick == tick) return e.Input; return new TickInput(null,false); }
}
