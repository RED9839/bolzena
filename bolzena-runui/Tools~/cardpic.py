# 고유 · 생성 카드의 원작 그림 — 짝 표(cardpic_picks.json)와 굽기(카드 그림 창 비율로 잘라 줄이기). Docs/카드그림.md
#   build_cardart.py 가 짝 표를 cardart.json 의 "pics"(카드 id → 그림 이름)로 옮기고,
#   copy_assets.py 가 bake() 로 RunArt/CardPic/<그림 이름>.png 를 만든다(원작 그림 — git 밖).
import hashlib, json, os, re
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
PICKS = os.path.join(HERE, "cardpic_picks.json")
SRC = r"C:\projects\볼제나\assets\cardart_src"

# 카드 그림 창(판 W.Card 196×276 · 전투 CardView 1.83×2.63) 비율 — 364×512(4의 배수라 크런치 압축이 먹는다).
# 4K(3440×1440) 에서 덱 보기 카드가 세로 450 언저리라 512 면 넉넉하고, 원본(1000×1400 · 930MB)을 넣지 않는다.
OUT_W, OUT_H = 364, 512
RATIO = OUT_W / OUT_H
FACE_Y = 0.35   # 얼굴이 창 위에서 몇 할 자리에 오나(아래 4할은 효과 글이 덮는다)

# 성격 색(Theme.NatureCard — 투명 그림(싸우는 SD · 물건) 뒤 바탕)
NATURE = {"순수": (0x4C, 0xB8, 0x3A), "광기": (0xE0, 0x48, 0x48), "냉정": (0x18, 0xC2, 0xE6),
          "우울": (0x8A, 0x5C, 0xE6), "활발": (0xE6, 0xC2, 0x1A), "공명": (0xF0, 0xA0, 0x70)}
FLAT = ("story", "cg", "schedule")   # 장면 그림 — 자른다(창을 덮는다 · cover). 그 밖(btn · aside · present)은 투명 — 사물 · SD
# 사물 · SD(투명 그림)는 2026-10-07 「카드 이미지가 너무 붕 뜬 것」 — 창 한 장으로 굽지 않고 따로 둔다(화면이 자리를 잡는다):
#   RunArt/CardObj/<이름>.png = 알파 경계로 잘라 낸 그림 + 발밑 그림자(긴 변 OBJ_MAX 까지 줄임 · 4의 배수 판)
#   RunArt/CardObj/_meta.json = {이름: [내용 x, y(위), 너비, 높이(판 px), 판 너비, 판 높이, 원본 내용 너비, 원본 내용 높이, 성격 번호]}
#   RunArt/CardPic/_back_<성격 번호>.png = 성격 색 바탕(그라데이션 + 둥근 빛 · 그림 없음)
#   자리 규칙은 runui CardArt.Place(판 W.Card · 전투 CardView 같은 함수) — 위 글 · 칩 아래 ~ 효과 판 장식 선 위 칸을 넉넉히 채우고 아래를 장식 선에 붙인다.
OBJ_MAX = 320
NATURES = ["순수", "광기", "냉정", "우울", "활발", "공명", ""]   # 바탕 번호(마지막 = 금빛 중립)


def load():
    return json.load(open(PICKS, encoding="utf-8"))["cards"] if os.path.exists(PICKS) else {}


def name_of(p):
    """그림 이름(Resources 경로) — 파일 + 자르기 값이 바뀌면 이름도 바뀌어 다시 굽는다."""
    stem = re.sub(r"[^A-Za-z0-9]+", "_", p["file"].replace("Character_", "").replace("ScheduleStory_", "S_").replace("MainLobby_", ""))
    h = hashlib.md5(json.dumps([p["file"], p.get("c"), p.get("nature")]).encode()).hexdigest()[:6]
    return f"{stem[:48]}_{h}"


def _crop(im, c):
    W, H = im.size
    x, y, s = (c or [5, 3.5, 0.6])
    x, y, s = x / 10, y / 10, max(0.2, min(1.0, s))
    ch = s * H
    cw = ch * RATIO
    if cw > W:
        cw = W
        ch = cw / RATIO
    if ch > H:
        ch = H
        cw = ch * RATIO
    left = min(max(0, x * W - cw / 2), W - cw)
    top = min(max(0, y * H - FACE_Y * ch), H - ch)
    return im.crop((round(left), round(top), round(left + cw), round(top + ch))).resize((OUT_W, OUT_H), Image.LANCZOS)


def _backdrop(nature, glow_box=(0.08, 0.14, 0.92, 0.66)):
    nc = NATURE.get(nature, (0xD9, 0xA4, 0x41))
    top = tuple(round(0x16 * 0.45 + v * 0.55) for v in nc)
    bot = (0x0A, 0x0F, 0x1C)
    g = Image.new("RGB", (1, OUT_H))
    for yy in range(OUT_H):
        t = min(1, yy / (OUT_H * 0.85))
        g.putpixel((0, yy), tuple(round(a + (b - a) * t) for a, b in zip(top, bot)))
    bg = g.resize((OUT_W, OUT_H)).convert("RGBA")
    # 그림 뒤 둥근 빛
    glow = Image.new("RGBA", (OUT_W, OUT_H), (0, 0, 0, 0))
    light = tuple(min(255, round(v * 0.6 + 255 * 0.4)) for v in nc)
    core = Image.new("L", (OUT_W, OUT_H), 0)
    from PIL import ImageDraw
    gx0, gy0, gx1, gy1 = glow_box
    ImageDraw.Draw(core).ellipse((OUT_W * gx0, OUT_H * gy0, OUT_W * gx1, OUT_H * gy1), fill=150)
    core = core.filter(ImageFilter.GaussianBlur(48))
    glow.paste(Image.new("RGBA", (OUT_W, OUT_H), light + (255,)), (0, 0), core)
    return Image.alpha_composite(bg, glow)


def _place(im, cat, nature):
    im = im.convert("RGBA")
    bb = im.getchannel("A").point(lambda a: 255 if a > 16 else 0).getbbox()
    if bb:
        im = im.crop(bb)
    box_w, box_h, cy = (OUT_W * 0.96, OUT_H * 0.56, 0.36) if cat == "btn" else (OUT_W * 0.80, OUT_H * 0.48, 0.34)
    k = min(box_w / im.width, box_h / im.height)
    im = im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)
    out = _backdrop(nature)
    x, y = round((OUT_W - im.width) / 2), round(OUT_H * cy - im.height / 2)
    sh = Image.new("RGBA", out.size, (0, 0, 0, 0))
    a = im.getchannel("A").point(lambda v: v * 0.55)
    sh.paste(Image.new("RGBA", im.size, (0, 0, 0, 255)), (x, y + 10), a)
    out = Image.alpha_composite(out, sh.filter(ImageFilter.GaussianBlur(10)))
    out.alpha_composite(im, (x, y))
    return out.convert("RGB")


def nature_index(n):
    return NATURES.index(n) if n in NATURES else len(NATURES) - 1


# 스탠딩 자르기(2026-10-08 — 시작 카드 · 남는 칸): file = "@stand/<그림 키>/<자리>" · webp = .shots/standing-webp 의 이름(한글 · 웹판 키)
#   자리 face = 얼굴 확대(성격 바탕 위에 잘라 창을 덮는다 — CardPic) · full = 전신(알파 경계로 자른 사물 꼴 — CardObj, 화면이 자리를 잡는다)
STAND = r"C:\projects\볼제나\.shots\standing-webp"


def is_stand(p):
    return str(p.get("file", "")).startswith("@stand/")


# 풀어 둔 원작 PNG(2026-10-09): file = "@raw/<mumu_pull/png/raw 상대 경로>" — 하이라이트 · 클리어 CG · 로그라이크 카드는 장면(자른다), 테마 등장 그림은 투명(사물 꼴)
RAW = r"C:\projects\bolzena-unity-tmp\mumu_pull\png\raw"
RAW_OBJ = ("00.themeevent/", "skillicons/")


def is_raw(p):
    return str(p.get("file", "")).startswith("@raw/")


def src_of(p):
    if is_raw(p):
        return os.path.join(RAW, *p["file"][5:].split("/")) + ".png"
    if is_stand(p):
        return os.path.join(STAND, p.get("webp", "") + ".webp")
    return os.path.join(SRC, *p["file"].split("/")) + ".png"


def _stand_image(p):
    """스탠딩 렌더 — 투명 가장자리를 자른 것(copy_assets.py 의 RunArt/Standing 과 같은 자르기 · 머리 자리 표 _meta.json 과 좌표가 맞는다)."""
    im = Image.open(src_of(p)).convert("RGBA")
    bb = im.getchannel("A").point(lambda a: 255 if a > 24 else 0).getbbox()
    if bb:
        im = im.crop((max(0, bb[0] - 8), max(0, bb[1] - 8), min(im.width, bb[2] + 8), min(im.height, bb[3] + 8)))
    return im


def is_obj(p):
    if is_raw(p):
        return p["file"][5:].startswith(RAW_OBJ)
    if is_stand(p):
        return p["file"].split("/")[2] == "full"
    return bool(p.get("file")) and os.path.basename(p["file"]).split("__")[0] not in FLAT


def bake_back(idx, dst):
    """성격 바탕 한 장(128×180 — 부드러워 늘려도 된다). 둥근 빛은 창 위에서 2할 ~ 7할(사물이 서는 칸) 가운데."""
    n = NATURES[idx]
    big = _backdrop(n if n else None, (0.04, 0.2, 0.96, 0.72))
    out = big.convert("RGB").resize((128, 180), Image.LANCZOS)
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    out.save(dst, optimize=True)


def bake_obj(p, dst):
    """사물 · SD 그림 — 알파 경계로 자르고 발밑 그림자를 붙인다. 돌려주는 값 = _meta.json 한 줄."""
    src = src_of(p)
    if not os.path.exists(src):
        return None
    im = _stand_image(p) if is_stand(p) else Image.open(src).convert("RGBA")
    bb = im.getchannel("A").point(lambda a: 255 if a > 16 else 0).getbbox() or (0, 0) + im.size
    im = im.crop(bb)
    sw, sh = im.size
    k = min(1.0, OBJ_MAX / max(sw, sh))
    if k < 1:
        im = im.resize((max(1, round(sw * k)), max(1, round(sh * k))), Image.LANCZOS)
    w, h = im.size
    # 판: 옆 · 아래로 그림자 자리. 4의 배수(크런치 · 웹 DXT)
    px, pt, pb = round(w * 0.06) + 2, 2, round(h * 0.07) + 4
    W, H = w + 2 * px, h + pt + pb
    W, H = (W + 3) // 4 * 4, (H + 3) // 4 * 4
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    from PIL import ImageDraw
    sh_ = Image.new("L", (W, H), 0)
    cx, by = px + w / 2, pt + h
    ImageDraw.Draw(sh_).ellipse((cx - w * 0.40, by - h * 0.045, cx + w * 0.40, by + h * 0.035), fill=150)
    sh_ = sh_.filter(ImageFilter.GaussianBlur(max(2, h * 0.025)))
    out.paste(Image.new("RGBA", (W, H), (0, 0, 0, 255)), (0, 0), sh_)
    # 그림 자체의 흐린 그림자(옛 굽기와 같은 결 — 아래로 조금)
    a = im.getchannel("A").point(lambda v: v * 0.45)
    drop = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    drop.paste(Image.new("RGBA", im.size, (0, 0, 0, 255)), (px, pt + max(1, round(h * 0.02))), a)
    out = Image.alpha_composite(out, drop.filter(ImageFilter.GaussianBlur(max(1.5, h * 0.015))))
    out.alpha_composite(im, (px, pt))
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    out.save(dst, optimize=True)
    return [px, pt, w, h, W, H, sw, sh, nature_index(p.get("nature"))]


def bake(p, dst):
    src = src_of(p)
    if not os.path.exists(src):
        return False
    if is_stand(p):
        # 얼굴 확대 — 투명 바깥은 성격 바탕(그림 창 밖으로 나가는 자리도 바탕으로 채운다)
        im = _stand_image(p)
        W, H = im.size
        x, y, s_ = p.get("c") or [5, 1.5, 0.3]
        ch = s_ * H
        cw = ch * RATIO
        left, top = x / 10 * W - cw / 2, y / 10 * H - FACE_Y * ch
        part = im.crop((round(left), round(top), round(left + cw), round(top + ch))).resize((OUT_W, OUT_H), Image.LANCZOS)
        out = _backdrop(p.get("nature"), (0.0, 0.0, 1.0, 0.7))
        out.alpha_composite(part)
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        out.convert("RGB").save(dst, optimize=True)
        return True
    cat = "cg" if is_raw(p) and not is_obj(p) else os.path.basename(p["file"]).split("__")[0]
    im = Image.open(src)
    c = p.get("c")
    if cat == "cg" and c:
        c = [c[0], c[1], 1.0]   # 만화 컷(2:1)은 세로 전체 — 더 당기면 머리가 잘린다(가로 자리만 고른다)
    out = _crop(im.convert("RGB"), c) if cat in FLAT else _place(im, cat, p.get("nature"))
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    out.save(dst, optimize=True)
    return True
