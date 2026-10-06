# -*- coding: utf-8 -*-
"""고학년 길이 표 — 점검 표(ultaudit_000.tsv)와 시트 tsv 로 사도마다 원래 · 앞 줄인 · 뒤 줄인 · 배속 · 최종 길이,
그리고 「가운데 타격(첫~마지막 타격)만으로 7초가 넘어 원칙의 예외」인 사도.
  python Tools/ult_length_table.py C:/projects/bolzena-unity-tmp/ult_sheets_after_1409
→ <폴더>/ult_length.tsv · ult_length.md
"""
import csv, os, sys
sys.stdout.reconfigure(encoding="utf-8", errors="replace")
LIMIT = 7000      # 컷인 포함 목표(ms)
CUTIN = 1400


def main():
    d = sys.argv[1]
    rows = list(csv.DictReader(open(os.path.join(d, "ultaudit_000.tsv"), encoding="utf-8"), delimiter="\t"))
    out = [["사도", "원래ms(1배속)", "앞줄임ms", "뒤줄임ms", "배속", "최종몸짓ms", "끝ms(컷인 뒤)", "컷인포함ms", "첫타ms", "끝타ms", "가운데ms", "판정"]]
    exc = []
    for r in rows:
        h = r["hero"]
        hits = []
        p = os.path.join(d, h + ".tsv")
        end = 0
        if os.path.exists(p):
            for line in open(p, encoding="utf-8"):
                q = line.rstrip("\n").split("\t")
                if q[0] == "#endMs": end = int(q[1])
                elif len(q) >= 3 and q[1] == "hit" and q[0].lstrip("-").isdigit(): hits.append(int(q[0]))
        nat = int(r.get("naturalMs") or 0); sp = float(r.get("speed") or 1)
        front = int(r.get("cutFrontMs") or 0); back = int(r.get("cutBackMs") or 0)
        mot = int(r.get("motionMs") or 0)
        first, last = (min(hits), max(hits)) if hits else (0, 0)
        mid = last - first
        total = end + CUTIN
        if total <= LIMIT: v = "7초 안"
        elif mid + CUTIN > LIMIT - 1500: v = "예외 — 가운데 타격만으로 김"; exc.append((h, nat, mid, total))
        else: v = "넘음 — 더 줄일 수 있음"
        out.append([h, nat, front, back, f"{sp:.2f}", mot, end, total, first, last, mid, v])
    with open(os.path.join(d, "ult_length.tsv"), "w", encoding="utf-8", newline="") as f:
        csv.writer(f, delimiter="\t").writerows(out)
    md = ["# 고학년 길이 표", "", f"목표: 컷인(약 {CUTIN}ms) 포함 {LIMIT}ms 안. 줄이기는 앞(첫 타격 전 준비) · 뒤(마지막 타격 뒤 꼬리)만, 그다음 최대 1.2배.", "",
          f"- 7초 안 {sum(1 for x in out[1:] if x[-1] == '7초 안')}명 · 예외 {len(exc)}명 · 넘음(더 줄일 수 있음) {sum(1 for x in out[1:] if x[-1].startswith('넘음'))}명", "",
          "## 예외 — 가운데 타격만으로 김(원작대로 둠)", "", "| 사도 | 원래 ms | 가운데(첫~끝 타격) ms | 컷인 포함 최종 ms |", "|---|---:|---:|---:|"]
    md += [f"| {h} | {n} | {m} | {t} |" for h, n, m, t in sorted(exc, key=lambda x: -x[3])]
    md += ["", "## 전체", "", "| " + " | ".join(out[0]) + " |", "|" + "---|" * len(out[0])]
    md += ["| " + " | ".join(map(str, x)) + " |" for x in out[1:]]
    open(os.path.join(d, "ult_length.md"), "w", encoding="utf-8").write("\n".join(md) + "\n")
    print(f"{len(out) - 1}명 · 예외 {len(exc)} → {d}/ult_length.md")


if __name__ == "__main__":
    main()
