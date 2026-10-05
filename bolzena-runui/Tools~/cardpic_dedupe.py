# 한 사도 안에서 고유 · 생성 카드 그림이 겹치지 않게 — cardpic_picks.json 에 「auto」 줄을 채운다(Docs/카드그림.md).
#   python "Tools~/cardpic_dedupe.py"   → 그 뒤 build_cardart.py · copy_assets.py
# 옛 규칙(스킬 아이콘)은 아이콘이 둘뿐인 사도가 많아 같은 아이콘이 여러 카드에 붙는다. 겹친 카드(먼저 가진 카드 = 손으로 고른 것 → id 순)는 이 순서로 바꾼다:
#   1) 그 사도의 안 쓴 원작 스킬 아이콘(입학 · 고학년 · 어사이드 1~3)
#   2) 안 쓴 원작 통짜 그림 — 애장품(aside) · 선물(present) · 싸우는 SD(btn): 투명 그림이라 자르지 않고 성격 색 바탕에 얹는다
#   3) 안 쓴 액자 일러스트(story) — 얼굴이 위쪽 가운데인 그림이 많아 기본 자리(가운데 · 위 3할)로 자른다
#   4) 그래도 없으면 같은 아이콘의 좌우 뒤집기(__f) · 뒤집기 + 성격 색 틴트(__t) — copy_assets.py 가 굽는다
# 손으로 고른 줄(file · icon · keep)은 그대로 두고, auto 줄만 지우고 다시 쓴다.
import glob, json, os, re, collections

HERE = os.path.dirname(os.path.abspath(__file__))
PICKS = os.path.join(HERE, "cardpic_picks.json")
CARDART = os.path.join(HERE, "..", "Runtime", "Resources", "RunUI", "cardart.json")
SRC = r"C:\projects\볼제나\assets\cardart_src"
SKILLICONS = r"C:\projects\볼제나\assets\skillicons"
HEROES = r"C:\projects\bolzena-content-v2\heroes"
DRAFT = r"C:\projects\bolzena-content-v2\_ref\사도카드그림.json"

picks = json.load(open(PICKS, encoding="utf-8"))
P = picks["cards"]
for k in [k for k, v in P.items() if v.get("auto")]:
    del P[k]
json.dump(picks, open(PICKS, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
import subprocess, sys
subprocess.run([sys.executable, os.path.join(HERE, "build_cardart.py")], check=True)   # auto 를 뺀 표로 옛 규칙 아이콘을 다시
base_icons = json.load(open(CARDART, encoding="utf-8"))["cards"]   # build_cardart 의 옛 규칙(auto 를 뺀 뒤 다시 돌려 쓴 값이어야 한다)
draft = json.load(open(DRAFT, encoding="utf-8"))["cards"]

heroes = {}
for f in sorted(glob.glob(os.path.join(HEROES, "*", "*.json"))):
    d = json.load(open(f, encoding="utf-8"))
    h = d["heroes"][0]
    mine = [c for c in d["cards"] if c["id"] not in set(h["starter"])]
    folder = next((draft[c["id"]].split("/")[0] for c in d["cards"] if c["id"] in draft), None)
    heroes[h["id"]] = (h, mine, folder)


def order_key(cid):
    m = re.search(r"_(u|t)(\d+)$", cid)
    return (0 if m and m.group(1) == "u" else 1, int(m.group(2)) if m else 99, cid)


stats = collections.Counter()
for hid, (h, mine, folder) in heroes.items():
    # 지금 그림 — 손으로 고른 원작 그림(file) · 고른 아이콘(icon) · 옛 규칙 아이콘
    def art_of(cid):
        p = P.get(cid, {})
        if p.get("file"):
            return "file:" + p["file"]
        if p.get("icon"):
            return "icon:" + p["icon"]
        return "icon:" + base_icons[cid] if cid in base_icons else None
    cur = {c["id"]: art_of(c["id"]) for c in mine}
    # 먼저 가진 카드가 이긴다 — 손으로 고른 것(file · icon) 먼저, 그다음 id 순
    hand = lambda cid: 0 if P.get(cid, {}).get("file") or P.get(cid, {}).get("icon") else 1
    seen, dups = set(), []
    for cid in sorted(cur, key=lambda c: (hand(c), order_key(c))):
        a = cur[cid]
        if a is None:
            continue
        if a in seen:
            dups.append(cid)
        else:
            seen.add(a)
    if not dups:
        continue
    used_files = {a[5:] for a in seen if a.startswith("file:")}
    used_icons = {a[5:] for a in seen if a.startswith("icon:")}
    # 후보 1 — 원작 스킬 아이콘(이름은 skillicons 의 소문자 이름)
    art_key = None
    for a in used_icons:
        m = re.match(r"(?:icon_admissionskill_|icon_graduateskill_|aside_skill_)([a-z0-9]+?)(?:_\d)?$", a)
        if m:
            art_key = m.group(1)
            break
    icons = []
    if art_key:
        for n in [f"icon_admissionskill_{art_key}", f"aside_skill_{art_key}_1", f"aside_skill_{art_key}_2", f"aside_skill_{art_key}_3", f"icon_graduateskill_{art_key}"]:
            if os.path.exists(os.path.join(SKILLICONS, n + ".png")) and n not in used_icons:
                icons.append(n)
    # 후보 2 · 3 — 원작 그림(통짜 → 액자)
    files = []
    if folder:
        for cat in ("aside", "present", "btn", "story"):
            for f in sorted(glob.glob(os.path.join(SRC, folder, cat + "__*.png"))):
                key = folder + "/" + os.path.basename(f)[:-4]
                if key not in used_files:
                    files.append((cat, key))
    # 같은 애장품의 강화판 · 기본판은 둘 다 쓰지 않는다(거의 같은 그림)
    def twin(key):
        return key.replace("_LimitLevelBreak", "")
    used_twins = {twin(k) for k in used_files}
    for cid in dups:
        old = cur[cid]
        if icons:
            n = icons.pop(0)
            P[cid] = {"icon": n, "auto": True, "why": f"겹침 풀기 — 이 사도의 안 쓴 원작 스킬 아이콘(전: {old})"}
            stats["스킬 아이콘"] += 1
            continue
        pick = next(((cat, k) for cat, k in files if twin(k) not in used_twins), None)
        if pick:
            cat, k = pick
            files.remove(pick)
            used_twins.add(twin(k))
            e = {"file": k, "nature": h["nature"], "auto": True, "why": f"겹침 풀기 — 이 사도의 안 쓴 원작 그림 {cat}(전: {old})"}
            if cat == "story":
                e["c"] = [5, 3.0, 0.62]
            P[cid] = e
            stats["원작 그림 " + cat] += 1
            continue
        base = old[5:] if old.startswith("icon:") else None
        if base is None:
            continue
        var = "__f" if (old[5:] + "__f") not in used_icons else "__t"
        P[cid] = {"icon": base + var, "auto": True, "why": f"겹침 풀기 — 남은 그림이 없어 같은 아이콘을 {'뒤집어' if var == '__f' else '뒤집고 성격 색으로 물들여'} 구분"}
        used_icons.add(base + var)
        stats["뒤집기/틴트 " + var] += 1

picks["cards"] = dict(sorted(P.items()))
json.dump(picks, open(PICKS, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print(dict(stats), "합", sum(stats.values()))
