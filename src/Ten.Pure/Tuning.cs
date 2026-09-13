using System;

namespace Ten.Pure;

/// <summary>
/// 調整値。**数値を型に埋め込まず、外から渡す**（types.md 5 節）。
///
/// 値の実体は docs/20_basic_design/balance.md。**ここは balance.md の写しであって、
/// 出典ではない。**[ADR-0012](../../docs/10_requirements/decisions/ADR-0012-balance-vs-tests.md)
/// により、テストの期待値にこれらの具体値を書かない。だから**ここは自由に動かせる。**
///
/// **既定値は「遊べる夜」になる値。**`new Tuning()` がそのまま本番の調整値になる
/// （ハーネスは中身を読まずに素通しする）。壊れた値は <see cref="Validate"/> が弾く。
/// </summary>
public readonly record struct Tuning
{
    public Tuning()
    {
    }

    // ------------------------------------------------------------------
    // 1. 時間の骨格（balance.md 1 節）
    // ------------------------------------------------------------------

    /// <summary>1 夜の長さ（tick）。20 Hz で 4 分 30 秒。</summary>
    public int NightTicks { get; init; } = 5400;

    /// <summary>「夜明け直前」と確定して分かる区間（REQ-045 の端点）。</summary>
    public int DawnImminentTicks { get; init; } = 120;

    // ------------------------------------------------------------------
    // 2. 状態値の範囲（balance.md 2 節）
    // ------------------------------------------------------------------

    public int ArousalMax { get; init; } = 100;

    /// <summary>再加点に必要な低下（REQ-052）。</summary>
    public int Rearm { get; init; } = 55;

    public int VigorMax { get; init; } = 100;

    public int VigorStart { get; init; } = 100;

    public int HabitMax { get; init; } = 100;

    /// <summary>親が寝ている間の覚醒度の自然低下（−1 / n tick）。</summary>
    public int ArousalDecaySleepTicks { get; init; } = 25;

    /// <summary>親がベッドを出ている間の自然低下（−1 / n tick）。</summary>
    public int ArousalDecayUpTicks { get; init; } = 12;

    /// <summary>覚醒行動 1 回が、親の落ち着きを止める長さ（REQ-030）。</summary>
    public int CalmBlockTicks { get; init; } = 60;

    /// <summary>元気の自然回復（+1 / n tick）。無操作の時計が進んでいる間だけ。</summary>
    public int VigorRegenTicks { get; init; } = 8;

    /// <summary>慣れの自然減衰（−1 / n tick。REQ-029）。</summary>
    public int HabitDecayTicks { get; init; } = 60;

    /// <summary>慣れの割引の分母。100 で半減になるよう 200。</summary>
    public int HabitDivisor { get; init; } = 200;

    // ------------------------------------------------------------------
    // 3. 覚醒行動（balance.md 3 節）
    // ------------------------------------------------------------------

    /// <summary>上昇量が覚醒度で割り引かれる下限（**これが無いと上限に到達しない**）。</summary>
    public int ArousalFloorMilli { get; init; } = 250;

    public int CryBase { get; init; } = 45;
    public int CryVigor { get; init; } = 20;
    public int CryTicks { get; init; } = 40;
    public int CryCareMilli { get; init; } = 800;

    public int FussBase { get; init; } = 15;
    public int FussVigor { get; init; } = 11;
    public int FussTicks { get; init; } = 24;
    public int FussCareMilli { get; init; } = 560;

    public int KickBase { get; init; } = 14;
    public int KickVigor { get; init; } = 12;
    public int KickTicks { get; init; } = 32;
    public int KickCareMilli { get; init; } = 900;

    /// <summary>**寝入りばな（`Grace`）に当てた一撃**（REQ-015 / 061）。その夜のどの行動より大きい。</summary>
    public int GraceBase { get; init; } = 45;

    /// <summary>寝入りばなの一撃だけ、覚醒度による割引の下限が高い。</summary>
    public int GraceFloorMilli { get; init; } = 550;

    /// <summary>寝入りばなに当てたときの倍率（1/1000）。**軽く触れただけでも差が出る**（REQ-061）。</summary>
    public int GraceMultiplierMilli { get; init; } = 1100;

    // ------------------------------------------------------------------
    // 4. 長押しの強度（ADR-0015 / REQ-060）
    // ------------------------------------------------------------------

    /// <summary>最大強度まで溜めるのに要る tick 数。</summary>
    public int ChargeTicks { get; init; } = 70;

    /// <summary>溜めている間の元気の減り（−1 / n tick）。**代償が無いと常に最大が正解になる。**</summary>
    public int ChargeDrainTicks { get; init; } = 16;

    /// <summary>元気の消費が強度の何乗に比例するか（1.7 乗）。分子 / 分母。</summary>
    public int VigorExponentNum { get; init; } = 17;

    public int VigorExponentDen { get; init; } = 10;

    // ------------------------------------------------------------------
    // 5. 親の対処（balance.md 4 節）
    // ------------------------------------------------------------------

    public int PatPatArousal { get; init; } = 5;
    public int PatPatVigor { get; init; } = 15;
    public int PatPatHabit { get; init; } = 25;
    public int PatPatTicks { get; init; } = 100;

    /// <summary>トントンの代償: 手を押さえられて「ばたつかせる」が封じられる。</summary>
    public int PatPatLockTicks { get; init; } = 60;

    public int MilkArousal { get; init; } = 10;
    public int MilkVigor { get; init; } = 40;
    public int MilkHabit { get; init; } = 20;
    public int MilkTicks { get; init; } = 160;

    /// <summary>ミルクの代償: 長い。終わったとき親が寝直しやすい。</summary>
    public int MilkArousalDrop { get; init; } = 6;

    /// <summary>ミルクの代償: 口が塞がって「泣く」がしばらく出せない。</summary>
    public int MilkLockTicks { get; init; } = 50;

    public int HoldArousal { get; init; } = 25;
    public int HoldVigor { get; init; } = 10;
    public int HoldHabit { get; init; } = 30;
    public int HoldTicks { get; init; } = 120;

    /// <summary>抱っこの代償: 落ち着いて、しばらく行動が効かない。</summary>
    public int HoldDampTicks { get; init; } = 60;

    /// <summary>減衰の強さ（1/1000。500 = 半減）。</summary>
    public int HoldDampMilli { get; init; } = 500;

    public int DiaperArousal { get; init; } = 30;
    public int DiaperVigor { get; init; } = 5;
    public int DiaperHabit { get; init; } = 15;
    public int DiaperTicks { get; init; } = 140;

    /// <summary>オムツ替えの代償: 終わると親が寝直しやすい（覚醒度 −n）。</summary>
    public int DiaperArousalDrop { get; init; } = 10;

    /// <summary>オムツ替えの代償: さっぱりして落ち着き、しばらく行動が効かない。</summary>
    public int DiaperDampTicks { get; init; } = 50;

    public int DiaperDampMilli { get; init; } = 400;

    /// <summary>対処までの遅れ（REQ-046）。**残量が少ないほど大きい。**</summary>
    public int CareDelayBase { get; init; } = 20;

    public int CareDelayStep { get; init; } = 6;

    /// <summary>遅れを測る基準の枚数。</summary>
    public int CareDelayRef { get; init; } = 14;

    // ------------------------------------------------------------------
    // 6. 山札（balance.md 4 節 / REQ-041）
    // ------------------------------------------------------------------

    public int HandMin { get; init; } = 8;

    public int HandMax { get; init; } = 14;

    /// <summary>各札種の最低枚数（REQ-041）。</summary>
    public int HandPerKindMin { get; init; } = 1;

    // ------------------------------------------------------------------
    // 7. 出来事（balance.md 7 節 / REQ-043 / 055）
    // ------------------------------------------------------------------

    public int EventMin { get; init; } = 4;

    public int EventMax { get; init; } = 5;

    /// <summary>夜を何等分した区間に置くか。</summary>
    public int EventSegments { get; init; } = 10;

    public int DiaperSoiledCards { get; init; } = 1;
    public int PartnerCards { get; init; } = 2;
    public int PhoneArousal { get; init; } = 8;
    public int RollTicks { get; init; } = 200;
    public int HungryTicks { get; init; } = 400;
    public int HungryDrainTicks { get; init; } = 40;

    // ------------------------------------------------------------------
    // 8. 寝たふり（balance.md 5 節）
    // ------------------------------------------------------------------

    /// <summary>寝たふりが成立する閉眼の長さ。</summary>
    public int ClosedThresholdTicks { get; init; } = 60;

    /// <summary>抱き上げ〜着地。**固定 tick**（D-07）。`Settling` と `Feint` で同じ。</summary>
    public int SettleTicks { get; init; } = 60;

    /// <summary>寝入りばな（着地後の猶予 / 深い眠りに落ちる瞬間）の長さ。</summary>
    public int GraceTicks { get; init; } = 36;

    /// <summary>親が自然に寝入りばなへ落ちる周期（REQ-061）。**乱数を使わない**（予兆が読めるため）。</summary>
    public int DrowsyPeriodTicks { get; init; } = 300;

    /// <summary>1 回目の寝たふりの成功率（1/1000）。</summary>
    public int PretendBaseMilli { get; init; } = 700;

    /// <summary>2 回目以降の減衰率（1/1000）。</summary>
    public int PretendDecayMilli { get; init; } = 600;

    /// <summary>成功率の下限（**0 にしない**。REQ-017）。</summary>
    public int PretendFloorMilli { get; init; } = 100;

    /// <summary>寝たふり中に本当に寝てしまう確率（REQ-018）。</summary>
    public int FallAsleepMilli { get; init; } = 40;

    // ------------------------------------------------------------------
    // 9. 待機中の寝落ち（balance.md 6 節 / REQ-047）
    // ------------------------------------------------------------------

    public int DozeIdleTicks { get; init; } = 2800;

    public int DozeStepTicks { get; init; } = 40;

    public int DozeProbMilli { get; init; } = 250;

    // ------------------------------------------------------------------
    // 10. 盤面の初期値
    // ------------------------------------------------------------------

    /// <summary>親の初期覚醒度の下限。**2 桁にしてある**（結果テキストへの数字の漏れを避ける。REQ-028）。</summary>
    public int InitialArousalMin { get; init; } = 10;

    /// <summary>親の初期覚醒度の上限。**上限未満**（開始時に得点済みにしない。B-5）。</summary>
    public int InitialArousalMax { get; init; } = 45;

    // ------------------------------------------------------------------
    // 11. 見た目の段階（balance.md 8 節）
    // ------------------------------------------------------------------

    public int ArousalStage1 { get; init; } = 25;
    public int ArousalStage2 { get; init; } = 60;
    public int ArousalStage3 { get; init; } = 90;

    public int VigorStage1 { get; init; } = 30;
    public int VigorStage2 { get; init; } = 70;

    /// <summary>終盤（1〜2 枚）と中盤の境。</summary>
    public int HandStage1 { get; init; } = 3;

    /// <summary>中盤と序盤（6 枚以上）の境。</summary>
    public int HandStage2 { get; init; } = 6;

    /// <summary>残り時間の 1 段階の長さ。</summary>
    public int TimeStageTicks { get; init; } = 1080;

    // ------------------------------------------------------------------
    // 12. 診断（diagnosis.md / ADR-0016 / REQ-062）
    // ------------------------------------------------------------------

    /// <summary>合計がこれ未満なら `DX-41` を固定で返す（RS-8）。</summary>
    public int DiagnosisMinTotal { get; init; } = 40;

    /// <summary>「うっすら」が付く合計の上限。</summary>
    public int DiagnosisThinTotal { get; init; } = 200;

    /// <summary>「どっぷり」が付く合計の下限。</summary>
    public int DiagnosisThickTotal { get; init; } = 900;

    /// <summary>診断の 1 回あたりの重み（1/10 単位）。</summary>
    public int DiagnosisWeight { get; init; } = 10;

    // ------------------------------------------------------------------
    // 検証
    // ------------------------------------------------------------------

    /// <summary>
    /// 壊れた調整値を弾く。**丸めて続行しない**（壊れた値で夜が進むと原因が分からなくなる）。
    /// </summary>
    /// <exception cref="ArgumentException">範囲が成立しない</exception>
    internal void Validate()
    {
        Require(NightTicks > 0, "NightTicks は 1 以上");
        Require(ArousalMax > 0, "ArousalMax は 1 以上");
        Require(VigorMax > 0, "VigorMax は 1 以上");
        Require(HabitMax > 0, "HabitMax は 1 以上");
        Require(HabitDivisor > 0, "HabitDivisor は 1 以上");

        // MOD-Score のエラー時: 再加点が不可能な設定を弾く
        Require(Rearm < ArousalMax, "Rearm は ArousalMax 未満（再加点が永久にできない）");
        Require(Rearm >= 0, "Rearm は 0 以上");

        // MOD-Board のエラー時
        Require(HandMin <= HandMax, "HandMin は HandMax 以下");
        Require(HandPerKindMin >= 1, "HandPerKindMin は 1 以上（REQ-041）");
        Require(HandMin >= HandPerKindMin * 4, "HandMin が札種の数に足りない");
        Require(EventMin >= 1 && EventMin <= EventMax, "EventMin / EventMax の範囲が不正");
        Require(EventSegments >= EventMax, "EventSegments は EventMax 以上（同じ tick に 2 件置かない）");
        Require(InitialArousalMin >= 0 && InitialArousalMin <= InitialArousalMax,
            "InitialArousal の範囲が不正");
        Require(InitialArousalMax < ArousalMax, "InitialArousalMax は ArousalMax 未満（B-5）");

        Require(ChargeTicks > 0, "ChargeTicks は 1 以上");
        Require(ChargeDrainTicks > 0, "ChargeDrainTicks は 1 以上");
        Require(VigorExponentDen > 0, "VigorExponentDen は 1 以上");
        Require(VigorRegenTicks > 0, "VigorRegenTicks は 1 以上");
        Require(HabitDecayTicks > 0, "HabitDecayTicks は 1 以上");
        Require(ArousalDecaySleepTicks > 0 && ArousalDecayUpTicks > 0, "覚醒度の低下周期は 1 以上");

        Require(ClosedThresholdTicks > 0, "ClosedThresholdTicks は 1 以上");
        Require(SettleTicks > 0, "SettleTicks は 1 以上");
        Require(GraceTicks > 0, "GraceTicks は 1 以上");
        Require(DrowsyPeriodTicks > GraceTicks, "DrowsyPeriodTicks は GraceTicks より長い");
        Require(PretendFloorMilli > 0, "PretendFloorMilli は 1 以上（REQ-017 の下限 > 0）");
        Require(PretendBaseMilli >= PretendFloorMilli, "PretendBaseMilli は下限以上");
        Require(PretendDecayMilli > 0 && PretendDecayMilli <= 1000, "PretendDecayMilli は 1〜1000");
        Require(DozeStepTicks > 0, "DozeStepTicks は 1 以上");
        Require(TimeStageTicks > 0, "TimeStageTicks は 1 以上");
        Require(DiagnosisWeight > 0, "DiagnosisWeight は 1 以上");
        Require(GraceMultiplierMilli > 1000, "GraceMultiplierMilli は 1000 より大きい（REQ-061）");
        Require(EventMax <= 5, "EventMax は出来事の種類数（5）以下（同じ夜に同じ出来事を重ねない）");
    }

    private static void Require(bool ok, string why)
    {
        if (!ok)
        {
            throw new ArgumentException($"Tuning が壊れている: {why}", "tuning");
        }
    }

    // ------------------------------------------------------------------
    // 種類ごとの引き当て（配列を持たないのは、値の等価比較を保つため）
    // ------------------------------------------------------------------

    internal int ActBase(ActionKind k) => k switch
    {
        ActionKind.Cry => CryBase,
        ActionKind.Fuss => FussBase,
        ActionKind.Kick => KickBase,
        _ => CryBase,
    };

    internal int ActVigor(ActionKind k) => k switch
    {
        ActionKind.Cry => CryVigor,
        ActionKind.Fuss => FussVigor,
        ActionKind.Kick => KickVigor,
        _ => CryVigor,
    };

    internal int ActTicks(ActionKind k) => k switch
    {
        ActionKind.Cry => CryTicks,
        ActionKind.Fuss => FussTicks,
        ActionKind.Kick => KickTicks,
        _ => CryTicks,
    };

    internal int ActCareMilli(ActionKind k) => k switch
    {
        ActionKind.Cry => CryCareMilli,
        ActionKind.Fuss => FussCareMilli,
        ActionKind.Kick => KickCareMilli,
        _ => CryCareMilli,
    };

    internal int CareArousal(CareKind k) => k switch
    {
        CareKind.PatPat => PatPatArousal,
        CareKind.Milk => MilkArousal,
        CareKind.Hold => HoldArousal,
        CareKind.DiaperChange => DiaperArousal,
        _ => PatPatArousal,
    };

    internal int CareVigor(CareKind k) => k switch
    {
        CareKind.PatPat => PatPatVigor,
        CareKind.Milk => MilkVigor,
        CareKind.Hold => HoldVigor,
        CareKind.DiaperChange => DiaperVigor,
        _ => PatPatVigor,
    };

    internal int CareHabit(CareKind k) => k switch
    {
        CareKind.PatPat => PatPatHabit,
        CareKind.Milk => MilkHabit,
        CareKind.Hold => HoldHabit,
        CareKind.DiaperChange => DiaperHabit,
        _ => PatPatHabit,
    };

    internal int CareTicks(CareKind k) => k switch
    {
        CareKind.PatPat => PatPatTicks,
        CareKind.Milk => MilkTicks,
        CareKind.Hold => HoldTicks,
        CareKind.DiaperChange => DiaperTicks,
        _ => PatPatTicks,
    };

    /// <summary>対処が終わったときの覚醒度の下げ幅（代償。REQ-013）。</summary>
    internal int CareArousalDrop(CareKind k) => k switch
    {
        CareKind.Milk => MilkArousalDrop,
        CareKind.DiaperChange => DiaperArousalDrop,
        _ => 0,
    };

    // ------------------------------------------------------------------
    // 等価比較とハッシュ
    //
    // **自動生成に任せない。**項目が 90 近くあるので、コンパイラが作る
    // 「1 つの式」が IL2CPP の吐く C++ で入れ子 256 段を超え、
    // **Android ビルドが通らなくなる**（clang: bracket nesting level exceeded）。
    // 1 項目 1 文で書く。
    // ------------------------------------------------------------------

    public bool Equals(Tuning other)
    {
        if (NightTicks != other.NightTicks) { return false; }
        if (DawnImminentTicks != other.DawnImminentTicks) { return false; }
        if (ArousalMax != other.ArousalMax) { return false; }
        if (Rearm != other.Rearm) { return false; }
        if (VigorMax != other.VigorMax) { return false; }
        if (VigorStart != other.VigorStart) { return false; }
        if (HabitMax != other.HabitMax) { return false; }
        if (ArousalDecaySleepTicks != other.ArousalDecaySleepTicks) { return false; }
        if (ArousalDecayUpTicks != other.ArousalDecayUpTicks) { return false; }
        if (CalmBlockTicks != other.CalmBlockTicks) { return false; }
        if (VigorRegenTicks != other.VigorRegenTicks) { return false; }
        if (HabitDecayTicks != other.HabitDecayTicks) { return false; }
        if (HabitDivisor != other.HabitDivisor) { return false; }
        if (ArousalFloorMilli != other.ArousalFloorMilli) { return false; }
        if (CryBase != other.CryBase) { return false; }
        if (CryVigor != other.CryVigor) { return false; }
        if (CryTicks != other.CryTicks) { return false; }
        if (CryCareMilli != other.CryCareMilli) { return false; }
        if (FussBase != other.FussBase) { return false; }
        if (FussVigor != other.FussVigor) { return false; }
        if (FussTicks != other.FussTicks) { return false; }
        if (FussCareMilli != other.FussCareMilli) { return false; }
        if (KickBase != other.KickBase) { return false; }
        if (KickVigor != other.KickVigor) { return false; }
        if (KickTicks != other.KickTicks) { return false; }
        if (KickCareMilli != other.KickCareMilli) { return false; }
        if (GraceBase != other.GraceBase) { return false; }
        if (GraceFloorMilli != other.GraceFloorMilli) { return false; }
        if (GraceMultiplierMilli != other.GraceMultiplierMilli) { return false; }
        if (ChargeTicks != other.ChargeTicks) { return false; }
        if (ChargeDrainTicks != other.ChargeDrainTicks) { return false; }
        if (VigorExponentNum != other.VigorExponentNum) { return false; }
        if (VigorExponentDen != other.VigorExponentDen) { return false; }
        if (PatPatArousal != other.PatPatArousal) { return false; }
        if (PatPatVigor != other.PatPatVigor) { return false; }
        if (PatPatHabit != other.PatPatHabit) { return false; }
        if (PatPatTicks != other.PatPatTicks) { return false; }
        if (PatPatLockTicks != other.PatPatLockTicks) { return false; }
        if (MilkArousal != other.MilkArousal) { return false; }
        if (MilkVigor != other.MilkVigor) { return false; }
        if (MilkHabit != other.MilkHabit) { return false; }
        if (MilkTicks != other.MilkTicks) { return false; }
        if (MilkArousalDrop != other.MilkArousalDrop) { return false; }
        if (MilkLockTicks != other.MilkLockTicks) { return false; }
        if (HoldArousal != other.HoldArousal) { return false; }
        if (HoldVigor != other.HoldVigor) { return false; }
        if (HoldHabit != other.HoldHabit) { return false; }
        if (HoldTicks != other.HoldTicks) { return false; }
        if (HoldDampTicks != other.HoldDampTicks) { return false; }
        if (HoldDampMilli != other.HoldDampMilli) { return false; }
        if (DiaperArousal != other.DiaperArousal) { return false; }
        if (DiaperVigor != other.DiaperVigor) { return false; }
        if (DiaperHabit != other.DiaperHabit) { return false; }
        if (DiaperTicks != other.DiaperTicks) { return false; }
        if (DiaperArousalDrop != other.DiaperArousalDrop) { return false; }
        if (DiaperDampTicks != other.DiaperDampTicks) { return false; }
        if (DiaperDampMilli != other.DiaperDampMilli) { return false; }
        if (CareDelayBase != other.CareDelayBase) { return false; }
        if (CareDelayStep != other.CareDelayStep) { return false; }
        if (CareDelayRef != other.CareDelayRef) { return false; }
        if (HandMin != other.HandMin) { return false; }
        if (HandMax != other.HandMax) { return false; }
        if (HandPerKindMin != other.HandPerKindMin) { return false; }
        if (EventMin != other.EventMin) { return false; }
        if (EventMax != other.EventMax) { return false; }
        if (EventSegments != other.EventSegments) { return false; }
        if (DiaperSoiledCards != other.DiaperSoiledCards) { return false; }
        if (PartnerCards != other.PartnerCards) { return false; }
        if (PhoneArousal != other.PhoneArousal) { return false; }
        if (RollTicks != other.RollTicks) { return false; }
        if (HungryTicks != other.HungryTicks) { return false; }
        if (HungryDrainTicks != other.HungryDrainTicks) { return false; }
        if (ClosedThresholdTicks != other.ClosedThresholdTicks) { return false; }
        if (SettleTicks != other.SettleTicks) { return false; }
        if (GraceTicks != other.GraceTicks) { return false; }
        if (DrowsyPeriodTicks != other.DrowsyPeriodTicks) { return false; }
        if (PretendBaseMilli != other.PretendBaseMilli) { return false; }
        if (PretendDecayMilli != other.PretendDecayMilli) { return false; }
        if (PretendFloorMilli != other.PretendFloorMilli) { return false; }
        if (FallAsleepMilli != other.FallAsleepMilli) { return false; }
        if (DozeIdleTicks != other.DozeIdleTicks) { return false; }
        if (DozeStepTicks != other.DozeStepTicks) { return false; }
        if (DozeProbMilli != other.DozeProbMilli) { return false; }
        if (InitialArousalMin != other.InitialArousalMin) { return false; }
        if (InitialArousalMax != other.InitialArousalMax) { return false; }
        if (ArousalStage1 != other.ArousalStage1) { return false; }
        if (ArousalStage2 != other.ArousalStage2) { return false; }
        if (ArousalStage3 != other.ArousalStage3) { return false; }
        if (VigorStage1 != other.VigorStage1) { return false; }
        if (VigorStage2 != other.VigorStage2) { return false; }
        if (HandStage1 != other.HandStage1) { return false; }
        if (HandStage2 != other.HandStage2) { return false; }
        if (TimeStageTicks != other.TimeStageTicks) { return false; }
        if (DiagnosisMinTotal != other.DiagnosisMinTotal) { return false; }
        if (DiagnosisThinTotal != other.DiagnosisThinTotal) { return false; }
        if (DiagnosisThickTotal != other.DiagnosisThickTotal) { return false; }
        if (DiagnosisWeight != other.DiagnosisWeight) { return false; }

        return true;
    }

    public override int GetHashCode()
    {
        // **畳み込みを断つ。**`hash = hash * 31 + X;` を 97 回並べると、
        // C# コンパイラが 1 つの式にまとめ、IL2CPP の C++ が入れ子 256 段を超える。
        // 配列に詰めてから回すと、式が段ごとに切れる
        var values = new int[97];

        values[0] = NightTicks;
        values[1] = DawnImminentTicks;
        values[2] = ArousalMax;
        values[3] = Rearm;
        values[4] = VigorMax;
        values[5] = VigorStart;
        values[6] = HabitMax;
        values[7] = ArousalDecaySleepTicks;
        values[8] = ArousalDecayUpTicks;
        values[9] = CalmBlockTicks;
        values[10] = VigorRegenTicks;
        values[11] = HabitDecayTicks;
        values[12] = HabitDivisor;
        values[13] = ArousalFloorMilli;
        values[14] = CryBase;
        values[15] = CryVigor;
        values[16] = CryTicks;
        values[17] = CryCareMilli;
        values[18] = FussBase;
        values[19] = FussVigor;
        values[20] = FussTicks;
        values[21] = FussCareMilli;
        values[22] = KickBase;
        values[23] = KickVigor;
        values[24] = KickTicks;
        values[25] = KickCareMilli;
        values[26] = GraceBase;
        values[27] = GraceFloorMilli;
        values[28] = GraceMultiplierMilli;
        values[29] = ChargeTicks;
        values[30] = ChargeDrainTicks;
        values[31] = VigorExponentNum;
        values[32] = VigorExponentDen;
        values[33] = PatPatArousal;
        values[34] = PatPatVigor;
        values[35] = PatPatHabit;
        values[36] = PatPatTicks;
        values[37] = PatPatLockTicks;
        values[38] = MilkArousal;
        values[39] = MilkVigor;
        values[40] = MilkHabit;
        values[41] = MilkTicks;
        values[42] = MilkArousalDrop;
        values[43] = MilkLockTicks;
        values[44] = HoldArousal;
        values[45] = HoldVigor;
        values[46] = HoldHabit;
        values[47] = HoldTicks;
        values[48] = HoldDampTicks;
        values[49] = HoldDampMilli;
        values[50] = DiaperArousal;
        values[51] = DiaperVigor;
        values[52] = DiaperHabit;
        values[53] = DiaperTicks;
        values[54] = DiaperArousalDrop;
        values[55] = DiaperDampTicks;
        values[56] = DiaperDampMilli;
        values[57] = CareDelayBase;
        values[58] = CareDelayStep;
        values[59] = CareDelayRef;
        values[60] = HandMin;
        values[61] = HandMax;
        values[62] = HandPerKindMin;
        values[63] = EventMin;
        values[64] = EventMax;
        values[65] = EventSegments;
        values[66] = DiaperSoiledCards;
        values[67] = PartnerCards;
        values[68] = PhoneArousal;
        values[69] = RollTicks;
        values[70] = HungryTicks;
        values[71] = HungryDrainTicks;
        values[72] = ClosedThresholdTicks;
        values[73] = SettleTicks;
        values[74] = GraceTicks;
        values[75] = DrowsyPeriodTicks;
        values[76] = PretendBaseMilli;
        values[77] = PretendDecayMilli;
        values[78] = PretendFloorMilli;
        values[79] = FallAsleepMilli;
        values[80] = DozeIdleTicks;
        values[81] = DozeStepTicks;
        values[82] = DozeProbMilli;
        values[83] = InitialArousalMin;
        values[84] = InitialArousalMax;
        values[85] = ArousalStage1;
        values[86] = ArousalStage2;
        values[87] = ArousalStage3;
        values[88] = VigorStage1;
        values[89] = VigorStage2;
        values[90] = HandStage1;
        values[91] = HandStage2;
        values[92] = TimeStageTicks;
        values[93] = DiagnosisMinTotal;
        values[94] = DiagnosisThinTotal;
        values[95] = DiagnosisThickTotal;
        values[96] = DiagnosisWeight;

        var hash = 17;

        for (var i = 0; i < values.Length; i++)
        {
            hash = hash * 31 + values[i];
        }

        return hash;
    }
}
