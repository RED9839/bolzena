# 엘다인 20명 카드 그림 점검 — 「지금 카드 종류 ↔ 붙은 그림 갈래」 표 (2026-10-11)
#   python cardart_audit.py [--md 파일]
# runui Tools~ 의 cardart_pool(재고 · resolved)을 그대로 쓴다. 규칙(사용자 2026-10-11):
#   공격 = 스탠딩 · 싸우는 그림 / 스킬 = SD 미니미 · 돕는 · 지키는 장면 / 강화 = 결의 · 변신(이름과 딱 맞는 그림 우선)
import json, os, re, sys

TOOLS = r"C:\projects\bolzena-runui\Tools~"
sys.path.insert(0, TOOLS)
import cardart_pool as P

ELDAIN = ["아야", "시온더다크불릿", "에피카", "비비", "클로에", "이드", "우이", "우이_기억", "리뉴아", "실비아", "비비_신성", "에르핀_왕도", "란",
          "디아나_왕년", "벨라", "죠안", "우로스", "티그_영웅", "네르_빡침", "요미"]

# 그림 갈래 → 큰 갈래
def group(key):
    if key is None:
        return "없음", "-"
    if key.startswith("@stand/"):
        return "스탠딩", key.split("/")[2]
    if key.startswith("pic:"):
        return "?", key
    c = P.cat_of(key)
    if c in ("schedule",):
        return "SD", c
    if c == "icon":
        return "SD", "초상"
    if c == "btn":
        return "싸우는SD", c
    if c in ("story", "cg", "hilite", "rogue", "theme"):
        return "장면", c
    return "아이콘", c


def verdict(typ, g, sub, picks_row):
    if typ == "공격" and g == "SD":
        return "어긋남(공격인데 미니미)"
    if typ == "스킬" and g == "스탠딩":
        return "어긋남(스킬인데 스탠딩)"
    if typ == "스킬" and g == "싸우는SD":
        return "어긋남(스킬인데 싸우는 그림)"
    return ""


def stale(c, row):
    """짝 표 줄의 why 에 적힌 카드 이름이 지금 이름과 다르면 — 카드 내용이 바뀐 옛 짝."""
    why = (row or {}).get("why", "")
    names = re.findall(r"「([^」]+)」", why)
    return bool(names) and c.get("name") not in names


H = P.heroes()
cardart = json.load(open(P.CARDART, encoding="utf-8"))
picks = P.load_picks()["cards"]
rows, bad = [], 0
for hid in ELDAIN:
    h = H.get(hid)
    if not h:
        continue
    res = P.resolved(hid, h, cardart, picks)
    for cid in h["basic"] + h["unique"] + h["token"]:
        c = h["cards"][cid]
        k = res[cid]
        g, sub = group(k)
        row = picks.get(cid)
        v = verdict(c.get("type"), g, sub, row)
        st = "옛 짝(이름 다름)" if stale(c, row) else ""
        if v or st:
            bad += 1
        rows.append((hid, cid, c.get("type"), c.get("name"), g, sub, (row or {}).get("why", "")[:40], v, st))

md = "--md" in sys.argv
if md:
    out = sys.argv[sys.argv.index("--md") + 1]
    with open(out, "w", encoding="utf-8") as f:
        f.write("| 사도 | 카드 | 종류 | 이름 | 그림 갈래 | 세부 | 짝 표 why | 판정 | 옛 짝 |\n|---|---|---|---|---|---|---|---|---|\n")
        for r in rows:
            f.write("| " + " | ".join(str(x) for x in r) + " |\n")
for r in rows:
    if r[7] or r[8] or "--all" in sys.argv:
        print(" · ".join(str(x) for x in r))
print(f"카드 {len(rows)} · 걸린 것 {bad}")
