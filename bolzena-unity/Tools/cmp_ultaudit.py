"""궁극기 점검 표(-battle -ultaudit 의 ultaudit_000.tsv) 전/후 비교 — 사도마다 연출 칸이 같은지 본다.
쓰는 법: python -I Tools/cmp_ultaudit.py before.tsv after.tsv 결과.md [--random-cols voice,...]

- 시간 칸(프레임 · 소리 길이 · 맞춤 ms)은 실행마다 흔들려 비교하지 않고 참고로만 적는다.
- 궁극기 기록(played)이 빈 줄은 「비교 불가」 로 따로 센다 — 빈 칸끼리 같다고 세지 않는다(11주차 요미).
- 갈래(played)가 달리 뽑힌 사도는 「갈래 다름」 으로 따로 센다. 점검이 UnityEngine.Random 씨앗을 고정한 뒤(2026-11-17)로는 나오지 않아야 한다.
- --random-cols: 씨앗을 고정하지 않은 옛 표끼리 비교할 때 무작위 칸(voice)을 빼려면 준다.
"""
import csv, sys

TIMING = {"worstMs", "avgMs", "dtWorstMs", "worstAt", "fxPeak", "sndSpanMs", "sndTailMs", "foeMinGap", "afterCutMs",
          "syncFirstMs", "syncMaxMs", "syncEach"}


def load(p):
    with open(p, encoding="utf-8") as f:
        return {r["hero"]: r for r in csv.DictReader(f, delimiter="\t")}


def main(argv):
    args = [a for a in argv if not a.startswith("--")]
    rand = set()
    if "--random-cols" in argv:
        rand = set(argv[argv.index("--random-cols") + 1].split(","))
        args.remove(argv[argv.index("--random-cols") + 1])
    bp, ap, outp = args[:3]
    b, a = load(bp), load(ap)
    cols = [c for c in next(iter(b.values())).keys() if c != "no"]
    content = [c for c in cols if c not in TIMING | rand]
    same, diff, branch, blank, missing = [], [], [], [], []
    for h in sorted(set(b) | set(a)):
        if h not in b or h not in a:
            missing.append(h); continue
        if not b[h]["played"].strip() or not a[h]["played"].strip() or b[h]["played"].startswith("못 씀") or a[h]["played"].startswith("못 씀"):
            blank.append((h, b[h]["played"] or "(빈칸)", a[h]["played"] or "(빈칸)")); continue
        if b[h]["played"] != a[h]["played"]:
            branch.append((h, b[h]["played"], a[h]["played"])); continue
        d = [(c, b[h][c], a[h][c]) for c in content if b[h][c] != a[h][c]]
        (diff.append((h, d)) if d else same.append(h))
    out = [f"# 궁극기 점검 전/후 비교 — 사도 {len(b)}명(전) · {len(a)}명(후)", "",
           f"- 전: `{bp}`", f"- 후: `{ap}`",
           f"- 비교한 연출 칸 {len(content)}개: {', '.join(content)}",
           f"- 시간 칸(참고만): {', '.join(sorted(TIMING & set(cols)))}"]
    if rand: out.append(f"- 무작위 칸(빼고 비교): {', '.join(sorted(rand))}")
    out += ["", f"## 결과: 같음 {len(same)}명 · 다름 {len(diff)}명 · 비교 불가 {len(blank)}명 · 갈래 다름 {len(branch)}명 · 한쪽만 {len(missing)}명", ""]
    for h, x, y in blank: out.append(f"- 비교 불가 {h}: 궁극기 기록 {x} → {y}")
    for h, x, y in branch: out.append(f"- 갈래 다름 {h}: {x} → {y}")
    for h in missing: out.append(f"- 한쪽에만 있음 {h}")
    if diff:
        out += ["", "| 사도 | 칸 | 전 | 후 |", "|---|---|---|---|"]
        for h, d in diff:
            for c, x, y in d: out.append(f"| {h} | {c} | {x[:60]} | {y[:60]} |")
    out += ["", "## 시간 칸(참고)", "| 사도 | worstMs 전 → 후 | avgMs 전 → 후 |", "|---|---|---|"]
    for h in sorted(set(b) & set(a)):
        out.append(f"| {h} | {b[h].get('worstMs', '')} → {a[h].get('worstMs', '')} | {b[h].get('avgMs', '')} → {a[h].get('avgMs', '')} |")
    open(outp, "w", encoding="utf-8").write("\n".join(out) + "\n")
    print(f"same {len(same)} diff {len(diff)} blank {len(blank)} branch {len(branch)} missing {len(missing)}")
    return 1 if diff or missing else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
