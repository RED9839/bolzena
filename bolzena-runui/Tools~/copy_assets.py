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
for vf in glob.glob(r"C:\projects\bolzena-content\world\villages\*.json"):
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
            im = np.asarray(Image.open(s).convert("RGBA")).astype(np.float32)
            a = im[..., 3:4]
            rgb = np.where(a > 0, im[..., :3] * 255.0 / np.maximum(a, 1), 0)
            Image.fromarray(np.concatenate([np.clip(rgb, 0, 255), a], axis=-1).astype(np.uint8), "RGBA").save(f"{dd}/{f}")
    print("스파인", name)


spine(f"{SRC}/spine/minimi", "minimi")
# 스탠딩 — 로비 메인 사도(에르핀) · 상점 주인(골디) · 코어 샘플 이벤트의 NPC
for key in ["erpin", "goldy", "sist", "alice", "jubee"]:
    p = f"{SRC}/spine/standing/{key}"
    if not os.path.isdir(p):
        p = f"{SRC}/standing/{key}"
    if os.path.isdir(p):
        spine(p, "st_" + key)
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
    src = f"{SRC}/skillicons/{name}.png"
    dst = f"{DST}/RunArt/Skill/{name}.png"
    if not os.path.exists(src) or os.path.exists(dst.replace(".png", "_blur.png")):
        continue
    cp(src, dst)
    im = Image.open(src).convert("RGB")
    # 가운데를 카드 비율(0.70)로 잘라 키우고 크게 흐린다 → 작게 줄여 둔다(늘려 깔면 더 부드럽다)
    cw = int(im.height * 0.70)
    im = im.crop(((im.width - cw) // 2, 0, (im.width + cw) // 2, im.height)).resize((280, 400), Image.BICUBIC)
    im = im.filter(ImageFilter.GaussianBlur(18)).resize((56, 80), Image.LANCZOS)
    im.save(dst.replace(".png", "_blur.png"))
    ni += 1
print("카드 아이콘", ni, "새로")
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
print("ok")
