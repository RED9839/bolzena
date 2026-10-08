# 카드 그림 의미 점검표 — bolzena-content-v2/_measure/카드그림_점검.md 를 쓴다(2026-10-09). Docs/카드그림.md
#   python "Tools~/cardart_review.py"
# 사도 135명 × 카드(기본 · 고유 · 생성)마다: 그림 · 갈래 · 판정(맞음 / 애매 / 틀림) · 이유. 함께 기계 검사도 한다:
#   저학년 칸(cardart_low.json 의 카드 = 저학년 아이콘 · 다른 카드는 저학년 아이콘 아님) · 다른 사도 그림이 섞였나(그림 키 폴더 ≠ 사도 그림 키) ·
#   스킨 그림(출시 서버 확인 못 함)이 쓰였나 · 그림 파일이 실제로 있나(원본 + 구운 Resources).
# 판정: Tools~/cardart_verdict.json(눈으로 본 것)이 먼저. 없으면 규칙 —
#   저학년 칸 = 맞음 · 기본 카드의 스탠딩 자리(상반신 · 얼굴 확대 · 전신)와 싸우는 SD = 맞음(카드마다 다르게 자른 사도 그림) ·
#   손으로 고른 짝(대조 시트 — why 가 있는 줄) = 맞음 · 자동 채움(fill · auto)과 옛 규칙 아이콘 = 애매(그 사도 그림 가운데 갈래로 고른 것).
import collections, json, os, re, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import cardart_pool as P
import cardpic

OUT = r"C:\projects\bolzena-content-v2\_measure\카드그림_점검.md"
V = json.load(open(os.path.join(HERE, "cardart_verdict.json"), encoding="utf-8"))
VER, FIXED = V["verdict"], V["fixed"]
H = P.heroes()
cardart = json.load(open(P.CARDART, encoding="utf-8"))
picks = P.load_picks()["cards"]
pics = cardart.get("pics", {})
WHY = {"hilite": "로비 하이라이트", "theme": "테마 등장 그림", "evicon": "이벤트 아이콘", "btn": "싸우는 SD", "cg": "만화 컷", "story": "액자", "schedule": "SD 일정", "present": "선물", "prop": "애장품", "head": "얼굴",
       "icon": "SD 초상", "clone": "클론판", "grad": "고학년 아이콘", "adm": "저학년 아이콘", "aside1": "어사이드1", "aside2": "어사이드2", "aside3": "어사이드3",
       "stand_card": "스탠딩 상반신", "stand_face": "스탠딩 얼굴", "stand_full": "스탠딩 전신"}


def src_exists(key, h):
    if key is None:
        return False
    if key.startswith("@stand/"):
        return bool(h["webp"]) and os.path.exists(os.path.join(P.STAND, h["webp"] + ".webp"))
    return os.path.exists(P.src_path(key))


rows, cnt, issues = [], collections.Counter(), []
for hid in sorted(H):
    h = H[hid]
    art = h["art"]
    res = P.resolved(hid, h, cardart, picks)
    for cid in h["basic"] + h["unique"] + h["token"]:
        c = h["cards"][cid]
        key = res[cid]
        cat = P.cat_of(key) if key else "-"
        p = picks.get(cid, {})
        probs = []
        # 저학년 칸
        adm = f"icon_admissionskill_{art}"
        if cid in P.LOW and key != adm:
            probs.append("저학년 칸인데 저학년 아이콘이 아님")
        if cid not in P.LOW and key == adm and c.get("unique"):
            probs.append("저학년 칸이 아닌 고유 카드에 저학년 아이콘")
        # 다른 사도 그림
        owner = (art if any(it["key"] == key for it in P.raw_items(art)) else "다른 이름") if key and key.startswith("@raw/") else             key.split("/")[1] if key and key.startswith("@stand/") else key.split("/")[0] if key and "/" in key else \
            (re.match(r"(?:icon_admissionskill_|icon_graduateskill_|aside_skill_)([a-z0-9]+)", key or "") or [None, None])[1]
        if owner and owner != art:
            probs.append(f"다른 사도 그림({owner})")
        if key and "/" in key and not key.startswith("@") and (re.search(r"skin\d", key.split("__", 1)[-1], re.I) or "/skin__" in key or "/profile__" in key):
            probs.append("스킨 그림(출시 서버 확인 못 함)")
        if key in P.AVOID and cid not in P.LOW:
            probs.append("쓰지 않기로 한 그림(" + P.AVOID[key] + ")")
        if not src_exists(key, h):
            probs.append("원본 그림 없음")
        if not P.baked(key, pics.get(cid)):
            probs.append("구운 그림 없음(copy_assets.py)")
        if cid in VER and (len(VER[cid]) < 3 or VER[cid][2] == key):
            verdict, why = VER[cid][:2]
            why = why or p.get("why", "")
        elif cid in P.LOW:
            verdict, why = "맞음", "원작 저학년 스킬 칸 — 저학년 아이콘"
        elif cid in h["basic"] and cat in ("stand_card", "stand_face", "stand_full", "btn", "schedule", "hilite", "story", "head", "clone", "icon"):
            verdict, why = "맞음", ("기본 공격 — " if c.get("type") == "공격" else "기본 방어 · 회복 — ") + WHY.get(cat, cat)
        elif (p.get("file") or p.get("icon")) and not p.get("fill") and not p.get("auto"):
            verdict, why = "맞음", p.get("why", "대조 시트로 고른 짝")
        else:
            verdict, why = "애매", (p.get("why") or f"{WHY.get(cat, cat)} — 이 사도 그림 가운데 갈래로 고른 것").split(" — ")[0] + " — 더 맞는 그림이 이 사도 재고에 없음"
        if probs:
            verdict = "틀림"
            issues.append(f"{cid}: {', '.join(probs)}")
        cnt[verdict] += 1
        name = (key or "-").split("/")[-1]
        fixed = " (점검에서 고침)" if cid in FIXED else ""
        rows.append(f"| {hid} | {cid} | {c.get('type')} | {c.get('name')} | {name} | {WHY.get(cat, cat)} | {verdict} | {why}{fixed}{' · ' + '; '.join(probs) if probs else ''} |")

L = ["# 카드 그림 의미 점검 (2026-10-09)\n",
     "> 도구 `bolzena-runui/Tools~/cardart_review.py` 가 쓴다(다시 돌리면 갱신). 지금 heroes 데이터(bolzena-content-v2 — 118명 재설계 반영) 기준. 눈 점검 판정은 `Tools~/cardart_verdict.json`, 고친 짝은 `Tools~/cardpic_picks.json`(\"checked\": true 줄).\n",
     "## 기준\n",
     "- **맞음**: 그림이 카드 이름 · 효과 · 종류와 맞다(같은 소품 · 행동 · 원작 스킬 장면). 기본 카드는 같은 사도 그림을 카드마다 다른 자리(상반신 · 얼굴 확대 · 전신)로 잘랐거나 싸우는 SD.",
     "- **애매**: 딱 맞는 그림이 이 사도의 뽑아 둔 재고에 없어 그나마 가까운 것(같은 사도 · 비슷한 물건 · 감정)을 골랐다. 비워 두지 않았다.",
     "- **틀림**: 내용이 어긋나거나(회복에 공격 컷 등) 기계 검사(저학년 칸 · 다른 사도 그림 · 스킨 · 파일 없음)에 걸린 것. 점검에서 찾은 틀림 %d장은 고쳤다(표의 「점검에서 고침」).\n" % len(FIXED),
     "## 합계\n",
     f"- 카드 {sum(cnt.values())}장 — 맞음 {cnt['맞음']} · 애매 {cnt['애매']} · **틀림 {cnt['틀림']}**",
     f"- 기계 검사 걸림: {len(issues)}" + ("" if not issues else "\n" + "\n".join("  - " + x for x in issues[:60])),
     "\n## 카드별\n",
     "| 사도 | 카드 | 종류 | 이름 | 그림 | 갈래 | 판정 | 이유 |", "|---|---|---|---|---|---|---|---|"] + rows
open(OUT, "w", encoding="utf-8").write("\n".join(L) + "\n")
print("점검표", OUT, dict(cnt), "기계 검사 걸림", len(issues))
for x in issues[:20]:
    print("  ", x)
