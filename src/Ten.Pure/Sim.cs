using System;

namespace Ten.Pure;

/// <summary>
/// MOD-Sim — 夜の状態と 1 tick。
///
/// 仕様: docs/30_detailed_design/MOD-Sim.md（処理順 1〜9 / 性質 S-1〜S-13）
/// 出典: screens.md 4.5（1 tick の処理順）/ balance.md（数値は <see cref="Tuning"/> 経由）
///
/// > **このモジュールが全 Must の大半を持つ。**ハーネス層のテストはここに集中する。
///
/// **`Advance` は 1 本だけ。**内部の段階を公開しない。テストは「状態 → 状態」だけを見る。
/// </summary>
public static class Sim
{
    /// <summary>強度の最大（1/1000 固定小数）。</summary>
    private const int StrengthMax = 1000;

    /// <summary>札種の数。</summary>
    private const int CareKinds = 4;

    /// <summary>対処の種類を引くときに通番をずらす幅（同じ通番で 2 つ引かないため）。</summary>
    private const int ChoiceKindOffset = 500000;

    // 診断の 7 軸（diagnosis.md 1 節。**並びは Diagnosis と同じ**）
    private const int AxisIrritation = 0;
    private const int AxisFatigue = 1;
    private const int AxisFutility = 2;
    private const int AxisSleepLoss = 3;
    private const int AxisAnxiety = 4;
    private const int AxisJerked = 5;
    private const int AxisFondness = 6;

    /// <summary>静かにしている時間が「いとおしさ」に変わる刻み。</summary>
    private const int FondnessTicks = 60;

    /// <summary>
    /// 寝入りばなを「潰した」と数える強度の下限（1/1000）。
    ///
    /// **たまたま当たったのは「狙った」ではない。**この線を引かないと、
    /// 寝不足が「行動した回数」に比例してしまい、いらだちと同じ軸になる（TC-162）。
    /// </summary>
    private const int GraceIntentMilli = 500;

    /// <summary>診断の「3 つ目の軸」を引き直す間隔（夜をこの長さの時間帯に区切る）。</summary>
    private const int DiagnosisWindowTicks = 600;

    /// <summary>夜の開始状態。盤面とプレイ回数から作る。</summary>
    public static NightState Begin(BoardSpec board, int playIndex, Tuning tuning)
    {
        tuning.Validate();

        return new NightState(
            PlayIndex: playIndex,
            Tick: 0,
            Baby: BabyPhase.Open,
            ActKind: null,
            ActRemain: 0,
            ActStrengthMilli: 0,
            ChargeTicks: 0,
            ActFired: false,
            Vigor: tuning.VigorStart,
            Parent: ParentPhase.Sleeping,
            Arousal: board.InitialArousal,
            ActiveCare: null,
            CareRemain: 0,
            PendingCare: null,
            CareDelay: 0,
            Habit: default,
            Hand: board.Hand,
            TIdle: 0,
            TClosed: 0,
            TSettle: 0,
            TGrace: 0,
            PretendN: 0,
            DozeN: 0,
            ChoiceN: 0,
            EventsFired: 0,
            RollUntil: 0,
            HungryUntil: 0,
            PartnerHere: false,
            LockUntil: 0,
            LockedKinds: ActionKind.Kick,
            DampUntil: 0,
            DampMilli: 0,
            Diag: default,
            Score: 0,
            ScoredEdge: false,
            PretendPrimed: false,
            CalmBlock: 0,
            Dozed: false,
            Over: null);
    }

    /// <summary>
    /// 1 tick 進める。**純関数**（引数以外の一切に触らない）。
    ///
    /// 処理順は screens.md 4.5 の 1〜8 と 1 対 1。**順序が決定論の前提**（REQ-039）。
    /// **得点（順 9）と終了（順 10）はここでは行わない。**呼び出し側が
    /// <see cref="Score.Apply"/> → <see cref="NightEnd.Evaluate"/> の順に適用する（REQ-051）。
    /// </summary>
    public static NightState Advance(NightState s, TickInput input, BoardSpec board, Tuning tuning)
    {
        // MOD-Sim のエラー時: 終了後に進めても壊れない
        if (s.Over is not null)
        {
            return s;
        }

        tuning.Validate();

        var seed = board.Seed;
        var t = s.Tick + 1;

        s = s with { Tick = t };

        // 発火した覚醒行動（順 1 / 4 で決まり、順 7 で効く）
        var fired = false;
        var firedKind = ActionKind.Cry;
        var firedStrength = 0;
        var openedNow = false;

        // ------------------------------------------------------------------
        // 順 1: 入力を適用する（1 tick 1 件）
        // ------------------------------------------------------------------
        if (s.Baby == BabyPhase.EyesClosed)
        {
            // **閉眼中は ToggleEyes だけ。**他は捨てて副作用も残さない（S-8 / REQ-059）
            if (input.ToggleEyes)
            {
                s = s with { Baby = BabyPhase.Open, TClosed = 0 };
                openedNow = true;
            }
        }
        else if (input.ToggleEyes)
        {
            // 行動の最中は眼を操作しない（行動が中断されると I-3 / I-4 が揺れる）
            if (s.Baby == BabyPhase.Open)
            {
                s = s with { Baby = BabyPhase.EyesClosed };
            }
        }
        else if (s.Baby == BabyPhase.Charging)
        {
            if (input.Held == s.ActKind && s.ActStrengthMilli < StrengthMax)
            {
                // 溜め続ける（順 4 で伸ばす）
            }
            else
            {
                // 離した、別の行動に移った、または満タンのまま押し続けた → 発火
                fired = true;
                firedKind = s.ActKind!.Value;
                firedStrength = s.ActStrengthMilli;
            }
        }
        else if (s.Baby == BabyPhase.Open && input.Held is ActionKind start && CanStart(s, start, tuning))
        {
            s = s with
            {
                Baby = BabyPhase.Charging,
                ActKind = start,
                ActRemain = 0,
                ActStrengthMilli = 0,
                ChargeTicks = 0,
                ActFired = false,
            };
        }

        // ------------------------------------------------------------------
        // 順 2: その時刻の出来事を発生させる
        // ------------------------------------------------------------------
        s = FireEvents(s, board, tuning, t);

        // ------------------------------------------------------------------
        // 順 3: 覚醒度に依存しない親の遷移
        // ------------------------------------------------------------------
        s = ParentTransitions(s, seed, tuning, t, openedNow);

        // ------------------------------------------------------------------
        // 順 4: 時計を進める（溜めと countdown もここ）
        // ------------------------------------------------------------------
        s = AdvanceClocks(s, tuning, ref fired, ref firedKind, ref firedStrength);

        // ------------------------------------------------------------------
        // 順 5: 寝たふりの判定
        // ------------------------------------------------------------------
        var judged = false;

        if (s.Baby == BabyPhase.EyesClosed
            && s.Parent == ParentPhase.Sleeping
            && s.TClosed >= tuning.ClosedThresholdTicks)
        {
            var n = s.PretendN + 1;

            judged = true;

            if (Rng.Chance(seed, RngPurpose.FallAsleep, n, tuning.FallAsleepMilli))
            {
                s = s with { PretendN = n, TClosed = 1, Dozed = true };
            }
            else
            {
                // **成功なら Settling、失敗なら Feint**（ADR-0017）。
                // どちらも TSettle を 0 から進め始め、山札も消費しない。違うのは満了後の行き先だけ
                var success = Rng.Chance(seed, RngPurpose.SleepPretend, n, PretendMilli(n, tuning));

                // **閉眼時計を巻き戻す。**戻さないと、満了した次の tick に即座に再判定が走り、
                // 「満了後に親が寝たままでいる」状態（C-10 の 6）が観測できなくなる。
                // 次の寝たふりには、もう一度閾値ぶん閉じていることが要る
                s = s with
                {
                    PretendN = n,
                    TClosed = 1,
                    Parent = success ? ParentPhase.Settling : ParentPhase.Feint,
                    TSettle = 0,
                    PendingCare = null,
                    CareDelay = 0,
                };

                if (success)
                {
                    // **寝たと信じて運んだのに、まだ起きている** → 徒労（diagnosis.md 1 節）
                    s = s with { Diag = s.Diag.Add(AxisFutility, tuning.DiagnosisWeight * 3) };
                }
            }
        }

        // ------------------------------------------------------------------
        // 順 6: 待機中の寝落ち（5 を引いた tick はスキップ。Settling / Feint / Grace は停止）
        // ------------------------------------------------------------------
        if (!judged
            && s.Parent is not (ParentPhase.Settling or ParentPhase.Feint or ParentPhase.Grace)
            && s.TIdle >= tuning.DozeIdleTicks
            && (s.TIdle - tuning.DozeIdleTicks) % tuning.DozeStepTicks == 0)
        {
            var n = s.DozeN + 1;

            s = s with { DozeN = n };

            if (Rng.Chance(seed, RngPurpose.DozeOff, n, tuning.DozeProbMilli))
            {
                s = s with { Dozed = true };
            }
        }

        // ------------------------------------------------------------------
        // 順 7: 状態値の更新
        // ------------------------------------------------------------------
        s = UpdateValues(s, seed, tuning, t, fired, firedKind, firedStrength);

        // ------------------------------------------------------------------
        // 順 8: 覚醒度に依存する親の遷移
        // ------------------------------------------------------------------
        s = ParentArousalTransitions(s, tuning);

        return s;
    }

    // ==================================================================
    // 順 1 の補助
    // ==================================================================

    /// <summary>覚醒行動を始められるか。**元気が尽きている間は選べない**（REQ-014）。</summary>
    private static bool CanStart(NightState s, ActionKind kind, Tuning tuning)
    {
        if (s.Vigor <= 0)
        {
            return false;
        }

        // 対処の副作用で封じられている行動（REQ-013 の代償）
        if (s.LockUntil > s.Tick && s.LockedKinds == kind)
        {
            return false;
        }

        return true;
    }

    // ==================================================================
    // 順 2: 出来事
    // ==================================================================

    private static NightState FireEvents(NightState s, BoardSpec board, Tuning tuning, int t)
    {
        var events = board.Events;

        if (events is null)
        {
            return s;
        }

        for (var i = 0; i < events.Count; i++)
        {
            if (events[i].Tick != t)
            {
                continue;
            }

            s = s with { EventsFired = s.EventsFired + 1 };

            switch (events[i].Kind)
            {
                case NightEventKind.DiaperSoiled:
                    s = s with { Hand = s.Hand.Plus(CareKind.DiaperChange, tuning.DiaperSoiledCards) };
                    break;

                case NightEventKind.PartnerWakes:
                    s = s with
                    {
                        Hand = s.Hand.Plus(CareKind.PatPat, tuning.PartnerCards),
                        PartnerHere = true,
                    };
                    break;

                case NightEventKind.PhoneRings:
                    s = s with { Arousal = Clamp(s.Arousal + tuning.PhoneArousal, 0, tuning.ArousalMax) };
                    break;

                case NightEventKind.ParentRolls:
                    s = s with { RollUntil = t + tuning.RollTicks };
                    break;

                case NightEventKind.GetsHungry:
                    s = s with { HungryUntil = t + tuning.HungryTicks };
                    break;
            }
        }

        return s;
    }

    // ==================================================================
    // 順 3: 覚醒度に依存しない親の遷移
    // ==================================================================

    private static NightState ParentTransitions(
        NightState s, string seed, Tuning tuning, int t, bool openedNow)
    {
        if (s.Parent is ParentPhase.Settling or ParentPhase.Feint)
        {
            // **開眼したら気づかれる。**成功側と失敗側で完全に同じ扱い（D-06 / ADR-0017）
            if (openedNow)
            {
                return StartCare(s, seed, tuning, NoticedKind(s, seed));
            }

            if (s.TSettle >= tuning.SettleTicks)
            {
                // 満了。**違うのは行き先だけ**
                return s.Parent == ParentPhase.Settling
                    ? s with { Parent = ParentPhase.Grace, TSettle = 0, TGrace = 0 }
                    : RestClosed(s with { Parent = ParentPhase.Sleeping, TSettle = 0 });
            }

            return s;
        }

        if (s.Parent == ParentPhase.Grace)
        {
            if (s.TGrace >= tuning.GraceTicks)
            {
                s = RestClosed(s with { Parent = ParentPhase.Sleeping, TGrace = 0 });
            }

            return s;
        }

        // 予告中の対処が来る（REQ-046。遅れは山札の残量で決まる）
        if (s.PendingCare is CareKind pending && s.Parent is ParentPhase.Sleeping or ParentPhase.Up)
        {
            if (s.Hand.Total <= 0)
            {
                // 山札 0 では対処に入れない（REQ-050）
                return s with { PendingCare = null, CareDelay = 0 };
            }

            if (s.CareDelay <= 0)
            {
                return StartCare(s, seed, tuning, Available(s, pending));
            }

            return s;
        }

        // **寝入りばな**（REQ-061）。親が深い眠りに落ちる瞬間。乱数を使わない（予兆が読めるため）
        if (s.Parent == ParentPhase.Sleeping
            && s.PendingCare is null
            && t % tuning.DrowsyPeriodTicks == 0)
        {
            return s with { Parent = ParentPhase.Grace, TGrace = 0 };
        }

        return s;
    }

    /// <summary>
    /// 寝たふりの一巡（`Settling` / `Feint` / `Grace`）が終わったところで**閉眼時計を数え直す。**
    ///
    /// 戻さないと、盲目区間（`t_settle` + `t_grace`）だけで閾値を超えてしまい、
    /// **満了した次の tick に即座に次の判定が走る。**
    /// そうなると「満了して親が寝たままに戻る」経路（screens.md C-10 の 6）が観測できず、
    /// `Feint` の長さも次の判定まで伸びて、成功側と区別できてしまう（ADR-0017 / TC-164）。
    /// </summary>
    private static NightState RestClosed(NightState s) =>
        s.TClosed > 0 ? s with { TClosed = 1 } : s;

    /// <summary>対処を始める。**山札 −1。**溜めていたものは飛ぶ（ADR-0015）。</summary>
    private static NightState StartCare(NightState s, string seed, Tuning tuning, CareKind kind)
    {
        if (s.Hand.Total <= 0)
        {
            return s with { Parent = ParentPhase.Sleeping, PendingCare = null, CareDelay = 0, TSettle = 0 };
        }

        s = s with
        {
            Parent = ParentPhase.Caring,
            ActiveCare = kind,
            CareRemain = tuning.CareTicks(kind),
            Hand = s.Hand.Minus(kind),
            PendingCare = null,
            CareDelay = 0,
            TSettle = 0,
            TGrace = 0,
        };

        // **対処が始まると溜めが飛ぶ。**残るなら、対処を待ってから撃つのが常に得になる
        if (s.Baby == BabyPhase.Charging)
        {
            s = s with { Baby = BabyPhase.Open, ActKind = null, ActRemain = 0 };
        }

        return s with { ActStrengthMilli = 0, ChargeTicks = 0 };
    }

    /// <summary>気づかれたときに親が出す対処。山札にあるものから選ぶ。</summary>
    private static CareKind NoticedKind(NightState s, string seed) =>
        Available(s, (CareKind)Rng.Range(seed, RngPurpose.ParentChoice, s.PretendN + ChoiceKindOffset, CareKinds));

    /// <summary>山札に残っている札種へ寄せる（0 枚の札は出せない）。</summary>
    private static CareKind Available(NightState s, CareKind wanted)
    {
        for (var i = 0; i < CareKinds; i++)
        {
            var kind = (CareKind)(((int)wanted + i) % CareKinds);

            if (s.Hand.Of(kind) > 0)
            {
                return kind;
            }
        }

        return wanted;
    }

    // ==================================================================
    // 順 4: 時計
    // ==================================================================

    private static NightState AdvanceClocks(
        NightState s, Tuning tuning, ref bool fired, ref ActionKind firedKind, ref int firedStrength)
    {
        // 無操作の時計。**眼の操作ではリセットしない**（D-03 / REQ-047）
        var acting = s.Baby is BabyPhase.Charging or BabyPhase.Acting;

        s = s with { TIdle = acting ? 0 : s.TIdle + 1 };

        s = s with { TClosed = s.Baby == BabyPhase.EyesClosed ? s.TClosed + 1 : 0 };

        s = s with
        {
            TSettle = s.Parent is ParentPhase.Settling or ParentPhase.Feint ? s.TSettle + 1 : 0,
            TGrace = s.Parent == ParentPhase.Grace ? s.TGrace + 1 : 0,
        };

        // 予告中の遅れ
        if (s.PendingCare is not null && s.CareDelay > 0)
        {
            s = s with { CareDelay = s.CareDelay - 1 };
        }

        // 溜め（ADR-0015）
        if (s.Baby == BabyPhase.Charging && !fired)
        {
            var ticks = s.ChargeTicks + 1;
            var strength = ticks * StrengthMax / tuning.ChargeTicks;

            s = s with
            {
                ChargeTicks = ticks,
                ActStrengthMilli = strength > StrengthMax ? StrengthMax : strength,
            };

            // **溜め続けることの代償。**これが無いと常に最大まで溜めるのが正解になる
            if (ticks % tuning.ChargeDrainTicks == 0)
            {
                s = s with { Vigor = Clamp(s.Vigor - 1, 0, tuning.VigorMax) };
            }

            // 元気が尽きたら、そこで出てしまう（REQ-014。溜めを維持できない）
            if (s.Vigor <= 0)
            {
                fired = true;
                firedKind = s.ActKind!.Value;
                firedStrength = s.ActStrengthMilli;
            }
        }

        // 行動の残り
        if (s.Baby == BabyPhase.Acting)
        {
            var remain = s.ActRemain - 1;

            s = remain > 0
                ? s with { ActRemain = remain }
                : s with { Baby = BabyPhase.Open, ActKind = null, ActRemain = 0 };
        }

        // 対処の残り。**0 になったらその tick で終える**（I-5 を割らないため）
        if (s.Parent == ParentPhase.Caring && s.ActiveCare is not null)
        {
            var remain = s.CareRemain - 1;

            s = remain > 0 ? s with { CareRemain = remain } : EndCare(s, tuning);
        }

        return s;
    }

    /// <summary>対処が終わる。**改善と代償をここで一度に載せる**（REQ-013）。</summary>
    private static NightState EndCare(NightState s, Tuning tuning)
    {
        var kind = s.ActiveCare!.Value;

        s = s with
        {
            Parent = ParentPhase.Sleeping,
            ActiveCare = null,
            CareRemain = 0,
            Vigor = Clamp(s.Vigor + tuning.CareVigor(kind), 0, tuning.VigorMax),
            Arousal = Clamp(
                s.Arousal + tuning.CareArousal(kind) - tuning.CareArousalDrop(kind), 0, tuning.ArousalMax),
            Habit = s.Habit.Plus(kind, tuning.CareHabit(kind)),
        };

        // **重い対処を引き出すほど親の体が削れる**（diagnosis.md 1 節: 疲労）。
        // トントンで収まった夜は「いとおしさ」に乗る
        var weight = tuning.DiagnosisWeight;

        s = kind switch
        {
            // トントンで収まった → いとおしさ
            CareKind.PatPat => s with { Diag = s.Diag.Add(AxisFondness, weight * 2) },

            // 抱っこ = 腕は使うが、救われてもいる
            CareKind.Hold => s with { Diag = s.Diag.Add(AxisFondness, weight) },

            // ミルク / オムツ替え = **重い対処**（diagnosis.md 1 節）→ 疲労
            _ => s with { Diag = s.Diag.Add(AxisFatigue, weight * 2) },
        };

        // 札ごとの代償（REQ-013。**どの対処も必ず何かを悪化させる**）
        if (kind == CareKind.PatPat)
        {
            // 手を押さえられて、ばたつかせられない
            s = s with { LockUntil = s.Tick + tuning.PatPatLockTicks, LockedKinds = ActionKind.Kick };
        }
        else if (kind == CareKind.Milk)
        {
            // 口が塞がって、泣けない
            s = s with { LockUntil = s.Tick + tuning.MilkLockTicks, LockedKinds = ActionKind.Cry };
        }
        else if (kind == CareKind.Hold)
        {
            // 落ち着いてしまって、しばらく何をしても効きが悪い
            s = s with { DampUntil = s.Tick + tuning.HoldDampTicks, DampMilli = tuning.HoldDampMilli };
        }
        else
        {
            // さっぱりして、しばらく何をしても効きが悪い
            s = s with { DampUntil = s.Tick + tuning.DiaperDampTicks, DampMilli = tuning.DiaperDampMilli };
        }

        return s;
    }

    // ==================================================================
    // 順 7: 状態値の更新
    // ==================================================================

    private static NightState UpdateValues(
        NightState s, string seed, Tuning tuning, int t,
        bool fired, ActionKind firedKind, int firedStrength)
    {
        var primed = false;

        if (fired)
        {
            s = s with
            {
                Baby = BabyPhase.Acting,
                ActKind = firedKind,
                ActRemain = tuning.ActTicks(firedKind),
                ActFired = true,
                ActStrengthMilli = 0,
                ChargeTicks = 0,
                TIdle = 0,
                CalmBlock = tuning.CalmBlockTicks,
            };

            // 元気の消費。強度の 1.7 乗（強く出すほど割に合わない）
            var cost = VigorCost(tuning.ActVigor(firedKind), firedStrength, tuning);

            s = s with { Vigor = Clamp(s.Vigor - cost, 0, tuning.VigorMax) };

            // **対処中と予告中は覚醒度を上げない**（REQ-049。順 3.5）
            var muted = s.Parent == ParentPhase.Caring || s.PendingCare is not null;
            var onGraceFire = !muted && s.Parent == ParentPhase.Grace;

            if (!muted)
            {
                var onGrace = onGraceFire;
                var gain = ArousalGain(s, tuning, firedKind, firedStrength, onGrace);

                s = s with { Arousal = Clamp(s.Arousal + gain, 0, tuning.ArousalMax) };

                if (onGrace)
                {
                    primed = true;
                }
            }

            s = AddFireDiagnosis(s, seed, tuning, t, firedKind, firedStrength, muted, onGraceFire);
            s = DrawCare(s, seed, tuning, firedKind, firedStrength);
        }

        s = s with { PretendPrimed = primed };

        // 覚醒度の自然低下。**騒いだ直後は落ち着かない**（REQ-030）
        if (s.CalmBlock > 0)
        {
            s = s with { CalmBlock = s.CalmBlock - 1 };
        }
        else
        {
            var period = s.Parent == ParentPhase.Up ? tuning.ArousalDecayUpTicks : tuning.ArousalDecaySleepTicks;

            if (t % period == 0)
            {
                s = s with { Arousal = Clamp(s.Arousal - 1, 0, tuning.ArousalMax) };
            }
        }

        // 元気の自然回復。**無操作の時計が進んでいる間だけ**（REQ-014）
        if (s.TIdle > 0 && s.TIdle % tuning.VigorRegenTicks == 0)
        {
            s = s with { Vigor = Clamp(s.Vigor + 1, 0, tuning.VigorMax) };
        }

        // 出来事「おなかが空く」の後遺症
        if (s.HungryUntil >= t && t % tuning.HungryDrainTicks == 0)
        {
            s = s with { Vigor = Clamp(s.Vigor - 1, 0, tuning.VigorMax) };
        }

        // **3 つ目の軸はランダムで乗る**（用途 Diagnosis。diagnosis.md 1 節「乗り方」）。
        //
        // **行動 1 回ごとには乗せない。**行動のたびにばらばらの軸へ乗せると、
        // どの軸も「行動した回数」に比例して伸び、7 軸が 1 軸に潰れる（TC-162）。
        // **夜を時間帯に区切り、その時間帯に 1 度だけ乗せる。**
        // こうすると「今夜はここに効いた」という偏りが夜ごとに変わる
        if (t % DiagnosisWindowTicks == 0)
        {
            var pick = Rng.Range(seed, RngPurpose.Diagnosis, t / DiagnosisWindowTicks, Diagnosis.Axes);

            s = s with { Diag = s.Diag.Add(pick, tuning.DiagnosisWeight * 2) };
        }

        // **元気があるのに静かにしている → いとおしさ**（diagnosis.md 1 節）
        if (!fired
            && s.Baby is BabyPhase.Open or BabyPhase.EyesClosed
            && s.Vigor >= tuning.VigorStage2
            && s.TIdle > 0
            && s.TIdle % FondnessTicks == 0)
        {
            s = s with { Diag = s.Diag.Add(AxisFondness, 1) };
        }

        // 慣れの自然減衰（REQ-029）
        if (s.TIdle > 0 && s.TIdle % tuning.HabitDecayTicks == 0)
        {
            s = s with { Habit = s.Habit.Decay(1) };
        }

        // 加点済みの解除（I-10 / REQ-052）
        if (s.ScoredEdge && s.Arousal <= tuning.Rearm)
        {
            s = s with { ScoredEdge = false };
        }

        return s;
    }

    /// <summary>
    /// 覚醒行動による覚醒度の上昇量。
    ///
    /// <code>
    /// ceil( 基礎値 × 強度 × max(床, 1 − 覚醒度/上限) × (1 − 慣れ/分母) × 減衰 )   最低 1
    /// </code>
    ///
    /// **床が無いと上昇量が上限の手前で 0 に漸近し、上限に到達しない**（balance.md 11 節の実測）。
    /// 覚醒度についての単調非増加は床を入れても保たれる（REQ-012 は「非増加」）。
    /// </summary>
    private static int ArousalGain(
        NightState s, Tuning tuning, ActionKind kind, int strengthMilli, bool onGrace)
    {
        var baseValue = onGrace ? tuning.GraceBase : tuning.ActBase(kind);
        var floor = onGrace ? tuning.GraceFloorMilli : tuning.ArousalFloorMilli;

        // 1/1000 固定小数だけで積む（浮動小数を通さない。NFR-004）
        var scale = 1000 - s.Arousal * 1000 / tuning.ArousalMax;

        if (scale < floor)
        {
            scale = floor;
        }

        var habit = 1000 - s.Habit.Max * 1000 / tuning.HabitDivisor;

        if (habit < 0)
        {
            habit = 0;
        }

        var damp = s.DampUntil > s.Tick ? 1000 - s.DampMilli : 1000;

        if (damp < 0)
        {
            damp = 0;
        }

        long milli = baseValue;

        milli = milli * strengthMilli / 1000;
        milli = milli * scale / 1000;
        milli = milli * habit / 1000;
        milli = milli * damp / 1000;

        var gain = (int)milli;

        if (gain < 1)
        {
            gain = 1;
        }

        if (!onGrace)
        {
            return gain;
        }

        // **寝入りばなは倍率で効かせる。**床（最低 1）に潰されると、軽く触れたときに差が消える
        var boosted = gain * tuning.GraceMultiplierMilli / 1000;

        return boosted <= gain ? gain + 1 : boosted;
    }

    /// <summary>元気の消費。強度の <c>VigorExponentNum / VigorExponentDen</c> 乗に比例する。</summary>
    private static int VigorCost(int baseCost, int strengthMilli, Tuning tuning)
    {
        // pow(x, 1.7) を整数で近似する: x^1 × x^0.7 を、x^0.7 ≒ x × (1000/x)^0.3 …ではなく
        // 「x^(num/den) = exp(num/den × ln x)」を使わず、**乗算と平方根だけで積む。**
        // num/den = 17/10 = 1 + 0.5 + 0.2 に丸めず、x^17 の den 乗根を段階的に取る。
        var x = strengthMilli;

        if (x <= 0)
        {
            return 1;
        }

        // x^(17/10) ≒ x^2 / x^(3/10)。x^(3/10) は x の 10 乗根の 3 乗だが、
        // ここでは **x^1.5（= x × sqrt(x)）と x^2 の間を線形に取る**近似で足りる。
        // 単調増加であること（REQ-060 の単調性）だけが要件で、正確な指数は要らない。
        var root = IntSqrt(x * 1000);          // sqrt(x) * 1000 相当（x は 1/1000 固定小数）
        var pow15 = (long)x * root / 1000;      // x^1.5
        var pow20 = (long)x * x / 1000;         // x^2

        // 17/10 は 1.5 と 2.0 の間を 0.4 の位置で内分する
        var num = tuning.VigorExponentNum;
        var den = tuning.VigorExponentDen;
        var frac = (num * 10 / den - 15) * 100 / 5;      // 0〜100
        var mixed = pow15 + (pow20 - pow15) * frac / 100;

        var cost = (int)(baseCost * mixed / 1000);

        return cost < 1 ? 1 : cost;
    }

    /// <summary>整数の平方根（Math.Sqrt を使わない。浮動小数を通さない）。</summary>
    private static int IntSqrt(long v)
    {
        if (v <= 0)
        {
            return 0;
        }

        var x = v;
        var y = (x + 1) / 2;

        while (y < x)
        {
            x = y;
            y = (x + v / x) / 2;
        }

        return (int)x;
    }

    /// <summary>親が対処を出すかどうかを引く。**確率は強度に比例**（ADR-0015）。</summary>
    private static NightState DrawCare(
        NightState s, string seed, Tuning tuning, ActionKind kind, int strengthMilli)
    {
        if (s.Parent is not (ParentPhase.Sleeping or ParentPhase.Up)
            || s.PendingCare is not null
            || s.Hand.Total <= 0)
        {
            return s;
        }

        var n = s.ChoiceN + 1;

        s = s with { ChoiceN = n };

        var prob = tuning.ActCareMilli(kind) * strengthMilli / 1000;

        if (!Rng.Chance(seed, RngPurpose.ParentChoice, n, prob))
        {
            return s;
        }

        var wanted = (CareKind)Rng.Range(seed, RngPurpose.ParentChoice, n + ChoiceKindOffset, CareKinds);

        // **対処までの遅れは山札の残量が少ないほど大きい**（REQ-046）
        var missing = tuning.CareDelayRef - s.Hand.Total;

        if (missing < 0)
        {
            missing = 0;
        }

        return s with
        {
            PendingCare = Available(s, wanted),
            CareDelay = tuning.CareDelayBase + missing * tuning.CareDelayStep,
        };
    }

    // ==================================================================
    // 順 8: 覚醒度に依存する親の遷移
    // ==================================================================

    private static NightState ParentArousalTransitions(NightState s, Tuning tuning)
    {
        if (s.Arousal >= tuning.ArousalMax)
        {
            if (s.Parent is ParentPhase.Sleeping or ParentPhase.Grace)
            {
                return s with { Parent = ParentPhase.Up, TGrace = 0, PretendPrimed = false };
            }

            if (s.Parent == ParentPhase.Caring)
            {
                return s with
                {
                    Parent = ParentPhase.Up,
                    ActiveCare = null,
                    CareRemain = 0,
                };
            }
        }

        // 寝入りばなに撃ち込んだが上限に届かなかった → 猶予は使い切られる
        if (s.PretendPrimed && s.Parent == ParentPhase.Grace)
        {
            return s with { Parent = ParentPhase.Sleeping, TGrace = 0, PretendPrimed = false };
        }

        if (s.Parent == ParentPhase.Up && s.Arousal <= tuning.Rearm)
        {
            return s with { Parent = ParentPhase.Sleeping };
        }

        return s;
    }

    // ==================================================================
    // 診断（ADR-0016 / REQ-062）。**単調非減少**（I-12）
    // ==================================================================

    /// <summary>
    /// 覚醒行動が診断に乗る（diagnosis.md 1 節「乗り方」）。
    ///
    /// **1 つの行動が 1〜3 個の軸に乗る。乗る先は親の状態と行動の種類で変わる。**
    /// 3 つ目は用途 <see cref="RngPurpose.Diagnosis"/> の乱数で決まる（**実行時の乱数を使わない**）。
    /// 値は減らない（I-12）。
    /// </summary>
    private static NightState AddFireDiagnosis(
        NightState s, string seed, Tuning tuning, int t,
        ActionKind kind, int strengthMilli, bool muted, bool onGrace)
    {
        var w = tuning.DiagnosisWeight;
        // **いらだちは「回数」で効く**（diagnosis.md 1 節「連続して泣く」）。
        // 強度だけに比例させると、強く撃つ方針が全軸で高くなり、軸が 1 本に潰れる（TC-162）
        var amount = w / 2 + w * strengthMilli / 4000;

        if (amount < 1)
        {
            amount = 1;
        }

        var d = s.Diag;

        if (muted)
        {
            // 対処の最中・予告中に割り込んだ → 翻弄（空振り）
            d = d.Add(AxisJerked, amount * 2);
        }
        else if (onGrace && strengthMilli >= GraceIntentMilli)
        {
            // **溜めて寝入りばなに撃ち込んだ** → 寝不足（睡眠の断片化）
            d = d.Add(AxisSleepLoss, amount * 3);
        }
        else
        {
            // **対処の直後にまた泣かれると「分からなさ」になる**（diagnosis.md 1 節: 不安）。
            // 直後かどうかは、対処の副作用（封じ・減衰）がまだ効いていることで分かる
            var justAfterCare = s.LockUntil > s.Tick || s.DampUntil > s.Tick;

            d = kind switch
            {
                // ばたつかせる = 体を動かされる → 疲労（実測で「ばたつき型」が疲労最大）
                ActionKind.Kick => d.Add(AxisFatigue, amount),

                // 対処の直後にまた声を出した → 不安
                _ when justAfterCare => d.Add(AxisAnxiety, amount * 2),

                // 泣く / ぐずる = 音の量 → いらだち
                _ => d.Add(AxisIrritation, amount),
            };
        }

        return s with { Diag = d };
    }

    // ==================================================================
    // 補助
    // ==================================================================

    /// <summary><c>n</c> 回目の寝たふりの成功率（1/1000）。**単調非増加で下限 > 0**（REQ-017）。</summary>
    private static int PretendMilli(int n, Tuning tuning)
    {
        long milli = tuning.PretendBaseMilli;

        for (var i = 1; i < n; i++)
        {
            milli = milli * tuning.PretendDecayMilli / 1000;

            if (milli <= tuning.PretendFloorMilli)
            {
                return tuning.PretendFloorMilli;
            }
        }

        return (int)milli;
    }

    private static int Clamp(int v, int lo, int hi) => v < lo ? lo : v > hi ? hi : v;
}

/// <summary>
/// MOD-Score — 得点とエッジ検出。
///
/// 仕様: docs/30_detailed_design/MOD-Score.md（SC-1〜SC-5）
/// </summary>
public static class Score
{
    /// <summary>
    /// 覚醒度が上限に達した瞬間に 1 回だけ加点する。**`NightEnd` より先に呼ぶ**（REQ-051）。
    /// </summary>
    public static NightState Apply(NightState s, Tuning tuning)
    {
        tuning.Validate();

        if (s.Over is not null)
        {
            return s;
        }

        if (!s.ScoredEdge && s.Arousal >= tuning.ArousalMax)
        {
            return s with { Score = s.Score + 1, ScoredEdge = true };
        }

        return s;
    }

    /// <summary>その日の最高成績を選ぶ。同点なら**先に遊んだほう**を残す（REQ-025）。</summary>
    public static bool IsBetter(BestPlay candidate, BestPlay? current) =>
        current is null || candidate.Score > current.Value.Score;
}

/// <summary>
/// MOD-End — 夜の終わり。
///
/// 仕様: docs/30_detailed_design/MOD-End.md（判定順 Dawn → FellAsleep → HandEmpty）
/// </summary>
public static class NightEnd
{
    /// <summary>
    /// 終了していれば <see cref="EndKind"/>、していなければ null。
    /// **`Score` の後に呼ぶ**（REQ-051。最後の 1 枚の得点は終了判定より先）。
    /// </summary>
    public static EndKind? Evaluate(NightState s, Tuning tuning)
    {
        tuning.Validate();

        // E-4: 一度 Over が付いたら変わらない
        if (s.Over is not null)
        {
            return s.Over;
        }

        // **Tick > NightTicks でも例外にしない**（復元時に境界を跨ぐことがある）
        if (s.Tick >= tuning.NightTicks)
        {
            return EndKind.Dawn;
        }

        if (s.Dozed)
        {
            return EndKind.FellAsleep;
        }

        // E-3: 対処の途中では終わらない（最後の対処は最後まで受けられる）
        if (s.Hand.Total == 0 && s.Parent != ParentPhase.Caring)
        {
            return EndKind.HandEmpty;
        }

        return null;
    }
}
