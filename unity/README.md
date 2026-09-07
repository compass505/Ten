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

## 初回に必要なこと（人の作業）

**Unity エディタはライセンス認証をしないと起動しない。**
2026-09-07 時点で未認証（`No valid Unity Editor license found`）。

1. Unity Hub を開く
2. Unity アカウントでサインインする
3. Personal ライセンスを有効化する
4. このフォルダ（`unity/`）を「Add project from disk」で開く

**開いた時点で `Library/` などが生成される。**それらは Git に入れない（`.gitignore` 済み）。

## テスト

**`Assets/Tests/` は PlayMode のテスト。**描いて見るしかないもの（TC-120〜129）、
実機でしか測れないもの（TC-145〜151）、実ファイルが要るもの（TC-109）だけを置く。

**規則で見られるものはここに置かない。**それは `tests/harness/` にあり、
`dotnet test` で秒で回る。

```bash
# Unity 側（PlayMode。16 件）
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
