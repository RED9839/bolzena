# -*- coding: utf-8 -*-
"""검토 표(review_findings.md)에 「고친 뒤」 열을 붙인다 — 고친 뒤 시트 tsv(ult_sheets_after/<사도>.tsv)를 보고 갈래마다 숫자로 판단한다.
  python Tools/ult_review_after.py C:/projects/bolzena-unity-tmp/ult_sheets C:/projects/bolzena-unity-tmp/ult_sheets_after
→ <전>/review_findings_after.md (원본은 그대로 둔다)
판단(자동 — 눈으로 다시 볼 것):
  너무 김 · 지루함 : 끝 ≤ 7000ms 이고 마지막 피해 뒤 남는 몫 ≤ 2000ms 면 「풀림」
  타수 불일치      : 우리 표시 = 원작 타수(또는 원작 < 우리 · 불명)면 「풀림」
  임팩트/타격 어긋남 · 그 밖(피해가 탄보다 먼저) : 충격↔타격 최대 ≤ 50ms 면 「풀림」
  이동 · 화면 밖 · 대상과 떨어짐 · 끊김 : 숫자로 못 가림 → 「눈으로」
"""
import os, re, sys
sys.stdout.reconfigure(encoding="utf-8", errors="replace")


def read(path):
    head, evs = {}, []
    if not os.path.exists(path): return None, []
    for line in open(path, encoding="utf-8"):
        p = line.rstrip("\n").split("\t")
        if p[0].startswith("#"): head[p[0][1:]] = p[1] if len(p) > 1 else ""
        elif len(p) >= 3:
            try: evs.append((int(p[0]), p[1], p[2]))
            except ValueError: pass
    return head, evs


def num(s, d=0):
    m = re.match(r"-?\d+", s or "")
    return int(m.group(0)) if m else d


def verdict(kind, h, evs):
    if h is None: return "시트 없음"
    end = num(h.get("endMs")); shown = num(h.get("shownHits")); orig = num(h.get("origHits"), -1); sync = num(h.get("syncMaxMs"))
    hits = [e[0] for e in evs if e[1] == "hit"]
    tail = end - (max(hits) if hits else 0)
    facts = f"끝 {end} · 표시 {shown}/원작 {orig if orig >= 0 else '-'} · 어긋남 {sync} · 꼬리 {tail}"
    if "김" in kind or "지루" in kind:
        ok = end <= 7000 and tail <= 2000
    elif "타수" in kind:
        ok = orig < 0 or shown >= orig
    elif "임팩트" in kind or "타격" in kind or "그 밖" in kind:
        ok = sync <= 50
    else:
        return "눈으로 · " + facts
    return ("풀림 · " if ok else "남음 · ") + facts


def main():
    before, after = sys.argv[1], sys.argv[2]
    src = os.path.join(before, "review_findings.md")
    out = []
    n_ok = n_all = 0
    for line in open(src, encoding="utf-8"):
        s = line.rstrip("\n")
        if s.startswith("| 심각"): out.append(s + " 고친 뒤 |"); continue
        if s.startswith("|---"): out.append(s + "---|"); continue
        if s.startswith("| ") and s.count("|") >= 6:
            cells = [c.strip() for c in s.strip("|").split("|")]
            hero, kind = cells[1], cells[2]
            h, evs = read(os.path.join(after, hero + ".tsv"))
            v = verdict(kind, h, evs)
            n_all += 1; n_ok += v.startswith("풀림")
            out.append(s + f" {v} |"); continue
        out.append(s)
    out.insert(1, f"\n고친 뒤 자동 판단: 풀림 {n_ok} / {n_all}건 (숫자로 가린 것 — 「눈으로」 · 「남음」은 _compare.png 로 확인)\n")
    dst = os.path.join(before, "review_findings_after.md")
    open(dst, "w", encoding="utf-8").write("\n".join(out) + "\n")
    print(f"풀림 {n_ok}/{n_all} → {dst}")


if __name__ == "__main__":
    main()
