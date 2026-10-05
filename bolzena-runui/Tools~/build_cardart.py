# 고유 카드 그림 표(임시) — 카드마다 그 사도의 원작 스킬 아이콘 하나를 고른다(Docs/카드그림.md).
#   python "Tools~/build_cardart.py"
# 읽기: 콘텐츠(C:\projects\bolzena-content · bolzena-content-v2 의 heroes) ·코어 샘플(bolzena-core/Data/Sample/heroes) · 사도 표(roster.json)
# 쓰기: Runtime/Resources/RunUI/cardart.json — 이름표만(그림은 copy_assets.py 가 시험 · 본 프로젝트 RunArt/Skill 로 복사, git 밖)
#
# 아이콘 다섯의 결(원작):
#   icon_admissionskill  — 기술 연출 한 장면(칼끝 · 빛 · 폭발) → 공격 카드
#   aside_skill_1        — 금빛 바탕의 사도/소품(버프 느낌) → 강화 카드
#   aside_skill_2 · _3   — 사도가 나오는 장면 그림 → 스킬 카드(남으면 공격)
#   icon_graduateskill   — 얼굴 클로즈업(고학년 표) → 마지막에만
# 순서: 대표(signature) 카드가 먼저 고르고 → 강화 → 공격 → 스킬. 한 사도 안에서 같은 아이콘은 한 번만.
import glob, json, os, re

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ICONS = r"C:\projects\볼제나\assets\skillicons"
SOURCES = [r"C:\projects\bolzena-content\heroes", r"C:\projects\bolzena-content-v2\heroes", r"C:\projects\bolzena-core\Data\Sample\heroes"]   # 뒤에 읽은 것이 이긴다(v2 = 본 게임 데이터)
ROSTERS = [r"C:\projects\bolzena-runui-test\Assets\Resources\RunUI\roster.json", r"C:\projects\bolzena-unity\Assets\Resources\RunUI\roster.json"]
OUT = os.path.join(ROOT, "Runtime", "Resources", "RunUI", "cardart.json")

have = set(f[:-4] for f in os.listdir(ICONS) if f.endswith(".png"))
roster = None
for p in ROSTERS:
    if os.path.exists(p):
        roster = json.load(open(p, encoding="utf-8"))["heroes"]
        break
norm = lambda s: (s or "").replace(" ", "")


def art_of(hero):
    n = norm(hero.get("name"))
    hit = next((h for h in roster if norm(h["ko"]) == n or norm(h["key"]) == n), None) \
        or next((h for h in roster if norm(h["key"]).startswith(n) or norm(h["ko"]).startswith(n)), None)
    return hit.get("art") if hit else None


PREF = {
    "공격": ["admission", "aside3", "aside2", "aside1", "grad"],
    "스킬": ["aside2", "aside3", "aside1", "admission", "grad"],
    "강화": ["aside1", "aside2", "aside3", "grad", "admission"],
}


def icon_name(slot, art):
    return {"admission": f"icon_admissionskill_{art}", "grad": f"icon_graduateskill_{art}",
            "aside1": f"aside_skill_{art}_1", "aside2": f"aside_skill_{art}_2", "aside3": f"aside_skill_{art}_3"}[slot]


cards, heroes = {}, {}
for src in SOURCES:
    for f in glob.glob(os.path.join(src, "**", "*.json"), recursive=True):
        d = json.load(open(f, encoding="utf-8"))
        if not isinstance(d, dict):
            continue
        for h in d.get("heroes", []):
            heroes[h["id"]] = h
        for c in d.get("cards", []):
            cards[c["id"]] = c

table, missing = {}, []
for hid, h in sorted(heroes.items()):
    art = art_of(h)
    uniq = [c for c in cards.values() if c.get("hero") == hid and c.get("unique")]
    if not uniq:
        continue
    if not art:
        missing.append(hid)
        continue
    order = sorted(uniq, key=lambda c: (0 if c.get("signature") else 1 if c.get("type") == "강화" else 2 if c.get("type") == "공격" else 3, c["id"]))
    used = set()
    for c in order:
        for slot in PREF.get(c.get("type"), PREF["스킬"]):
            name = icon_name(slot, art)
            if name in have and name not in used:
                used.add(name)
                table[c["id"]] = name
                break
        else:
            # 아이콘이 모자란 사도 — 이미 쓴 것 가운데 결이 맞는 것을 한 번 더
            for slot in PREF.get(c.get("type"), PREF["스킬"]):
                name = icon_name(slot, art)
                if name in have:
                    table[c["id"]] = name
                    break

# 고른 원작 그림(사도마다 대조 시트로 골랐다 — Tools~/cardpic_picks.json · cardpic_sheet.py) — 고유 · 생성 카드만, 시작 카드는 늘 스탠딩
#   "file" = 장면 · SD · 물건 그림 → "pics"(copy_assets.py 가 RunArt/CardPic 으로 굽는다) · "icon" = 내용이 맞는 원작 스킬 아이콘 → "cards" 를 덮는다
#   "keep" = 어울리는 그림이 없어 위 규칙의 스킬 아이콘 그대로
import sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import cardpic
pics = {}
for cid, p in cardpic.load().items():
    if p.get("file"):
        pics[cid] = cardpic.name_of(p)
    elif p.get("icon"):
        table[cid] = p["icon"]

out = {
    "_meta": {
        "what": "cards = 고유 카드 → 원작 스킬 아이콘 · pics = 고유 · 생성 카드 → 고른 원작 그림(RunArt/CardPic, 있으면 이것이 먼저). 시작 카드는 사도 스탠딩, 교주 · 상태 카드는 무늬 그대로 — 이 표에 없다.",
        "tool": "Tools~/build_cardart.py", "doc": "Docs/카드그림.md",
        "count": len(table), "no_art_heroes": missing, "pic_count": len(pics),
    },
    "cards": dict(sorted(table.items())),
    "pics": dict(sorted(pics.items())),
}
os.makedirs(os.path.dirname(OUT), exist_ok=True)
json.dump(out, open(OUT, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print("카드", len(table), "· 원작 그림", len(pics), "· 그림 없는 사도", missing)
