using Ten.Boundary;
using UnityEngine;

namespace Ten.View
{
    /// <summary>
    /// MOD-Share の本番。**通信しない。**OS の共有シートに文字列を渡すだけ（SH-1 / NFR-002）。
    /// Android 以外（エディタ・Mac ビルド）では共有先が無いので、コピーに落ちる（ShareRule）。
    /// </summary>
    public sealed class DeviceShare : IShare
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        public bool IsAvailable => true;
#else
        public bool IsAvailable => false;
#endif

        public void CopyToClipboard(string text) => GUIUtility.systemCopyBuffer = text;

        public void ShareIntent(string text)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using var intentClass = new AndroidJavaClass("android.content.Intent");
            using var intent = new AndroidJavaObject("android.content.Intent");

            intent.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND")).Dispose();
            intent.Call<AndroidJavaObject>("setType", "text/plain").Dispose();
            intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text).Dispose();

            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            using var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, "結果を送る");

            activity.Call("startActivity", chooser);
#else
            CopyToClipboard(text);
#endif
        }
    }

    /// <summary>MOD-Power の本番。`FLAG_KEEP_SCREEN_ON` 相当。**権限は要らない**（PW-4 / NFR-003）。</summary>
    public sealed class DevicePower : IPower
    {
        public bool IsAwake { get; private set; }

        public void KeepAwake(bool on)
        {
            if (on == IsAwake)
            {
                return;
            }

            IsAwake = on;
            UnityEngine.Screen.sleepTimeout = on ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
        }
    }
}
