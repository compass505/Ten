using System;
using System.Collections.Generic;
using System.IO;
using Ten.Boundary;
using Ten.Pure;
using UnityEngine;
using AppScreen = Ten.Boundary.Screen;
using UScreen = UnityEngine.Screen;

namespace Ten.View
{
    /// <summary>
    /// MOD-Shell の本番 — 画面の流れと、保存・中断・共有・戻る操作。
    ///
    /// 仕様: docs/20_basic_design/screens.md 1〜3 節 / MOD-Shell SL-1〜SL-9 / data_model.md 6〜7 節 /
    /// 文言とレイアウト: docs/20_basic_design/presentation.md 3〜5 節
    ///
    /// **遷移の判断は <see cref="ShellRule"/> にある。**ここはそれを Unity のライフサイクルに載せるだけ。
    ///
    /// **画面の見た目は仮**（Codex の ui 包みが入るまでの代役）。IMGUI で描く。
    /// uGUI を使わないのは、夜の画面に UI 部品が 1 つでも生きていると TC-126 が数えるため。
    /// </summary>
    public sealed class TenApp : MonoBehaviour
    {
        private const int SchemaVersion = 1;

        private readonly Tuning _tuning = new();
        private readonly IShare _share = new DeviceShare();
        private readonly IPower _power = new DevicePower();

        private IStorage _storage;
        private ICalendar _calendar;
        private RoomView _view;

        private AppScreen _screen = AppScreen.Boot;
        private DeviceData _device;
        private TodayData _today;
        private SavedRun? _run;
        private bool _recoverWipesDevice;

        private NightSession _session;
        private bool _hadPointer;

        // チュートリアル（MOD-Tutorial）
        private TutorialStep? _tutorialStep;
        private bool _tutorialFinishing;
        private bool _tutorialDonePanel;

        // 結果（SCR-Result）
        private BestPlay _lastPlay;
        private bool _lastWasBest;
        private bool _lastCountsForToday;

        private string _toast = string.Empty;
        private float _toastUntil;

        private string BeatsPath => Path.Combine(Application.persistentDataPath, "beats.dat");

        // ==================================================================
        // ライフサイクル
        // ==================================================================

        private void Start()
        {
            Application.targetFrameRate = 60;
            Input.multiTouchEnabled = false;   // REQ-005: 同時接触を要求しない

            _storage = new FileStorage(Application.persistentDataPath);
            _calendar = new DeviceCalendar(() => DateTimeOffset.Now);
            _view = new RoomView(RoomRig.Instance);

            Boot();
        }

        /// <summary>SCR-Boot（T-01〜T-03）。**入力を受けない。通過するだけ。**</summary>
        private void Boot()
        {
            var today = _calendar.BoardDate;

            // --- Device ---
            var device = _storage.LoadDevice();
            var deviceStatus = _storage.LastStatus;

            // **初回起動は壊れていない。**ファイルが無いだけ（ShellRule は Missing を Ok と区別しない）
            if (deviceStatus == LoadStatus.Missing)
            {
                device = new DeviceData(SchemaVersion, Board.SpecVersion, TutorialDone: false);
                deviceStatus = LoadStatus.Ok;
                _storage.SaveDevice(device.Value);
            }

            _device = device ?? new DeviceData(SchemaVersion, Board.SpecVersion, false);

            var specChanged = deviceStatus == LoadStatus.Ok && _device.BoardSpecVersion != Board.SpecVersion;

            // --- Today ---
            var todayData = _storage.LoadToday();
            var todayStatus = _storage.LastStatus == LoadStatus.Missing ? LoadStatus.Ok : _storage.LastStatus;

            // --- Run ---
            _run = _storage.LoadRun();
            var runStatus = _storage.LastStatus == LoadStatus.Missing ? LoadStatus.Ok : _storage.LastStatus;

            _screen = ShellRule.Boot(new BootState(
                _device.TutorialDone, deviceStatus, todayStatus, runStatus, specChanged));

            // 壊れていたときに捨てる範囲（data_model.md 7 節）
            if (specChanged)
            {
                // **破損ではない。**Today と Run を捨てて進む（TC-144）
                todayData = null;
                _run = null;
                _storage.ClearRun();
                _device = _device with { BoardSpecVersion = Board.SpecVersion };
                _storage.SaveDevice(_device);
            }

            if (runStatus == LoadStatus.Corrupt)
            {
                _run = null;
                _storage.ClearRun();
                DeleteBeats();
            }

            _recoverWipesDevice = deviceStatus != LoadStatus.Ok;

            _today = todayData is TodayData t && t.BoardDate == today ? t : new TodayData(today, 0, null);

            if (_screen == AppScreen.Tutorial)
            {
                BeginTutorial(TutorialStep.Arousal);
            }

            ShowRoom(_screen == AppScreen.Tutorial);
        }

        private void Update()
        {
            HandleBack();

            if (_session is not null && (_screen == AppScreen.Night || _screen == AppScreen.Tutorial))
            {
                foreach (var sample in Samples())
                {
                    _session.Feed(sample);
                }

                _session.Update(Time.deltaTime);

                var stages = Ten.Pure.Display.Map(_session.State, _session.Board, _tuning);
                var (yaw, pitch) = _session.Look;

                _view.Render(_session.State, stages, _session.Board, _tuning, yaw, pitch);

                if (_screen == AppScreen.Tutorial)
                {
                    TickTutorial();
                }
                else if (_session.IsOver)
                {
                    FinishPlay();
                }
            }

            _power.KeepAwake(PowerRule.ShouldKeepAwake(
                _screen is AppScreen.Night or AppScreen.Tutorial,
                _session?.IsPaused ?? true,
                _session?.IsOver ?? true));
        }

        /// <summary>REQ-009 / SL-3: 前面から外れたら時間を止め、その tick を保存する。</summary>
        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                PauseNight();
            }
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                PauseNight();
            }
        }

        private void PauseNight()
        {
            if (_session is null || _session.IsOver || _session.IsPaused)
            {
                return;
            }

            if (_screen == AppScreen.Night)
            {
                _session.Pause();
                SaveRun();
            }
            else if (_screen == AppScreen.Tutorial)
            {
                _session.Pause();
            }
        }

        /// <summary>screens.md 3 節。Android の戻るは Escape として届く。</summary>
        private void HandleBack()
        {
            if (!Input.GetKeyDown(KeyCode.Escape))
            {
                return;
            }

            switch (ShellRule.Back(_screen))
            {
                case BackAction.ExitApp:
                    Application.Quit();
                    break;

                case BackAction.Pause:
                    PauseNight();
                    break;

                case BackAction.GoHome:
                    CloseResult();
                    break;
            }
        }

        // ==================================================================
        // 遷移（T-04〜T-13）
        // ==================================================================

        private void Go(ShellEvent e) => _screen = ShellRule.Next(_screen, e);

        /// <summary>Home に入るたびに日付を見直す。**正午を越えていたら別の日**（REQ-019 / D-08）。</summary>
        private void RefreshDay()
        {
            var date = _calendar.BoardDate;

            if (_today.BoardDate != date)
            {
                _today = new TodayData(date, 0, null);
                _storage.SaveToday(_today);
            }
        }

        /// <summary>T-06 / T-08。**プレイ回数を +1 してから始める**（REQ-031 / 037 / 048）。</summary>
        private void StartPlay(ShellEvent e)
        {
            RefreshDay();

            if (e == ShellEvent.Restart)
            {
                _storage.ClearRun();
                DeleteBeats();
            }

            _today = _today with { PlayCount = _today.PlayCount + 1 };
            _storage.SaveToday(_today);

            var board = Board.Generate(_today.BoardDate, _tuning);

            _session = NewSession(board, Sim.Begin(board, _today.PlayCount, _tuning),
                Array.Empty<Result.Beat>(), _today.BoardDate);

            Go(e);
            SaveRun();
            ShowRoom(true);
        }

        /// <summary>T-07。**回数は増やさない。盤面は開始時の日付のもの**（REQ-010 / 036）。</summary>
        private void ResumePlay()
        {
            if (_run is not SavedRun run)
            {
                return;
            }

            var board = Board.Generate(run.BoardDate, _tuning);

            _session = NewSession(board, run.State, LoadBeats(), run.BoardDate);

            Go(ShellEvent.Resume);
            ShowRoom(true);
        }

        /// <summary>T-11。**回数はそのまま**（REQ-037）。</summary>
        private void AbandonPlay()
        {
            _storage.ClearRun();
            DeleteBeats();
            _run = null;
            _session = null;

            Go(ShellEvent.Abandon);
            RefreshDay();
            ShowRoom(false);
        }

        /// <summary>T-10。最高成績を判定して保存し、進行中のプレイを消す（data_model.md 6 節）。</summary>
        private void FinishPlay()
        {
            var s = _session.State;
            var entry = Result.Diagnose(s.Diag, _tuning);

            _lastPlay = new BestPlay(s.Score, s.PlayIndex, s.Over!.Value,
                Commentary.Encode(Commentary.Pick(_session.Beats)), s.Diag, entry.Id);

            RefreshDay();

            // **日付をまたいだプレイは完走できるが、前の日の最高成績には入らない**（D-08）
            _lastCountsForToday = _session.BoardDate == _today.BoardDate;
            _lastWasBest = _lastCountsForToday && Score.IsBetter(_lastPlay, _today.Best);

            if (_lastWasBest)
            {
                _today = _today with { Best = _lastPlay };
            }

            _storage.SaveToday(_today);
            _storage.ClearRun();
            DeleteBeats();
            _run = null;

            Go(ShellEvent.Finish);
            ShowRoom(false);
        }

        /// <summary>T-12。</summary>
        private void CloseResult()
        {
            _session = null;
            Go(ShellEvent.CloseResult);
            RefreshDay();
        }

        private void ShareToday()
        {
            var text = ShareText();

            if (text.Length == 0)
            {
                return;
            }

            switch (ShareRule.Decide(_share.IsAvailable, clipboardAvailable: true))
            {
                case ShareAction.Intent:
                    _share.ShareIntent(text);
                    break;

                case ShareAction.Copy:
                    _share.CopyToClipboard(text);
                    Toast("結果をコピーした");
                    break;
            }
        }

        /// <summary>
        /// 貼るための文字列（REQ-026〜028 / 031 / 040 / 062）。
        /// **その日の最高成績のプレイ 1 回分**（REQ-025）。盤面の答えを含めない。
        /// </summary>
        private string ShareText()
        {
            if (_today.Best is not BestPlay best)
            {
                return string.Empty;
            }

            var entry = Result.Diagnose(best.Diag, _tuning);
            var title = Result.Title(entry, Result.StrengthOf(best.Diag, _tuning));
            var body = Result.Compose(best, _today.PlayCount, Board.SpecVersion, Commentary.Decode(best.Commentary));

            return $"Ten {DateLabel(_today.BoardDate)}\n{body}\n［{title}］{entry.Text}";
        }

        private void RecoverAndReboot()
        {
            if (_recoverWipesDevice)
            {
                _storage.ClearAll();
            }
            else
            {
                _storage.SaveToday(new TodayData(_calendar.BoardDate, 0, null));
                _storage.ClearRun();
            }

            DeleteBeats();

            // T-05 は Home へ。**端末データごと失っていればチュートリアルからやり直す**（MOD-Shell のエラー時）
            Go(ShellEvent.Recovered);
            Boot();
        }

        // ==================================================================
        // チュートリアル（MOD-Tutorial / D-09）
        // ==================================================================

        private void BeginTutorial(TutorialStep step)
        {
            var board = Board.Tutorial(_tuning);

            // **プレイ回数に数えない。保存もしない**（TU-3）
            _session = NewSession(board, Sim.Begin(board, 1, _tuning), Array.Empty<Result.Beat>(), board.Seed);
            _tutorialStep = step;
            _tutorialFinishing = false;
            _tutorialDonePanel = false;
        }

        private void TickTutorial()
        {
            var s = _session.State;

            if (_tutorialDonePanel)
            {
                return;
            }

            if (_tutorialStep is TutorialStep step)
            {
                _tutorialStep = TutorialRule.Advance(step, s);

                if (_tutorialStep is null)
                {
                    // **寝たふりが通った瞬間に終わらせない。**着地して泣くまでを体験させる
                    _tutorialFinishing = true;
                }
            }

            if (_tutorialFinishing && (s.Parent is not (ParentPhase.Settling or ParentPhase.Grace) || s.Over is not null))
            {
                _tutorialDonePanel = true;
                _session.Pause();
                return;
            }

            // **止めない。**夜が終わったら同じ段からやり直す（MOD-Tutorial のエラー時）
            if (s.Over is not null && _tutorialStep is TutorialStep again)
            {
                BeginTutorial(again);
                Toast("もう一度");
            }
        }

        private void CompleteTutorial()
        {
            _device = _device with { TutorialDone = true };
            _storage.SaveDevice(_device);
            _session = null;

            Go(ShellEvent.TutorialDone);
            RefreshDay();
            ShowRoom(false);
        }

        // ==================================================================
        // 道具
        // ==================================================================

        private NightSession NewSession(BoardSpec board, NightState state, IReadOnlyList<Result.Beat> beats, string date)
        {
            var session = new NightSession(board, state, beats, date, _tuning, UScreen.width, UScreen.height);

            session.Resume();

            return session;
        }

        private void SaveRun()
        {
            if (_session is null || _screen != AppScreen.Night)
            {
                return;
            }

            _run = new SavedRun(_session.BoardDate, _session.State);
            _storage.SaveRun(_run.Value);

            try
            {
                File.WriteAllText(BeatsPath, Commentary.Encode(_session.Beats));
            }
            catch (IOException)
            {
                // 実況の素材が残らないだけで、夜は壊れない（MOD-Storage のエラー時）
            }
        }

        private IReadOnlyList<Result.Beat> LoadBeats()
        {
            try
            {
                return File.Exists(BeatsPath) ? Commentary.Decode(File.ReadAllText(BeatsPath)) : Array.Empty<Result.Beat>();
            }
            catch (IOException)
            {
                return Array.Empty<Result.Beat>();
            }
        }

        private void DeleteBeats()
        {
            try
            {
                if (File.Exists(BeatsPath))
                {
                    File.Delete(BeatsPath);
                }
            }
            catch (IOException)
            {
            }
        }

        private static void ShowRoom(bool on) => RoomRig.Instance.SetCameraEnabled(on);

        private void Toast(string text)
        {
            _toast = text;
            _toastUntil = Time.unscaledTime + 2.2f;
        }

        private static string DateLabel(string boardDate) =>
            DateTime.TryParse(boardDate, out var d) ? $"{d.Month}月{d.Day}日" : boardDate;

        private static string EndLabel(EndKind kind) => kind switch
        {
            EndKind.Dawn => "夜が明けた",
            EndKind.FellAsleep => "先に寝てしまった",
            EndKind.HandEmpty => "お母さんがリビングへ行った",
            _ => "夜が終わった",
        };

        /// <summary>接触を値にする。**マウスもタッチも同じ形**（エディタと Mac ビルドで触るため）。</summary>
        private IEnumerable<PointerSample> Samples()
        {
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);

                yield return new PointerSample(touch.fingerId, touch.position.x, touch.position.y,
                    touch.phase switch
                    {
                        TouchPhase.Began => PointerPhase.Down,
                        TouchPhase.Ended => PointerPhase.Up,
                        TouchPhase.Canceled => PointerPhase.Cancel,
                        _ => PointerPhase.Move,
                    });

                yield break;
            }

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

        // ==================================================================
        // 描く（仮の見た目。presentation.md 3〜5 節）
        // ==================================================================

        private Ui _ui;

        private void OnGUI()
        {
            _ui ??= new Ui();
            _ui.Ensure();

            switch (_screen)
            {
                case AppScreen.Boot:
                    _ui.Background();
                    break;

                case AppScreen.Home:
                    DrawHome();
                    break;

                case AppScreen.Night:
                    DrawNight();
                    break;

                case AppScreen.Tutorial:
                    DrawTutorial();
                    break;

                case AppScreen.Result:
                    DrawResult();
                    break;

                case AppScreen.Recover:
                    DrawRecover();
                    break;
            }

            if (Time.unscaledTime < _toastUntil)
            {
                _ui.Toast(_toast);
            }
        }

        private void DrawHome()
        {
            _ui.Background();

            var y = _ui.Top(0.08f);

            y = _ui.Small(y, "Ten");
            y = _ui.Body(y, $"{DateLabel(_today.BoardDate)}の夜");
            y += _ui.Gap;

            if (_today.Best is BestPlay best)
            {
                var entry = Result.Diagnose(best.Diag, _tuning);

                y = _ui.Small(y, "今日のいちばん");
                y = _ui.Big(y, $"{best.Score} 回");
                y = _ui.Small(y, $"{best.PlayIndex} 回目のプレイ・{EndLabel(best.EndKind)}");
                y = _ui.Body(y, $"［{Result.Title(entry, Result.StrengthOf(best.Diag, _tuning))}］");
            }
            else
            {
                y = _ui.Body(y, "まだ今日は遊んでいない");
            }

            y += _ui.Gap;
            _ui.Small(y, $"今日遊んだ回数  {_today.PlayCount} 回");

            // **押せるものは下半分だけ**（REQ-005）。いちばん押すものを一番下（親指の近く）に置く
            var buttons = new List<(string Label, Action Act)>();

            if (_today.Best is not null)
            {
                buttons.Add(("結果を送る", ShareToday));
            }

            if (_run is not null)
            {
                buttons.Add(("はじめからやり直す", () => StartPlay(ShellEvent.Restart)));
                buttons.Add(("続きから", ResumePlay));
            }
            else
            {
                buttons.Add(("遊ぶ", () => StartPlay(ShellEvent.Play)));
            }

            _ui.Buttons(buttons);
        }

        private void DrawNight()
        {
            // **夜の最中は何も描かない**（REQ-044）。中断中だけ、戻り方を出す
            if (_session is null || !_session.IsPaused)
            {
                return;
            }

            _ui.Dim();

            var y = _ui.Top(0.30f);

            _ui.Body(y, "とめています");

            _ui.Buttons(new List<(string, Action)>
            {
                ("やめてホームへ", AbandonPlay),
                ("つづける", () => _session.Resume()),
            });
        }

        private void DrawTutorial()
        {
            if (_session is null)
            {
                return;
            }

            if (_tutorialDonePanel)
            {
                _ui.Dim();

                var y = _ui.Top(0.22f);

                y = _ui.Body(y, "これで全部。");
                y = _ui.Small(y, "起こす・元気・寝たふり。\nあとは毎日かわる盤面で、何回起こせるか。");

                _ui.Buttons(new List<(string, Action)> { ("はじめる", CompleteTutorial) });
                return;
            }

            if (_session.IsPaused)
            {
                _ui.Dim();
                _ui.Buttons(new List<(string, Action)> { ("つづける", () => _session.Resume()) });
                return;
            }

            var (title, text) = TutorialCoach.For(_tutorialStep, _tutorialFinishing, _session.State);

            _ui.Coach(title, text);
            _ui.ZoneLabels(_session.State.Baby == BabyPhase.EyesClosed);
        }

        private void DrawResult()
        {
            _ui.Background();

            var entry = Result.Diagnose(_lastPlay.Diag, _tuning);
            var y = _ui.Top(0.07f);

            y = _ui.Small(y, EndLabel(_lastPlay.EndKind));
            y = _ui.Big(y, $"{_lastPlay.Score} 回 起こした");
            y = _ui.Small(y, !_lastCountsForToday
                ? "日付が変わったので、今日の記録には入らない"
                : _lastWasBest ? "今日のいちばん" : $"今日のいちばんは {_today.Best?.Score ?? 0} 回");
            y += _ui.Gap;
            y = _ui.Body(y, $"［{Result.Title(entry, Result.StrengthOf(_lastPlay.Diag, _tuning))}］");
            y = _ui.Paragraph(y, entry.Text);
            y += _ui.Gap;
            _ui.Small(y, $"{_lastPlay.PlayIndex} 回目のプレイ・盤面 v{Board.SpecVersion}");

            var buttons = new List<(string, Action)>();

            if (_today.Best is not null)
            {
                buttons.Add(("結果を送る", ShareToday));
            }

            buttons.Add(("閉じる", CloseResult));

            _ui.Buttons(buttons);
        }

        private void DrawRecover()
        {
            _ui.Background();

            var y = _ui.Top(0.20f);

            y = _ui.Body(y, "保存していた記録が読めなかった。");
            _ui.Small(y, _recoverWipesDevice
                ? "記録を消して、はじめからにします。"
                : "今日の記録を消して、今日をはじめからにします。");

            _ui.Buttons(new List<(string, Action)> { ("はじめからにする", RecoverAndReboot) });
        }
    }

    /// <summary>
    /// チュートリアルの一言（presentation.md 5 節）。**読ませるのではなく、次にやることだけを言う**（TU-1）。
    /// </summary>
    internal static class TutorialCoach
    {
        public static (string Title, string Text) For(TutorialStep? step, bool finishing, NightState s)
        {
            if (s.Baby == BabyPhase.EyesClosed)
            {
                return finishing
                    ? ("3/3 寝たふり", "抱き上げられている…かもしれない。\n置かれたと思ったら、右下で目を開けて、すぐ泣く。")
                    : ("目を閉じている", "何も見えない。じっと待つと、お母さんが寝たと思うかもしれない。\n右下でもう一度押すと目を開ける。");
            }

            if (finishing)
            {
                return ("3/3 寝たふり", "いま！ 左下を押して泣く。");
            }

            return step switch
            {
                TutorialStep.Arousal when s.Parent == ParentPhase.Up =>
                    ("1/3 起こす", "起きた。お母さんは部屋を出ていった。\n静かにしていると、また戻ってきて眠る。"),
                TutorialStep.Arousal =>
                    ("1/3 起こす", "画面の下を押すと声が出る。長く押すほど強い。\n上をドラッグして首を振り、お母さんの顔を見ながら起こしきる。"),
                TutorialStep.Vigor =>
                    ("2/3 元気", "泣き続けると元気が減る。減るほど視界のゆれが小さくなり、\n尽きると端が暗くなる。一度使い切ってみる。"),
                TutorialStep.Pretend =>
                    ("3/3 寝たふり", "右下を押して目を閉じる。何も見えなくなるが、そのまま待つ。"),
                _ => (string.Empty, string.Empty),
            };
        }
    }

    /// <summary>
    /// 仮の見た目（IMGUI）。**数値は `presentation.md` 4 節の仮の値**で、ui 包みが入ったら置き換える。
    /// </summary>
    internal sealed class Ui
    {
        // 暗所で見る（NFR-007）。背景はほぼ黒、文字は明るすぎない生成り
        private static readonly Color Ground = new(0.043f, 0.047f, 0.070f, 1f);
        private static readonly Color Ink = new(0.79f, 0.76f, 0.73f, 1f);
        private static readonly Color InkDim = new(0.49f, 0.47f, 0.44f, 1f);
        private static readonly Color Accent = new(0.85f, 0.65f, 0.36f, 1f);
        private static readonly Color Button = new(0.106f, 0.114f, 0.153f, 1f);
        private static readonly Color ButtonDown = new(0.165f, 0.176f, 0.227f, 1f);

        private GUIStyle _small, _body, _big, _paragraph, _button, _coach, _zone;
        private Texture2D _ground, _buttonTex, _buttonDown, _veil;
        private int _height = -1;

        private float W => UScreen.width;
        private float H => UScreen.height;
        private float Margin => W * 0.07f;

        public float Gap => H * 0.022f;

        public void Ensure()
        {
            if (_height == UScreen.height && _small != null)
            {
                return;
            }

            _height = UScreen.height;

            _ground ??= Solid(Ground);
            _buttonTex ??= Solid(Button);
            _buttonDown ??= Solid(ButtonDown);
            _veil ??= Solid(new Color(0.02f, 0.02f, 0.03f, 0.82f));

            // 1080 × 2400 基準で 本文 48px ≒ 16sp 相当（ui 包みの条件）
            _small = Style(0.017f, InkDim);
            _body = Style(0.022f, Ink);
            _big = Style(0.050f, Accent, FontStyle.Bold);
            _paragraph = Style(0.019f, Ink);
            _paragraph.wordWrap = true;
            _coach = Style(0.018f, Ink);
            _coach.wordWrap = true;
            _zone = Style(0.016f, InkDim);
            _zone.alignment = TextAnchor.LowerCenter;

            _button = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(H * 0.022f),
                alignment = TextAnchor.MiddleCenter,
            };
            _button.normal.background = _button.hover.background = _buttonTex;
            _button.active.background = _buttonDown;
            _button.normal.textColor = _button.hover.textColor = _button.active.textColor = Ink;
        }

        public void Background() => GUI.DrawTexture(new Rect(0, 0, W, H), _ground);

        public void Dim() => GUI.DrawTexture(new Rect(0, 0, W, H), _veil);

        public float Top(float ratio) => H * ratio;

        public float Small(float y, string text) => Line(y, text, _small);

        public float Body(float y, string text) => Line(y, text, _body);

        public float Big(float y, string text) => Line(y, text, _big);

        public float Paragraph(float y, string text)
        {
            var width = W - Margin * 2;
            var height = _paragraph.CalcHeight(new GUIContent(text), width);

            GUI.Label(new Rect(Margin, y, width, height), text, _paragraph);

            return y + height;
        }

        /// <summary>下から積む。**全部が画面の下半分に収まる**（REQ-005）。</summary>
        public void Buttons(IReadOnlyList<(string Label, Action Act)> buttons)
        {
            var height = H * 0.075f;
            var gap = H * 0.018f;
            var y = H * 0.92f - height;

            for (var i = buttons.Count - 1; i >= 0; i--)
            {
                if (y < H * 0.5f)
                {
                    break;
                }

                if (GUI.Button(new Rect(Margin, y, W - Margin * 2, height), buttons[i].Label, _button))
                {
                    buttons[i].Act();
                }

                y -= height + gap;
            }
        }

        public void Coach(string title, string text)
        {
            var width = W - Margin * 2;
            var y = H * 0.05f;

            GUI.Label(new Rect(Margin, y, width, H * 0.03f), title, _small);
            GUI.Label(new Rect(Margin, y + H * 0.03f, width, H * 0.12f), text, _coach);
        }

        /// <summary>
        /// 下半分の 4 つの押し場所。**チュートリアルでだけ出す**（夜の画面には印を出さない。REQ-044）。
        /// </summary>
        public void ZoneLabels(bool eyesClosed)
        {
            var labels = eyesClosed
                ? new[] { string.Empty, string.Empty, string.Empty, "目を開ける" }
                : new[] { "泣く", "ぐずる", "ばたつく", "目を閉じる" };

            var width = W / labels.Length;

            for (var i = 0; i < labels.Length; i++)
            {
                GUI.Label(new Rect(width * i, H * 0.80f, width, H * 0.12f), labels[i], _zone);
            }
        }

        public void Toast(string text)
        {
            var rect = new Rect(Margin, H * 0.46f, W - Margin * 2, H * 0.05f);

            GUI.DrawTexture(rect, _buttonTex);
            GUI.Label(rect, text, new GUIStyle(_body) { alignment = TextAnchor.MiddleCenter });
        }

        private float Line(float y, string text, GUIStyle style)
        {
            var width = W - Margin * 2;
            var height = style.CalcHeight(new GUIContent(text), width);

            GUI.Label(new Rect(Margin, y, width, height), text, style);

            return y + height + H * 0.006f;
        }

        private GUIStyle Style(float size, Color color, FontStyle font = FontStyle.Normal)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(H * size),
                fontStyle = font,
                wordWrap = true,
            };

            style.normal.textColor = color;

            return style;
        }

        private static Texture2D Solid(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);

            texture.SetPixel(0, 0, color);
            texture.Apply();

            return texture;
        }
    }
}
