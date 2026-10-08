# 사도 카드 그림 — 공용 부품(재고 · 채우기 · 검사가 같이 쓴다). Docs/카드그림.md
#   cardart_stock.py(재고 표) · cardart_fill.py(카드마다 다른 그림 채우기) · cardart_check.py(검사)
# 원작 그림은 이미 뽑아 둔 것만 읽는다(새로 뽑지 않는다):
#   C:\projects\볼제나\assets\cardart_src\<그림 키>\<갈래>__<원작 이름>.png · assets\skillicons · .shots\standing-webp(스탠딩 Normal · Idle_1 한 장)
# 한국 서버 기준: 뽑은 곳은 한국판 클라이언트(com.epidgames.trickcalrevive — assets/available.json). 스킨 옷(icon__<키>SkinN · profile · skin 방 배경 · btn …skin)은
#   어느 서버에 나왔는지 이름 · 메타로 가릴 수 없어 「확인 못 함」— 카드에 쓰지 않는다.
import glob, json, os, re

HERE = os.path.dirname(os.path.abspath(__file__))
ASSETS = r"C:\projects\볼제나\assets"
SRC = os.path.join(ASSETS, "cardart_src")
SKILL = os.path.join(ASSETS, "skillicons")
STAND = r"C:\projects\볼제나\.shots\standing-webp"
HEROES = r"C:\projects\bolzena-content-v2\heroes"
ROSTERS = [r"C:\projects\bolzena-unity\Assets\Resources\RunUI\roster.json", r"C:\projects\bolzena-runui-test\Assets\Resources\RunUI\roster.json"]
UNITY_RES = r"C:\projects\bolzena-unity\Assets\Resources"
CARDART = os.path.join(HERE, "..", "Runtime", "Resources", "RunUI", "cardart.json")
PICKS = os.path.join(HERE, "cardpic_picks.json")
AVOID = json.load(open(os.path.join(HERE, "cardart_avoid.json"), encoding="utf-8"))["avoid"]
LOW = json.load(open(os.path.join(HERE, "cardart_low.json"), encoding="utf-8"))["cards"]

norm = lambda s: (s or "").replace(" ", "")


def roster():
    for p in ROSTERS:
        if os.path.exists(p):
            return json.load(open(p, encoding="utf-8"))["heroes"]
    raise SystemExit("roster.json 없음")


def art_of(hero, ro):
    n = norm(hero.get("name"))
    hit = next((h for h in ro if norm(h["ko"]) == n or norm(h["key"]) == n), None) \
        or next((h for h in ro if norm(h["key"]).startswith(n) or norm(h["ko"]).startswith(n)), None)
    return hit


def heroes():
    """사도 id → {hero, art, webp, cards(id → 카드), basic[s*], unique[u*], token[그 밖]} — bolzena-content-v2 만(본 게임 데이터)."""
    ro = roster()
    out = {}
    for f in sorted(glob.glob(os.path.join(HEROES, "**", "*.json"), recursive=True)):
        d = json.load(open(f, encoding="utf-8"))
        if not isinstance(d, dict):
            continue
        for h in d.get("heroes", []):
            r = art_of(h, ro)
            cs = [c for c in d.get("cards", []) if c.get("hero") == h["id"]]
            webp = next((n for n in ((r or {}).get("ko"), (r or {}).get("key")) if n and os.path.exists(os.path.join(STAND, n + ".webp"))), None)
            out[h["id"]] = {
                "hero": h, "art": (r or {}).get("art"), "webp": webp, "file": f,
                "cards": {c["id"]: c for c in cs},
                "basic": sorted(c["id"] for c in cs if re.search(r"_s\d+$", c["id"]) and not c.get("unique")),
                "unique": sorted(c["id"] for c in cs if c.get("unique")),
                "token": sorted(c["id"] for c in cs if not c.get("unique") and not re.search(r"_s\d+$", c["id"])),
            }
    return out


# ── 그림 갈래 ──
#   key = 짝 표에 쓰는 값(icon 이름 · cardart_src 상대 경로 · @stand/<키>/<자리>) · ident = 같은 그림인지 가르는 값(강화판 애장품 = 기본판)
SKIN = re.compile(r"skin\d", re.I)


# 이미 풀어 둔 원작 PNG(2026-10-09 추가 후보 — 새로 뽑지 않았다) — cardart_src 에 없는 것 가운데 파일 이름에 사도 원작 이름이 정확히 박힌 것만
RAW = r"C:\projects\bolzena-unity-tmp\mumu_pull\png\raw"
# (갈래, 정규식 — {n} = 사도 원작 이름(cardart_src 의 head__<이름>), 투명 그림인가)
RAW_RULES = [
    ("hilite", r"lobbysetting/Highlight_{n}\.png", False),              # 로비 꾸미기 하이라이트(사도 일러스트 판 320×230)
    ("cg", r"dialoguecut/Theme_Event_{n}_ClearCG\d+\.png", False),       # 테마 이벤트 클리어 CG
    ("evicon", r"skillicons/Icon_Event_{n}_\d+\.png", False),           # 이벤트 스킬 아이콘
    ("theme", r"00\.themeevent/[^/]+/(?:[^/]+/)?[A-Za-z]+(?:Battle|MainLobby)_Entry_{n}(?:_\d)?\.png", True),   # 테마 이벤트 등장 그림(투명)
]
_names, _raw = None, None


def raw_names():
    """그림 키 → 원작 이름(head__<이름>.png)."""
    global _names
    if _names is None:
        _names = {}
        for k in os.listdir(SRC):
            h = [f for f in os.listdir(os.path.join(SRC, k)) if f.startswith("head__")]
            if h:
                _names[k] = h[0][6:-4]
    return _names


def raw_items(art):
    global _raw
    if _raw is None:
        _raw = []
        if os.path.isdir(RAW):
            for root, _, fs in os.walk(RAW):
                for f in fs:
                    if f.endswith(".png"):
                        _raw.append(os.path.relpath(os.path.join(root, f), RAW).replace(os.sep, "/"))
    n = raw_names().get(art)
    out = []
    if not n:
        return out
    for cat, rx, obj in RAW_RULES:
        r = re.compile(rx.format(n=re.escape(n)))
        for rel in sorted(_raw):
            if r.fullmatch(rel):
                out.append({"key": "@raw/" + rel[:-4], "kind": "raw", "cat": cat, "obj": obj, "ok": True})
    return out


def raw_is_obj(key):
    return any(obj and re.fullmatch(rx.format(n="[A-Za-z0-9]+"), key[5:] + ".png") for _, rx, obj in RAW_RULES)


def stock(art):
    """한 사도의 원작 그림 재고 — [{key, kind(icon · file · stand), cat, ok(카드에 쓰나), why}]"""
    items = []
    for slot, n in (("adm", f"icon_admissionskill_{art}"), ("grad", f"icon_graduateskill_{art}"),
                    ("aside1", f"aside_skill_{art}_1"), ("aside2", f"aside_skill_{art}_2"), ("aside3", f"aside_skill_{art}_3")):
        if os.path.exists(os.path.join(SKILL, n + ".png")):
            items.append({"key": n, "kind": "icon", "cat": slot, "ok": True})
    d = os.path.join(SRC, art)
    for f in sorted(os.listdir(d)) if os.path.isdir(d) else []:
        if not f.endswith(".png"):
            continue
        cat, name = f[:-4].split("__", 1)
        key = f"{art}/{f[:-4]}"
        it = {"key": key, "kind": "file", "cat": cat, "ok": True}
        if cat == "skill":
            continue   # skillicons 와 같은 그림(위 아이콘 다섯)
        if cat in ("profile", "skin") or (cat in ("icon", "btn") and SKIN.search(name)):
            it.update(ok=False, why="스킨 — 출시 서버를 이름 · 메타로 가릴 수 없음(확인 못 함)")
        elif cat == "bg":
            it.update(ok=False, why="배경(사도 없음) — 카드 그림으로 안 씀")
        elif cat == "aside" and name.endswith("_LimitLevelBreak"):
            it["cat"] = "aside"
        items.append(it)
    items += raw_items(art)
    items.append({"key": f"@stand/{art}/card", "kind": "stand", "cat": "stand_card", "ok": True, "why": "스탠딩 상반신(시작 카드 기본 — 표에 안 적는다)"})
    items.append({"key": f"@stand/{art}/face", "kind": "stand", "cat": "stand_face", "ok": True, "why": "스탠딩 얼굴 확대"})
    items.append({"key": f"@stand/{art}/full", "kind": "stand", "cat": "stand_full", "ok": True, "why": "스탠딩 전신(성격 바탕)"})
    return items


def cat_of(key):
    """짝 값 → 갈래(채우기 선호 순서에 쓴다)."""
    if key.startswith("@stand/"):
        return "stand_" + key.split("/")[2]
    if key.startswith("@raw/"):
        for cat, rx, _ in RAW_RULES:
            if re.fullmatch(rx.format(n="[A-Za-z0-9]+"), key[5:] + ".png"):
                return cat
        return "raw"
    if "/" not in key:
        m = re.match(r"(icon_admissionskill|icon_graduateskill|aside_skill)_.+?(?:_(\d))?(?:__[ft])?$", key)
        if not m:
            return "icon"
        return {"icon_admissionskill": "adm", "icon_graduateskill": "grad"}.get(m.group(1)) or "aside" + (m.group(2) or "1")
    c = os.path.basename(key).split("__")[0]
    return "prop" if c == "aside" else c


def ident(key):
    """같은 그림인지 — 뒤집기 · 틴트 꼬리(__f · __t)와 애장품 강화판(_LimitLevelBreak)은 같은 그림으로 센다. skill__ 원본 = 스킬 아이콘."""
    if key is None:
        return None
    k = re.sub(r"__[ft]$", "", key)
    k = k.replace("_LimitLevelBreak", "")
    m = re.match(r"[^/]+/skill__(.+)$", k)
    if m:
        k = m.group(1).lower()
    if k.endswith(".png"):
        k = k[:-4]
    # SD 초상(icon__<이름>)은 스탠딩 전신과 같은 자세 그림이라 같은 것으로 센다(2026-10-09 전수 검토)
    m = re.match(r"([^/]+)/icon__[A-Za-z0-9]+$", k)
    if m:
        k = f"@stand/{m.group(1)}/full"
    return k


def load_picks():
    return json.load(open(PICKS, encoding="utf-8"))


def resolved(hid, H, cardart, picks):
    """화면이 그리는 그림(CardArt.Of 와 같은 순서) — 카드 id → 짝 값(그림 key · 아이콘 이름 · @stand/<키>/card) 또는 None."""
    import cardpic
    name2key = {}
    for cid, p in picks.items():
        if p.get("file"):
            name2key[cardpic.name_of(p)] = p["file"]
    pics, icons = cardart.get("pics", {}), cardart.get("cards", {})
    out = {}
    for cid in H["basic"] + H["unique"] + H["token"]:
        c = H["cards"][cid]
        if cid in pics:
            out[cid] = name2key.get(pics[cid], "pic:" + pics[cid])
        elif cid in icons:
            out[cid] = icons[cid]
        elif c.get("unique"):
            out[cid] = None
        else:
            out[cid] = f"@stand/{H['art']}/card"
    return out


def baked(key, cardart_name=None):
    """구운 그림이 Resources 에 있나(본 프로젝트)."""
    R = os.path.join(UNITY_RES, "RunArt")
    if key is None:
        return False
    if key.startswith("@stand/") and key.endswith("/card"):
        return os.path.exists(os.path.join(R, "Standing", key.split("/")[1] + ".png"))
    if cardart_name:
        return os.path.exists(os.path.join(R, "CardPic", cardart_name + ".png")) or os.path.exists(os.path.join(R, "CardObj", cardart_name + ".png"))
    return os.path.exists(os.path.join(R, "Skill", key + ".png"))


# ── 가로로 긴 그림(만화 컷 · 하이라이트)에서 사도 자리 찾기 ──
#   사도 대화창 얼굴(head__<이름>)의 색(피부 · 흰색 · 검정을 뺀 머리 · 옷 색) 분포를 가로 그림에 되비춰(back-projection),
#   카드 비율 창(세로 전체)을 옆으로 밀며 그 색이 가장 많은 자리를 고른다. 빗나가면 cardart_crop.json 손보정이 이긴다.
def find_x(img_path, art, ratio=364 / 512):
    import cv2, numpy as np
    head = os.path.join(SRC, art, f"head__{raw_names().get(art, '')}.png")
    if not os.path.exists(head) or not os.path.exists(img_path):
        return 5.0
    from PIL import Image
    rd = lambda f: cv2.cvtColor(np.asarray(Image.open(f).convert("RGBA")), cv2.COLOR_RGBA2BGRA)   # cv2.imread 는 한글 경로를 못 읽는다
    h = rd(head)
    hsv = cv2.cvtColor(h[:, :, :3], cv2.COLOR_BGR2HSV)
    a = h[:, :, 3] > 128 if h.shape[2] == 4 else np.ones(h.shape[:2], bool)
    s, v, hu = hsv[:, :, 1], hsv[:, :, 2], hsv[:, :, 0]
    skin = (hu < 25) & (s < 110) & (v > 150)
    mask = (a & (s > 50) & (v > 50) & ~skin).astype(np.uint8)
    if mask.sum() < 50:
        mask = (a & (v > 40)).astype(np.uint8)
    hist = cv2.calcHist([hsv], [0, 1], mask, [30, 16], [0, 180, 0, 256])
    cv2.normalize(hist, hist, 0, 255, cv2.NORM_MINMAX)
    im = rd(img_path)
    ih = cv2.cvtColor(im[:, :, :3], cv2.COLOR_BGR2HSV)
    bp = cv2.calcBackProject([ih], [0, 1], hist, [0, 180, 0, 256], 1).astype(np.float32)
    if im.shape[2] == 4:
        bp *= im[:, :, 3] > 128
    H, W = bp.shape
    cw = min(W, int(H * ratio))
    col = bp.sum(axis=0)
    cs = np.concatenate([[0], np.cumsum(col)])
    best = max(range(0, W - cw + 1, max(1, W // 200)), key=lambda x: cs[x + cw] - cs[x])
    return round((best + cw / 2) / W * 10, 2)


def src_path(key):
    """짝 값 → 원본 그림 경로(스탠딩 자리는 None)."""
    if key.startswith("@raw/"):
        return os.path.join(RAW, *key[5:].split("/")) + ".png"
    if key.startswith("@stand/"):
        return None
    if "/" in key:
        return os.path.join(SRC, *key.split("/")) + ".png"
    return os.path.join(SKILL, re.sub(r"__[ft]$", "", key) + ".png")
