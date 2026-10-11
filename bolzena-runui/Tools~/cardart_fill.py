# 사도 카드마다 다른 원작 그림 — 짝 표(cardpic_picks.json)의 빈칸 · 겹침을 채운다(2026-10-08). Docs/카드그림.md
#   python "Tools~/cardart_fill.py"   → 짝 표에 "fill": true 줄을 다시 쓰고 build_cardart.py 를 돌린다 · 그 뒤 copy_assets.py(굽기) · cardart_check.py(검사)
# 카드 내용이 바뀌면(id 는 그대로) 이것만 다시 돌리면 된다 — 자기가 쓴 줄(fill)만 지우고 다시 고른다. 손으로 고른 줄(file · icon · keep · auto)은 그대로.
#
# 한 사도 안에서(기본 3 · 고유 5 · 생성 카드) 그림이 겹치지 않게:
#   1) 그대로 두는 것 — 저학년 칸(cardart_low.json — 저학년 아이콘) → 손으로 고른 짝 → 옛 자동 짝(auto) → 옛 규칙 아이콘. 겹치면 앞 것이 이긴다.
#   2) 채우는 것 — 그림 없는 고유 카드(u5 등) · 겹친 고유 카드 → 기본 카드(s1~s3, 표에 없으면 늘 스탠딩이라 셋이 같았다) → 그림 없거나 겹친 생성 카드
#   3) 후보 = 그 사도의 원작 그림 재고(cardart_pool.stock — 스킨 · 배경은 뺀다) 가운데 안 쓴 것. 저학년 아이콘은 저학년 칸 것이라 다른 카드에 주지 않는다.
#      카드 종류 · 이름으로 갈래 순서를 정한다(아래 PREF · 물건 낱말). 기본 카드는 공격 = 싸우는 SD · 스탠딩 상반신, 방어 · 회복 = 스탠딩 얼굴 확대 · SD 액자 · 전신.
#      스탠딩 자리 셋(상반신 = 표에 안 적는 기본 · 얼굴 확대 · 전신)은 서로 다른 그림으로 센다.
import collections, json, os, re, subprocess, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import cardart_pool as P

# 2026-10-11 사용자 「공격 카드인데 미니미, 스킬 카드인데 스탠딩이 많다」: 공격 = 스탠딩 · 싸우는 그림(장면) — SD 미니미(일정 · 초상) 안 씀,
#   스킬 = SD 미니미 · 돕는 · 지키는 장면 — 스탠딩 · 싸우는 SD 안 씀, 강화 = 결의 · 변신(물건 · 어사이드). 목록에 없는 갈래는 고르지 않는다.
PREF = {
    "공격": ["stand_full", "cg", "hilite", "story", "btn", "theme", "rogue", "aside3", "aside2", "grad", "evicon", "prop", "present", "aside1"],
    "스킬": ["story", "schedule", "cg", "hilite", "present", "prop", "aside2", "aside3", "aside1", "evicon", "rogue", "theme", "grad"],
    "강화": ["aside1", "prop", "present", "grad", "evicon", "aside2", "aside3", "story", "cg", "hilite", "rogue", "theme"],
}
# 같은 사도의 기본 공격 두 번째 장(가벼운 덱 1코 · 1코, 무거운 덱 1코 · 2코) — 첫 장(스탠딩 상반신)과 다른 그림: 스탠딩 전신 · 싸우는 그림
BASIC2 = ["btn", "stand_full", "hilite", "cg", "story", "theme", "rogue"]
# 기본 카드(2026-10-09 전수 검토 「상반신 · 얼굴 확대가 사실상 같은 그림」): 한 사도에 스탠딩 상반신과 얼굴 확대를 같이 쓰지 않는다(take 가 막는다).
#   공격 = 싸우는 SD · 스탠딩 상반신 · 하이라이트 · 만화 컷 / 방어 · 회복 = SD 일정 · 액자 · 하이라이트 · 전신 — 얼굴 확대는 다른 것이 하나도 없을 때만
BASIC = {
    "공격": ["btn", "stand_card", "hilite", "cg", "theme", "story", "rogue", "stand_full", "clone", "icon", "head", "schedule", "aside3", "aside2", "grad", "present", "prop", "aside1", "stand_face"],
    "방어": ["schedule", "story", "hilite", "stand_full", "theme", "cg", "icon", "head", "rogue", "present", "prop", "aside1", "aside2", "aside3", "grad", "clone", "btn", "stand_card", "stand_face"],
}
# 생성 카드는 물건 · 아이콘 그림부터 — 종류 규칙(공격엔 미니미 없음 · 스킬엔 스탠딩 없음)은 PREF 와 같게
TOKEN = {
    "공격": ["aside3", "aside2", "evicon", "prop", "present", "aside1", "grad", "btn", "cg", "hilite", "story", "rogue", "theme"],
    "스킬": ["aside1", "aside2", "aside3", "prop", "present", "evicon", "grad", "story", "schedule", "cg", "hilite", "rogue", "theme"],
}
# 이름에 물건 낱말이 있으면 물건 그림(애장품 · 선물)부터
PROP_WORDS = re.compile(r"케이크|간식|과자|사탕|쿠키|빵|젤리|주스|우유|두유|차 |커피|도시락|밥|떡|꽃|책|편지|상자|선물|병|단지|인형|스티커|모자|가방|사과|당근|물고기|생선|메모|지도|깃펜|두루마리|열쇠|보석|돈|동전|금화|통조림|폭탄")
C_DEFAULT = {"story": [4.3, 3.5, 0.6], "schedule": [5, 5.6, 0.6], "cg": [5, 5, 1.0]}
WHY = {"hilite": "로비 하이라이트 일러스트", "theme": "테마 이벤트 등장 그림", "rogue": "원작 로그라이크 카드 그림", "evicon": "이벤트 스킬 아이콘", "btn": "싸우는 SD", "cg": "만화 컷", "story": "액자 일러스트", "schedule": "SD 일정 액자", "present": "좋아하는 선물(물건)", "prop": "애장품(물건)",
       "head": "대화창 얼굴", "icon": "SD 초상", "clone": "클론(적)판 초상", "grad": "고학년 아이콘", "aside1": "어사이드 1 아이콘", "aside2": "어사이드 2 아이콘",
       "aside3": "어사이드 3 아이콘", "stand_card": "스탠딩 상반신", "stand_face": "스탠딩 얼굴 확대", "stand_full": "스탠딩 전신"}

data = P.load_picks()
picks = data["cards"]
for k in [k for k, v in picks.items() if v.get("fill")]:
    del picks[k]
# 기본 카드 규칙(2026-10-09 사용자 결정 — 135명 통일): 기본 공격 = 그 사도 스탠딩 일러(상반신 자르기 — 표에 안 적는다),
#   기본 스킬(방어 · 실드 · 회복) = 그 사도의 SD 꼬마 전신 그림(원작 SD 일정 그림 ScheduleStory — 135명 모두 있다 · 같은 자르기).
#   기본 카드끼리는 같은 그림이어도 된다. 기본 카드 줄은 늘 이 규칙으로 다시 쓴다(손 줄도 지운다).
BASIC_IDS = {cid for h in P.heroes().values() for cid in h["basic"]}
for k in [k for k in picks if k in BASIC_IDS]:
    del picks[k]
SCHED_C = [5, 5.6, 0.6]
overlap = []
json.dump(data, open(P.PICKS, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
subprocess.run([sys.executable, os.path.join(HERE, "build_cardart.py")], check=True, stdout=subprocess.DEVNULL)
cardart = json.load(open(P.CARDART, encoding="utf-8"))
CROP = json.load(open(os.path.join(HERE, "cardart_crop.json"), encoding="utf-8"))["crop"]   # 그림별 자르기 손보정
smeta = {}
mp = os.path.join(P.UNITY_RES, "RunArt", "Standing", "_meta.json")
if os.path.exists(mp):
    smeta = json.load(open(mp, encoding="utf-8"))


def rank(cid):
    p = picks.get(cid, {})
    if cid in P.LOW:
        return 0
    if (p.get("file") or p.get("icon") or p.get("keep")) and not p.get("auto"):   # keep(옛 규칙 아이콘으로 둠)도 손 줄
        return 1
    if p.get("auto"):
        return 2
    return 3


def order(cid):
    m = re.search(r"_([sut])(\d+)$", cid)
    return (m.group(1) if m else "z", int(m.group(2)) if m else 99, cid)


stats = collections.Counter()
short = []
for hid, h in sorted(P.heroes().items()):
    art = h["art"]
    if not art:
        continue
    res = P.resolved(hid, h, cardart, picks)
    keep, need = {}, []
    used = set()
    # 1) 그대로 두는 것(고유 · 생성) — 순위가 높은 것이 먼저 자리를 잡는다
    sched = next((it["key"] for it in P.stock(art) if it["cat"] == "schedule"), None)
    used.add(P.ident(f"@stand/{art}/card"))
    basic2 = []
    for cid in h["basic"]:
        c = h["cards"][cid]
        if c.get("type") == "공격":
            # 2026-10-11 사용자: 기본 공격은 1코든 2코든 그 사도의 같은 그림 하나(가벼운 덱은 같은 카드 2장) — 두 번째 기본 공격도 스탠딩 그대로
            stats["기본 공격 = 스탠딩"] += 1
            continue
        if sched:
            picks[cid] = {"file": sched, "nature": h["hero"].get("nature"), "c": SCHED_C, "sd": True, "basic": True,
                          "why": f"기본 스킬 = SD 꼬마 전신(SD 일정 그림) — {c.get('type')} 「{c.get('name')}」"}
            stats["기본 스킬 = SD"] += 1
        else:
            short.append(f"{hid} {cid} (SD 일정 그림 없음)")
    if sched:
        used.add(P.ident(sched))
    for cid in sorted(h["unique"] + h["token"], key=lambda c: (rank(c), order(c))):
        k = res[cid]
        if k is not None and sched and P.ident(k) == P.ident(sched) and rank(cid) <= 1:
            keep[cid] = k   # 손으로 고른 고유 짝은 바꾸지 않는다 — 기본 스킬과 같은 그림이 된다(보고)
            overlap.append(cid)
            continue
        typ = h["cards"][cid].get("type")
        allow = (TOKEN if cid in h["token"] else PREF).get(typ, PREF["스킬"]) + ["adm"]
        bad_kind = k is not None and not k.startswith("pic:") and cid not in P.LOW and P.cat_of(k) not in allow   # 종류에 안 맞는 그림(공격에 미니미 · 스킬에 스탠딩)
        if bad_kind:
            picks.pop(cid, None)
        # 2026-10-11: 옛 규칙 아이콘(짝 표에 없는 카드 — rank 3)도 다시 고른다. 원작 그림(장면 · 물건)이 종류 규칙으로 먼저 가고, 아이콘은 그 뒤
        if k is None or bad_kind or rank(cid) == 3 or k.startswith("pic:") or (k in P.AVOID and cid not in P.LOW) or P.ident(k) in used or (k.endswith("/card") and cid in h["token"]):
            need.append(cid)
            continue
        keep[cid] = k
        used.add(P.ident(k))
    used.add(P.ident(f"icon_admissionskill_{art}"))   # 저학년 아이콘은 저학년 칸 것
    pool = [it for it in P.stock(art) if it["ok"] and it["cat"] != "adm" and it["key"] not in P.AVOID]

    def take(cid, prefs, allow_card):
        c = h["cards"][cid]
        if PROP_WORDS.search(c.get("name", "")) and prefs is not BASIC2:   # 기본 공격(인형 휘두르기 …)은 물건 그림으로 가지 않는다
            prefs = ["prop", "present"] + prefs
        cands = [it for it in pool if it["cat"] in prefs and P.ident(it["key"]) not in used and (allow_card or it["cat"] != "stand_card")
                 and not (it["cat"] in ("stand_card", "stand_face") and ({f"@stand/{art}/card", f"@stand/{art}/face"} & used))]
        if not cands:
            return None
        rk = lambda it: (prefs.index(it["cat"]) if it["cat"] in prefs else 99, it["key"])
        return min(cands, key=rk)

    fills = [(cid, BASIC2, False) for cid in basic2]
    for cid in sorted([c for c in need if c in h["unique"]], key=order):
        fills.append((cid, PREF.get(h["cards"][cid].get("type"), PREF["스킬"]), False))
    for cid in sorted([c for c in need if c in h["token"]], key=order):
        fills.append((cid, TOKEN.get(h["cards"][cid].get("type"), TOKEN["스킬"]), False))
    for cid, prefs, allow_card in fills:
        it = take(cid, prefs, allow_card)
        c = h["cards"][cid]
        if it is None:
            short.append(f"{hid} {cid}")
            continue
        used.add(P.ident(it["key"]))
        why = f"{WHY.get(it['cat'], it['cat'])} — {c.get('type')} 「{c.get('name')}」"
        stats[it["cat"]] += 1
        if it["cat"] == "stand_card":
            continue   # 기본 — 표에 안 적으면 스탠딩 상반신
        if it["kind"] == "icon":
            picks[cid] = {"icon": it["key"], "fill": True, "why": why}
        elif it["kind"] == "stand":
            e = {"file": it["key"], "webp": h["webp"], "nature": h["hero"].get("nature"), "fill": True, "why": why}
            if it["cat"] == "stand_face":
                m = smeta.get(art) or [0.5, 1, 1, 0.08]
                s = 0.40   # 머리가 큰 그림체(머리 ≈ 키의 3할) — 아래 4할은 효과 글이 덮으니 이마 ~ 턱이 보이게
                e["c"] = CROP.get(it["key"]) or [round(m[0] * 10, 2), round((m[3] + 0.06 + 0.35 * s) * 10, 2), s]
            picks[cid] = e
        else:
            e = {"file": it["key"], "nature": h["hero"].get("nature"), "fill": True, "why": why}
            if it["key"] in CROP:
                e["c"] = CROP[it["key"]]
            elif it["cat"] in ("cg", "hilite", "rogue") and not it.get("obj"):
                # 가로로 긴 그림 — 사도 자리(얼굴 색)를 찾아 그쪽을 자른다(왼쪽만 잘려 사도가 빠지던 것)
                src = P.src_path(it["key"])
                e["c"] = [P.find_x(src, art), 5, 1.0]
            elif it["cat"] in C_DEFAULT:
                e["c"] = C_DEFAULT[it["cat"]]
            picks[cid] = e

data["cards"] = dict(sorted(picks.items()))
json.dump(data, open(P.PICKS, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
subprocess.run([sys.executable, os.path.join(HERE, "build_cardart.py")], check=True)
print("채움", sum(stats.values()), dict(stats.most_common()))
print("후보가 모자라 못 채운 카드", len(short), short[:30])
print("손으로 고른 고유 짝이 기본 스킬(SD 일정)과 같은 그림", len(overlap), overlap)
