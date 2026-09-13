// C# 10 の record / init アクセサが必要とする型。
//
// **Unity 側（.NET Standard 2.1）に存在しないので、ここで補う。**
// `dotnet build`（net8.0）には標準で入っているため、そちらでは定義しない
// （二重定義になる）。
//
// （Ten.Pure と同じ内容。アセンブリごとに要る）
// なぜ record が要るか: types.md 0 節が「状態はすべて不変
// （readonly record struct）」と定めている。値の等価比較と `with` が
// 決定論のテスト（状態列の比較）を成り立たせている。
//
// なぜ langversion の指定が要るか: Unity 6 の既定は C# 9 で、
// record struct と file-scoped namespace が使えない。
// 各アセンブリの csc.rsp で `-langversion:10` を指定している。

#if !NET5_0_OR_GREATER

namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}

#endif
