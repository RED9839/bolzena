# 사도 카드 그림 검사 — 사도마다 「카드(기본 + 고유)의 그림이 모두 다른가 · 그림 없는 카드 0」을 센다. Docs/카드그림.md
#   python "Tools~/cardart_check.py" [--all]     (--all = 모든 사도 줄을 찍는다 · 기본은 걸린 사도만)
# 읽기: Runtime/Resources/RunUI/cardart.json(화면이 읽는 표) · Tools~/cardpic_picks.json · bolzena-content-v2/heroes · 본 프로젝트 Resources/RunArt(구운 그림이 있나)
# 예외: 기본 카드끼리는 같은 그림이어도 된다(사용자 결정 2026-10-09). 기본 · 고유가 같은 그림이면 걸린다.
# 같은 그림: 뒤집기 · 틴트(__f · __t)와 애장품 강화판(_LimitLevelBreak)은 같은 그림으로 센다. 스탠딩은 자리(상반신 · 얼굴 · 전신)가 다르면 다른 그림.
# 그림 없음: 짝이 없거나(고유 카드) 구운 파일이 Resources 에 없다.
import collections, json, os, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import cardart_pool as P

ALL = "--all" in sys.argv
H = P.heroes()
cardart = json.load(open(P.CARDART, encoding="utf-8"))
picks = P.load_picks()["cards"]
pics = cardart.get("pics", {})

ok = 0
bad_dup, bad_miss, tok_issue = [], [], []
lines = []
for hid in sorted(H):
    h = H[hid]
    res = P.resolved(hid, h, cardart, picks)
    main = h["basic"] + h["unique"]
    miss = [cid for cid in main + h["token"] if not P.baked(res[cid], pics.get(cid))]
    ids = collections.defaultdict(list)
    for cid in main:
        ids[P.ident(res[cid])].append(cid)
    # 기본 카드끼리는 같은 그림이어도 된다(2026-10-09 사용자 결정 — 기본 공격 = 스탠딩 일러 · 기본 스킬 = SD 꼬마 전신, 135명 통일)
    dups = [v for k, v in ids.items() if len(v) > 1 and not all(c in h["basic"] for c in v)]
    # 생성 카드 — 서로 · 사도 카드와 겹치나
    allids = collections.defaultdict(list)
    for cid in main + h["token"]:
        allids[P.ident(res[cid])].append(cid)
    tdups = [v for k, v in allids.items() if len(v) > 1 and any(c in h["token"] for c in v)]
    good = not dups and not [c for c in main if c in miss]
    ok += good
    if not good:
        (bad_dup if dups else bad_miss).append(hid)
    if tdups or [c for c in h["token"] if c in miss]:
        tok_issue.append(hid)
    if ALL or not good or tdups:
        lines.append(f"{'OK ' if good else 'XX '}{hid}\t카드 {len(main)}장 · 그림 {len(ids)}종" + (f"\t겹침 {dups}" if dups else "") + (f"\t없음 {miss}" if miss else "") + (f"\t생성 카드 겹침 {tdups}" if tdups else ""))
        if ALL:
            for cid in main + h["token"]:
                lines.append(f"     {cid}\t{h['cards'][cid].get('type')}\t{h['cards'][cid].get('name')}\t{res[cid]}")

print("\n".join(lines))
print(f"\n사도 {len(H)}명 · 카드 모두 다른 그림 + 그림 없는 카드 0 = {ok}명 / {len(H)}")
print(f"  겹침 남은 사도 {len(bad_dup)} · 그림 없는 카드가 있는 사도 {len(bad_miss)} · 생성 카드 겹침/없음 사도 {len(tok_issue)} {tok_issue[:20]}")
sys.exit(0 if ok == len(H) else 1)
