using UnityEngine;

namespace Ten.View
{
    /// <summary>
    /// シーンの入口。**寝室は実行時に組み立てる**（<see cref="RoomRig"/>）。
    ///
    /// シーンアセットに寝室を置かないのは、e2e が「いま画面に何が映っているか」を
    /// 撮って測るため（MOD-View / ScreenProbe）。シーンに置くと、
    /// テストが読むのはシーンの中身であって画面ではなくなる。
    /// </summary>
    public sealed class TenBoot : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            // 触れた時点で組み上がる
            var _ = RoomRig.Instance;

            // 夜を動かす
            gameObject.AddComponent<NightDriver>();
        }
    }
}
