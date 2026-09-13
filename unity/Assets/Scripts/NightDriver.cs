using System;
using System.Collections.Generic;
using Ten.Boundary;
using Ten.Pure;
using UnityEngine;
using UDisplay = UnityEngine.Display;
using UScreen = UnityEngine.Screen;

namespace Ten.View
{
    /// <summary>
    /// 夜を実際に動かす。**ここが唯一「1 tick 進める順序」を持つ場所**（REQ-051）。
    ///
    /// <code>
    /// 実時間 → IClock → tick → IInputSource → Sim.Advance → Score.Apply → NightEnd.Evaluate → Display.Map → RoomView
    /// </code>
    ///
    /// **判断はすべて純粋層にある。**ここは実時間とタッチを tick と入力に写すだけ。
    /// 順序を入れ替えると、山札の最後の 1 枚で得た点が消える（MOD-Sim / TC-080）。
    /// </summary>
    public sealed class NightDriver : MonoBehaviour
    {
        /// <summary>首の可動範囲（balance.md 9 節 / REQ-002）。</summary>
        private static readonly LookLimits Limits = new(55f, -15f, 30f);

        private readonly Tuning _tuning = new();
        private readonly RealClock _clock = new();

        private BoardSpec _board;
        private NightState _state;
        private PointerInputSource _input;
        private RoomView _view;
        private DiagnosisEntry _diagnosis;
        private int _playIndex = 1;
        private int _tick;
        private bool _hadPointer;

        private void Start()
        {
            Application.targetFrameRate = 60;

            _view = new RoomView(RoomRig.Instance);

            var calendar = new DeviceCalendar(() => DateTimeOffset.Now);

            _board = Board.Generate(calendar.BoardDate, _tuning);

            BeginNight();
        }

        private void BeginNight()
        {
            _state = Sim.Begin(_board, _playIndex, _tuning);
            _input = new PointerInputSource(UScreen.width, UScreen.height, Limits);
            _tick = 0;
            _clock.Reset();
            _clock.Resume();
        }

        private void Update()
        {
            if (_state.Over is not null)
            {
                return;
            }

            var ticks = _clock.Consume(Time.deltaTime);

            for (var i = 0; i < ticks && _state.Over is null; i++)
            {
                _tick++;

                FeedPointer(_tick);

                var input = _input.Sample(_tick);

                // **この順序が REQ-051。**入れ替えない
                var advanced = Sim.Advance(_state, input, _board, _tuning);
                var scored = Score.Apply(advanced, _tuning);
                var over = NightEnd.Evaluate(scored, _tuning);

                _state = over is null ? scored : scored with { Over = over };
            }

            var stages = Ten.Pure.Display.Map(_state, _board, _tuning);
            var (yaw, pitch) = _input.Look;

            _view.Render(_state, stages, yaw, pitch);

            if (_state.Over is not null)
            {
                _diagnosis = Result.Diagnose(_state.Diag, _tuning);
            }
        }

        /// <summary>
        /// 端末から来た接触を、その tick に割り付ける。
        /// **マウスもタッチも同じ形（<see cref="PointerSample"/>）に詰め替えるだけ。**
        /// 判断は <see cref="PointerInputSource"/> にある。
        /// </summary>
        private void FeedPointer(int tick)
        {
            foreach (var sample in Samples())
            {
                _input.Feed(tick, sample);
            }
        }

        private IEnumerable<PointerSample> Samples()
        {
            if (Input.touchCount > 0)
            {
                for (var i = 0; i < Input.touchCount; i++)
                {
                    var touch = Input.GetTouch(i);

                    yield return new PointerSample(touch.fingerId, touch.position.x, touch.position.y,
                        touch.phase switch
                        {
                            TouchPhase.Began => PointerPhase.Down,
                            TouchPhase.Ended => PointerPhase.Up,
                            TouchPhase.Canceled => PointerPhase.Cancel,
                            _ => PointerPhase.Move,
                        });
                }

                yield break;
            }

            // エディタと macOS で触れるように、マウスも同じ形で流す
            var position = Input.mousePosition;

            if (Input.GetMouseButtonDown(0))
            {
                _hadPointer = true;

                yield return new PointerSample(0, position.x, position.y, PointerPhase.Down);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _hadPointer = false;

                yield return new PointerSample(0, position.x, position.y, PointerPhase.Up);
            }
            else if (_hadPointer)
            {
                yield return new PointerSample(0, position.x, position.y, PointerPhase.Move);
            }
        }

        /// <summary>
        /// **結果だけを出す**（`SCR-Result`）。
        /// 夜の最中は何も描かない — 数値・ゲージ・アイコンの禁止は
        /// `SCR-Night` の中だけに掛かる（screens.md 0 節 / REQ-044）。
        /// </summary>
        private void OnGUI()
        {
            if (_state.Over is null)
            {
                return;
            }

            var width = Mathf.Min(UScreen.width - 40, 620);
            var area = new Rect((UScreen.width - width) / 2f, UScreen.height * 0.25f, width, UScreen.height * 0.5f);

            GUI.Box(area, GUIContent.none);
            GUILayout.BeginArea(new Rect(area.x + 24, area.y + 24, area.width - 48, area.height - 48));

            GUILayout.Label(Result.Compose(
                new BestPlay(_state.Score, _playIndex, _state.Over.Value, string.Empty, _state.Diag, _diagnosis.Id),
                _playIndex, _board.SpecVersion, Array.Empty<Result.Beat>()));

            GUILayout.Space(12);
            GUILayout.Label($"［{_diagnosis.Name}］");
            GUILayout.Label(_diagnosis.Text);
            GUILayout.Space(20);

            if (GUILayout.Button("もう一度", GUILayout.Height(40)))
            {
                _playIndex++;
                BeginNight();
            }

            GUILayout.EndArea();
        }
    }
}
