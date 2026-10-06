# -*- coding: utf-8 -*-
"""원작 고학년 타수 기준표(조사 담당 — C:\\projects\\bolzena-unity-tmp\\ult_hits\\ult_hits.tsv, 읽기만) → Runtime/Motion/ult_hits.json.
사도마다 원작 기준 타수(n) · 우리 타수 · 판정 · 원작 타격 시각(ms, 1배속 n 개)과 그 시각을 고른 까닭.

시각 고르기(앞에서부터):
  1) 되풀이 이펙트 중 횟수가 n 인 것 → 그 시각들(셰이디 1002322 ×6)
  2) 맞는 소리 칸이 n 번 → 그 시각들(림(혼돈) 1100 · 2867)
  3) 가장 많이 되풀이되는 이펙트(2번 넘게) → n 보다 많으면 고르게 n 개, 적으면 그 사이 · 뒤로 고르게 채움
  4) 그 밖 — 첫 시각(맞는 소리 · 되풀이 · 이펙트 묶음 첫 것)부터 이펙트 묶음 끝까지 고르게 n 개(간격 최소 90ms)
  python Tools/ult_hits_json.py
"""
import csv, json, os, re, sys
sys.stdout.reconfigure(encoding="utf-8", errors="replace")
SRC = r"C:\projects\bolzena-unity-tmp\ult_hits\ult_hits.tsv"
OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Runtime", "Motion", "ult_hits.json")


def nums(s): return [int(float(x)) for x in re.findall(r"\d+(?:\.\d+)?", s or "")]


def reps(s):
    out = []
    for part in (s or "").split(" "):
        m = re.match(r"(\d+):(\d+)@(.+)", part.strip())
        if m: out.append((int(m.group(2)), [int(x) for x in m.group(3).split("·") if x.strip().isdigit()]))
    # 칸 사이 공백으로 나뉘지 않은 꼴도 받는다
    if not out:
        for m in re.finditer(r"(\d+):(\d+)@([\d·]+)", s or ""):
            out.append((int(m.group(2)), [int(x) for x in m.group(3).split("·") if x]))
    return out


def even(a, b, n):
    if n <= 1: return [a]
    b = max(b, a + 90 * (n - 1))
    return [round(a + (b - a) * i / (n - 1)) for i in range(n)]


def pick(ts, n):
    if len(ts) == n: return ts
    if len(ts) > n: return [ts[round(i * (len(ts) - 1) / (n - 1))] for i in range(n)] if n > 1 else [ts[0]]
    return even(ts[0], ts[-1] + 120 * (n - len(ts)), n)


# 손으로 고친 시각 — 표의 「되풀이 이펙트」가 원작 타격이 아닌 사도. 리뉴아: 끝의 되풀이(11633~)만 잡혀 앞 11초가 비었다 →
# 원작 영상(0.6~11초 내내 타격)과 원작 애니 이펙트 표시(1533 · 2667 · 3333 · 4233 · 11633 · 12967)로
FIX = {"리뉴아": ([1533, 2667, 3333, 4233, 11633, 12967], "원작 애니 표시 · 영상"),
       "네르_빡침": ([2967], "원작 애니 표시 · 영상")}   # 네르(빡침) 영상: 날개 섬광(1.9초 표시) 뒤 뛰어들어 한 번(2.97초 표시)


def main():
    rows = list(csv.DictReader(open(SRC, encoding="utf-8-sig"), delimiter="\t"))
    res = {}
    for r in rows:
        try: n = int(r["원작기준타수"])
        except ValueError: n = 0
        if n <= 0: continue
        hit = nums(r["맞는소리ms"])
        rp = reps(r["되풀이이펙트(번호:횟수@ms)"])
        grp = nums(r["이펙트묶음ms"])
        times, why = None, ""
        exact = [ts for c, ts in rp if len(ts) == n]
        if exact: times, why = exact[0], "되풀이 이펙트"
        elif len(hit) == n: times, why = hit, "맞는 소리"
        else:
            big = max((ts for c, ts in rp if len(ts) > 2), key=len, default=None)
            if big: times, why = pick(big, n), "되풀이 이펙트(고르게)"
            else:
                first = (hit or [ts[0] for c, ts in rp if ts] or grp or [0])[0]
                last = max(grp[-1] if grp else first, first)
                times, why = even(first, last, n), "고르게"
        if r["키"] in FIX: times, why = FIX[r["키"]][0], FIX[r["키"]][1]
        res[r["키"]] = {"n": n, "ours": r["우리타수"], "verdict": r["판정"], "times": times, "why": why}
    json.dump({"_meta": {"from": SRC, "note": "원작 기준 타수 · 타격 시각(ms, 1배속). bolzena-fx/Tools/ult_hits_json.py 가 만든다"}, "heroes": res},
              open(OUT, "w", encoding="utf-8"), ensure_ascii=False, indent=0)
    print(f"{len(res)}명 → {OUT}")


if __name__ == "__main__":
    main()
