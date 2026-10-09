# 볼제나 원작 그림 가운데 판 화면이 쓰는 것만 시험 프로젝트로 복사한다(원본은 읽기만 · 복사본은 git 에 넣지 않는다).
#   python "Tools~/copy_assets.py" [유니티 프로젝트 경로]
# 배경 · 사도 초상 · 성격/역할/줄/종족 아이콘 · 골드 · 미니미 스파인 · 스탠딩 스파인(로비 · 상점 · 이벤트 NPC)
# · 사도 스탠딩 렌더(상세 · 목록 · 시작 카드) · 고유 카드 그림 아이콘(cardart.json 에 적힌 것만)
import os, sys, shutil, json
import numpy as np
from PIL import Image

SRC = r"C:\projects\볼제나\assets"
PROJ = sys.argv[1] if len(sys.argv) > 1 else r"C:\projects\bolzena-runui-test"
DST = os.path.join(PROJ, "Assets", "Resources")


def cp(src, dst, size=None):
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    if os.path.exists(dst):
        return
    if size:
        im = Image.open(src)
        im.thumbnail(size, Image.LANCZOS)
        im.save(dst, quality=90) if dst.endswith(".jpg") else im.save(dst)
    else:
        shutil.copyfile(src, dst)


# 배경 — 로비(stage1_1) · 마을 층 배경(코어 villages.json 의 bg) · 캠프/상점/이벤트
BG = {"stage1_1", "stage2_1", "stage3_1", "stage3_2", "stage3_3", "stage16_1", "stage23_1", "stage25_1", "stage6_1", "globalstage1"}
try:
    for v in json.load(open(r"C:\projects\bolzena-core\Data\Sample\villages.json", encoding="utf-8")):
        for f in v["floors"]:
            BG.update((f.get("bg") or {}).values())
except Exception as e:
    print("villages.json 못 읽음", e)
# 콘텐츠 폴더(bolzena-content/world/villages/*.json — { villages: [...] })의 층 배경도
import glob
for vf in glob.glob(r"C:\projects\bolzena-content\world\villages\*.json") + glob.glob(r"C:\projects\bolzena-content-v2\world\villages\*.json"):
    if os.path.basename(vf).startswith("_"):
        continue
    try:
        for v in json.load(open(vf, encoding="utf-8")).get("villages", []):
            for f in v.get("floors", []):
                BG.update((f.get("bg") or {}).values())
    except Exception as e:
        print(vf, "못 읽음", e)
for b in sorted(BG):
    p = f"{SRC}/bg/{b}.jpg"
    if os.path.exists(p):
        cp(p, f"{DST}/RunArt/Bg/{b}.jpg", (1920, 1080))

# 사도 초상 — 135명(skin 빼고 기본 그림만)
roster = json.load(open(os.path.join(DST, "RunUI", "roster.json"), encoding="utf-8"))["heroes"]
n = 0
for h in roster:
    p = f"{SRC}/heroicons/{h['art']}.png"
    if h.get("art") and os.path.exists(p):
        cp(p, f"{DST}/RunArt/Heroes/{h['art']}.png", (192, 192))
        n += 1
print("초상", n)

# 작은 아이콘(성격 · 역할 · 줄 · 종족 · 별) · 골드
for f in os.listdir(f"{SRC}/uiicons"):
    if f.endswith(".png"):
        cp(f"{SRC}/uiicons/{f}", f"{DST}/RunArt/Icons/{f}")
cp(f"{SRC}/currency/CurrencyIcon_0008.png", f"{DST}/RunArt/Icons/gold.png")
# 크레파스 4등급(하급 · 중급 · 상급 · 최상급) — 교주 능력치 보드(Flow.Crayon.cs PastelIcon)
for i in range(1, 5):
    cp(f"{SRC}/currency/Item_Crayon{i}.png", f"{DST}/RunArt/Item/Item_Crayon{i}.png")
# 원작 상태 아이콘(iconsrc/stateicons/StateIcon_N, 64) — 전투 상태 칩이 뜻이 확실한 것만 쓴다(Docs/상태칩.md · bolzena-unity ChipRow.Original)
for f in os.listdir(f"{SRC}/iconsrc/stateicons"):
    if f.startswith("StateIcon_") and f[10:-4].isdigit():
        cp(f"{SRC}/iconsrc/stateicons/{f}", f"{DST}/RunArt/State/{f}")
# 성격 아이콘은 512 판으로 덮는다(Tools~/nature512.py — 128 보석 + 원작 512 글리프, 4K 에서 선명하게)
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import nature512
for ko in nature512.NAT:
    dst = f"{DST}/RunArt/Icons/성격_{ko}.png"
    if Image.open(dst).size[0] < 512:
        nature512.make(ko, dst)
        print("성격 512", ko)


# 스파인 — .skel → .skel.bytes, .atlas → .atlas.txt. 그림은 PMA → 곧은 알파(Linear 색 공간에서 바래지 않게 — bolzena-unity 와 같은 규칙)
def spine(src_dir, name):
    dd = f"{DST}/Spine/{name}"
    if os.path.isdir(dd):
        return
    os.makedirs(dd, exist_ok=True)
    for f in os.listdir(src_dir):
        s = os.path.join(src_dir, f)
        if f.endswith(".skel"):
            shutil.copyfile(s, f"{dd}/{f}.bytes")
        elif f.endswith(".atlas"):
            shutil.copyfile(s, f"{dd}/{f}.txt")
        elif f.endswith(".png"):
            # PMA → 곧은 알파 + 번짐 채움(Tools~/spine_straight.py) — 그냥 나누면 낮은 알파 가장자리가 원색 점 · 흰 테두리로 번진다
            import spine_straight
            spine_straight.convert(s, f"{dd}/{f}")
    print("스파인", name)


# 미니미(spine/minimi)는 더 쓰지 않는다(2026-10-06 — 장면은 SD 전투 스파인 SceneHero)
# 스탠딩 — 로비 메인 사도(에르핀) · 상점 주인(골디) · 코어 샘플 이벤트의 NPC
for key in ["erpin", "goldy", "sist", "alice", "jubee", "noone"]:   # noone = 겨우살이(특별 이벤트 「겨우살이가 걸린 나무」 — event_style.json)
    p = f"{SRC}/spine/standing/{key}"
    if not os.path.isdir(p):
        p = f"{SRC}/standing/{key}"
    if os.path.isdir(p):
        spine(p, "st_" + key)
# 코어 샘플의 판에 데려가는 사도(편성 큰 카드 · 사도 상세 · 목록 · 주인 고르기가 스파인으로 움직인다) — 원본 폴더는 한글 이름, 그림 키는 roster.json 의 art
for folder, art in [("리코타", "ricota"), ("캬롯", "kyarot"), ("시온더다크불릿", "xxionx")]:
    p = f"{SRC}/spine/standing/{folder}"
    if os.path.isdir(p):
        spine(p, "st_" + art)
# 이벤트 대상 · NPC(콘텐츠 events 의 npc · event_style 의 target) 스탠딩 — 원본 폴더 한글 이름 → 그림 키(roster.json art). SD 는 위 Spine/<한글 키> 로 이미 있다
for folder, art in [("가비아", "gabia"), ("레이지", "lazy"), ("림", "rim"), ("마고", "mago"), ("멜루나", "meluna"), ("모모", "momo"), ("베니", "beni"),
                    ("셰럼", "sherum"), ("스피키", "speaki"), ("실라", "sylla"), ("아네트", "arnet"), ("알레트", "allet"), ("에슈르", "ashur"), ("오르", "orr"),
                    ("유미미", "yumimi"), ("이프리트", "ifrit"), ("타이다", "taida"), ("티그", "tig"), ("폴랑", "polan"), ("프리클", "fricle"), ("힐데", "hilde"), ("셰이디", "shady")]:
    p = f"{SRC}/spine/standing/{folder}"
    if os.path.isdir(p):
        spine(p, "st_" + art)
# 스탠딩 렌더(웹판 .shots/standing-webp/<이름>.webp — 스파인 Idle_1 · Normal 한 장) → RunArt/Standing/<그림 키>.png
#   사도 상세 · 편성 큰 카드 · 사도 목록 · 시작 카드 그림이 쓴다. 투명 가장자리를 잘라(위 = 머리 끝) 긴 변 1024 까지 줄이고,
#   2의 거듭제곱 판의 왼쪽 위에 붙인다(크런치 압축이 먹게 — 남는 자리는 투명).
#   _meta.json — 그림 키 → [머리 가운데 x(0~1, 그림 안), 그림 너비, 그림 높이(픽셀), 머리 끝 y(0~1)] — 빗나간 사도는 standing_fix.json 이 이긴다 : 화면이 그림 자리 · 상반신 자르기에 쓴다(Runtime/Data/CardArt.cs)
STAND = r"C:\projects\볼제나\.shots\standing-webp"
meta_path = f"{DST}/RunArt/Standing/_meta.json"
meta = json.load(open(meta_path, encoding="utf-8")) if os.path.exists(meta_path) else {}
pow2 = lambda n: 1 << (max(1, n) - 1).bit_length()


def head(im):
    # 머리 자리 — (가운데 x, 머리 끝 y) 둘 다 0~1. 줄마다 가장 긴 불투명 구간을 잰다:
    #   머리 끝 = 위 6할 안에서 가장 넓은 줄의 38% 가 넘는 첫 줄(왕관 · 귀 · 날개 · 반짝이 같은 가는 장식은 건너뛴다)
    #   가운데 = 머리 끝부터 높이 22% 띠에서 넓은 줄 절반의 가운데 중앙값
    a = np.asarray(im.getchannel("A")) > 128
    h, w = a.shape
    L = np.zeros(h); C = np.zeros(h)
    for y in range(h):
        r = a[y]
        if not r.any():
            continue
        d = np.diff(np.concatenate([[0], r.astype(np.int8), [0]]))
        st = np.nonzero(d == 1)[0]; en = np.nonzero(d == -1)[0]
        i = int(np.argmax(en - st)); L[y] = en[i] - st[i]; C[y] = (st[i] + en[i]) / 2
    up = L[: int(h * 0.6)]
    th = 0.38 * up.max() if up.max() > 0 else 1
    top = int(np.argmax(L >= th))
    rows = sorted(((L[y], C[y]) for y in range(top, min(h, top + int(h * 0.22))) if L[y] > 0), reverse=True)
    rows = rows[: max(1, len(rows) // 2)]
    cx = float(np.median([c for _, c in rows]) / w) if rows else 0.5
    return cx, top / h


FIX = json.load(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "standing_fix.json"), encoding="utf-8"))


def head_meta(art, im):
    cx, tp = FIX[art] if art in FIX else head(im)
    return [round(cx, 4), im.width, im.height, round(tp, 4)]


ns = 0
for h in roster:
    art = h.get("art")
    src = next((f"{STAND}/{n}.webp" for n in (h["ko"], h["key"]) if os.path.exists(f"{STAND}/{n}.webp")), None)
    if not art or not src:
        continue
    dst = f"{DST}/RunArt/Standing/{art}.png"
    if os.path.exists(dst):
        if len(meta.get(art, [])) == 4 and art not in FIX:
            continue
        # 그림은 그대로 두고 표만 다시(그림을 다시 쓰면 유니티가 크런치를 다시 돌린다)
        im = Image.open(dst).convert("RGBA")
        bb = im.getchannel("A").getbbox() or (0, 0, im.width, im.height)
        im = im.crop((0, 0, bb[2], bb[3]))
        meta[art] = head_meta(art, im)
        continue
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    im = Image.open(src).convert("RGBA")
    bb = im.getchannel("A").point(lambda a: 255 if a > 24 else 0).getbbox()
    if bb:
        im = im.crop((max(0, bb[0] - 8), max(0, bb[1] - 8), min(im.width, bb[2] + 8), min(im.height, bb[3] + 8)))
    k = min(1.0, 1024 / max(im.width, im.height))
    w, hh = max(4, round(im.width * k)), max(4, round(im.height * k))
    im = im.resize((w, hh), Image.LANCZOS)
    canvas = Image.new("RGBA", (pow2(w), pow2(hh)), (0, 0, 0, 0))
    canvas.paste(im, (0, 0))
    meta[art] = head_meta(art, im)
    canvas.save(dst, optimize=True)
    ns += 1
json.dump(meta, open(meta_path, "w", encoding="utf-8"), indent=0)
print("스탠딩", ns, "새로 ·", len(meta), "명")

# 고유 카드 그림(임시) — 패키지의 카드 그림 표(Runtime/Resources/RunUI/cardart.json)에 적힌 원작 스킬 아이콘만
#   RunArt/Skill/<이름>.png(128 그대로) · <이름>_blur.png(크게 흐린 바탕 — 카드 비율 작은 그림, 화면이 늘려 깐다)
from PIL import ImageFilter
table = json.load(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Runtime", "Resources", "RunUI", "cardart.json"), encoding="utf-8"))["cards"]
ni = 0
for name in sorted(set(table.values())):
    # 「…__f」 = 좌우 뒤집기 · 「…__t」 = 뒤집기 + 금빛 틴트(한 사도 안에서 같은 아이콘이 겹칠 때 — Tools~/cardpic_dedupe.py)
    var = name[-3:] if name.endswith(("__f", "__t")) else ""
    src = f"{SRC}/skillicons/{name[:-3] if var else name}.png"
    dst = f"{DST}/RunArt/Skill/{name}.png"
    if not os.path.exists(src) or os.path.exists(dst.replace(".png", "_blur.png")):
        continue
    if var:
        from PIL import ImageOps
        vi = ImageOps.mirror(Image.open(src).convert("RGBA"))
        if var == "__t":
            gold = Image.new("RGBA", vi.size, (255, 196, 90, 255))
            vi = Image.composite(Image.blend(vi, gold, 0.32), vi, vi.getchannel("A"))
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        vi.save(dst)
        src = dst
    else:
        cp(src, dst)
    im = Image.open(src).convert("RGB")
    # 가운데를 카드 비율(0.70)로 잘라 키우고 크게 흐린다 → 작게 줄여 둔다(늘려 깔면 더 부드럽다)
    cw = int(im.height * 0.70)
    im = im.crop(((im.width - cw) // 2, 0, (im.width + cw) // 2, im.height)).resize((280, 400), Image.BICUBIC)
    im = im.filter(ImageFilter.GaussianBlur(18)).resize((56, 80), Image.LANCZOS)
    im.save(dst.replace(".png", "_blur.png"))
    ni += 1
print("카드 아이콘", ni, "새로")

# 고학년 아이콘 — 원작이 고학년(궁극기) 단추 · 스킬 창에 쓰는 사도별 「볼따구」 얼굴(skillicons/Icon_GraduateSkill_<사도>, 128 불투명 네모)
#   사도 상세 「고학년」 칸이 RunArt/Skill/icon_graduateskill_<art> 를 쓴다(원 마스크로 오린다). 135명 모두 있다.
#   ultimate_icon_common3 = 원작 공용 고학년 아이콘(에르핀 볼따구 · 케이크) — 사도 그림이 없을 때 대신 쓴다.
ng = 0
for name in [f"icon_graduateskill_{h['art']}" for h in roster if h.get("art")] + ["ultimate_icon_common3"]:
    src, dst = f"{SRC}/skillicons/{name}.png", f"{DST}/RunArt/Skill/{name}.png"
    if os.path.exists(src) and not os.path.exists(dst):
        cp(src, dst)
        ng += 1
print("고학년 아이콘", ng, "새로")

# 고유 · 생성 카드 원작 그림 — 짝 표(Tools~/cardpic_picks.json, 사도마다 대조 시트로 골랐다)의 그림을 카드 그림 창 비율(364×512)로 잘라 굽는다.
#   원본(cardart_src, 1000×1400 · 930MB)은 넣지 않는다. 짝 표에서 빠진 옛 그림은 지운다(Resources 는 통째로 빌드에 들어간다).
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import cardpic
picdir = f"{DST}/RunArt/CardPic"
objdir = f"{DST}/RunArt/CardObj"
# 사물 · SD(투명 그림)는 CardObj 에 알파 경계로 잘라 따로(cardpic.bake_obj) — 자리는 화면이 잡는다(CardArt.Place).
#   BOLZENA_KEEP_OLD_CARDPIC=1 이면 예전 한 장 굽기(CardPic)도 남긴다(전후 시트 -oldpicfit 용)
keep_old = os.environ.get("BOLZENA_KEEP_OLD_CARDPIC") == "1"
want, objs = {}, {}
for cid, p in cardpic.load().items():
    if p.get("file"):
        (objs if cardpic.is_obj(p) else want)[cardpic.name_of(p)] = p
        if keep_old and cardpic.is_obj(p):
            want[cardpic.name_of(p)] = p
for i in range(len(cardpic.NATURES)):
    want[f"_back_{i}"] = None
np_ = 0
for name, p in sorted(want.items()):
    dst = f"{picdir}/{name}.png"
    if os.path.exists(dst):
        continue
    if p is None:
        cardpic.bake_back(int(name.split("_")[-1]), dst)
        np_ += 1
    elif cardpic.bake(p, dst):
        np_ += 1
metap = f"{objdir}/_meta.json"
meta = json.load(open(metap, encoding="utf-8")) if os.path.exists(metap) else {}
no = 0
for name, p in sorted(objs.items()):
    dst = f"{objdir}/{name}.png"
    if os.path.exists(dst) and name in meta:
        continue
    m = cardpic.bake_obj(p, dst)
    if m:
        meta[name] = m
        no += 1
meta = {k: v for k, v in meta.items() if k in objs}
os.makedirs(objdir, exist_ok=True)
json.dump(meta, open(metap, "w", encoding="utf-8"), ensure_ascii=False, separators=(",", ":"))
gone = 0
for d, keep in ((picdir, want), (objdir, objs)):
    if not os.path.isdir(d):
        continue
    for f in os.listdir(d):
        if f.endswith(".png") and f[:-4] not in keep:
            os.remove(os.path.join(d, f))
            if os.path.exists(os.path.join(d, f + ".meta")):
                os.remove(os.path.join(d, f + ".meta"))
            gone += 1
print("카드 원작 그림", np_, "새로 · 사물", no, "새로 ·", gone, "지움 · 장면", len(want), "· 사물", len(objs), "장")
# 장비 · 교주 카드(상태 · 저주 · 선물) 그림 — itemart.json(Tools~/build_itemart.py) 에 적힌 원작 아이콘만
#   RunArt/Item/<이름>.png(256 판 가운데 — 128 짜리 재화 아이콘은 그대로) · <이름>_blur.png(카드 바탕용 흐린 그림)
items = json.load(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Runtime", "Resources", "RunUI", "itemart.json"), encoding="utf-8"))
nit = 0
for name in sorted(set(items["equips"].values()) | set(items["cards"].values())):
    src = next((f"{SRC}/{sub}/{name}.{ext}" for sub, ext in (("gear", "webp"), ("spell", "webp"), ("currency", "png")) if os.path.exists(f"{SRC}/{sub}/{name}.{ext}")), None)
    dst = f"{DST}/RunArt/Item/{name}.png"
    if src is None or os.path.exists(dst.replace(".png", "_blur.png")):
        continue
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    im = Image.open(src).convert("RGBA")
    im.thumbnail((256, 256), Image.LANCZOS)
    if im.size != (256, 256) and max(im.size) > 128:
        # 2의 거듭제곱 판 가운데에 — 252 · 254 꼴이면 압축이 안 먹어 장당 0.3MB 가 된다
        sq = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
        sq.paste(im, ((256 - im.width) // 2, (256 - im.height) // 2))
        im = sq
    im.save(dst)
    bg = Image.new("RGB", im.size, (34, 40, 66))
    bg.paste(im, (0, 0), im)
    cw = int(bg.height * 0.70)
    bg = bg.crop(((bg.width - cw) // 2, 0, (bg.width + cw) // 2, bg.height)).resize((280, 400), Image.BICUBIC)
    bg.filter(ImageFilter.GaussianBlur(18)).resize((56, 80), Image.LANCZOS).save(dst.replace(".png", "_blur.png"))
    nit += 1
print("장비 · 교주 카드 그림", nit, "새로")
# 사도 말풍선(HeroBubble D1) — 원작 Common_TalkBubble_1 · Common_TalkBubbleTail_1 에 갈색 테를 둘러 RunArt/Ui 로(Tools~/bubble_art.py)
import bubble_art
bubble_art.make(PROJ)
# 교주 보드 칸 능력 아이콘(2026-10-09 사용자 「보드 아이콘 통일」 — 칸마다 능력 아이콘 + 같은 틀) → RunArt/Board/<칸 종류>.png
#   능력치 다섯은 원작 능력치 스티커 한 벌(iconsrc/commonicons/Icon_*, 60 → 120 으로 키움), 나머지는 원작 재화 아이콘(같은 굵은 테 화풍).
#   원작에 딱 맞는 그림이 없는 칸: 보물 지도(eliteUp) → 보물상자 Box_03 · 용돈 주머니(gold) → 골드 동전 CurrencyIcon_0008 — Flow.Crayon.BoardAbility
BOARD = {
    "atk": "iconsrc/commonicons/Icon_AttackPhysic.png", "def": "iconsrc/commonicons/Icon_DefensePhysic.png", "hp": "iconsrc/commonicons/Icon_Hp.png",
    "crit": "iconsrc/commonicons/Icon_CriticalRate.png", "critDmg": "iconsrc/commonicons/Icon_CriticalMult.png",
    "gold": "currency/CurrencyIcon_0008.png", "oracleChance": "currency/CurrencyIcon_024002.png", "removeCost": "currency/CurrencyIcon_0038.png",
    "credits": "currency/CurrencyIcon_0072.png", "oraclePick": "currency/2AnniversaryIcon_000001.png", "eliteUp": "iconsrc/treasureboxicon/Box_03.png",
    "shopDiscount": "currency/CurrencyIcon_0038.png", "startOracle": "currency/CurrencyIcon_0072.png",   # 골디 할인권 = 이용권 · 예습 노트(시작 신탁) = 공책
}
for kind, rel in BOARD.items():
    dst = f"{DST}/RunArt/Board/{kind}.png"
    if os.path.exists(dst):
        continue
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    im = Image.open(f"{SRC}/{rel}").convert("RGBA")
    if max(im.size) < 120:
        im = im.resize((im.width * 2, im.height * 2), Image.LANCZOS)
    im.thumbnail((128, 128), Image.LANCZOS)
    im.save(dst)
print("보드 능력 아이콘", len(BOARD))
# 학년 진급 · 졸업 「참!잘햇어요.」 도장(2026-10-09 사용자 「트릭컬에 참 잘했어요 도장이 있으니 그걸 써라」) — 원작 생활 재료 Icon_PerfectStamp 그대로(128, 고치지 않는다)
#   이미 뽑아 둔 MuMu 아틀라스(bolzena-unity-tmp/mumu_pull — 원작 그림이라 git 밖)에서 RunArt/Ui/perfect_stamp.png 로
cp(r"C:\projects\bolzena-unity-tmp\mumu_pull\png\atlas\lifematerial\Icon_PerfectStamp.png", f"{DST}/RunArt/Ui/perfect_stamp.png")
# 학년 HUD 장식 「교단 증명서」(2026-10-09 사용자) — 원작 재화 아이콘 CurrencyIcon_0016(새싹 · 파란 리본 초록 카드) 그대로
cp(f"{SRC}/currency/CurrencyIcon_0016.png", f"{DST}/RunArt/Ui/order_cert.png")
# 학년 칭찬 스티커 공책 조각(2026-10-09 사용자 「직접 그린 그림을 원작 그림으로」) — 이미 뽑아 둔 원작 아틀라스(bolzena-unity-tmp/mumu_pull, git 밖)에서 RunArt/Ui/grade/
#   스티커 = 원작 미션 도장(사도 SD 얼굴) NewMission_Stamp01~12 · 빈 칸 = 미션 칸 NewMission_BodyItem · 선물 상자 = 앨범 기록 Album_Record_Icon_Present
#   열림 반짝이 = 감정표현 Sparkle · 별 = Popup_Star_01 · 집게 = 미션 탭 집게 NewMission_TabClipFront04 · 테이프 = Album_Record_Sticker · 종이 = 잉클 로비 메모 InkleLobby_Book_Memo
MUMU = r"C:\projects\bolzena-unity-tmp\mumu_pull\png\atlas"
GRADE_ART = {f"sticker{n:02d}": f"quest/NewMission_Stamp{n:02d}.png" for n in range(1, 13)}
GRADE_ART.update({"slot": "quest/NewMission_BodyItem.png", "gift": "archives_album/Album_Record_Icon_Present.png", "sparkle": "emoticon/Sparkle.png",
                  "clip": "quest/NewMission_TabClipFront04.png", "tape": "archives_album/Album_Record_Sticker.png", "memo": "inklemainlobby/InkleLobby_Book_Memo.png"})
for name, rel in GRADE_ART.items():
    src = os.path.join(MUMU, rel)
    if not os.path.exists(src):
        print("학년 공책 원작 조각 없음", rel)
        continue
    cp(src, f"{DST}/RunArt/Ui/grade/{name}.png", (128, 128) if name.startswith("sticker") else None)
cp(f"{SRC}/iconsrc/commonicons/Popup_Star_01.png", f"{DST}/RunArt/Ui/grade/star.png")
print("학년 공책 원작 조각", len(GRADE_ART) + 1)
print("ok")
