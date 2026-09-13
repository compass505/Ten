using Ten.Boundary;
using Ten.Pure;

namespace Ten.View
{
    /// <summary>
    /// MOD-View — 一人称の寝室を見せる。
    ///
    /// 仕様: docs/30_detailed_design/MOD-View.md（V-1〜V-11）/ MOD-Present.md
    ///
    /// **判断をしない**（V-1）。何を描くかは <see cref="PresentRule"/>（境界層）が決め、
    /// ここはそれを寝室に写すだけ。
    ///
    /// **`Settling` と `Feint` を同じコードパスから出す**（V-11 / ADR-0017 / 0022）。
    /// 写像の段階で同じ <see cref="Presentation"/> に潰れているので、ここで分岐する手段が無い。
    /// </summary>
    public sealed class RoomView
    {
        private readonly RoomRig _rig;

        public RoomView(RoomRig rig) => _rig = rig;

        public RoomView() : this(RoomRig.Instance)
        {
        }

        /// <summary>毎フレーム呼ぶ。</summary>
        public void Render(
            NightState s, Ten.Pure.Display.Stages stages, BoardSpec board, Tuning tuning, float yawDeg, float pitchDeg)
        {
            var p = PresentRule.Of(s, stages, board, tuning);

            _rig.LookAt(yawDeg, pitchDeg);
            _rig.SetEyesClosed(p.EyesClosed);
            _rig.SetCarried(p.Body == BodyClip.Carry, p.CarryTick);

            // 段階が -1（見えない）なら**出さない。推測して埋めない**（MOD-View のエラー時）
            _rig.SetStages(stages.Arousal, stages.Vigor, stages.Hand, stages.TimeLeft);
            _rig.Present(p);
        }
    }
}
