# 콘텐츠 생성 도우미 — 사도 한 명 = 묶음 JSON 한 파일
import json, os

OUT = r"C:\projects\bolzena-content\heroes"

def _t(d, target):
    if target: d["target"] = target
    return d

def D(r, t="oneEnemy", hits=None, base=None):
    d = {"k": "dmg", "ratio": r, "target": t}
    if hits: d["hits"] = hits
    if base: d["base"] = base
    return d
def DD(r, t="oneEnemy", hits=None): return D(r, t, hits, "def")
def B(r): return {"k": "block", "ratio": r}
def SH(r): return {"k": "shield", "ratio": r, "target": "party"}
def H(r): return {"k": "heal", "ratio": r}
def PAY(v): return {"k": "payHp", "v": v}
def ST(i, v, t=None): return _t({"k": "status", "id": i, "v": v}, t)
def K(i, v, t=None): return _t({"k": "stack", "id": i, "v": v}, t)
def SP(i, v=None): return {"k": "spend", "id": i, "all": True} if v is None else {"k": "spend", "id": i, "v": v}
def PER(i): return {"k": "perStack", "id": i}
def IFS(i, n=None, neg=False):
    d = {"k": "ifStack", "id": i}
    if n: d["n"] = n
    if neg: d["not"] = True
    return d
def IFB(): return {"k": "ifBroken"}
def DRAW(v): return {"k": "draw", "v": v}
def AP(v): return {"k": "ap", "v": v}
def TOUGH(v, t="oneEnemy"): return {"k": "tough", "v": v, "target": t}
def RUSH(v, t="oneEnemy"): return {"k": "rushDown", "v": v, "target": t}
def CLEANSE(v): return {"k": "cleanse", "v": v}
def GAUGE(v): return {"k": "gauge", "v": v}
def MOD(k, v, t="self", run=False, turns=None):
    d = {"k": k, "v": v, "target": t}
    if run: d["run"] = True
    if turns: d["turns"] = turns
    return d
def ATK(v, t="self"): return MOD("atkMod", v, t, run=True)
def DEF(v, t="self"): return MOD("defMod", v, t, run=True)

def O(name, fx, cost=None, tags=None, power=False):
    d = {"name": name}
    if cost is not None: d["cost"] = cost
    if tags: d["tags"] = tags
    d["fx"] = fx
    if power: d["power"] = True
    return d
def BL(name, kind=None, fx=None, tags=None):
    d = {"name": name}
    if kind: d["kind"] = kind
    if fx: d["fx"] = fx
    if tags: d["tags"] = tags
    return d

def card(hid, n, name, cost, typ, fx, tags=None, oracles=None, blesses=None, sig=False, blurb=None):
    d = {"id": f"{hid}_{n}", "name": name, "hero": hid, "cost": cost, "type": typ}
    if tags: d["tags"] = tags
    d["fx"] = fx
    if oracles is not None:
        d["unique"] = True
        if sig: d["signature"] = True
        d["oracles"] = oracles
        d["blesses"] = blesses or []
    if blurb: d["blurb"] = blurb
    return d

def starters(hid, role, names):
    """시작 카드 넷 — 공통 꼴(딜러: 1코 110% ×2 · 2코 240% · 방어 / 탱커: 방어 기반 60% ×2 · 방어 ×2 / 서포터: 방어 기반 60% ×2 · 회복 ×2)."""
    a, b, c = names
    if role == "딜러":
        cs = [card(hid, "s1", a, 1, "공격", [D(1.1)]), card(hid, "s2", b, 2, "공격", [D(2.4)]), card(hid, "s3", c, 1, "스킬", [B(2.0)])]
        return cs, [f"{hid}_s1", f"{hid}_s1", f"{hid}_s2", f"{hid}_s3"]
    if role == "탱커":
        cs = [card(hid, "s1", a, 1, "공격", [DD(0.6)]), card(hid, "s2", b, 1, "스킬", [B(2.0)])]
        return cs, [f"{hid}_s1", f"{hid}_s1", f"{hid}_s2", f"{hid}_s2"]
    cs = [card(hid, "s1", a, 1, "공격", [DD(0.6)]), card(hid, "s2", b, 1, "스킬", [H(2.1)])]
    return cs, [f"{hid}_s1", f"{hid}_s1", f"{hid}_s2", f"{hid}_s2"]

def P(name, on, fx, conds=None, limit=None, **w):
    d = {"name": name, "when": dict({"on": on}, **w)}
    if conds: d["conds"] = conds
    if limit: d["limit"] = {"per": limit[0], "n": limit[1]}
    d["fx"] = fx
    return d
def HPLOW(p=0.5): return {"c": "hp", "pct": p}

def hero(folder, hid, base, keyword, passives, ult, start_names, uniques):
    sc, starter = starters(hid, base["role"], start_names)
    h = dict(id=hid, **base)
    h["keyword"] = keyword
    h["passives"] = passives
    h["ult"] = ult
    h["starter"] = starter
    data = {"heroes": [h], "cards": sc + uniques}
    os.makedirs(os.path.join(OUT, folder), exist_ok=True)
    with open(os.path.join(OUT, folder, hid + ".json"), "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=1)

def NAP(v): return {"k": "nextAp", "v": v}
def BURN(v=1, rnd=False):
    d = {"k": "burn", "v": v}
    if rnd: d["random"] = True
    return d
def PERPAID(per=None):
    d = {"k": "perPaid"}
    if per: d["per"] = per
    return d
def PERDEB(): return {"k": "perDebuff"}
def IFDEB(n): return {"k": "ifDebuffs", "n": n}
