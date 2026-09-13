# MOD-Rng — 用途別の決定論的乱数

種別: リファレンス / 層: **純粋層**
更新トリガー: 用途が増えたとき / ハッシュ実装を変えたとき（**盤面仕様の版が上がる**）
対応要件: REQ-019 / 048 / NFR-004

出典: [ADR-0008](../10_requirements/decisions/ADR-0008-randomness-scope.md)

## 公開 IF

```csharp
public static class Rng {
    /// 値は [0, 1) の固定小数（0〜999）。**double を返さない**（端末差を作らない）
    public static int Milli(string seed, RngPurpose purpose, int ordinal);

    /// 0 <= 結果 < exclusiveMax。剰余バイアスを除いた一様整数
    public static int Range(string seed, RngPurpose purpose, int ordinal, int exclusiveMax);

    /// probMilli/1000 の確率で true
    public static bool Chance(string seed, RngPurpose purpose, int ordinal, int probMilli);

    /// **公開しない**（internal）。テストからのみ見える
    internal static uint Hash(string seed, RngPurpose purpose, int ordinal);
}
```

**`Hash` は公開 IF に含めない。**`internal` + `InternalsVisibleTo("Ten.Tests.Unit")` で
テストアセンブリにだけ見せる（2026-09-06）。

理由: [参照ベクタ](../../tests/vectors/rng.json)の `hash` 節 140 件は**32 ビット全部**を固定するが、
`Milli`（上位 10 ビット相当）と `Range`（下位数ビット相当）だけでは
**最終撹拌の中間ビットが未検証のまま残る。**そこを間違えても既存のベクタは全件通り、
後から `Chance` を細かい確率で使った時点で端末間でずれる（NFR-004 が崩れる）。
→ [TC-165](../40_test/cases/TC-pure-board.md)

## 満たすこと

| # | 性質 | テスト |
| --- | --- | --- |
| R-1 | **純関数。**同じ (seed, purpose, ordinal) は常に同じ値 | 例示 + 反復 |
| R-2 | 用途が違えば独立。`purpose` を変えると他の用途の値が動かない | 性質 |
| R-3 | `ordinal` を 1 つ飛ばしても、それ以降の値が動かない | 性質 |
| R-4 | 一様性: 10 万サンプルで `Range(…, n)` の各値の出現が期待値 ±3% | 性質 |
| R-5 | **実装をこちらで持つ。**`System.Random` / `UnityEngine.Random` を使わない | 静的検査 |
| R-6 | 環境非依存: 同じ入力なら OS・ランタイム・ビルド構成を問わず一致 | NFR-004 |

## 実装の制約

- ハッシュは **FNV-1a（32bit）+ 最終撹拌**。文字列連結は `seed + '|' + (int)purpose + '|' + ordinal`
- **`purpose` は列挙の数値を使う。**名前の文字列を使うと、リネームで全結果が変わる
- `Range` は剰余バイアスを避けるため、`exclusiveMax` が 2 の冪でない場合は棄却再抽選（`ordinal` を内部で進めず、撹拌値を回す）

## エラー時

| 状況 | 振る舞い |
| --- | --- |
| `ordinal < 0` | `ArgumentOutOfRangeException`（**呼び出し側のバグ。握り潰さない**） |
| `exclusiveMax <= 0` | 同上 |
| `seed` が空 | 同上 |

## 決めていないこと

| ID | 論点 |
| --- | --- |
| RNG-01 | ハッシュを 32bit のままにするか。プレイ回数を通番に含める（ADR-0011）と通番が伸びる |
