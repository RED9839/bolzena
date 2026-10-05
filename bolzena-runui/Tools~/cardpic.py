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
FLAT = ("story", "cg", "schedule")   # 장면 그림 — 자른다. 그 밖(btn · aside · present)은 투명 — 바탕 위에 통째로


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


def _backdrop(nature):
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
    ImageDraw.Draw(core).ellipse((OUT_W * 0.08, OUT_H * 0.14, OUT_W * 0.92, OUT_H * 0.66), fill=150)
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


def bake(p, dst):
    src = os.path.join(SRC, *p["file"].split("/")) + ".png"
    if not os.path.exists(src):
        return False
    cat = os.path.basename(p["file"]).split("__")[0]
    im = Image.open(src)
    c = p.get("c")
    if cat == "cg" and c:
        c = [c[0], c[1], 1.0]   # 만화 컷(2:1)은 세로 전체 — 더 당기면 머리가 잘린다(가로 자리만 고른다)
    out = _crop(im.convert("RGB"), c) if cat in FLAT else _place(im, cat, p.get("nature"))
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    out.save(dst, optimize=True)
    return True
