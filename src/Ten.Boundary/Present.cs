using System;
using Ten.Pure;

namespace Ten.Boundary;

/// <summary>母の体（顔と姿勢）の動き。**名前は Codex のアニメーション名（r57）と 1 対 1**。</summary>
public enum BodyClip
{
    /// <summary>映さない（部屋にいない / 目を閉じている）</summary>
    Hidden,
    BreathDeep,
    BreathLight,
    BreathHalf,
    BreathAwake,
    DozeWarn,
    DozeDrop,
    TurnAway,
    Carry,
}

/// <summary>母の手の動き。</summary>
public enum HandClip { Rest, Reach, PatSteady, PatRough, PatStall, CareMilk, CareHold, CareDiaper }

/// <summary>赤ちゃん（視界そのもの）の動き。</summary>
public enum BabyMotion { Breathe, Charge, Cry, Fuss, Kick }

/// <summary>
/// 1 フレームの見せ方。**表示層はこれだけを見て描く。**
/// </summary>
/// <param name="Reaching">手が伸びてきているとき、何の対処が来るか（REQ-006）</param>
/// <param name="Window">窓の明るさ 0〜5。5 は夜明け直前（端点）</param>
/// <param name="Vignette">視界の周辺の沈み 0〜2</param>
/// <param name="CarryTick">運ばれている間の経過 tick。**成否に依存しない**</param>
public readonly record struct Presentation(
    bool EyesClosed,
    BodyClip Body,
    HandClip Hand,
    CareKind? Reaching,
    bool Sniff,
    bool LampOn,
    bool PhoneFlash,
    bool PartnerHere,
    int Window,
    int Vignette,
    bool CameraLifted,
    bool BottleInFace,
    BabyMotion Baby,
    int BabyStrengthMilli,
    int CarryTick
);

/// <summary>
/// MOD-Present — 状態 → 見せ方（アセットの差し込み口）。
///
/// 仕様: docs/30_detailed_design/MOD-Present.md（PR-1〜PR-7）/ setting.md 4〜8 節
///
/// **判断をしない写像。**ゲームの結果はここで変わらない。
/// Unity の描画から切り離してあるので、REQ-016（寝たふりの成否が見た目に漏れない）を
/// `dotnet test` で見張れる。
/// </summary>
public static class PresentRule
{
    /// <summary>寝入りばなの予兆の長さ（setting.md 4.2 の 4.5 秒。1 tick = 50 ms）。**見せ方の値**。</summary>
    public const int DozeWarnTicks = 90;

    /// <summary>オムツが汚れたとき鼻をひくつかせる長さ（2 秒）。</summary>
    public const int SniffTicks = 40;

    /// <summary>スマホが光る長さ（setting.md 6 節「一瞬」。0.8 秒）。</summary>
    public const int PhoneFlashTicks = 16;

    public static Presentation Of(NightState s, Display.Stages stages, BoardSpec board, Tuning tuning)
    {
        var window =stages.DawnImminent ? 5 : stages.TimeLeft < 0 ? 0 : stages.TimeLeft;

        // **盲目区間は成功側も失敗側も同じ 1 本**（ADR-0022 / PR-2）
        var carried = s.Parent is ParentPhase.Settling or ParentPhase.Feint;

        if (s.Baby == BabyPhase.EyesClosed)
        {
            // **閉眼中に出してよいのは、窓の明るさと「運ばれている」だけ**（PR-1 / ADR-0014 / 0017）。
            // 覚醒度・山札・出来事・元気のどれを見ても、ここから逆算できてはならない
            return new Presentation(
                EyesClosed: true,
                Body: carried ? BodyClip.Carry : BodyClip.Hidden,
                Hand: HandClip.Rest,
                Reaching: null,
                Sniff: false,
                LampOn: false,
                PhoneFlash: false,
                PartnerHere: false,
                Window: window,
                Vignette: 0,
                CameraLifted: carried,
                BottleInFace: false,
                Baby: BabyMotion.Breathe,
                BabyStrengthMilli: 0,
                CarryTick: carried ? s.TSettle : 0);
        }

        var up = s.Parent == ParentPhase.Up;
        var caring = s.Parent == ParentPhase.Caring && s.ActiveCare is not null;

        return new Presentation(
            EyesClosed: false,
            Body: BodyOf(s, stages, tuning, up),
            Hand: HandOf(s, stages, up, caring),
            Reaching: !up && !caring ? s.PendingCare : null,
            Sniff: !up && Within(board, NightEventKind.DiaperSoiled, s.Tick, SniffTicks),
            LampOn: up || stages.Hand == 0,
            PhoneFlash: Within(board, NightEventKind.PhoneRings, s.Tick, PhoneFlashTicks),
            PartnerHere: s.PartnerHere,
            Window: window,
            Vignette: (s.HungryUntil > s.Tick ? 1 : 0) + (stages.Vigor == 0 ? 1 : 0),
            CameraLifted: caring && s.ActiveCare == CareKind.Hold,
            BottleInFace: caring && s.ActiveCare == CareKind.Milk,
            Baby: BabyOf(s),
            BabyStrengthMilli: s.Baby is BabyPhase.Charging or BabyPhase.Acting
                ? s.ActStrengthMilli
                : (stages.Vigor < 0 ? 0 : stages.Vigor) * 500,
            CarryTick: 0);
    }

    private static BodyClip BodyOf(NightState s, Display.Stages stages, Tuning tuning, bool up)
    {
        if (up)
        {
            return BodyClip.Hidden;
        }

        // 寝返りの間は覚醒度を姿勢に出さない（MOD-Display D-6 / TC-130）
        if (s.RollUntil > s.Tick)
        {
            return BodyClip.TurnAway;
        }

        // 寝入りばな（balance.md 15 節: Grace が寝入りばなそのもの）
        if (s.Parent == ParentPhase.Grace)
        {
            return BodyClip.DozeDrop;
        }

        // 予兆。**乱数を使わない周期なので、見ていれば読める**（REQ-061）
        if (s.Parent == ParentPhase.Sleeping && s.PendingCare is null)
        {
            var phase = s.Tick % tuning.DrowsyPeriodTicks;

            if (phase >= tuning.DrowsyPeriodTicks - DozeWarnTicks)
            {
                return BodyClip.DozeWarn;
            }
        }

        return stages.Arousal switch
        {
            0 => BodyClip.BreathDeep,
            1 => BodyClip.BreathLight,
            2 => BodyClip.BreathHalf,
            3 => BodyClip.BreathAwake,
            _ => BodyClip.Hidden,
        };
    }

    private static HandClip HandOf(NightState s, Display.Stages stages, bool up, bool caring)
    {
        if (up)
        {
            return HandClip.Rest;
        }

        if (caring)
        {
            return s.ActiveCare switch
            {
                // 手つきで山札の残量を伝える（setting.md 4.3）
                CareKind.PatPat => stages.Hand switch
                {
                    0 => HandClip.PatStall,
                    1 => HandClip.PatRough,
                    _ => HandClip.PatSteady,
                },
                CareKind.Milk => HandClip.CareMilk,
                CareKind.Hold => HandClip.CareHold,
                _ => HandClip.CareDiaper,
            };
        }

        return s.PendingCare is not null ? HandClip.Reach : HandClip.Rest;
    }

    private static BabyMotion BabyOf(NightState s) => s.Baby switch
    {
        BabyPhase.Charging => BabyMotion.Charge,
        BabyPhase.Acting => s.ActKind switch
        {
            ActionKind.Cry => BabyMotion.Cry,
            ActionKind.Fuss => BabyMotion.Fuss,
            _ => BabyMotion.Kick,
        },
        _ => BabyMotion.Breathe,
    };

    /// <summary>その種類の出来事が、いまから <paramref name="length"/> tick 以内に起きたか。</summary>
    private static bool Within(BoardSpec board, NightEventKind kind, int tick, int length)
    {
        var events = board.Events;

        if (events is null)
        {
            return false;
        }

        for (var i = 0; i < events.Count; i++)
        {
            if (events[i].Kind == kind && events[i].Tick <= tick && tick < events[i].Tick + length)
            {
                return true;
            }
        }

        return false;
    }
}
