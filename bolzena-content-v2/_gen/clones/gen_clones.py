# 보스 클론 키트 생성기 — 사도 데이터(heroes JSON)에서 보스 클론 정의를 만든다(_measure/클론_키트_생성.md 의 A안).
#   python _gen/clones/gen_clones.py            마을 JSON 에 넣는다(clone_g_* 는 지우고 다시 — 같은 입력이면 같은 결과)
#   python _gen/clones/gen_clones.py --dry      쓰지 않고 목록만
# 손질은 _gen/clones/overrides.json — {사도키: {k: 때리기 배율, s: 회복 · 방어 배율, say: {수 번호: 이름}, blurb: 글, skip: true}}
# 체력 · 강인도는 그 자리 몸(원래 클론)을 따른다. 엔진 GameData.MakeClone 이 다른 층 자리에서는 체력 비율로 수치를 맞춘다.
import json, glob, os, re, sys, math

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CORE = r"C:\projects\bolzena-core\Runtime\Run\Run.cs"
OVR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "overrides.json")
PREFIX = "clone_g_"
NL = chr(10)
NATURES = ["순수", "냉정", "광기", "활발", "우울"]
PARTY_ST = {"약화": "약화", "취약": "취약", "고통": "고통", "균열": "균열", "충격": "충격", "충격파": "충격", "손상": "손상", "표식": "취약", "그을림": "고통"}


def load():
    H, C = {}, {}
    for f in sorted(glob.glob(os.path.join(ROOT, "heroes", "*", "*.json"))):
        d = json.load(open(f, encoding="utf-8"))
        for h in d["heroes"]: H[h["id"]] = h
        for c in d.get("cards", []): C[c["id"]] = c
    V = {}
    for f in sorted(glob.glob(os.path.join(ROOT, "world", "villages", "*.json"))):
        if os.path.basename(f).startswith("_"): continue
        V[f] = json.load(open(f, encoding="utf-8"))
    return H, C, V


def no_clone():
    s = open(CORE, encoding="utf-8").read()
    m = re.search(r"NO_CLONE = new\(\) \{(.*?)\};", s, re.S)
    return set(re.findall(r'"([^"]+)"', m.group(1)))


def base(i): return i.split("_")[0]


def star_fits(floor, star): return star <= 2 if floor == 0 else star >= 3


def candidates(H, NO, race, nature, floor):
    c = sorted([h["id"] for h in H.values() if h["race"] == race and h["nature"] == nature and h["id"] not in NO and star_fits(floor, h["star"])])
    seen, out = set(), []
    for i in c:
        if base(i) in seen: continue
        seen.add(base(i)); out.append(i)
    return out


def floor_cands(H, NO, race, nature, floor):
    c = candidates(H, NO, race, nature, floor)
    return candidates(H, NO, race, nature, 1) if not c and floor == 0 else c


def slots(H, V, NO):
    """사도마다 보스 자리 [(마을 파일, 층, 몸 id)] — 엔진 Run.FillBosses 와 같은 후보."""
    out = {}
    for f, d in V.items():
        E = {e["id"]: e for e in d["enemies"]}
        for v in d["villages"]:
            for fi, fl in enumerate(v["floors"]):
                b = next((x for x in fl["boss"] if E.get(x, {}).get("boss")), fl["boss"][0] if fl["boss"] else None)
                if not b or not E.get(b, {}).get("clone"): continue
                for nat in NATURES:
                    for h in floor_cands(H, NO, v["race"], nat, fi):
                        out.setdefault(h, [])
                        if (f, fi, b) not in out[h]: out[h].append((f, fi, b))
    return out


def clean(n): return (n or "").strip().rstrip(".").strip()


OTHERS = set()   # 다른 사도 이름(그 사도 것만 빼고) — 수 이름 · 설명에 섞이지 않게(main 에서 채운다)
SAFE2 = {"이드", "오르", "아야", "림"}   # 흔한 말 조각과 겹치는 두 글자 이름 — 이름 검사에서 뺀다


def mixes(text, own):
    return any(n in (text or "") for n in OTHERS if n != own and not own.startswith(n))


def hero_cards(C, hid, own=""):
    cs = sorted([c for c in C.values() if c.get("hero") == hid and not c.get("token")], key=lambda c: (not c.get("unique", False), c["id"]))
    A = [clean(c["name"]) for c in cs if c["type"] == "공격" and not mixes(c["name"], own)]
    S = [clean(c["name"]) for c in cs if c["type"] != "공격" and not mixes(c["name"], own)]
    return A or S or ["일격"], S or A or ["준비"]


def walk_fx(fx):
    for f in fx or []:
        yield f
        for k in ("then", "fx", "else"):
            if isinstance(f.get(k), list): yield from walk_fx(f[k])


def status_pref(h, C, role):
    cnt = {}
    fxs = list(walk_fx((h.get("ult") or {}).get("fx")))
    for c in C.values():
        if c.get("hero") == h["id"]: fxs += list(walk_fx(c.get("fx")))
    for f in fxs:
        if f.get("k") == "status" and f.get("id") in PARTY_ST and "Enem" in str(f.get("target", "Enemy")):
            cnt[PARTY_ST[f["id"]]] = cnt.get(PARTY_ST[f["id"]], 0) + 1
    if cnt: return sorted(cnt.items(), key=lambda x: (-x[1], x[0]))[0][0]
    return {"딜러": "약화", "탱커": "취약", "서포터": "약화"}[role]


def kw_trigger(h):
    kw = h["keyword"]
    for p in h.get("passives") or []:
        if not any(f.get("k") == "stack" and f.get("id") == kw["name"] for f in walk_fx(p.get("fx"))): continue
        w = p.get("when", {})
        on = w.get("on")
        if on == "play":
            t = w.get("type")
            return ("onCard", t if t in ("공격", "스킬", "강화") else None)
        if on in ("hurt", "hit"): return ("onHit", None)
        if on == "turnEnd": return ("onTurnEnd", None)
    return ("onTurnStart", None)


def josa(word, a, b):
    ch = word[-1] if word else "가"
    if "가" <= ch <= "힣": return a if (ord(ch) - 0xAC00) % 28 else b
    return a if ch in "013678" else b


def gen(h, C, body, floor, ov):
    role = h["role"]; hp = body["hp"]; B = hp / 19.0
    k = ov.get("k", 1.0); s = ov.get("s", 1.0)
    own = re.sub(r"\(.*\)", "", h["name"]).strip()
    A, S = hero_cards(C, h["id"], own)
    a = lambda i: A[i % len(A)]
    sk = lambda i: S[i % len(S)]
    hit = lambda x: max(1, int(round(x * B * k)))
    sus = lambda x: max(1, int(round(x * B * s)))
    st = status_pref(h, C, role)
    kw = h["keyword"]; kn = kw["name"]
    kdesc = clean(kw.get("desc"))
    if mixes(kdesc, own): kdesc = ""
    ult = clean((h.get("ult") or {}).get("name")) or kn
    if role == "딜러":
        I = [dict(t="back", v=hit(1.05), say=a(0), w=3), dict(t="multi", v=hit(0.3), n=3, say=a(1), w=3),
             dict(t="attackAll", v=hit(0.62), say=a(2), id=st, n=1, w=2), dict(t="buff", id="사기", v=1, say=sk(0), w=2),
             dict(t="count", id=kn, v=1, say=sk(1), w=2)]
        P = [dict(t="back", v=hit(1.2), say=a(3), w=3), dict(t="multi", v=hit(0.36), n=3, say=a(1), w=3),
             dict(t="attackAll", v=hit(0.7), say=a(2), w=2), dict(t="count", id=kn, v=2, say=sk(1), w=2)]
    elif role == "탱커":
        I = [dict(t="attack", v=hit(0.85), say=a(0), w=3), dict(t="block", v=sus(1.0), say=sk(0), w=3),
             dict(t="buff", id="피해 감소", v=2, say=sk(1), w=2), dict(t="attackAll", v=hit(0.55), say=a(1), id=st, n=1, w=2),
             dict(t="back", v=hit(0.95), say=a(2), w=2)]
        P = [dict(t="back", v=hit(1.1), say=a(3), w=3), dict(t="block", v=sus(1.2), say=sk(0), w=2),
             dict(t="attackAll", v=hit(0.65), say=a(1), w=2), dict(t="buff", id="불굴", v=2, say=sk(2), w=2)]
    else:
        I = [dict(t="back", v=hit(0.95), say=a(0), w=3), dict(t="heal", v=sus(0.8), say=sk(0), w=2),
             dict(t="debuff", id=st, v=2, say=sk(1), w=2), dict(t="attackAll", v=hit(0.6), say=a(1), w=2),
             dict(t="buff", id="사기", v=1, say=sk(2), all=True, w=2)]
        P = [dict(t="back", v=hit(1.1), say=a(2), w=3), dict(t="multi", v=hit(0.33), n=3, say=a(1), w=3),
             dict(t="heal", v=sus(1.0), say=sk(0), w=2), dict(t="attackAll", v=hit(0.7), say=a(3), w=2)]
    for i, nm in (ov.get("say") or {}).items():
        i = int(i); (I + P)[i]["say"] = nm
    # 쌓이는 수치 — 사도 키워드
    trig, ty = kw_trigger(h)
    cap = max(3, min(8, int(kw.get("cap") or 5)))
    if trig == "onCard": cap = max(cap, 4 if ty else 6)
    per = kw.get("per") or []
    stats = {p.get("stat") for p in per if isinstance(p, dict)}
    act_say = clean(C.get(kw.get("onMax", {}).get("make", ""), {}).get("name")) if isinstance(kw.get("onMax"), dict) else ""
    act_say = act_say if act_say and not mixes(act_say, own) else a(4)
    ctr = {"name": kn, "desc": kdesc or kn, trig: 1}
    if ty: ctr["cardType"] = ty
    if stats & {"def", "taken"} and not stats & {"dealt", "atk"}: ctr["taken"] = -0.04
    else: ctr["dealt"] = 0.04 if stats & {"dealt", "atk", "crit"} else 0.03
    ctr.update({"max": cap, "at": cap, "act": dict(t="attackAll", v=hit(0.72), say=act_say), "mode": "next"})
    # 패시브 — 사도 패시브(앞 둘) + 격파되면 쌓인 것이 흩어짐
    ps = []
    for p in (h.get("passives") or [])[:2]:
        w = p.get("when", {}); on = w.get("on")
        once = on in ("lowHp", "fightStart")
        e = {"name": clean(p["name"]) if not mixes(p["name"], own) else f"{kn} {len(ps) + 1}"}
        if on == "lowHp": e.update(on="lowHp", at=float(w.get("pct", 0.3)))
        elif on == "fightStart": e["on"] = "fightStart"
        elif on == "turnStart": e["on"] = "turnStart"
        elif on == "turnEnd": e["on"] = "turnEnd"
        elif on in ("hurt", "hit"): e["on"] = "hurt"
        elif on == "play":
            e["on"] = "card"
            if w.get("type") in ("공격", "스킬", "강화"): e["type"] = w["type"]
        elif on in ("shieldBreak",): e["on"] = "guardBreak"
        elif on == "debuff": e["on"] = "debuffed"
        else: e["on"] = "act"
        do = None
        for f in walk_fx(p.get("fx")):
            kk = f.get("k")
            if kk == "heal": do = dict(t="selfHeal", v=sus(1.6 if once else 0.3)); break
            if kk == "shield": do = dict(t="block", v=sus(0.9 if once else 0.45)); break
            if kk == "status" and f.get("id") in PARTY_ST and "Enem" in str(f.get("target", "")): do = dict(t="debuff", id=PARTY_ST[f["id"]], v=1); break
            if kk == "status": do = dict(t="buff", id="사기", v=1); break
            if kk in ("dmg", "extra"): do = dict(t="attack", v=hit(0.5 if not once else 0.8)); break
            if kk == "stack": do = dict(t="count", id=kn, v=max(1, min(3, int(f.get("v", 1) or 1)))); break
        do = do or dict(t="count", id=kn, v=1)
        do["say"] = e["name"]
        e["do"] = do
        ps.append(e)
    ps.append({"name": f"흩어진 {kn}", "on": "broken", "do": dict(t="count", id=kn, v=-cap, say=f"흩어진 {kn}")})
    # 설명 글
    tr = {"onCard": f"파티가 {ty + ' ' if ty else ''}카드를 낼 때마다", "onHit": "맞을 때마다", "onTurnEnd": "턴이 끝날 때마다", "onTurnStart": "턴마다"}[trig]
    eff = f"1개당 받는 피해 −4%" if "taken" in ctr else f"1개당 주는 피해 +{int(ctr['dealt'] * 100)}%"
    blurb = ov.get("blurb") or (f"{floor + 1}층 보스 · {h['name']}(클론) — {kdesc or h['name'] + '의 ' + kn}. {tr} 「{kn}」 {josa(kn, '이', '가')} 쌓여({eff}) {cap}{josa(str(cap), '이', '가')} 되면 다음 차례에 「{act_say}」 로 전체를 칩니다. "
                                 f"고학년 「{ult}」 도 씁니다. 격파하면 쌓인 {kn}{josa(kn, '이', '가')} 흩어집니다.")
    e = {"id": PREFIX + h["id"], "name": h["name"] + " (클론)", "hp": hp, "row": body.get("row", "back"), "nature": h["nature"], "clone": h["id"],
         "tough": body.get("tough", 7), "boss": True, "pick": "shuffle", "blurb": blurb, "intents": I,
         "phase": {"at": 0.5, "say": f"「{ult}…!」", "intents": P}, "counters": [ctr], "passives": ps}
    return e


def main():
    dry = "--dry" in sys.argv
    H, C, V = load()
    NO = no_clone()
    for h in H.values():
        n = re.sub(r"\(.*\)", "", h["name"]).strip()
        if len(n) >= 2 and n not in SAFE2: OTHERS.add(n)
    ov_all = json.load(open(OVR, encoding="utf-8")) if os.path.exists(OVR) else {}
    allE = {e["id"]: e for d in V.values() for e in d["enemies"]}
    own = {e["clone"] for e in allE.values() if e.get("clone") and not e["id"].startswith(PREFIX)}
    sl = slots(H, V, NO)
    made = {f: [] for f in V}
    side = {}
    for hid in sorted(sl):
        if hid in own or ov_all.get(hid, {}).get("skip"): continue
        # 몸 = 그 사도가 가장 높은 층으로 서는 자리(2층 우선) — 다른 층은 엔진이 체력 비율로 맞춘다
        f, fi, b = sorted(sl[hid], key=lambda x: (-x[1], x[0], x[2]))[0]
        made[f].append(gen(H[hid], C, allE[b], fi, ov_all.get(hid, {})))
        side[PREFIX + hid] = {"hero": hid, "body": b, "floor": fi, "role": H[hid]["role"]}
    n = sum(len(x) for x in made.values())
    print(f"클론 키트 {n}명 · 이미 키트 있는 사도 {len(own)} · 후보 사도 {len(sl)}")
    if dry:
        for f, l in made.items(): print(os.path.basename(f), len(l), " ".join(e["clone"] for e in l))
        return
    with open(os.path.join(os.path.dirname(OVR), "slots.json"), "w", encoding="utf-8", newline=NL) as w: w.write(json.dumps(side, ensure_ascii=False, indent=1) + NL)
    for f, d in V.items():
        d["enemies"] = [e for e in d["enemies"] if not e["id"].startswith(PREFIX)] + made[f]
        tmp = f + ".tmp"
        with open(tmp, "w", encoding="utf-8", newline="\n") as w: w.write(json.dumps(d, ensure_ascii=False, indent=2) + "\n")
        os.replace(tmp, f)
        print(os.path.basename(f), len(made[f]))


if __name__ == "__main__":
    main()
