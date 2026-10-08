# -*- coding: utf-8 -*-
"""이벤트 · 클론 보스 대사의 말투 검사 — 말하는 사도의 말투 프로필(존댓말 / 반말)과 대사 어미가 맞나.

  python Tools~/event_voice_check.py [--data C:/projects/bolzena-content-v2] [-v]

말하는 이:
  - 이벤트 scene · 선택지 say · gamble say · judge passSay 의 따옴표 대사 = 장면 npc,
    선택지 `by`(대사 순서대로 — null = npc · "hero" = 선택지를 연 파티 사도 · 그 밖 = 이름)가 있으면 그것.
  - 마을 enemies[].clone 이 있는 적의 phase.say · phase2.say = 그 사도(클론).
말투 기준: hero_lines.json 의 reg(casual · royal = 반말, polite = 존댓말, special = 보지 않음).
  hero_lines 에 없으면 사도 데스크 talk-ko.json 의 style(읽기만).
문장이 존댓말 / 반말로 섞이면(문장마다 다르면) 보지 않는다. 말투가 일부러 다른 줄은 ACCEPT 에 이유를 적는다.
어긋난 줄이 있으면 끝 코드 1.
"""
import glob, json, os, re, sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
LINES = os.path.join(ROOT, "Runtime", "Resources", "RunUI", "hero_lines.json")
ROSTER = "C:/projects/bolzena-unity/Assets/Resources/RunUI/roster.json"
TALK = "C:/projects/사도 데스크/prototype/data/talk-ko.json"

# (이벤트 id 또는 마을 파일:클론, 말하는 이) → 이유 — 원작 말투를 확인하고 그대로 둔 것
ACCEPT = {
    ("G04", "아네트"): "원작: 교주에게도 「허접」 놀림 · 흥분하면 반말(나무위키 · bible) — 결투 도발 장면",
    ("B1", "셰럼"): "원작: 기록체 음슴체(~함 · ~됨, 어미 비율 noun 0.3)",
    ("M6", "알레트"): "원작: 군대식 ~임다 · ~슴다",
    ("M9", "칸나"): "프로필 formal 0.37 · casual 0.37 — 감탄 반말",
    ("F3", "베니"): "프로필 casual 0.53 · polite 0.4",
    ("F9", "디아나"): "권유 반말 섞임(더 먹어요, 더)",
    ("E5", "폴랑"): "구호 · 외침",
    ("E1", "에슈르"): "원작: 해요체 기본, 빵집 소리엔 반말로 발끈(「빵집 아니라고!」 — bible)",
    ("D8", "네티"): "원작: 말끝 ~용",
    ("D9", "리츠"): "원작: 싸움 앞에선 반말로 돌변",
    ("H1", "스피키"): "원작: 말끝 늘이기 ~요오",
    ("H3", "림"): "말버릇 「푸흡」",
    ("H5", "림(혼돈)"): "말버릇 「푸흡」 · 외침",
    ("furry.json", "베니"): "클론 보스 — 프로필 casual 0.53",
    ("ghost.json", "스피키"): "클론 보스 — 따라 하는 클론(일부러 다른 말투)",
    ("monatium.json", "칸나"): "클론 보스 — 프로필 formal · casual 반반",
}

POL_RE = re.compile(r"(요|요오|세요|죠|지요|시오|십시오|소서|옵니다|사와요|용|죵)$")
TRAIL = re.compile(r"[\s!?.,~…♪♡\-—)」』]+$")
Q = re.compile(r"[\"“]([^\"“”]+)[\"”]")


def polite(s):
    if POL_RE.search(s): return True
    if (s.endswith("니다") or s.endswith("니까")) and len(s) >= 3:
        c = ord(s[-3]) - 0xAC00
        return 0 <= c < 11172 and c % 28 == 17
    return False


# 반말 어미로 끝나는 문장만 반말로 센다(「딱 한 번만」 같은 토막 · 이름 부르기는 말 단계를 모른다)
CAS_END = set("다야어아지해게래자냐니걸군네라나거대데여줘봐와마돼서고까렴려구지렁")


def sentence_reg(x):
    x = re.sub(r",\s*[가-힣]{1,4}$", "", x)   # 끝의 부르는 말 · 말버릇(「…나오나요, 사장님」 「…돼요, 오이」)
    x = TRAIL.sub("", x)
    if len(x) < 2: return None
    if polite(x): return "polite"
    return "casual" if x[-1] in CAS_END else None


def reg_of(q):
    rs = [sentence_reg(TRAIL.sub("", p)) for p in re.split(r"(?<=[.!?…~])\s+", q.strip())]
    rs = [r for r in rs if r]
    if not rs: return None
    return rs[0] if all(r == rs[0] for r in rs) else "mixed"


def arg(name, default=None):
    if name in sys.argv:
        i = sys.argv.index(name)
        return sys.argv[i + 1] if i + 1 < len(sys.argv) else default
    return default


def main():
    data = arg("--data", "C:/projects/bolzena-content-v2")
    verbose = "-v" in sys.argv
    roster = json.load(open(ROSTER, encoding="utf-8-sig"))["heroes"]
    key2ko = {h["key"]: h["ko"] for h in roster}
    kos = set(key2ko.values())
    want = {}
    if os.path.exists(LINES):
        for e in json.load(open(LINES, encoding="utf-8-sig"))["heroes"]:
            r = e.get("reg")
            want[key2ko.get(e["key"], e["key"])] = "casual" if r in ("casual", "royal") else "polite" if r == "polite" else None
    if os.path.exists(TALK):
        for v in json.load(open(TALK, encoding="utf-8"))["heroes"].values():
            if v["ko"] not in want:
                want[v["ko"]] = "polite" if v["style"] in ("polite", "formal") else "casual" if v["style"] in ("casual", "royal") else None

    bad, ok, accepted = [], 0, []

    def judge(where, tag, whos, q):
        nonlocal ok
        whos = [w for w in whos if w in kos]
        exps = {want.get(w) for w in whos} - {None}
        rg = reg_of(q)
        if not exps or not rg or rg == "mixed": return
        if rg in exps: ok += 1; return   # 「hero」 — 선택지를 열 수 있는 사도 가운데 누구 말투든 맞으면
        who = " · ".join(whos); exp = "/".join(sorted(exps))
        why = ACCEPT.get((tag, who))
        if why: accepted.append(f"{where} {who}: 「{q}」 — {why}"); return
        bad.append(f"{where} {who}(기대 {exp}): 「{q}」 → {rg}")

    for f in sorted(glob.glob(os.path.join(data, "world", "events", "*.json"))):
        if os.path.basename(f).startswith("_"): continue
        for e in json.load(open(f, encoding="utf-8-sig"))["events"]:
            npc = e.get("npc")
            eid = e.get("id")
            texts = [("scene", e.get("scene"), None, None)]
            for i, o in enumerate(e.get("options", [])):
                opener = [key2ko.get(h, h) for h in (o.get("hero") or [])] + ([key2ko.get(o["price"]["hero"], o["price"]["hero"])] if isinstance(o.get("price"), dict) and o["price"].get("hero") else [])
                texts.append((f"선택지[{i}] say", o.get("say"), o.get("by"), opener))
                for g in o.get("gamble") or []: texts.append((f"선택지[{i}] gamble", g.get("say"), g.get("by") or o.get("by"), opener))
                if isinstance(o.get("judge"), dict): texts.append((f"선택지[{i}] passSay", o["judge"].get("passSay"), o.get("by"), opener))
            for fld, txt, by, opener in texts:
                if not isinstance(txt, str): continue
                for qi, q in enumerate(Q.findall(txt)):
                    b = by[qi] if by and qi < len(by) else None
                    whos = opener if b == "hero" else [b] if b else [npc]
                    if any(w in kos for w in whos): judge(f"{os.path.basename(f)} {eid} {fld}", eid, whos, q)
    for f in sorted(glob.glob(os.path.join(data, "world", "villages", "*.json"))):
        if os.path.basename(f).startswith("_"): continue
        for en in json.load(open(f, encoding="utf-8-sig")).get("enemies", []):
            c = en.get("clone")
            if c not in kos: continue
            for ph in ("phase", "phase2"):
                s = (en.get(ph) or {}).get("say")
                if s: judge(f"{os.path.basename(f)} 클론 {ph}.say", os.path.basename(f), [c], s.strip("「」"))

    if verbose:
        for a in accepted: print("허용:", a)
    for b in bad: print("어긋남:", b)
    print(f"맞음 {ok} · 허용 {len(accepted)} · 어긋남 {len(bad)}")
    sys.exit(1 if bad else 0)


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
