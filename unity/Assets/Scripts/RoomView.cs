using Ten.Pure;
using UnityEngine;

namespace Ten.View
{
    /// <summary>
    /// MOD-View — 一人称の寝室を見せる。
    ///
    /// 仕様: docs/30_detailed_design/MOD-View.md（V-1〜V-11）
    ///
    /// **判断をしない**（V-1）。状態と段階を受け取って見せるだけで、
    /// ここでゲームの結果が変わらない。
    ///
    /// **`Settling` と `Feint` を同じコードパスから出す**（V-11 / ADR-0017）。
    /// 分岐して別々に書くと、片方だけ直したときに差が生まれ、
    /// そこから寝たふりの成否が逆算できる。**画面側にはその差に気づく手段が無い。**
    /// </summary>
    public sealed class RoomView
    {
        private readonly RoomRig _rig;

        public RoomView(RoomRig rig) => _rig = rig;

        public RoomView() : this(RoomRig.Instance)
        {
        }

        /// <summary>毎フレーム呼ぶ。</summary>
        public void Render(NightState s, Ten.Pure.Display.Stages stages, float yawDeg, float pitchDeg)
        {
            _rig.LookAt(yawDeg, pitchDeg);
            _rig.SetEyesClosed(s.Baby == BabyPhase.EyesClosed);

            // **盲目区間は成功側も失敗側も同じ扱い**（V-10 / V-11 / ADR-0017）。
            // ここで `Settling` と `Feint` を分けて書いた時点で REQ-016 が壊れる
            var carried = s.Parent == ParentPhase.Settling || s.Parent == ParentPhase.Feint;

            _rig.SetCarried(carried, s.TSettle);

            // 段階が -1（見えない）なら**出さない。推測して埋めない**（MOD-View のエラー時）
            _rig.SetStages(stages.Arousal, stages.Vigor, stages.Hand, stages.TimeLeft);
        }
    }
}
