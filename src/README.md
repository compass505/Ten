# src

実装。**フェーズ 5 に入るまで、シグネチャだけの空実装しか置かない。**

## いま置いてあるもの（2026-09-06）

| | 中身 |
| --- | --- |
| `Ten.Pure/` | 純粋層のアセンブリ。**`UnityEngine` を参照しない** |
| `Ten.Pure/Rng.cs` | MOD-Rng の公開 IF。**本体は `throw new NotImplementedException()` の 1 行だけ** |
| `Ten.Pure/RngPurpose.cs` | 乱数の用途（types.md 1 節の確定済み定義） |

**なぜフェーズ 4 でここにファイルがあるのか。**C# は静的コンパイル言語なので、
`src/` が空だとテストは「落ちる」のではなく「ビルドが通らず 1 件も走らない」。
それではフェーズ 4 の DoD（**全テストが落ちることの確認**）ができない。

**置いてよいものの線引きは
[docs/00_process/test_first.md](../docs/00_process/test_first.md) 5.1 にある。**
判断を含むコードを 1 行でも書いたら違反。

## 構成

[architecture.md](../docs/20_basic_design/architecture.md) のとおり、
**純粋層と環境に触る層を別アセンブリに分ける。**
純粋層が `System.Random` / `DateTime` / `UnityEngine` を参照しないことが、
ハーネスが成立する前提（NFR-004）。**TC-006 が機械的に見張っている。**

技術検証のコードは `../scratch/` へ。**`scratch/` を `src/` から import しない。**
