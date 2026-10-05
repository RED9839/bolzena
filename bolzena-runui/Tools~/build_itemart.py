# 장비 · 교주 카드 그림 표 — 콘텐츠 쪽이 정한 원작 그림 짝을 그대로 옮긴다(Docs/카드그림.md). 화면이 지어서 고르지 않는다.
#   python "Tools~/build_itemart.py"
# 읽기: C:\projects\bolzena-content-v2\_ref\그림짝.json — equip(아티팩트 86: 범용 64 + 애착 22) · neutral(교주 카드 = 스펠 카드 43)
#       id → 원작 아이콘(C:\projects\볼제나\assets\gear\ArtifactIcon_N.webp · assets\spell\SpellCardIcon_N.webp)
# 쓰기: Runtime/Resources/RunUI/itemart.json — {"equips": {id: 아이콘}, "cards": {id: 아이콘}} 이름만. 그림은 copy_assets.py 가 RunArt/Item 으로(git 밖)
# 짝에 없는 것(상태 · 저주 · 선물 카드, 코어 샘플의 장비 · 교주 카드)은 그림 없이 둔다 — 화면이 종류 무늬 · 칸 무늬로 떨어진다.
import json, os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PAIRS = r"C:\projects\bolzena-content-v2\_ref\그림짝.json"
WEB = r"C:\projects\볼제나\assets"
OUT = os.path.join(ROOT, "Runtime", "Resources", "RunUI", "itemart.json")

src = json.load(open(PAIRS, encoding="utf-8"))
miss = []


def keep(table, sub):
    out = {}
    for k, v in sorted(table.items()):
        if os.path.exists(os.path.join(WEB, sub, v + ".webp")):
            out[k] = v
        else:
            miss.append(f"{k}:{v}")
    return out


equips = keep(src.get("equip", {}), "gear")
cards = keep(src.get("neutral", {}), "spell")
json.dump({
    "_meta": {"what": "장비 · 교주 카드 → 원작 아이콘(콘텐츠 _ref/그림짝.json 을 그대로).", "tool": "Tools~/build_itemart.py",
              "source": PAIRS, "doc": "Docs/카드그림.md", "n_equips": len(equips), "n_cards": len(cards), "missing": miss},
    "equips": equips, "cards": cards,
}, open(OUT, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print("장비", len(equips), "· 교주 카드", len(cards), "· 그림 없음", miss)
