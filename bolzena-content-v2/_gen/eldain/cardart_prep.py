# 엘다인 리뉴얼(2026-10-11) 뒤 카드 그림 짝 정리 — runui Tools~ 의 짝 표 · 저학년 칸을 새 카드에 맞춘다.
#   python cardart_prep.py   → 그 뒤 runui 「Tools~/cardart_fill.py」(채우기 · 표) → 굽기(copy_assets)
# 1) 저학년 칸(cardart_low.json): 옛 카드 id → 지금 그 이름(원작 저학년)의 카드 id 로 옮긴다(이름이 바뀐 것은 손 짝).
# 2) 짝 표(cardpic_picks.json): 엘다인 고유 · 생성 카드의 옛 손 짝을 카드 이름으로 옮긴다(이름이 같은 카드가 지금 다른 id 면 그리로).
#    지금 없는 이름의 짝은 지운다 — 채우기가 지금 종류 규칙으로 다시 고른다(종류에 안 맞는 그림은 채우기가 다시 고른다).
import glob, json, os

TOOLS = r"C:\projects\bolzena-runui\Tools~"
OLD = os.environ.get("BZ_ART_OLD", r"C:\projects\_backup\eldain_v1_20261011\heroes")   # 4차: BZ_ART_OLD=C:\projects\_backup\eldain_v3_20261011\heroes
NEW = r"C:\projects\bolzena-content-v2\heroes"
LOG = os.path.join(os.path.dirname(os.path.abspath(__file__)), "measure", os.environ.get("BZ_ART_LOG", "cardart_prep.log"))


def cards(root):
    out, heroes = {}, set()
    for f in glob.glob(os.path.join(root, "*", "*.json")):
        d = json.load(open(f, encoding="utf-8"))
        for h in d["heroes"]:
            heroes.add(h["id"])
        for c in d["cards"]:
            out[c["id"]] = c
    return out, heroes


old, _ = cards(OLD)
new, eldain = cards(NEW)
log = []
# 1) 저학년 칸
lp = os.path.join(TOOLS, "cardart_low.json")
low = json.load(open(lp, encoding="utf-8"))
RENAME = {"찢기": "찢는다", "순환의 고리": "순환"}   # 원작 저학년 이름 → 지금 카드 이름(리뉴얼에서 줄인 것)
byname = {}
for cid, c in new.items():
    if c.get("hero") in eldain and (c.get("unique") or c.get("token")):
        byname[(c["hero"], c["name"])] = cid
moved = {}
for cid, name in list(low["cards"].items()):
    hero = next((h for h in eldain if cid.startswith(h + "_")), None)
    if hero is None or (cid.startswith("이드_재활")):
        continue
    nid = byname.get((hero, RENAME.get(name, name)))
    del low["cards"][cid]
    if nid:
        moved[nid] = RENAME.get(name, name)
        log.append(f"저학년 {cid} 「{name}」 → {nid}")
    else:
        log.append(f"저학년 {cid} 「{name}」 — 지금 그 이름의 카드 없음(뺌)")
low["cards"].update(moved)
low["cards"] = dict(sorted(low["cards"].items()))
json.dump(low, open(lp, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
# 2) 짝 표
pp = os.path.join(TOOLS, "cardpic_picks.json")
data = json.load(open(pp, encoding="utf-8"))
picks = data["cards"]
drop = moved_n = 0
# 옛 짝을 이름으로 옮긴다 — 옛 카드 이름이 지금 다른 id 에 있으면 그 카드로(같은 사도 · 같은 이름이라 고른 까닭이 그대로 맞는다)
oldpicks = {cid: picks[cid] for cid in list(picks) if cid in old and old[cid].get("hero") in eldain and (old[cid].get("unique") or old[cid].get("token"))}
for cid in oldpicks:
    del picks[cid]
for cid, row in oldpicks.items():
    o = old[cid]
    nid = byname.get((o["hero"], o["name"]))
    if nid and nid not in low["cards"] and nid not in picks:
        picks[nid] = row
        if nid != cid:
            moved_n += 1
            log.append(f"짝 옮김 {cid} → {nid} 「{o['name']}」 · {row.get('why', '')}")
    else:
        drop += 1
        log.append(f"짝 지움 {cid} 옛 「{o['name']}」({o['type']}) — 지금 「{(new.get(cid) or {}).get('name')}」 · {row.get('why', '')}")
json.dump(data, open(pp, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
open(LOG, "w", encoding="utf-8").write("\n".join(log) + f"\n저학년 {len(moved)} · 짝 지움 {drop}\n")
print(f"저학년 {len(moved)} · 짝 옮김 {moved_n} · 짝 지움 {drop} → {LOG}")
