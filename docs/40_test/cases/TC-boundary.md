# TC-098〜114 — 境界層

対象: Clock / Input / Storage / Calendar / Share / Power

| TC | 対象 REQ | モジュール | 前提 | 手順 | 期待値 |
| --- | --- | --- | --- | --- | --- |
| TC-098 | NFR-005 | Clock | — | 不揃いな `deltaSeconds` を与える | **端数が持ち越され、tick が落ちない** |
| TC-099 | REQ-009 | Clock | `Pause()` 済み | 時間を与える | **消化 tick が常に 0** |
| TC-100 | REQ-009 | Clock | 長い停止からの復帰 | 大きな `deltaSeconds` | **1 フレームの消化 tick に上限がかかる** |
| TC-101 | — | Clock | `deltaSeconds < 0` | 与える | 0 を返す（**負の時間を進めない**） |
| TC-102 | ADR-0002 D2 | Clock | — | `StepClock` に差し替え | 実時間を待たずに任意 tick 進む |
| TC-103 | REQ-039 | Input | 同 tick に複数入力 | `Sample` | **先着 1 件だけ。**残りは捨てられ、次の tick にも残らない |
| TC-104 | REQ-005 | Input | 画面上半分のタッチ | `Sample` | **行動として解釈されない**（首振りのみ） |
| TC-105 | REQ-002 | Input | 可動範囲を超えるドラッグ | `Look` | 左右 ±55°、上下 −15〜+30° に丸められる |
| TC-106 | — | Input | 押しっぱなし中に画面外で離す | `Sample` | **押しっぱなしが解除される**（残り続けない） |
| TC-107 | REQ-005 | Input | 2 本目の指 | `Sample` | **無視される**（最初の 1 本のみ） |
| TC-108 | **REQ-010 / 020** | Storage | 任意の tick で保存 | 復元して続きを再生 | **中断しなかった場合と状態列が完全一致。**10 シード × 各 5 箇所の中断位置で行う |
| TC-109 | REQ-053 | Storage | 保存中に落ちる | 再起動 | **前の版が壊れていない**（原子的な書き込み） |
| TC-110 | REQ-033 | Storage | `Run` だけ壊す | 読む | `LastStatus = Corrupt`。**例外を投げない。**`Today` は読める |
| TC-111 | REQ-033 | Storage | 範囲外の値を含むデータ | 読む | `Corrupt` として扱う |
| TC-112 | REQ-033 | Storage | 未知の `schemaVersion` | 読む | `VersionMismatch` |
| TC-113 | **REQ-019** | Calendar | 端末時刻 11:59:59 と 12:00:00 | `BoardDate` | **境界で日付が変わる**（正午が境界） |
| TC-114 | REQ-036 | Calendar | プレイ中に日付が変わる | 再生 | **盤面が変わらない**（開始時に確定） |
| TC-115 | NFR-002 | Share | — | `ShareIntent` を呼ぶ | **通信を行わない**（ネットワーク呼び出しが 0 件） |
| TC-116 | REQ-027 | Share | 共有先が無い | `ShareIntent` | **コピーにフォールバック。**例外を投げない |
| TC-117 | REQ-035 | Power | `SCR-Night` かつ `ST-N-Run` | 無操作を続ける | **抑止が有効** |
| TC-118 | REQ-009 | Power | `ST-N-Pause` に入る | — | **抑止が解除される** |
| TC-119 | NFR-003 | Power / Share / Storage | — | 要求権限を検査 | **0 件** |
