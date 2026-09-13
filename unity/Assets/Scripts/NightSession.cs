using System;
using System.Collections.Generic;
using Ten.Boundary;
using Ten.Pure;

namespace Ten.View
{
    /// <summary>
    /// 一晩ぶんの進行。**ここが唯一「1 tick 進める順序」を持つ場所**（REQ-051 / SL-9）。
    ///
    /// <code>
    /// 実時間 → IClock → tick → IInputSource → Sim.Advance → Score.Apply → NightEnd.Evaluate → Commentary.Observe
    /// </code>
    ///
    /// **判断はすべて純粋層にある。**ここは実時間と接触を tick と入力に写すだけ。
    /// Unity に触らないので、表示層の外からも同じ形で回せる。
    /// </summary>
    public sealed class NightSession
    {
        /// <summary>首の可動範囲（balance.md 9 節 / REQ-002）。</summary>
        public static readonly LookLimits Limits = new(55f, -15f, 30f);

        /// <summary>
        /// 夜の初期視線（screens.md 4.2.1「初期視線は天井」）。
        /// 実寸の寝室では**真上**がどの対象にも向かない（`RoomRig.InitialYawDeg` / `InitialPitchDeg`）。
        /// </summary>
        public static readonly (float YawDeg, float PitchDeg) InitialLook = (RoomRig.InitialYawDeg, RoomRig.InitialPitchDeg);

        private readonly Tuning _tuning;
        private readonly RealClock _clock = new();
        private readonly PointerInputSource _input;

        /// <summary>
        /// 端末から来た接触。**押す・離すは 1 tick に 1 件ずつ流す。**
        /// 同じ tick に「押す」と「離す」を流すと、離すが捨てられて押しっぱなしが残る（IN-1）。
        /// フレームに tick が 1 つも無いとき（60fps に対して 20 tick/秒）も、ここに溜めて取りこぼさない。
        /// </summary>
        private readonly Queue<PointerSample> _pending = new();

        private int _claimedTick = -1;
        private int? _activeFinger;

        public NightSession(
            BoardSpec board, NightState state, IReadOnlyList<Result.Beat> beats, string boardDate,
            Tuning tuning, float screenWidth, float screenHeight)
        {
            Board = board;
            State = state;
            Beats = beats ?? Array.Empty<Result.Beat>();
            BoardDate = boardDate;
            _tuning = tuning;
            _input = new PointerInputSource(screenWidth, screenHeight, Limits, InitialLook);
            _clock.Reset();
        }

        public BoardSpec Board { get; }

        /// <summary>このプレイの盤面の日付。**開始時に確定し、日付が変わっても変えない**（REQ-036）。</summary>
        public string BoardDate { get; }

        public NightState State { get; private set; }

        /// <summary>実況の素材（MOD-Result RES-02）。</summary>
        public IReadOnlyList<Result.Beat> Beats { get; private set; }

        public bool IsPaused => _clock.IsPaused;

        public bool IsOver => State.Over is not null;

        public (float YawDeg, float PitchDeg) Look => _input.Look;

        public void Feed(PointerSample sample)
        {
            if (IsPaused || IsOver)
            {
                return;
            }

            _pending.Enqueue(sample);
        }

        /// <summary>ゲーム内時間を止める（REQ-009）。**触れていた指は離したことにする**（押しっぱなしを残さない）。</summary>
        public void Pause()
        {
            _pending.Clear();

            if (_activeFinger is int finger)
            {
                _input.Feed(State.Tick + 1, new PointerSample(finger, 0f, 0f, PointerPhase.Cancel));
                _activeFinger = null;
            }

            _clock.Pause();
        }

        public void Resume()
        {
            _clock.Reset();
            _clock.Resume();
        }

        public void Update(double deltaSeconds)
        {
            if (IsOver)
            {
                return;
            }

            if (IsPaused)
            {
                _pending.Clear();
                return;
            }

            Drain(State.Tick + 1);

            var ticks = _clock.Consume(deltaSeconds);

            for (var i = 0; i < ticks && !IsOver; i++)
            {
                var tick = State.Tick + 1;

                Drain(tick);

                var input = _input.Sample(tick);

                // **この順序が REQ-051。**入れ替えない
                var advanced = Sim.Advance(State, input, Board, _tuning);
                var scored = Score.Apply(advanced, _tuning);
                var over = NightEnd.Evaluate(scored, _tuning);
                var next = over is null ? scored : scored with { Over = over };

                Beats = Commentary.Observe(Beats, State, next, _tuning);
                State = next;
            }
        }

        private void Drain(int tick)
        {
            while (_pending.Count > 0)
            {
                var sample = _pending.Peek();

                if (sample.Phase == PointerPhase.Move)
                {
                    _input.Feed(tick, sample);
                    _pending.Dequeue();
                    continue;
                }

                if (_claimedTick == tick)
                {
                    return;
                }

                _input.Feed(tick, sample);
                _pending.Dequeue();
                _claimedTick = tick;
                _activeFinger = sample.Phase == PointerPhase.Down ? sample.FingerId : null;
            }
        }
    }
}
