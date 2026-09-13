# unity

Unity プロジェクト。**エディタ版は 6000.0.83f1（Unity 6 LTS）に固定**（[ADR-0001](../docs/10_requirements/decisions/ADR-0001-tech-stack.md)）。

## ソースの持ち方

**`src/` のコードをローカルパッケージとして参照する**（[architecture.md](../docs/20_basic_design/architecture.md)）。
コピーも DLL も作らない。**同じファイルを `dotnet test` と Unity Test Runner が見る。**

```
unity/Packages/manifest.json
  "jp.ten.pure":     "file:../../src/Ten.Pure"
  "jp.ten.boundary": "file:../../src/Ten.Boundary"
```

**`.asmdef` の `noEngineReferences: true` が効く。**
純粋層・境界層に `using UnityEngine` を書くと、**Unity 側のコンパイルが落ちる。**
TC-006 の静的検査と二重に見張っている。

## ここに置くもの / 置かないもの

| | |
| --- | --- |
| 置く | 表示層 3 件（`MOD-View` / `MOD-Tutorial` / `MOD-Shell`）、Unity API を直接叩く境界層の実装（タッチ・端末の保存先・画面消灯抑止）、シーン、アセット |
| **置かない** | **純粋層と、値だけで完結する境界層の処理。**それは `src/` にある |

## ライセンスと Android モジュール

**2026-09-12 時点で認証済み。**batchmode での起動・PlayMode の実行・Android ビルドが通ることを確認した。
Android の SDK / NDK / OpenJDK は Unity 同梱のものが入っている。

もし `No valid Unity Editor license found` が出たら:

1. Unity Hub を開く
2. Unity アカウントでサインインする
3. Personal ライセンスを有効化する

**`Library/` などは Git に入れない**（`.gitignore` 済み）。

## ここに置いた実装（フェーズ 5）

| 置き場 | 中身 |
| --- | --- |
| `Assets/Scripts/` | `RoomRig`（一人称の寝室と仮の母・小物。**実行時に組み立てる**）/ `RoomView`（MOD-View。`PresentRule` の結果を写す）/ `TenBoot` |
| `Assets/Scripts/TenApp.cs` | **画面の流れ**（MOD-Shell の本番）。保存・再開・中断・戻る操作・共有・チュートリアル。**見た目は仮の IMGUI**（2026-09-13） |
| `Assets/Scripts/NightSession.cs` | 一晩の進行。**tick の順序（REQ-051）を持つ唯一の場所**。旧 `NightDriver` を置き換えた |
| `Assets/Scripts/DeviceServices.cs` | Android の共有シート / クリップボード、画面消灯の抑止 |
| `Assets/Editor/TenBuild.cs` | Android ビルド。`-executeMethod Ten.Editor.TenBuild.Android` |
| `Assets/Plugins/Android/` | 追加マニフェストと Gradle の雛形。**要求権限 0 件**（NFR-003）と lint 停止（NFR-002） |
| `Assets/Scenes/Night.unity` | 入口。**中身は空で、`TenBoot` が実行時に寝室を組む** |

**寝室をシーンアセットに置いていない。**e2e（`ScreenProbe`）が
「いま画面に何が映っているか」を撮って測るため。シーンに置くと、
テストが読むのはシーンの中身であって画面ではなくなる。

## テスト

**`Assets/Tests/` は PlayMode のテスト。**描いて見るしかないもの（TC-120〜129）、
実機でしか測れないもの（TC-145〜151）、実ファイルが要るもの（TC-109）だけを置く。

**規則で見られるものはここに置かない。**それは `tests/harness/` にあり、
`dotnet test` で秒で回る。

```bash
# Android ビルド（TC-115 / 119 / 147 の前提。**成果物を見ないと通らない**）
"/Applications/Unity/Hub/Editor/6000.0.83f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -quit -projectPath unity -buildTarget Android \
  -executeMethod Ten.Editor.TenBuild.Android -logFile -

# Unity 側（PlayMode。16 件中 11 件が green。**残る 5 件は実機でしか測れない**）
"/Applications/Unity/Hub/Editor/6000.0.83f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -runTests -testPlatform PlayMode \
  -projectPath unity -testResults /tmp/playmode.xml -logFile -

# 純粋層と境界層（Unity 不要）
export PATH="$HOME/.dotnet:$PATH"
cd tests/unit && dotnet test
cd tests/harness && dotnet test
```

**PlayMode で実行する。**`EditMode` では 0 件になる
（asmdef の `includePlatforms` を空にしてあるため、PlayMode 側の資産として扱われる）。

### コンパイルだけ確かめる

```bash
"/Applications/Unity/Hub/Editor/6000.0.83f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -quit -projectPath unity -logFile -
```

**`rc=0` なら `Ten.Pure` / `Ten.Boundary` が Unity 側でも通っている。**
`using UnityEngine` を純粋層に書くと、ここで落ちる（`noEngineReferences`）。
