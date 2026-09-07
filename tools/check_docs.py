#!/usr/bin/env python3
"""ドキュメントの機械的な健全性チェック。

改善ループ（docs/00_process/loop.md B節）の「現状把握」を、印象ではなく事実で取るための道具。
判断はしない。事実だけを出す。

    python3 tools/check_docs.py
"""
import re
import sys
import pathlib

ROOT = pathlib.Path(__file__).resolve().parent.parent
findings = []


def add(kind, msg):
    findings.append((kind, msg))


# 生成物と外部由来のファイル。**書いた覚えのないものを検査しない。**
# unity/Library は Unity がパッケージを展開する場所で、中の文書は Unity のもの
# （UTF-8 でないものが混ざる）。scratch/visual はモデル制作の中間ファイル。
SKIP_DIRS = (".git", "node_modules", "Library", "Temp", "Logs", "obj", "bin", "visual")


def md_files():
    for p in ROOT.rglob("*.md"):
        if any(part in SKIP_DIRS for part in p.parts):
            continue
        yield p


def strip_code_blocks(text):
    """``` フェンス内は example なので検査対象から外す。"""
    out, fenced = [], False
    for line in text.splitlines():
        if line.lstrip().startswith("```"):
            fenced = not fenced
            continue
        out.append("" if fenced else line)
    return "\n".join(out)


# 1. リンク切れ
# スキル定義（.claude/skills/**）はリポジトリルート基準でパスを書く規約なので、
# 解決の基準ディレクトリが他の md と異なる。
for md in md_files():
    in_skill = ".claude" in md.parts
    base = ROOT if in_skill else md.parent
    for m in re.finditer(r"\]\(([^)]+)\)", strip_code_blocks(md.read_text(encoding="utf-8"))):
        target = m.group(1)
        if target.startswith(("http://", "https://", "#", "mailto:")):
            continue
        rel = target.split("#")[0]
        if (base / rel).resolve().exists():
            continue
        # スキル内の相対リンク（同じスキルディレクトリ内）も許す
        if in_skill and (md.parent / rel).resolve().exists():
            continue
        add("リンク切れ", f"{md.relative_to(ROOT)} → {target}")

# 2. ADR: ステータスと採番
adr_dir = ROOT / "docs/10_requirements/decisions"
seen = {}
if adr_dir.exists():
    for adr in sorted(adr_dir.glob("ADR-*.md")):
        text = adr.read_text(encoding="utf-8")
        num = adr.name.split("-")[1]
        if num in seen:
            add("ADR 番号重複", f"{adr.name} と {seen[num]}")
        seen[num] = adr.name
        m = re.search(r"^- ステータス: (.+)$", text, re.M)
        if not m:
            add("ADR 形式", f"{adr.name}: ステータス行が無い")
            continue
        status = m.group(1)
        if "Proposed" in status:
            add("ADR 未承認", f"{adr.name} が Proposed のまま（承認 or 差し戻しが要る）")
        for section in ("## 背景", "## 選択肢", "## 決定", "## 結果"):
            if section not in text:
                add("ADR 形式", f"{adr.name}: {section} が無い")

# 3. 規約ファイルに更新トリガーがあるか
for proc in sorted((ROOT / "docs/00_process").glob("*.md")):
    if "更新トリガー" not in proc.read_text(encoding="utf-8"):
        add("更新トリガー欠落", f"{proc.relative_to(ROOT)}（腐る経路が塞がれていない）")

# 4. CLAUDE.md の長さ（長いと指示が埋もれる）
claude_md = ROOT / "CLAUDE.md"
if claude_md.exists():
    n = len(claude_md.read_text(encoding="utf-8").splitlines())
    if n > 200:
        add("CLAUDE.md 肥大", f"{n} 行（200 行が目安。削るかスキルへ出す）")

# 5. traceability の穴: Must 要件で TC が無いもの
trace = ROOT / "docs/40_test/traceability.md"
if trace.exists():
    for line in trace.read_text(encoding="utf-8").splitlines():
        if not line.startswith("| REQ-"):
            continue
        cols = [c.strip() for c in line.strip("|").split("|")]
        if len(cols) >= 3 and cols[1] == "Must" and not cols[2]:
            add("トレーサビリティの穴", f"{cols[0]} は Must だが TC が無い")

# 6. open_issues に残っている論点
issues = ROOT / "docs/10_requirements/open_issues.md"
if issues.exists():
    for line in issues.read_text(encoding="utf-8").splitlines():
        if not line.startswith("| ISS-"):
            continue
        cols = [c.strip() for c in line.strip("|").split("|")]
        if len(cols) < 3:
            continue
        # 「未解決」は「解決」を含むので、状態列だけを見て部分一致を避ける
        state = cols[2]
        if "解決" in state and "未解決" not in state:
            continue
        add("未解決の論点", f"{cols[0]} {cols[1]}（{state}）")

# 出力
if not findings:
    print("指摘なし")
    sys.exit(0)

current = None
for kind, msg in findings:
    if kind != current:
        print(f"\n[{kind}]")
        current = kind
    print(f"  - {msg}")
print(f"\n計 {len(findings)} 件")
