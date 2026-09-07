using System;
using Ten.Pure;

namespace Ten.Boundary;

/// <summary>読めたか / 壊れていたか（MOD-Storage）。</summary>
public enum LoadStatus { Ok, Missing, Corrupt, VersionMismatch }

/// <summary>端末に置く不変のデータ（types.md 6 節）。</summary>
public readonly record struct DeviceData(int SchemaVersion, int BoardSpecVersion, bool TutorialDone);

/// <summary>その日のデータ（types.md 6 節）。</summary>
public readonly record struct TodayData(string BoardDate, int PlayCount, BestPlay? Best);

/// <summary>進行中のプレイ（types.md 6 節）。**`NightState` をそのまま保存する。**</summary>
public readonly record struct SavedRun(string BoardDate, NightState State);

/// <summary>
/// MOD-Storage — 保存と復元。
///
/// 仕様: docs/30_detailed_design/MOD-Storage.md（ST-1〜ST-6）
/// **D3: I/O 差し替え**（test_first.md 2 節）。
///
/// **読めない / 壊れているときに例外を投げない。**起動できなくならないことが REQ-033。
/// </summary>
public interface IStorage
{
    DeviceData? LoadDevice();
    TodayData? LoadToday();
    SavedRun? LoadRun();

    void SaveDevice(DeviceData d);
    void SaveToday(TodayData t);
    void SaveRun(SavedRun r);

    void ClearRun();
    void ClearAll();

    /// <summary>直近の読み取りの結果。</summary>
    LoadStatus LastStatus { get; }
}

/// <summary>
/// 本番の保存。**原子的に書く**（一時ファイルに書き切ってから rename。ST-1 / REQ-053）。
///
/// 保存先の取得だけが端末依存なので、**書き込み先をコンストラクタで受ける。**
/// Unity 側は `Application.persistentDataPath` を渡すだけ。
///
/// **これはシグネチャだけの空実装である**（test_first.md 5.1）。
/// 形式は未決（[STO-01](../../docs/30_detailed_design/MOD-Storage.md)）。
/// </summary>
public sealed class FileStorage : IStorage
{
    private const string NotYet = "フェーズ 5（実装）で書く（test_first.md 5.1）";

    public FileStorage(string rootDirectory) => RootDirectory = rootDirectory;

    public string RootDirectory { get; }

    public LoadStatus LastStatus => throw new NotImplementedException(NotYet);

    public DeviceData? LoadDevice() => throw new NotImplementedException(NotYet);
    public TodayData? LoadToday() => throw new NotImplementedException(NotYet);
    public SavedRun? LoadRun() => throw new NotImplementedException(NotYet);

    public void SaveDevice(DeviceData d) => throw new NotImplementedException(NotYet);
    public void SaveToday(TodayData t) => throw new NotImplementedException(NotYet);
    public void SaveRun(SavedRun r) => throw new NotImplementedException(NotYet);

    public void ClearRun() => throw new NotImplementedException(NotYet);
    public void ClearAll() => throw new NotImplementedException(NotYet);
}

/// <summary>
/// テスト用（ST-6 / ADR-0002 D3）。**中身は持つが、壊れ方は作れる。**
///
/// **これはシグネチャだけの空実装である**（test_first.md 5.1）。
/// 中身を書くのはフェーズ 5。テスト側は「壊す」「未来の版にする」といった
/// 仕込みをここに頼るので、道具ではなく検証対象として扱う。
/// </summary>
public sealed class MemoryStorage : IStorage
{
    private const string NotYet = "フェーズ 5（実装）で書く（test_first.md 5.1）";

    public LoadStatus LastStatus => throw new NotImplementedException(NotYet);

    public DeviceData? LoadDevice() => throw new NotImplementedException(NotYet);
    public TodayData? LoadToday() => throw new NotImplementedException(NotYet);
    public SavedRun? LoadRun() => throw new NotImplementedException(NotYet);

    public void SaveDevice(DeviceData d) => throw new NotImplementedException(NotYet);
    public void SaveToday(TodayData t) => throw new NotImplementedException(NotYet);
    public void SaveRun(SavedRun r) => throw new NotImplementedException(NotYet);

    public void ClearRun() => throw new NotImplementedException(NotYet);
    public void ClearAll() => throw new NotImplementedException(NotYet);

    /// <summary>`Run` だけを壊す（TC-110）。</summary>
    public void CorruptRun() => throw new NotImplementedException(NotYet);

    /// <summary>未知の（未来の）`schemaVersion` にする（TC-112）。</summary>
    public void SetFutureSchemaVersion() => throw new NotImplementedException(NotYet);
}
