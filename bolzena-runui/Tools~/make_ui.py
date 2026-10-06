# 판 화면 그림(판 · 버튼 · 지도 칸 · 아이콘)을 코드로 그린다 — 원작 그림이 아니라서 패키지에 넣는다.
# bolzena-unity/Tools/make_ui.py 와 같은 결: 어두운 남색 판 + 금빛 테두리, 금빛 버튼, 4배로 그려 줄여 가장자리를 매끈하게.
#   python "Tools~/make_ui.py"  ->  Runtime/Resources/RunUI/Sprites/*.png
import math, os
from PIL import Image, ImageDraw, ImageFilter, ImageChops

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Runtime", "Resources", "RunUI", "Sprites")
os.makedirs(OUT, exist_ok=True)
SS = 4


def canvas(w, h):
    return Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))


def save(im, name):
    w, h = im.size[0] // SS, im.size[1] // SS
    im.resize((w, h), Image.LANCZOS).save(os.path.join(OUT, name))


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(len(a)))


def vgrad(w, h, top, bot):
    g = Image.new("RGBA", (1, h))
    for y in range(h):
        g.putpixel((0, y), lerp(top, bot, y / max(1, h - 1)))
    return g.resize((w, h))


def rr_mask(w, h, r):
    m = Image.new("L", (w, h), 0)
    ImageDraw.Draw(m).rounded_rectangle((0, 0, w - 1, h - 1), r, fill=255)
    return m


def paste_grad(im, box, top, bot, r):
    x0, y0, x1, y1 = [int(v) for v in box]
    w, h = x1 - x0, y1 - y0
    g = vgrad(w, h, top, bot)
    im.alpha_composite(Image.composite(g, Image.new("RGBA", (w, h)), rr_mask(w, h, r)), (x0, y0))


# ── 판(9칸) — 96×96, 테두리 r ──
def panel(name, top, bot, rim=None, rimw=2, r=18, size=96, inner_hl=True):
    im = canvas(size, size)
    S = size * SS
    if rim:
        paste_grad(im, (0, 0, S, S), rim[0], rim[1], r * SS)
        o = rimw * SS
        paste_grad(im, (o, o, S - o, S - o), top, bot, (r - rimw) * SS)
    else:
        paste_grad(im, (0, 0, S, S), top, bot, r * SS)
    if inner_hl:
        d = ImageDraw.Draw(im)
        d.line(((r) * SS, (rimw + 1) * SS, S - r * SS, (rimw + 1) * SS), fill=(255, 255, 255, 40), width=SS)
    save(im, name)


# ── 판 화면 톤(Docs/톤.md) — 어두운 남색 판 + 얇은 금빛 테두리. 모서리는 작게(10~12), 테두리는 2(1600×900 기준 단위) ──
NAVY_T, NAVY_B = (22, 32, 60), (12, 18, 36)          # 판      #16203C → #0C1224
CELL_T, CELL_B = (31, 42, 74), (19, 27, 50)          # 칸      #1F2A4A → #131B32
GOLD_HI, GOLD_LO = (236, 204, 136), (168, 128, 66)   # 금 테두리 #ECCC88 → #A88042
# 큰 판 — 남색 + 금빛 가는 테두리
panel("panel.png", NAVY_T + (242,), NAVY_B + (246,), rim=(GOLD_HI + (235,), GOLD_LO + (210,)), rimw=2, r=12)
# 판 안의 칸 — 조금 밝은 남색, 테두리는 흰빛 흐리게
panel("cell.png", CELL_T + (238,), CELL_B + (238,), rim=((255, 255, 255, 44), (255, 255, 255, 18)), rimw=2, r=10)
# 고른 칸 — 금빛 테두리 두껍게, 속은 살짝 따뜻하게
panel("cell_on.png", (44, 46, 64, 244), (24, 28, 46, 244), rim=((255, 232, 160, 255), (222, 164, 72, 255)), rimw=3, r=10)
# 버튼 — 금(주 동작) · 남색+금 테두리(보조) · 빨강(위험)
panel("btn_gold.png", (255, 231, 160, 255), (233, 172, 72, 255), rim=((255, 246, 214, 255), (170, 112, 38, 255)), rimw=2, r=12)
panel("btn_dark.png", (32, 43, 74, 246), (18, 25, 46, 246), rim=(GOLD_HI + (210,), GOLD_LO + (170,)), rimw=2, r=12)
panel("btn_brown.png", (38, 50, 86, 250), (22, 30, 54, 250), rim=((255, 228, 160, 255), (196, 140, 60, 255)), rimw=2, r=12)
panel("btn_red.png", (222, 94, 110, 255), (156, 44, 66, 255), rim=((255, 204, 210, 255), (112, 26, 44, 255)), rimw=2, r=12)
# 유리판(카제나 결의 정보 칸) — 반투명 남색, 얇은 흰 테두리
panel("glass.png", (20, 29, 54, 214), (10, 15, 30, 232), rim=((255, 255, 255, 62), (255, 255, 255, 26)), rimw=2, r=10, inner_hl=False)
panel("glass_on.png", (42, 44, 62, 236), (20, 24, 40, 240), rim=((255, 232, 160, 255), (226, 166, 74, 255)), rimw=3, r=10, inner_hl=False)
panel("glass_dim.png", (14, 18, 30, 150), (8, 11, 20, 172), rim=((255, 255, 255, 30), (255, 255, 255, 14)), rimw=2, r=10, inner_hl=False)
# 알약 버튼 — 금(주 동작) · 장밋빛(위험) · 남색+금 테두리(보조)
panel("pill_gold.png", (255, 231, 160, 255), (233, 172, 72, 255), rim=((255, 246, 214, 255), (170, 112, 38, 255)), rimw=2, r=47, inner_hl=False)
panel("pill_rose.png", (222, 94, 110, 255), (156, 44, 66, 255), rim=((255, 204, 210, 255), (112, 26, 44, 255)), rimw=2, r=47, inner_hl=False)
panel("pill_dark.png", (26, 35, 62, 240), (14, 20, 38, 240), rim=(GOLD_HI + (200,), GOLD_LO + (150,)), rimw=2, r=47, inner_hl=False)
# 알약(둥근 띠)
panel("pill.png", (255, 255, 255, 255), (255, 255, 255, 255), r=47, inner_hl=False)
panel("round.png", (255, 255, 255, 255), (255, 255, 255, 255), r=14, inner_hl=False)
# 말풍선(밝은 종이 — 인물의 말은 판과 다르게 읽힌다)
panel("paper.png", (252, 249, 240, 250), (240, 234, 220, 250), rim=((255, 255, 255, 255), (196, 176, 140, 255)), rimw=2, r=14, inner_hl=False)


# 테두리만(속이 빈 9칸) — 올림 · 고름 빛을 판 위에 겹친다
def frame(name, r, w, size=96):
    im = canvas(size, size)
    S = size * SS
    m = Image.new("L", (S, S), 0)
    ImageDraw.Draw(m).rounded_rectangle((0, 0, S - 1, S - 1), r * SS, fill=255)
    m2 = Image.new("L", (S, S), 0)
    o = w * SS
    ImageDraw.Draw(m2).rounded_rectangle((o, o, S - 1 - o, S - 1 - o), max(1, (r - w)) * SS, fill=255)
    m = ImageChops.subtract(m, m2)
    white = Image.new("RGBA", (S, S), (255, 255, 255, 255))
    im.paste(white, (0, 0), m)
    save(im, name)


frame("frame.png", 12, 2)
frame("frame_thick.png", 12, 4)
frame("frame_pill.png", 47, 2)

# ── 그림자 · 빛 · 단색 ──
Image.new("RGBA", (8, 8), (255, 255, 255, 255)).save(os.path.join(OUT, "white.png"))

sh = Image.new("L", (128, 128), 0)
ImageDraw.Draw(sh).rounded_rectangle((24, 24, 104, 104), 18, fill=255)
sh = sh.filter(ImageFilter.GaussianBlur(10))
shadow = Image.new("RGBA", (128, 128), (0, 0, 0, 0))
shadow.putalpha(sh)
shadow.save(os.path.join(OUT, "shadow.png"))


def radial(size, power, name):
    im = Image.new("RGBA", (size, size))
    px = im.load()
    c = (size - 1) / 2
    for y in range(size):
        for x in range(size):
            d = math.hypot(x - c, y - c) / c
            px[x, y] = (255, 255, 255, int(255 * max(0.0, 1 - d) ** power))
    im.save(os.path.join(OUT, name))


radial(128, 2.2, "soft.png")
radial(128, 0.7, "softhard.png")

vg = Image.new("RGBA", (320, 180))
px = vg.load()
for y in range(180):
    for x in range(320):
        d = math.hypot((x - 159.5) / 159.5, (y - 89.5) / 89.5)
        a = min(1, max(0, (d - 0.45) / 0.7)) ** 1.5
        px[x, y] = (0, 0, 0, int(255 * a))
vg.save(os.path.join(OUT, "vignette.png"))

# 아래로 어두워지는 띠(배경 위에 글을 얹을 때)
g = Image.new("RGBA", (4, 256))
for y in range(256):
    g.putpixel((0, y), (0, 0, 0, int(255 * (y / 255) ** 1.4)))
    for x in range(1, 4):
        g.putpixel((x, y), (0, 0, 0, int(255 * (y / 255) ** 1.4)))
g.save(os.path.join(OUT, "fade_down.png"))

im = canvas(128, 128)
ImageDraw.Draw(im).ellipse((2 * SS, 2 * SS, 126 * SS, 126 * SS), fill=(255, 255, 255, 255))
save(im, "circle.png")
im = canvas(128, 128)
ImageDraw.Draw(im).ellipse((3 * SS, 3 * SS, 125 * SS, 125 * SS), outline=(255, 255, 255, 255), width=6 * SS)
save(im, "ring.png")


# ── 지도 칸 — 마름모 보석(종류마다 색) + 그 위 글리프 ──
def gem(name, c_top, c_bot, rim):
    S = 160 * SS
    im = canvas(160, 160)
    m = Image.new("L", (S, S), 0)
    ImageDraw.Draw(m).polygon([(S / 2, 8 * SS), (S - 8 * SS, S / 2), (S / 2, S - 8 * SS), (8 * SS, S / 2)], fill=255)
    m = m.filter(ImageFilter.GaussianBlur(SS * 1.5))
    rimimg = Image.new("RGBA", (S, S), rim + (255,))
    im.paste(rimimg, (0, 0), m)
    m2 = Image.new("L", (S, S), 0)
    ImageDraw.Draw(m2).polygon([(S / 2, 18 * SS), (S - 18 * SS, S / 2), (S / 2, S - 18 * SS), (18 * SS, S / 2)], fill=255)
    m2 = m2.filter(ImageFilter.GaussianBlur(SS))
    im.paste(vgrad(S, S, c_top + (255,), c_bot + (255,)), (0, 0), m2)
    # 윗면 반짝임
    hl = Image.new("L", (S, S), 0)
    ImageDraw.Draw(hl).polygon([(S / 2, 22 * SS), (S - 40 * SS, S / 2 - 18 * SS), (40 * SS, S / 2 - 18 * SS)], fill=70)
    hl = hl.filter(ImageFilter.GaussianBlur(SS * 4))
    w = Image.new("RGBA", (S, S), (255, 255, 255, 0))
    w.putalpha(hl)
    im.alpha_composite(w)
    save(im, name)


gem("gem_fight.png", (240, 100, 120), (170, 40, 70), (255, 214, 200))
gem("gem_elite.png", (150, 110, 230), (80, 50, 160), (230, 210, 255))
gem("gem_boss.png", (255, 90, 70), (130, 20, 30), (255, 220, 120))
gem("gem_event.png", (70, 190, 200), (30, 110, 140), (200, 250, 255))
gem("gem_camp.png", (240, 170, 90), (170, 90, 40), (255, 236, 190))
gem("gem_shop.png", (250, 210, 90), (190, 120, 30), (255, 250, 210))
gem("gem_start.png", (80, 80, 110), (40, 40, 60), (210, 210, 230))
gem("gem_done.png", (90, 90, 100), (50, 50, 60), (150, 150, 160))

# 마름모 빛 테두리(갈 수 있는 칸)
S = 200 * SS
im = canvas(200, 200)
m = Image.new("L", (S, S), 0)
ImageDraw.Draw(m).polygon([(S / 2, 10 * SS), (S - 10 * SS, S / 2), (S / 2, S - 10 * SS), (10 * SS, S / 2)], outline=255, width=8 * SS)
m = m.filter(ImageFilter.GaussianBlur(5 * SS))
glow = Image.new("RGBA", (S, S), (255, 255, 255, 0))
glow.putalpha(m)
save(glow, "gem_glow.png")


# ── 지도 칸 — 납작한 마름모 타일(웹판 czn.css .mring 의 결: 정사각형을 45° 돌려 세로 0.58 로 누름) ──
TW, TH = 200, 120


def diamond(S_w, S_h, inset):
    cx, cy = S_w / 2, S_h / 2
    hw, hh = S_w / 2 - inset, (S_h / 2 - inset * 0.58)
    return [(cx, cy - hh), (cx + hw, cy), (cx, cy + hh), (cx - hw, cy)]


def tile(name, c1, c2, edge):
    W_, H_ = TW * SS, TH * SS
    im = canvas(TW, TH)
    # 바깥 어두운 테(배경에서 떼어 낸다)
    m = Image.new("L", (W_, H_), 0)
    ImageDraw.Draw(m).polygon(diamond(W_, H_, 2 * SS), fill=255)
    m = m.filter(ImageFilter.GaussianBlur(SS))
    im.paste(Image.new("RGBA", (W_, H_), (8, 10, 20, 150)), (0, 0), m)
    # 밝은 테두리
    m = Image.new("L", (W_, H_), 0)
    ImageDraw.Draw(m).polygon(diamond(W_, H_, 9 * SS), fill=255)
    im.paste(Image.new("RGBA", (W_, H_), edge + (255,)), (0, 0), m)
    # 속 — 위에서 아래로(왼위 밝게)
    m2 = Image.new("L", (W_, H_), 0)
    ImageDraw.Draw(m2).polygon(diamond(W_, H_, 13 * SS), fill=255)
    im.paste(vgrad(W_, H_, c1 + (255,), c2 + (255,)), (0, 0), m2)
    # 윗면 반짝임(위쪽 두 변을 따라)
    hl = Image.new("L", (W_, H_), 0)
    d = diamond(W_, H_, 16 * SS)
    ImageDraw.Draw(hl).polygon([d[0], (d[1][0] - 30 * SS, d[1][1] - 6 * SS), (W_ / 2, H_ / 2 - 6 * SS), (d[3][0] + 30 * SS, d[3][1] - 6 * SS)], fill=60)
    hl = hl.filter(ImageFilter.GaussianBlur(SS * 5))
    w = Image.new("RGBA", (W_, H_), (255, 255, 255, 0))
    w.putalpha(ImageChops.multiply(hl, m2))
    im.alpha_composite(w)
    save(im, name)


tile("tile_fight.png", (255, 111, 143), (184, 48, 79), (255, 194, 207))
tile("tile_elite.png", (165, 124, 240), (90, 55, 168), (216, 196, 255))
tile("tile_event.png", (79, 208, 224), (31, 127, 150), (184, 242, 250))
tile("tile_camp.png", (255, 181, 88), (201, 112, 28), (255, 224, 176))
tile("tile_shop.png", (246, 213, 142), (192, 138, 46), (255, 243, 207))
tile("tile_boss.png", (255, 90, 106), (140, 31, 44), (255, 179, 187))
tile("tile_start.png", (74, 84, 112), (38, 45, 68), (201, 210, 234))
tile("tile_done.png", (74, 79, 98), (42, 46, 60), (150, 156, 176))

# 갈 수 있는 칸의 금빛 테(흐린 마름모 선)
W_, H_ = TW * SS, TH * SS
im = canvas(TW, TH)
m = Image.new("L", (W_, H_), 0)
ImageDraw.Draw(m).polygon(diamond(W_, H_, 6 * SS), outline=255, width=5 * SS)
m = m.filter(ImageFilter.GaussianBlur(3 * SS))
g = Image.new("RGBA", (W_, H_), (255, 255, 255, 0))
g.putalpha(m)
im.alpha_composite(g)
save(im, "tile_glow.png")

# ── 글리프(흰색, 칸 위에 얹는다) — 128 ──
def glyph(name, fn, size=128, outline=True):
    im = canvas(size, size)
    fn(ImageDraw.Draw(im), size * SS, im)
    if outline:
        a = im.split()[3].filter(ImageFilter.MaxFilter(3 * SS + 1))
        back = Image.new("RGBA", im.size, (20, 10, 20, 0))
        back.putalpha(a.point(lambda v: int(v * 0.55)))
        back.alpha_composite(im)
        im = back
    save(im, name)


W = (255, 255, 255, 255)


def g_swords(d, S, im):
    k = S / 128
    for sgn in (1, -1):
        cx = 64 * k
        pts = [(-6, -46), (6, -46), (6, 24), (-6, 24)]
        ang = math.radians(40 * sgn)
        rot = lambda p: (cx + p[0] * k * math.cos(ang) - p[1] * k * math.sin(ang), 64 * k + p[0] * k * math.sin(ang) + p[1] * k * math.cos(ang))
        d.polygon([rot(p) for p in [(-7, -40), (0, -54), (7, -40), (7, 22), (-7, 22)]], fill=W)
        d.polygon([rot(p) for p in [(-20, 22), (20, 22), (20, 30), (-20, 30)]], fill=W)
        d.polygon([rot(p) for p in [(-5, 30), (5, 30), (5, 48), (-5, 48)]], fill=W)


def g_skull(d, S, im):
    k = S / 128
    d.polygon([(30 * k, 22 * k), (48 * k, 44 * k), (34 * k, 52 * k)], fill=W)
    d.polygon([(98 * k, 22 * k), (80 * k, 44 * k), (94 * k, 52 * k)], fill=W)
    d.ellipse((30 * k, 36 * k, 98 * k, 100 * k), fill=W)
    d.ellipse((44 * k, 58 * k, 58 * k, 74 * k), fill=(0, 0, 0, 0))
    d.ellipse((70 * k, 58 * k, 84 * k, 74 * k), fill=(0, 0, 0, 0))
    d.polygon([(58 * k, 84 * k), (64 * k, 78 * k), (70 * k, 84 * k)], fill=(0, 0, 0, 0))


def g_crown(d, S, im):
    k = S / 128
    d.polygon([(20 * k, 40 * k), (42 * k, 64 * k), (64 * k, 26 * k), (86 * k, 64 * k), (108 * k, 40 * k), (100 * k, 96 * k), (28 * k, 96 * k)], fill=W)
    d.rectangle((28 * k, 100 * k, 100 * k, 110 * k), fill=W)
    for x in (20, 64, 108):
        d.ellipse(((x - 7) * k, (26 if x == 64 else 40) * k - 14 * k, (x + 7) * k, (26 if x == 64 else 40) * k), fill=W)


def g_q(d, S, im):
    k = S / 128
    d.arc((36 * k, 18 * k, 92 * k, 74 * k), 180, 60, fill=W, width=int(14 * k))
    d.line((80 * k, 66 * k, 64 * k, 76 * k), fill=W, width=int(14 * k))
    d.line((64 * k, 72 * k, 64 * k, 88 * k), fill=W, width=int(14 * k))
    d.ellipse((55 * k, 96 * k, 73 * k, 114 * k), fill=W)


def g_fire(d, S, im):
    k = S / 128
    d.polygon([(64 * k, 14 * k), (90 * k, 52 * k), (96 * k, 78 * k), (84 * k, 100 * k), (64 * k, 108 * k), (44 * k, 100 * k), (32 * k, 78 * k), (40 * k, 54 * k), (52 * k, 66 * k)], fill=W)
    d.polygon([(64 * k, 60 * k), (78 * k, 84 * k), (64 * k, 100 * k), (50 * k, 84 * k)], fill=(0, 0, 0, 0))
    d.line((26 * k, 116 * k, 102 * k, 104 * k), fill=W, width=int(8 * k))
    d.line((26 * k, 104 * k, 102 * k, 116 * k), fill=W, width=int(8 * k))


def g_bag(d, S, im):
    # 동전 주머니 — 둥근 몸 · 묶은 목 · 위로 벌어진 주둥이 · 가운데 동전 무늬(자물쇠와 헷갈리지 않게)
    k = S / 128
    d.ellipse((22 * k, 44 * k, 106 * k, 116 * k), fill=W)
    d.polygon([(48 * k, 48 * k), (80 * k, 48 * k), (72 * k, 36 * k), (56 * k, 36 * k)], fill=W)
    d.polygon([(40 * k, 14 * k), (56 * k, 34 * k), (72 * k, 34 * k), (88 * k, 14 * k), (64 * k, 24 * k)], fill=W)
    d.rounded_rectangle((44 * k, 32 * k, 84 * k, 42 * k), 4 * k, fill=(0, 0, 0, 0))
    d.ellipse((50 * k, 64 * k, 78 * k, 92 * k), outline=(0, 0, 0, 0), width=int(7 * k))


def g_flag(d, S, im):
    k = S / 128
    d.rectangle((34 * k, 18 * k, 44 * k, 112 * k), fill=W)
    d.polygon([(44 * k, 20 * k), (100 * k, 38 * k), (44 * k, 66 * k)], fill=W)


def g_check(d, S, im):
    k = S / 128
    d.line((28 * k, 66 * k, 54 * k, 92 * k, 102 * k, 36 * k), fill=W, width=int(16 * k), joint="curve")


def g_heart(d, S, im):
    k = S / 128
    d.ellipse((18 * k, 24 * k, 66 * k, 72 * k), fill=W)
    d.ellipse((62 * k, 24 * k, 110 * k, 72 * k), fill=W)
    d.polygon([(21 * k, 58 * k), (107 * k, 58 * k), (64 * k, 108 * k)], fill=W)


def g_deck(d, S, im):
    k = S / 128
    d.rounded_rectangle((26 * k, 30 * k, 82 * k, 106 * k), 8 * k, outline=W, width=int(8 * k))
    d.rounded_rectangle((46 * k, 20 * k, 102 * k, 96 * k), 8 * k, fill=W)


def g_cog(d, S, im):
    k = S / 128
    c = 64 * k
    for i in range(8):
        a = i * math.pi / 4
        d.line((c + math.cos(a) * 28 * k, c + math.sin(a) * 28 * k, c + math.cos(a) * 52 * k, c + math.sin(a) * 52 * k), fill=W, width=int(18 * k))
    d.ellipse((c - 38 * k, c - 38 * k, c + 38 * k, c + 38 * k), fill=W)
    d.ellipse((c - 16 * k, c - 16 * k, c + 16 * k, c + 16 * k), fill=(0, 0, 0, 0))


def g_back(d, S, im):
    k = S / 128
    d.line((80 * k, 24 * k, 40 * k, 64 * k, 80 * k, 104 * k), fill=W, width=int(16 * k), joint="curve")


def g_plus(d, S, im):
    k = S / 128
    d.line((64 * k, 26 * k, 64 * k, 102 * k), fill=W, width=int(16 * k))
    d.line((26 * k, 64 * k, 102 * k, 64 * k), fill=W, width=int(16 * k))


def g_sword(d, S, im):
    k = S / 128
    d.polygon([(64 * k, 10 * k), (76 * k, 24 * k), (72 * k, 84 * k), (56 * k, 84 * k), (52 * k, 24 * k)], fill=W)
    d.rounded_rectangle((34 * k, 84 * k, 94 * k, 96 * k), 5 * k, fill=W)
    d.rounded_rectangle((58 * k, 96 * k, 70 * k, 116 * k), 3 * k, fill=W)


def g_shield(d, S, im):
    k = S / 128
    d.polygon([(64 * k, 10 * k), (110 * k, 26 * k), (104 * k, 74 * k), (64 * k, 118 * k), (24 * k, 74 * k), (18 * k, 26 * k)], fill=W)


def g_ring(d, S, im):
    k = S / 128
    d.ellipse((26 * k, 40 * k, 102 * k, 116 * k), outline=W, width=int(14 * k))
    d.polygon([(64 * k, 10 * k), (82 * k, 30 * k), (64 * k, 50 * k), (46 * k, 30 * k)], fill=W)


def g_moon(d, S, im):
    k = S / 128
    d.ellipse((22 * k, 18 * k, 106 * k, 102 * k), fill=W)
    d.ellipse((46 * k, 6 * k, 124 * k, 84 * k), fill=(0, 0, 0, 0))


def g_spark(d, S, im):
    k = S / 128
    pts = []
    for i in range(8):
        a = math.pi * i / 4 - math.pi / 2
        r = (56 if i % 2 == 0 else 14) * k
        pts.append((64 * k + r * math.cos(a), 64 * k + r * math.sin(a)))
    d.polygon(pts, fill=W)


def g_trash(d, S, im):
    k = S / 128
    d.rounded_rectangle((34 * k, 40 * k, 94 * k, 112 * k), 8 * k, fill=W)
    d.rectangle((24 * k, 26 * k, 104 * k, 36 * k), fill=W)
    d.rectangle((52 * k, 16 * k, 76 * k, 26 * k), fill=W)


def g_refresh(d, S, im):
    k = S / 128
    d.arc((22 * k, 22 * k, 106 * k, 106 * k), 30, 320, fill=W, width=int(14 * k))
    d.polygon([(96 * k, 14 * k), (110 * k, 52 * k), (74 * k, 46 * k)], fill=W)


def g_lock(d, S, im):
    k = S / 128
    d.rounded_rectangle((28 * k, 56 * k, 100 * k, 112 * k), 10 * k, fill=W)
    d.arc((40 * k, 18 * k, 88 * k, 74 * k), 180, 360, fill=W, width=int(12 * k))


def g_book(d, S, im):
    k = S / 128
    d.rounded_rectangle((20 * k, 24 * k, 62 * k, 104 * k), 6 * k, fill=W)
    d.rounded_rectangle((66 * k, 24 * k, 108 * k, 104 * k), 6 * k, fill=W)


def g_x(d, S, im):
    k = S / 128
    d.line((34 * k, 34 * k, 94 * k, 94 * k), fill=W, width=int(16 * k))
    d.line((94 * k, 34 * k, 34 * k, 94 * k), fill=W, width=int(16 * k))


def g_full(d, S, im):
    k = S / 128
    for (x, y, dx, dy) in [(24, 24, 1, 1), (104, 24, -1, 1), (24, 104, 1, -1), (104, 104, -1, -1)]:
        d.line((x * k, y * k, (x + 28 * dx) * k, y * k), fill=W, width=int(12 * k))
        d.line((x * k, y * k, x * k, (y + 28 * dy) * k), fill=W, width=int(12 * k))


def g_play(d, S, im):
    k = S / 128
    d.polygon([(40 * k, 22 * k), (104 * k, 64 * k), (40 * k, 106 * k)], fill=W)


def g_menu(d, S, im):
    k = S / 128
    for y in (34, 64, 94):
        d.rounded_rectangle((22 * k, (y - 7) * k, 106 * k, (y + 7) * k), 7 * k, fill=W)


def g_zoom(d, S, im):
    k = S / 128
    d.ellipse((18 * k, 18 * k, 86 * k, 86 * k), outline=W, width=int(12 * k))
    d.line((78 * k, 78 * k, 110 * k, 110 * k), fill=W, width=int(16 * k))


def g_info(d, S, im):
    k = S / 128
    d.ellipse((54 * k, 18 * k, 74 * k, 38 * k), fill=W)
    d.rounded_rectangle((55 * k, 50 * k, 73 * k, 110 * k), 6 * k, fill=W)


def g_talk(d, S, im):
    # 대화 — 둥근 말풍선 + 아래 왼쪽 꼬리, 속에 점 셋을 비운다(이벤트 「대화로 이어지는 선택지」 표 — 작은 「i」 는 「!」 로 읽혔다)
    k = S / 128
    d.rounded_rectangle((14 * k, 20 * k, 114 * k, 90 * k), 26 * k, fill=W)
    d.polygon([(34 * k, 84 * k), (60 * k, 86 * k), (28 * k, 112 * k)], fill=W)
    c = (0, 0, 0, 0)
    for x in (40, 64, 88):
        d.ellipse(((x - 8) * k, 47 * k, (x + 8) * k, 63 * k), fill=c)


def g_bless(d, S, im):
    # 축복 — 위에 후광 고리, 아래 반짝임(네 갈래 별)
    k = S / 128
    d.ellipse((30 * k, 10 * k, 98 * k, 38 * k), outline=W, width=int(9 * k))
    pts = []
    for i in range(8):
        a = math.pi * i / 4 - math.pi / 2
        r = (46 if i % 2 == 0 else 12) * k
        pts.append((64 * k + r * math.cos(a), 78 * k + r * math.sin(a)))
    d.polygon(pts, fill=W)


def g_copy(d, S, im):
    # 복제 — 겹친 카드 두 장(뒤는 테두리, 앞은 속을 채우고 가운데 「+」 를 비운다)
    k = S / 128
    d.rounded_rectangle((18 * k, 14 * k, 78 * k, 94 * k), 9 * k, outline=W, width=int(9 * k))
    d.rounded_rectangle((50 * k, 34 * k, 110 * k, 114 * k), 9 * k, fill=W)
    c = (0, 0, 0, 0)
    d.rectangle((75 * k, 52 * k, 85 * k, 96 * k), fill=c)
    d.rectangle((58 * k, 69 * k, 102 * k, 79 * k), fill=c)


for n, f in [("menu", g_menu), ("zoom", g_zoom), ("info", g_info), ("swords", g_swords), ("skull", g_skull), ("crown", g_crown), ("question", g_q), ("fire", g_fire), ("bag", g_bag),
             ("flag", g_flag), ("check", g_check), ("heart", g_heart), ("deck", g_deck), ("cog", g_cog), ("back", g_back),
             ("plus", g_plus), ("sword", g_sword), ("shield", g_shield), ("ring_slot", g_ring), ("moon", g_moon), ("spark", g_spark),
             ("trash", g_trash), ("refresh", g_refresh), ("lock", g_lock), ("book", g_book), ("x", g_x), ("full", g_full), ("play", g_play),
             ("bless", g_bless), ("copy", g_copy), ("talk", g_talk)]:
    glyph("ic_" + n + ".png", f)

# 위 · 아래 띠(배경 위 글을 읽히게) — 세로 그라데이션
g = Image.new("RGBA", (4, 256))
for y in range(256):
    for x in range(4):
        g.putpixel((x, y), (0, 0, 0, int(255 * (1 - y / 255) ** 1.6)))
g.save(os.path.join(OUT, "fade_top.png"))
# 키 큰 카드의 아래 어둠(초상 위 이름 칸)
g = Image.new("RGBA", (4, 256))
for y in range(256):
    for x in range(4):
        g.putpixel((x, y), (8, 6, 14, int(255 * min(1, max(0, (y / 255 - 0.35) / 0.55)) ** 1.2)))
g.save(os.path.join(OUT, "fade_card.png"))

# 지도 길(점선의 점) · 길 빛
im = canvas(32, 32)
ImageDraw.Draw(im).ellipse((4 * SS, 4 * SS, 28 * SS, 28 * SS), fill=(255, 255, 255, 255))
save(im, "dot.png")
print("ok", len(os.listdir(OUT)))
