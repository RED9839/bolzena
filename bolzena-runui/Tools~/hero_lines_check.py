# -*- coding: utf-8 -*-
"""사도별 대사 표(Runtime/Resources/RunUI/hero_lines.json) 검사.

  python Tools~/hero_lines_check.py [--talk <talk-ko.json>] [--roster <roster.json>] [-v]

본다:
  1) 로스터 사도가 모두 있나 · 표에만 있는 키(오타)
  2) 칸마다 줄 수 — lobby 4~6 · cheek 2 · tickle 2 · angry 2 · knock(꿀밤) 1~2 · pat 1~2 · party 1
     판 진행 한마디(2026-10-09) — rest · shop · reward · victory · bossStart · lowhp · event 각 2~3
  3) 줄 길이(36자 넘으면 경고) · 같은 줄 겹침
  4) 말 단계 — 반말(casual · royal) 사도 줄이 「요 · 니다 · 세요 · 죠 …」 로 끝나면 오류(0 이어야 한다).
     존댓말(polite) 사도는 존댓말 어미 비율이 70% 아래면 경고.
  5) 말투 프로필(사도 데스크 talk-ko.json, 있으면)과 비교 — style 과 reg 가 어긋나면 경고,
     교주 호칭이 「교주」 인데 「교주님」 을 쓰면(또는 그 반대) 경고. (프로필 파일은 읽기만 한다)
  6) 다른 사도 이름이 줄에 들어가면 경고.
  7) 판 진행 한마디 — 같은 갈래에서 같은 머리(한글 앞 네 글자)로 시작하는 줄이 사도 4명 넘게면 경고(문장 틀 남발),
     「장비」(화면 용어는 「아티팩트」) · 따옴표가 들어가면 오류.
오류가 있으면 끝 코드 1.
"""
import json, os, re, sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
LINES = os.path.join(ROOT, "Runtime", "Resources", "RunUI", "hero_lines.json")
ROSTER = "C:/projects/bolzena-unity/Assets/Resources/RunUI/roster.json"
TALK = "C:/projects/사도 데스크/prototype/data/talk-ko.json"
NAMES = "C:/projects/사도 데스크/prototype/data/names-ko.json"

KINDS = {"lobby": (4, 6), "cheek": (2, 2), "tickle": (2, 2), "angry": (2, 2), "knock": (1, 2), "pat": (1, 2), "party": (1, 1)}
RUN_KINDS = {k: (2, 3) for k in ("rest", "shop", "reward", "victory", "bossStart", "lowhp", "event")}   # 판 진행 한마디(2026-10-09)
KINDS.update(RUN_KINDS)
POLITE_END_RE = re.compile(r"(요|세요|죠|지요|시오|십시오|소서|옵니다|사와요)$")


class _Polite:
    """존댓말 어미 — 「~니다 · ~니까」 는 앞 글자 받침이 ㅂ(합니다 · 입니까)일 때만(아니다 · 다니까는 반말)."""
    @staticmethod
    def search(s):
        if POLITE_END_RE.search(s): return True
        if (s.endswith("니까") or s.endswith("니다")) and len(s) >= 3:
            c = ord(s[-3]) - 0xAC00
            return 0 <= c < 11172 and c % 28 == 17
        return False


POLITE_END = _Polite()
TRAIL = re.compile(r"[\s!?.,~…♪♡ㅡ\-—)」』\"'~]+$")
# 이름이 흔한 낱말과 같은 사도(「아야!」 「오르다」 「메이드」 「우이씨」) — 이름 검사에서 뺀다
AMBIG_NAMES = {"아야", "오르", "이드", "우이", "마요", "샤샤", "마리", "바나", "피라", "다야", "로니", "리온"}
# 프로필(talk-ko)과 다르게 둔 것 — 사도 데스크 bible.json 말투 설명을 따른 경우
ACCEPT = {
    "에슈르": "bible: 해요체 존댓말이 기본(흥분하면 반말), 교주는 「교주님」",
    "슈팡": "bible: 거친 반말, 교주는 「교주」(가끔 「교주님」)",
}
STYLE_REG = {"polite": "polite", "formal": "polite", "casual": "casual", "royal": "royal"}


def arg(name, default=None):
    if name in sys.argv:
        i = sys.argv.index(name)
        return sys.argv[i + 1] if i + 1 < len(sys.argv) else default
    return default


def ending(line):
    """문장 끝 — 마지막 문장의 꼬리 문장부호를 뗀 것."""
    s = TRAIL.sub("", line.strip())
    return s


def sentences(line):
    """문장 끝마다 나눈다(. ! ? … 뒤) — 각 문장의 어미를 본다."""
    parts = re.split(r"(?<=[.!?…~])\s+", line.strip())
    return [p for p in parts if TRAIL.sub("", p)]


def main():
    verbose = "-v" in sys.argv
    data = json.load(open(arg("--lines", LINES), encoding="utf-8-sig"))
    heroes = {e["key"]: e for e in data["heroes"]}
    roster = json.load(open(arg("--roster", ROSTER), encoding="utf-8-sig"))["heroes"]
    rkeys = [h["key"] for h in roster]
    ko_of = {h["key"]: h["ko"] for h in roster}

    talk = {}
    tp = arg("--talk", TALK)
    if os.path.exists(tp):
        t = json.load(open(tp, encoding="utf-8"))["heroes"]
        by_ko = {v["ko"]: v for v in t.values()}
        for k in rkeys:
            p = by_ko.get(ko_of[k])
            if p: talk[k] = p

    errs, warns = [], []
    total = 0
    reg_count = {}
    # 다른 사도 이름(괄호 앞 기본 이름, 두 글자 이상)
    base = lambda ko: re.sub(r"\(.*\)", "", ko).strip()
    names = sorted({base(h["ko"]) for h in roster if len(base(h["ko"])) >= 2}, key=len, reverse=True)

    for k in rkeys:
        if k not in heroes: errs.append(f"{k}: 대사 없음"); continue
    for k in heroes:
        if k not in ko_of: errs.append(f"{k}: 로스터에 없는 키(오타?)")

    seen = {}
    heads = {}
    report = []
    for k in rkeys:
        e = heroes.get(k)
        if not e: continue
        reg = e.get("reg")
        reg_count[reg] = reg_count.get(reg, 0) + 1
        if reg not in ("casual", "royal", "polite", "special"): errs.append(f"{k}: reg 「{reg}」 모름")
        n_lines, bad_casual, polite_hits = 0, [], 0
        for kind, (lo, hi) in KINDS.items():
            arr = e.get(kind) or []
            if not (lo <= len(arr) <= hi): errs.append(f"{k}: {kind} {len(arr)}줄(기대 {lo}~{hi})")
            for line in arr:
                n_lines += 1
                if len(line) > 36: warns.append(f"{k}/{kind}: {len(line)}자 「{line}」")
                if line in seen and seen[line] != k: warns.append(f"{k}/{kind}: {seen[line]} 와 같은 줄 「{line}」")
                if kind in RUN_KINDS:
                    if "장비" in line: errs.append(f"{k}/{kind}: 「장비」 → 「아티팩트」 — 「{line}」")
                    if re.search(r"[\"“”'‘’]", line): errs.append(f"{k}/{kind}: 따옴표 — 「{line}」")
                    hd = re.sub(r"[^가-힣]", "", line)[:4]
                    if len(hd) >= 2: heads.setdefault((kind, hd), set()).add(k)
                seen.setdefault(line, k)
                sents = sentences(line)
                pol = any(POLITE_END.search(ending(s)) for s in sents)
                if pol: polite_hits += 1
                if reg in ("casual", "royal"):
                    for s in sents:
                        if POLITE_END.search(ending(s)): bad_casual.append(f"{kind}: 「{line}」")
                # 다른 사도 이름
                me = base(ko_of[k])
                for nm in names:
                    if nm in AMBIG_NAMES or nm == me or nm in me: continue
                    if re.search(r"(^|[\s,.!?…「])" + re.escape(nm) + r"(?=$|[\s,.!?…~」]|[이가은는을를도랑과와의한께야아님])", line):
                        warns.append(f"{k}/{kind}: 다른 사도 이름 「{nm}」? 「{line}」"); break
                # 호칭
                p = talk.get(k)
                if p and k not in ACCEPT:
                    addr = p.get("addr")
                    if addr == "교주" and "교주님" in line: warns.append(f"{k}/{kind}: 호칭 프로필 「교주」 인데 「교주님」 — 「{line}」")
                    if addr == "교주님" and re.search(r"교주(?!님)", line): warns.append(f"{k}/{kind}: 호칭 프로필 「교주님」 인데 「교주」 — 「{line}」")
        total += n_lines
        for b in bad_casual: errs.append(f"{k}({reg}): 반말 사도인데 존댓말 어미 — {b}")
        ratio = polite_hits / max(1, n_lines)
        if reg == "polite" and ratio < 0.7: warns.append(f"{k}(polite): 존댓말 어미 {ratio:.0%}")
        p = talk.get(k)
        exp = STYLE_REG.get(p["style"]) if p else None
        if k in ACCEPT and verbose: print(f"허용: {k} — {ACCEPT[k]}")
        if exp and k not in ACCEPT and reg != "special" and not (exp == reg or {exp, reg} == {"casual", "royal"}):
            warns.append(f"{k}: 프로필 style={p['style']} 인데 reg={reg}" + (f" (note: {e.get('note')})" if e.get("note") else ""))
        report.append(f"{k:<14} {reg or '-':<8} {n_lines:>3}줄  존댓말 어미 {ratio:4.0%}  " + (f"프로필 {p['style']}/{p.get('addr')}" if p else "프로필 없음"))

    for (kind, hd), ks in sorted(heads.items()):
        if len(ks) > 4: warns.append(f"{kind}: 「{hd}…」 로 시작하는 줄이 사도 {len(ks)}명 — 문장 틀 반복 ({', '.join(sorted(ks))})")
    if verbose: print("\n".join(report))
    print(f"사도 {len([k for k in rkeys if k in heroes])}/{len(rkeys)} · 대사 {total}줄 · 말 단계 {reg_count}")
    casual_keys = [k for k in rkeys if heroes.get(k, {}).get("reg") in ("casual", "royal")]
    bad = sum(1 for x in errs if "반말 사도인데" in x)
    print(f"반말 사도 {len(casual_keys)}명 — 「요 · 니다」 류로 끝나는 줄 {bad}")
    if "에르핀" in heroes:
        e = heroes["에르핀"]
        n = sum(1 for kind in KINDS for l in e.get(kind) or [] for s in sentences(l) if POLITE_END.search(ending(s)))
        print(f"에르핀(reg={e.get('reg')}) 존댓말 어미 {n}")
    for w in warns: print("경고:", w)
    for x in errs: print("오류:", x)
    print(f"오류 {len(errs)} · 경고 {len(warns)}")
    sys.exit(1 if errs else 0)


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
