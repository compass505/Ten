using System;
using System.Globalization;
using System.IO;
using System.Text;
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
    public FileStorage(string rootDirectory) => RootDirectory = rootDirectory;

    public string RootDirectory { get; }

    private LoadStatus _status = LoadStatus.Missing;
    public LoadStatus LastStatus => _status;
    private string PathFor(string name) => Path.Combine(RootDirectory, name + ".dat");

    public DeviceData? LoadDevice() { try { var p=PathFor("device"); if(!File.Exists(p)){_status=LoadStatus.Missing;return null;} var a=File.ReadAllLines(p); var d=new DeviceData(int.Parse(a[0]),int.Parse(a[1]),bool.Parse(a[2])); _status=d.SchemaVersion==1?LoadStatus.Ok:LoadStatus.VersionMismatch; return _status==LoadStatus.Ok?(DeviceData?)d:null; } catch { _status=LoadStatus.Corrupt; return null; } }
    public TodayData? LoadToday() { try { var p=PathFor("today"); if(!File.Exists(p)){_status=LoadStatus.Missing;return null;} var a=File.ReadAllLines(p); var t=new TodayData(Decode(a[0]),int.Parse(a[1],CultureInfo.InvariantCulture),null); _status=LoadStatus.Ok; return t; } catch { _status=LoadStatus.Corrupt; return null; } }
    public SavedRun? LoadRun() { try { var p=PathFor("run"); if(!File.Exists(p)){_status=LoadStatus.Missing;return null;} var a=File.ReadAllLines(p); var state=(NightState)ReadRecord(typeof(NightState),a,1); var r=new SavedRun(Decode(a[0]),state); _status=LoadStatus.Ok; return r; } catch { _status=LoadStatus.Corrupt; return null; } }

    public void SaveDevice(DeviceData d) => AtomicWrite("device", d.SchemaVersion+"\n"+d.BoardSpecVersion+"\n"+d.TutorialDone);
    public void SaveToday(TodayData t) => AtomicWrite("today", Encode(t.BoardDate)+"\n"+t.PlayCount);
    public void SaveRun(SavedRun r) { var lines=new StringBuilder(Encode(r.BoardDate)); lines.Append('\n').Append(WriteRecord(r.State)); AtomicWrite("run",lines.ToString()); }

    public void ClearRun() { TryDelete(PathFor("run")); }
    public void ClearAll() { TryDelete(PathFor("device")); TryDelete(PathFor("today")); TryDelete(PathFor("run")); }
    private void AtomicWrite(string name, string contents) { try { Directory.CreateDirectory(RootDirectory); var tmp=Path.Combine(RootDirectory,name+".tmp"); File.WriteAllText(tmp,contents,Encoding.UTF8); var dst=PathFor(name); if(File.Exists(dst)) File.Replace(tmp,dst,null); else File.Move(tmp,dst); _status=LoadStatus.Ok; } catch { _status=LoadStatus.Corrupt; } }
    private static string Encode(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s ?? string.Empty));
    private static string Decode(string s) => Encoding.UTF8.GetString(Convert.FromBase64String(s));
    private static string WriteRecord(object value) { var b=new StringBuilder(); foreach(var f in value.GetType().GetFields()) { if(b.Length>0)b.Append('\t'); var v=f.GetValue(value); b.Append(v==null?"":Encode(Convert.ToString(v,CultureInfo.InvariantCulture))); } return b.ToString(); }
    private static object ReadRecord(Type type, string[] lines, int line) { var fs=type.GetFields(); var parts=lines[line].Split('\t'); var args=new object[fs.Length]; for(var i=0;i<fs.Length;i++) args[i]=ParseValue(fs[i].FieldType, parts[i]); return Activator.CreateInstance(type,args); }
    private static object ParseValue(Type type, string text) { if(text.Length==0 && Nullable.GetUnderlyingType(type)!=null)return null; var t=Nullable.GetUnderlyingType(type)??type; var s=Decode(text); if(t==typeof(string))return s; if(t==typeof(bool))return bool.Parse(s); if(t.IsEnum)return Enum.Parse(t,s); return Convert.ChangeType(s,t,CultureInfo.InvariantCulture); }
    private static void TryDelete(string p) { try { if(File.Exists(p)) File.Delete(p); } catch { } }
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
    private DeviceData? _device; private TodayData? _today; private SavedRun? _run; private LoadStatus _status = LoadStatus.Missing; private bool _runCorrupt;

    public LoadStatus LastStatus => _status;

    public DeviceData? LoadDevice() { if (!_device.HasValue) { _status=LoadStatus.Missing; return null; } if (_device.Value.SchemaVersion != 1) { _status=LoadStatus.VersionMismatch; return null; } _status=LoadStatus.Ok; return _device; }
    public TodayData? LoadToday() { _status=_today.HasValue?LoadStatus.Ok:LoadStatus.Missing; return _today; }
    public SavedRun? LoadRun() { if (_runCorrupt) { _status=LoadStatus.Corrupt; return null; } if (!_run.HasValue) { _status=LoadStatus.Missing; return null; } var s = _run.Value.State; if (s.Arousal < 0 || s.Arousal > 100 || s.Vigor < 0 || s.Vigor > 100 || s.Tick < 0 || s.ActStrengthMilli < 0 || s.ActStrengthMilli > 1000) { _status=LoadStatus.Corrupt; return null; } _status=LoadStatus.Ok; return _run; }

    public void SaveDevice(DeviceData d) => _device=d;
    public void SaveToday(TodayData t) => _today=t;
    public void SaveRun(SavedRun r) { _run=r; _runCorrupt=false; }

    public void ClearRun() { _run=null; _runCorrupt=false; }
    public void ClearAll() { _device=null; _today=null; _run=null; _runCorrupt=false; }

    /// <summary>`Run` だけを壊す（TC-110）。</summary>
    public void CorruptRun() { _run=null; _runCorrupt=true; _status=LoadStatus.Corrupt; }

    /// <summary>未知の（未来の）`schemaVersion` にする（TC-112）。</summary>
    public void SetFutureSchemaVersion() { _device = new DeviceData(int.MaxValue, 0, false); _status=LoadStatus.VersionMismatch; }
}
