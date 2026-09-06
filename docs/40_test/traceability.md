# トレーサビリティ

要件がテストで担保されているかを一覧で見る表。要件を足したら必ずここを更新する。

> 要件は **2026-09-02 に確定した**（REQ-001〜058 / NFR-001〜009）。
> **REQ-059〜062 も確定**（2026-09-06。ADR-0013 / 0015 / 0016 承認）。**暫定の要件は 0 件。**
> **2026-09-05: TC を発番した**（TC-001〜163）。
> **2026-09-06: ADR-0017 の承認により TC-164 を追加**（判別不能性）。ケースは [cases/](cases/) にある。
> **テストコードはまだ無い。**フェーズ 4 の DoD「全て落ちる」はコードを書いてから。

非機能要件の対応:

| NFR | TC |
| --- | --- |
| NFR-001 | TC-145 |
| NFR-002 | TC-115 / 146 |
| NFR-003 | TC-119 / 147 |
| NFR-004 | TC-005 / 006 / 021 / 148 |
| NFR-005 | TC-022 / 098 |
| NFR-006 | TC-063 / 149 |
| NFR-007 | TC-128 |
| NFR-008 | TC-150 |
| NFR-009 | TC-151 |

| REQ | 優先度 | TC | テストコード | 状態 |
| --- | --- | --- | --- | --- |
| [REQ-001](../10_requirements/requirements.md) | Must | TC-120 | 未作成 | 起票済み |
| [REQ-002](../10_requirements/requirements.md) | Must | TC-105 / 120 | 未作成 | 起票済み |
| [REQ-003](../10_requirements/requirements.md) | Must | TC-023 / 025 | 未作成 | 起票済み |
| [REQ-004](../10_requirements/requirements.md) | Must | TC-093 / 123 / 124 | 未作成 | 起票済み |
| [REQ-005](../10_requirements/requirements.md) | Must | TC-104 / 107 / 129 | 未作成 | 起票済み |
| [REQ-006](../10_requirements/requirements.md) | Must | TC-096 / 121 / 122 | 未作成 | 起票済み |
| [REQ-007](../10_requirements/requirements.md) | Must | TC-063 / 077 | 未作成 | 起票済み |
| [REQ-008](../10_requirements/requirements.md) | Must | TC-092 / 093 | 未作成 | 起票済み |
| [REQ-009](../10_requirements/requirements.md) | Must | TC-099 / 118 / 137 | 未作成 | 起票済み |
| [REQ-010](../10_requirements/requirements.md) | Must | TC-108 / 138 | 未作成 | 起票済み |
| [REQ-011](../10_requirements/requirements.md) | Must | TC-030 | 未作成 | 起票済み |
| [REQ-012](../10_requirements/requirements.md) | Must | TC-031 / 032 | 未作成 | 起票済み |
| [REQ-013](../10_requirements/requirements.md) | Must | TC-045 | 未作成 | 起票済み |
| [REQ-014](../10_requirements/requirements.md) | Must | TC-028 / 040 / 041 / 042 | 未作成 | 起票済み |
| [REQ-015](../10_requirements/requirements.md) | Must | TC-050 / 051 / 057 / 058 | 未作成 | 起票済み |
| [REQ-016](../10_requirements/requirements.md) | Must | TC-052 / 125 / **164** | 未作成 | 起票済み。TC-164 は ADR-0017 の判別不能性 |
| [REQ-017](../10_requirements/requirements.md) | Must | TC-053 | 未作成 | 起票済み |
| [REQ-018](../10_requirements/requirements.md) | Must | TC-054 | 未作成 | 起票済み |
| [REQ-019](../10_requirements/requirements.md) | Must | TC-001 / 010 / 113 | 未作成 | 起票済み |
| [REQ-020](../10_requirements/requirements.md) | Must | TC-021 / 089 / 108 | 未作成 | 起票済み |
| [REQ-021](../10_requirements/requirements.md) | Must | TC-010 | 未作成 | 起票済み |
| [REQ-022](../10_requirements/requirements.md) | Must | TC-158 | 未作成 | 起票済み |
| [REQ-023](../10_requirements/requirements.md) | Must | TC-072 / 073 | 未作成 | 起票済み |
| [REQ-024](../10_requirements/requirements.md) | Must | TC-157 | 未作成 | 起票済み |
| [REQ-025](../10_requirements/requirements.md) | Must | TC-074 / 075 | 未作成 | 起票済み |
| [REQ-026](../10_requirements/requirements.md) | Must | TC-084 | 未作成 | 起票済み |
| [REQ-027](../10_requirements/requirements.md) | Must | TC-115 / 116 | 未作成 | 起票済み |
| [REQ-028](../10_requirements/requirements.md) | Should | TC-088 | 未作成 | 起票済み |
| [REQ-029](../10_requirements/requirements.md) | Must | TC-033 / 034 | 未作成 | 起票済み |
| [REQ-030](../10_requirements/requirements.md) | Must | TC-037 / 038 | 未作成 | 起票済み |
| [REQ-031](../10_requirements/requirements.md) | Must | TC-085 / 133 | 未作成 | 起票済み |
| [REQ-032](../10_requirements/requirements.md) | Must | TC-108 | 未作成 | 起票済み |
| [REQ-033](../10_requirements/requirements.md) | Must | TC-110 / 111 / 112 / 143 / 144 | 未作成 | 起票済み |
| [REQ-034](../10_requirements/requirements.md) | Must | TC-015 / 016 / 131 / 132 / 134 | 未作成 | 起票済み |
| [REQ-035](../10_requirements/requirements.md) | Must | TC-117 | 未作成 | 起票済み |
| [REQ-036](../10_requirements/requirements.md) | Must | TC-114 / 141 | 未作成 | 起票済み |
| [REQ-037](../10_requirements/requirements.md) | Should | TC-139 | 未作成 | 起票済み |
| [REQ-038](../10_requirements/requirements.md) | Must | TC-024 / 025 | 未作成 | 起票済み |
| [REQ-039](../10_requirements/requirements.md) | Must | TC-029 / 103 | 未作成 | 起票済み |
| [REQ-040](../10_requirements/requirements.md) | Should | TC-087 | 未作成 | 起票済み |
| [REQ-041](../10_requirements/requirements.md) | Must | TC-011 / 046 | 未作成 | 起票済み |
| [REQ-042](../10_requirements/requirements.md) | Must | TC-078 / 079 | 未作成 | 起票済み |
| [REQ-043](../10_requirements/requirements.md) | Must | TC-012 / 013 / 065 / 067 | 未作成 | 起票済み |
| [REQ-044](../10_requirements/requirements.md) | Must | TC-126 / 127 | 未作成 | 起票済み |
| [REQ-045](../10_requirements/requirements.md) | Must | TC-090 / 091 / 092 | 未作成 | 起票済み |
| [REQ-046](../10_requirements/requirements.md) | Must | TC-036 / 047 | 未作成 | 起票済み |
| [REQ-047](../10_requirements/requirements.md) | Must | TC-043 / 062 / 063 | 未作成 | 起票済み |
| [REQ-048](../10_requirements/requirements.md) | Must | TC-017 | 未作成 | 起票済み |
| [REQ-049](../10_requirements/requirements.md) | Must | TC-035 / 036 | 未作成 | 起票済み |
| [REQ-050](../10_requirements/requirements.md) | Must | TC-048 / 082 | 未作成 | 起票済み |
| [REQ-051](../10_requirements/requirements.md) | Must | **TC-080 / 142** | 未作成 | 起票済み |
| [REQ-052](../10_requirements/requirements.md) | Must | TC-014 / 070 / 071 / 072 | 未作成 | 起票済み |
| [REQ-053](../10_requirements/requirements.md) | Must | TC-109 | 未作成 | 起票済み |
| [REQ-054](../10_requirements/requirements.md) | Must | TC-073 / 081 / 086 | 未作成 | 起票済み |
| [REQ-055](../10_requirements/requirements.md) | Must | TC-049 / 066 / 095 | 未作成 | 起票済み |
| [REQ-056](../10_requirements/requirements.md) | Must | TC-069 | 未作成 | 起票済み |
| [REQ-057](../10_requirements/requirements.md) | Must | TC-068 | 未作成 | 起票済み |
| [REQ-058](../10_requirements/requirements.md) | Must | TC-091 | 未作成 | 起票済み |
| [REQ-059](../10_requirements/requirements.md) | Must | TC-026 / 027 | 未作成 | 起票済み |
| [REQ-060](../10_requirements/requirements.md) | Must | TC-152 / 153 | 未作成 | 確定（ADR-0015 承認 2026-09-06） |
| [REQ-061](../10_requirements/requirements.md) | Must | TC-154 / 155 / 156 | 未作成 | 確定（ADR-0015 承認 2026-09-06） |
| [REQ-062](../10_requirements/requirements.md) | Must | TC-159 / 160 / 161 / 162 / 163 | 未作成 | 確定（ADR-0016 承認 2026-09-06） |

## 穴チェック

- Must なのに TC が無い要件 → **0 件であること**（現在はフェーズ 4 前なので全件が空）
- TC はあるがテストコードが無い → フェーズ 4 未完
- テストコードはあるが対応する REQ が無い → 要件の書き漏れか、不要なテスト
