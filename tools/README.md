# tools

検証・自動化スクリプト置き場。本番コードから import しない。

| スクリプト | 役割 | 使うタイミング |
| --- | --- | --- |
| [`check_docs.py`](check_docs.py) | ドキュメントの機械的チェック | 改善ループの「現状把握」/ フェーズ完了判定前 / ADR 確定前 |

```bash
python3 tools/check_docs.py
```

## check_docs.py が見るもの

リンク切れ / ADR の形式と採番と未承認の残り / 規約ファイルの更新トリガー欠落 /
`CLAUDE.md` の肥大 / traceability の穴（Must なのに TC が無い）/ 未解決の論点。

**判断はしない。事実だけを出す。** 判断が要る観点は `doc-auditor` エージェントの担当。

指摘件数は改善ループの目標条件に使える
（例:「`check_docs.py` の指摘を 5 → 0 件にする」）。
