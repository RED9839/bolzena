# 사도별 원작 그림 재고 표 — bolzena-content-v2/_measure/카드그림_재고.md 를 쓴다. Docs/카드그림.md
#   python "Tools~/cardart_stock.py"
# 이미 뽑아 둔 그림만 센다(cardart_pool.stock). 카드에 쓰는 것 = 스킨 · 배경을 뺀 것. 스탠딩 자리 셋(상반신 · 얼굴 확대 · 전신)은 따로 센다.
import collections, json, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import cardart_pool as P

OUT = r"C:\projects\bolzena-content-v2\_measure\카드그림_재고.md"
COLS = [("hilite", "하이라이트"), ("theme", "테마 등장"), ("evicon", "이벤트 아이콘"), ("adm", "저학년"), ("grad", "고학년"), ("aside1", "어사이드1"), ("aside2", "어사이드2"), ("aside3", "어사이드3"),
        ("story", "액자"), ("cg", "만화 컷"), ("schedule", "SD 일정"), ("btn", "싸우는 SD"), ("prop", "애장품"), ("present", "선물"),
        ("head", "얼굴"), ("icon", "SD 초상"), ("clone", "클론판")]
H = P.heroes()
rows, short, tot = [], [], collections.Counter()
skin_tot = 0
for hid in sorted(H):
    h = H[hid]
    st = P.stock(h["art"])
    ok = [it for it in st if it["ok"] and it["kind"] != "stand"]
    ids = collections.defaultdict(set)
    for it in ok:
        ids[P.cat_of(it["key"])].add(P.ident(it["key"]))
    skins = sum(1 for it in st if not it["ok"] and "스킨" in it.get("why", ""))
    skin_tot += skins
    n_orig = len({P.ident(it["key"]) for it in ok})
    need = len(h["basic"]) + len(h["unique"]) + len(h["token"])
    for c, _ in COLS:
        tot[c] += len(ids[c])
    rows.append((hid, h["art"], [len(ids[c]) for c, _ in COLS], n_orig, skins, len(h["basic"]) + len(h["unique"]), len(h["token"])))
    # 원작 그림(스탠딩 자르기 빼고)만으로 카드 · 생성 카드를 다 못 채우는 사도. 저학년 아이콘은 저학년 칸 하나에만 쓴다.
    if n_orig - 1 < need - 1:
        short.append((hid, n_orig, need))

L = []
L.append("# 카드 그림 재고 — 사도별 원작 그림 (2026-10-08)\n")
L.append("> 도구 `bolzena-runui/Tools~/cardart_stock.py` 가 쓴다(다시 돌리면 갱신). 이미 뽑아 둔 그림만 셌다 — `볼제나/assets/cardart_src/<그림 키>`(사도 이름이 박힌 원작 그림) · `assets/skillicons` · `.shots/standing-webp`(스탠딩 Normal · Idle_1 한 장) · `bolzena-unity-tmp/mumu_pull/png/raw`(이미 풀어 둔 원작 PNG — 2026-10-09 추가: 로비 하이라이트 일러스트 · 테마 이벤트 클리어 CG · 테마 등장 그림 · 이벤트 스킬 아이콘, 파일 이름에 사도 원작 이름이 정확히 박힌 것만). 새로 뽑거나 풀지 않았다. 쓰지 않는 그림(다른 인물이 가운데인 인연 아이콘 · 흐린 아이콘 · 바랜 하이라이트)은 `Tools~/cardart_avoid.json`.\n")
L.append("## 한국 서버 기준\n")
L.append("- 뽑은 곳은 한국판 클라이언트(`com.epidgames.trickcalrevive` — `assets/available.json`). 이름 · 설정은 한국판 데이터(bolzena-content-v2)를 따랐다.")
L.append("- **스킨 옷 그림은 「확인 못 함」** — `icon__<키>SkinN`(스킨 초상) · `profile__…SkinN` · `btn__…skinN` · `skin__…_Standing`(스킨 방 배경, 사도 없음). 어느 서버에 나온 스킨인지(콜라보 · 서버 한정) 파일 이름 · 메타로 가릴 수 없어 카드에 **쓰지 않았다**. 합 %d장." % skin_tot)
L.append("- 배경(`bg__` 픽업 · 도전 배경)은 사도가 없어 쓰지 않았다. 스탠딩 덧스킨은 규칙대로 Normal 만(스탠딩 렌더가 Normal · Idle_1).")
L.append("- 뽑아 둔 것 가운데 **없는 갈래**: 이모티콘 · 스티커, 교류(인연) CG 묶음, 고학년 컷인 정지 그림(전투 컷인은 스탠딩 스파인을 그대로 튼다), SD 표정 · 다른 포즈 렌더, 스탠딩 다른 표정 · 포즈 렌더(스파인은 있으나 정지 그림으로 구운 것이 Idle_1 한 장뿐). 새로 뽑거나 굽지 않는 조건이라 비워 둔다.\n")
L.append("## 갈래\n")
L.append("| 갈래 | 원작 | 쓰는 카드 |\n|---|---|---|")
L.append("| 저학년 · 고학년 · 어사이드 1~3 | 스킬 아이콘 128px | 저학년 = 저학년 칸(cardart_low.json) 하나만 · 그 밖 고유 · 생성 카드 |")
L.append("| 액자(story) · 만화 컷(cg — 일정 CG · 테마 이벤트) · SD 일정(schedule) | 장면 그림 1000×1400 · 1024×512 | 고유 스킬 · 공격, 남으면 기본 카드 |")
L.append("| 싸우는 SD(btn — 67명만) | 로비 전투 단추 SD | 공격(기본 공격 먼저) |")
L.append("| 애장품(aside — 강화판은 같은 그림으로 센다) · 선물(present) | 물건 그림 | 강화 · 물건 이름 카드 · 생성 카드 |")
L.append("| 얼굴(head) · SD 초상(icon) · 클론판(clone) | 대화창 얼굴 · 초상 · 적판 초상 | 남는 칸 |")
L.append("| 스탠딩 자리 셋 | 스탠딩 한 장을 상반신 · 얼굴 확대 · 전신(성격 바탕)으로 | 기본 카드(방어 · 회복 = 얼굴 확대) · 모자랄 때 |\n")
L.append("## 합계(135명)\n")
L.append("| " + " | ".join(n for _, n in COLS) + " |")
L.append("|" + "---|" * len(COLS))
L.append("| " + " | ".join(str(tot[c]) for c, _ in COLS) + " |\n")
L.append(f"사도마다 원작 그림(스탠딩 자르기 빼고) 최소 {min(r[3] for r in rows)} · 중간 {sorted(r[3] for r in rows)[len(rows)//2]} · 최대 {max(r[3] for r in rows)}종 + 스탠딩 자리 3. 카드는 사도마다 7~8종 + 생성 카드 0~{max(r[6] for r in rows)}장.\n")
L.append("## 모자란 사도\n")
if short:
    L.append("원작 그림만으로는 카드(기본 + 고유 + 생성)를 다 못 채워 스탠딩 자리(얼굴 확대 · 전신)에 기대는 사도:\n")
    L.append("| 사도 | 원작 그림 | 카드 + 생성 |\n|---|---|---|")
    for hid, n, need in short:
        L.append(f"| {hid} | {n} | {need} |")
else:
    L.append("없음.")
L.append(f"\n스탠딩 자리 셋까지 넣으면 135명 모두 카드 수보다 그림이 많다.\n")
L.append("## 사도별\n")
L.append("| 사도 | 그림 키 | " + " | ".join(n for _, n in COLS) + " | 원작 계 | 스킨(안 씀) | 카드 | 생성 |")
L.append("|" + "---|" * (len(COLS) + 6))
for hid, art, cnt, n, skins, nc, nt in rows:
    L.append(f"| {hid} | {art} | " + " | ".join(str(x) if x else "·" for x in cnt) + f" | {n} | {skins} | {nc} | {nt} |")
# 지금 짝 결과(cardart_check.py 마지막 줄)
import subprocess
r = subprocess.run([sys.executable, os.path.join(HERE, "cardart_check.py")], capture_output=True, text=True, encoding="utf-8", env={**os.environ, "PYTHONIOENCODING": "utf-8"})
L.append("\n## 지금 짝 결과(cardart_fill.py → cardart_check.py)\n")
L.append("```\n" + "\n".join(r.stdout.strip().splitlines()[-3:]) + "\n```")
open(OUT, "w", encoding="utf-8").write("\n".join(L) + "\n")
print("재고 표", OUT, "· 모자란 사도", len(short))
