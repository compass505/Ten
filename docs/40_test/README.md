# テスト

規約は [../00_process/test_first.md](../00_process/test_first.md)。理由は [../00_process/rationale.md](../00_process/rationale.md)。

| 置き場 | 内容 |
| --- | --- |
| [cases/](cases/) | 日本語のテストケース `TC-xxx` |
| [traceability.md](traceability.md) | REQ ↔ TC ↔ テストコードの対応表 |
| `../../tests/` | 実際のテストコード（unit / harness / e2e） |

## DoD（フェーズ 4）

- 全 Must 要件に対応する `TC-xxx` が存在する
- `TC-xxx` がテストコードになっている
- **全て落ちる。** 落ちないテストは何も検証していない疑いがある
- 失敗出力に、再現に必要なシードと入力列が含まれている（決定論の条件 D4）
