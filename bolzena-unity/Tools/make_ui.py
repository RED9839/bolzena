# 화면 그림(카드 틀 · 구슬 · 아이콘 · 막대 …)을 코드로 그린다. 4배로 그려 줄여서 가장자리를 매끈하게.
# python Tools/make_ui.py  ->  Assets/Bolzena/Resources/UI/*.png
import math, os
from PIL import Image, ImageDraw, ImageFilter, ImageChops

OUT = r"C:\projects\bolzena-unity\Assets\Bolzena\Resources\UI"
os.makedirs(OUT, exist_ok=True)
SS = 4
# 내보내는 배율 — 논리 크기의 2배 픽셀로 저장한다(4K 에서도 늘어나 흐리지 않게). 가져오기는 9칸만 400px = 1 단위(테두리 굵기 그대로),
# 나머지는 크기를 코드가 월드 단위로 맞추므로(Make.Box) 배율과 무관하다.
UP = 2


def canvas(w, h):
    return Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))


def save(im, name, w=None, h=None):
    if w is None:
        w, h = im.size[0] // SS, im.size[1] // SS
    im.resize((w * UP, h * UP), Image.LANCZOS).save(os.path.join(OUT, name))


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


# ── 단색 · 부드러운 빛 · 그림자 ──
Image.new("RGBA", (8, 8), (255, 255, 255, 255)).save(os.path.join(OUT, "white.png"))


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
radial(128, 0.8, "softhard.png")

sh = Image.new("RGBA", (256, 96))
px = sh.load()
for y in range(96):
    for x in range(256):
        d = math.hypot((x - 127.5) / 127.5, (y - 47.5) / 47.5)
        px[x, y] = (0, 0, 0, int(255 * max(0, 1 - d) ** 1.2 * 0.8))
sh.save(os.path.join(OUT, "shadow.png"))

vg = Image.new("RGBA", (320, 180))
px = vg.load()
for y in range(180):
    for x in range(320):
        d = math.hypot((x - 159.5) / 159.5, (y - 89.5) / 89.5)
        a = min(1, max(0, (d - 0.55) / 0.6)) ** 1.6
        px[x, y] = (255, 255, 255, int(255 * a))
vg.save(os.path.join(OUT, "vignette.png"))

# ── 카드 틀 ── 380x540, 그림 창 · 이름 띠 · 설명 칸
CW, CH = 380, 540


# 남색 몸 + 얇은 테(웹판 czn.css 의 --glass 톤을 남색으로). 200px = 1 단위 —
#   그림 창 (16,16)-(364,282) 1.74×1.33 · 이름 띠 288~336 · 종류 줄 ~352 · 설명 칸 366~526 (CardView 의 글 자리와 짝)
#   금은 신탁 카드에만 — 보통 카드는 종류 빛(붉은 · 푸른) 테 하나, 띠 위 선은 흰 16%
def card(name, rim_top, rim_bot, accent, band_l, band_r):
    im = canvas(CW, CH)
    W, H = CW * SS, CH * SS
    R = 20 * SS
    paste_grad(im, (0, 0, W, H), rim_top, rim_bot, R)
    paste_grad(im, (3 * SS, 3 * SS, W - 3 * SS, H - 3 * SS), (24, 32, 58, 255), (10, 14, 28, 255), R - 2 * SS)
    d = ImageDraw.Draw(im)
    ax0, ay0, ax1, ay1 = 16 * SS, 16 * SS, W - 16 * SS, 282 * SS
    d.rounded_rectangle((ax0 - 2 * SS, ay0 - 2 * SS, ax1 + 2 * SS, ay1 + 2 * SS), 13 * SS, fill=accent + (120,))
    paste_grad(im, (ax0, ay0, ax1, ay1), (36, 46, 78, 255), (12, 16, 30, 255), 11 * SS)
    # 이름 띠 — 종류 빛이 왼쪽에서 번지는 남색 띠
    bw, bh = W - 16 * SS, 48 * SS
    row = Image.new("RGBA", (bw, 1))
    for x in range(bw):
        row.putpixel((x, 0), lerp(band_l, band_r, min(1.0, x / max(1, bw - 1) * 1.3)) + (255,))
    g = row.resize((bw, bh))
    im.alpha_composite(Image.composite(g, Image.new("RGBA", (bw, bh)), rr_mask(bw, bh, 6 * SS)), (8 * SS, 288 * SS))
    d = ImageDraw.Draw(im)
    d.line((14 * SS, 288 * SS, W - 14 * SS, 288 * SS), fill=(255, 255, 255, 40), width=2 * SS)
    paste_grad(im, (12 * SS, 366 * SS, W - 12 * SS, H - 14 * SS), (28, 37, 66, 255), (19, 26, 48, 255), 10 * SS)
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((12 * SS, 366 * SS, W - 12 * SS, H - 14 * SS), 10 * SS, outline=(255, 255, 255, 18), width=1 * SS)
    save(im, name)


card("card_attack.png", (236, 128, 132, 255), (120, 38, 54, 255), (226, 92, 108), (112, 34, 50), (22, 28, 50))
card("card_skill.png", (132, 186, 246, 255), (34, 70, 140, 255), (92, 160, 236), (32, 66, 124), (22, 28, 50))
card("card_epi.png", (255, 246, 205, 255), (214, 150, 46, 255), (246, 213, 142), (150, 108, 34), (40, 34, 30))

im = canvas(CW, CH)
ImageDraw.Draw(im).rounded_rectangle((0, 0, CW * SS - 1, CH * SS - 1), 26 * SS, fill=(255, 255, 255, 255))
save(im, "card_mask.png")

G = 60
gl = Image.new("L", (CW + 2 * G, CH + 2 * G), 0)
ImageDraw.Draw(gl).rounded_rectangle((G, G, G + CW, G + CH), 30, fill=255)
gl = gl.filter(ImageFilter.GaussianBlur(22))
inner = Image.new("L", gl.size, 0)
ImageDraw.Draw(inner).rounded_rectangle((G + 6, G + 6, G + CW - 6, G + CH - 6), 24, fill=255)
gl = ImageChops.subtract(gl, inner)
glow = Image.new("RGBA", gl.size, (255, 255, 255, 0))
glow.putalpha(gl)
glow.save(os.path.join(OUT, "card_glow.png"))


# ── 구슬(비용 · AP) ──
def orb(size, name, c0, c1, ring):
    im = canvas(size, size)
    S = size * SS
    d = ImageDraw.Draw(im)
    d.ellipse((0, 0, S - 1, S - 1), fill=ring + (255,))
    g = Image.new("RGBA", (S, S))
    gp = ImageDraw.Draw(g)
    for i in range(60):
        t = i / 59
        r = S * 0.43 * (1 - t) + 1
        cy = S / 2 - S * 0.06 * t
        gp.ellipse((S / 2 - r, cy - r, S / 2 + r, cy + r), fill=lerp(c1, c0, t) + (255,))
    m = Image.new("L", (S, S), 0)
    ImageDraw.Draw(m).ellipse((S * 0.07, S * 0.07, S * 0.93, S * 0.93), fill=255)
    im.paste(g, (0, 0), m)
    hl = Image.new("L", (S, S), 0)
    ImageDraw.Draw(hl).ellipse((S * 0.25, S * 0.12, S * 0.75, S * 0.42), fill=110)
    hl = hl.filter(ImageFilter.GaussianBlur(S * 0.03))
    white = Image.new("RGBA", (S, S), (255, 255, 255, 0))
    white.putalpha(hl)
    im.alpha_composite(white)
    save(im, name)


orb(128, "orb_cost.png", (70, 104, 180), (12, 18, 44), (246, 213, 142))
orb(200, "orb_ap.png", (255, 230, 140), (150, 60, 10), (255, 245, 210))
orb(200, "orb_ap_empty.png", (90, 90, 110), (25, 25, 35), (150, 140, 120))


# ── 막대(9칸) ──
def bar(name, fill, outline=None, r=10, w=96, h=32):
    im = canvas(w, h)
    ImageDraw.Draw(im).rounded_rectangle((0, 0, w * SS - 1, h * SS - 1), r * SS, fill=fill, outline=outline, width=2 * SS if outline else 0)
    save(im, name)


bar("bar_bg_9s.png", (10, 12, 20, 230), (255, 255, 255, 90))
bar("bar_fill_9s.png", (255, 255, 255, 255), None, 8)
# 판 — 톤.md 의 큰 판: 금 테 2(#ECCC88 → #A88042) · 남색 #16203C → #0C1224, 모서리 12. 툴팁 · 창이 쓴다(뒤가 비치지 않게 거의 불투명)
im = canvas(96, 96)
W9 = 96 * SS
paste_grad(im, (0, 0, W9, W9), (236, 204, 136, 255), (168, 128, 66, 255), 12 * SS)
paste_grad(im, (2 * SS, 2 * SS, W9 - 2 * SS, W9 - 2 * SS), (22, 32, 60, 252), (12, 18, 36, 252), 10 * SS)
save(im, "panel_9s.png")
# 칸 — 톤.md 의 칸: 흰 18% 테 2 · #1F2A4A → #131B32, 모서리 10(판 속 묶음 · 탭 · 칩 줄)
im = canvas(96, 96)
paste_grad(im, (0, 0, W9, W9), (255, 255, 255, 52), (255, 255, 255, 34), 10 * SS)
paste_grad(im, (2 * SS, 2 * SS, W9 - 2 * SS, W9 - 2 * SS), (31, 42, 74, 255), (19, 27, 50, 255), 8 * SS)
save(im, "cell_9s.png")
# 고른 칸 — 금 테 3
im = canvas(96, 96)
paste_grad(im, (0, 0, W9, W9), (242, 207, 122, 255), (217, 164, 65, 255), 10 * SS)
paste_grad(im, (3 * SS, 3 * SS, W9 - 3 * SS, W9 - 3 * SS), (36, 48, 84, 255), (22, 31, 56, 255), 7 * SS)
save(im, "cell_on_9s.png")
# 알약 — 금(주 동작) · 남색(보조). 끝까지 둥글게(높이 64 → 반지름 32)
im = canvas(160, 64)
PW, PH = 160 * SS, 64 * SS
paste_grad(im, (0, 0, PW, PH), (255, 246, 214, 255), (170, 112, 38, 255), 32 * SS)
paste_grad(im, (2 * SS, 2 * SS, PW - 2 * SS, PH - 2 * SS), (255, 231, 160, 255), (233, 172, 72, 255), 30 * SS)
save(im, "pill_gold_9s.png")
im = canvas(160, 64)
paste_grad(im, (0, 0, PW, PH), (236, 204, 136, 255), (168, 128, 66, 255), 32 * SS)
paste_grad(im, (2 * SS, 2 * SS, PW - 2 * SS, PH - 2 * SS), (32, 43, 74, 255), (18, 25, 46, 255), 30 * SS)
save(im, "pill_dark_9s.png")
bar("btn_9s.png", (255, 255, 255, 255), None, 18, 96, 96)


# ── 아이콘 ──
def icon(name, draw_fn, size=128):
    im = canvas(size, size)
    draw_fn(ImageDraw.Draw(im), size * SS)
    a = im.split()[3].filter(ImageFilter.MaxFilter(4 * SS + 1))
    back = Image.new("RGBA", im.size, (16, 12, 20, 0))
    back.putalpha(a)
    back.alpha_composite(im)
    save(back, name)


def sword(d, S):
    k = S / 128
    d.polygon([(64 * k, 8 * k), (76 * k, 22 * k), (72 * k, 84 * k), (56 * k, 84 * k), (52 * k, 22 * k)], fill=(235, 240, 255, 255))
    d.polygon([(64 * k, 8 * k), (76 * k, 22 * k), (72 * k, 84 * k), (64 * k, 84 * k)], fill=(180, 190, 215, 255))
    d.rounded_rectangle((34 * k, 84 * k, 94 * k, 96 * k), 5 * k, fill=(255, 200, 80, 255))
    d.rounded_rectangle((58 * k, 96 * k, 70 * k, 116 * k), 3 * k, fill=(140, 80, 40, 255))
    d.ellipse((56 * k, 112 * k, 72 * k, 126 * k), fill=(255, 200, 80, 255))


def shield(d, S):
    k = S / 128
    d.polygon([(64 * k, 10 * k), (110 * k, 26 * k), (104 * k, 74 * k), (64 * k, 118 * k), (24 * k, 74 * k), (18 * k, 26 * k)], fill=(120, 190, 255, 255))
    d.polygon([(64 * k, 24 * k), (96 * k, 35 * k), (91 * k, 70 * k), (64 * k, 102 * k)], fill=(70, 130, 220, 255))


def crack(d, S):
    k = S / 128
    d.polygon([(20 * k, 14 * k), (70 * k, 50 * k), (50 * k, 62 * k), (110 * k, 116 * k), (40 * k, 70 * k), (60 * k, 58 * k)], fill=(255, 220, 90, 255))


def down(d, S):
    k = S / 128
    d.polygon([(40 * k, 14 * k), (88 * k, 14 * k), (88 * k, 64 * k), (112 * k, 64 * k), (64 * k, 116 * k), (16 * k, 64 * k), (40 * k, 64 * k)], fill=(190, 120, 255, 255))


def vuln(d, S):
    k = S / 128
    d.polygon([(64 * k, 10 * k), (110 * k, 26 * k), (104 * k, 74 * k), (64 * k, 118 * k), (24 * k, 74 * k), (18 * k, 26 * k)], fill=(255, 110, 90, 255))
    d.line([(70 * k, 12 * k), (52 * k, 50 * k), (76 * k, 66 * k), (58 * k, 116 * k)], fill=(40, 10, 20, 255), width=int(8 * k))


def star(d, S):
    k = S / 128
    pts = []
    for i in range(8):
        a = math.pi * i / 4 - math.pi / 2
        r = (58 if i % 2 == 0 else 16) * k
        pts.append((64 * k + r * math.cos(a), 64 * k + r * math.sin(a)))
    d.polygon(pts, fill=(255, 240, 160, 255))


icon("ic_sword.png", sword)
icon("ic_shield.png", shield)
icon("ic_break.png", crack)
icon("ic_weak.png", down)
icon("ic_vuln.png", vuln)
icon("ic_star.png", star)


def up(d, S):
    k = S / 128
    d.polygon([(40 * k, 114 * k), (88 * k, 114 * k), (88 * k, 64 * k), (112 * k, 64 * k), (64 * k, 12 * k), (16 * k, 64 * k), (40 * k, 64 * k)], fill=(120, 230, 140, 255))


def bolt(d, S):
    k = S / 128
    d.polygon([(76 * k, 6 * k), (26 * k, 72 * k), (60 * k, 72 * k), (48 * k, 122 * k), (102 * k, 52 * k), (68 * k, 52 * k)], fill=(255, 220, 70, 255))


def info(d, S):
    k = S / 128
    d.ellipse((8 * k, 8 * k, 120 * k, 120 * k), fill=(235, 225, 200, 255))
    d.ellipse((56 * k, 26 * k, 72 * k, 42 * k), fill=(40, 30, 30, 255))
    d.rounded_rectangle((56 * k, 52 * k, 72 * k, 100 * k), 4 * k, fill=(40, 30, 30, 255))


def pause(d, S):
    k = S / 128
    d.rounded_rectangle((30 * k, 22 * k, 54 * k, 106 * k), 6 * k, fill=(240, 232, 210, 255))
    d.rounded_rectangle((74 * k, 22 * k, 98 * k, 106 * k), 6 * k, fill=(240, 232, 210, 255))


def gear(d, S):
    k = S / 128
    for i in range(8):
        a = math.pi * i / 4
        cx, cy = 64 * k + math.cos(a) * 44 * k, 64 * k + math.sin(a) * 44 * k
        d.ellipse((cx - 14 * k, cy - 14 * k, cx + 14 * k, cy + 14 * k), fill=(240, 232, 210, 255))
    d.ellipse((22 * k, 22 * k, 106 * k, 106 * k), fill=(240, 232, 210, 255))
    d.ellipse((46 * k, 46 * k, 82 * k, 82 * k), fill=(40, 34, 44, 255))


def cards(d, S):
    k = S / 128
    d.rounded_rectangle((18 * k, 26 * k, 82 * k, 112 * k), 8 * k, fill=(150, 160, 200, 255), outline=(40, 40, 60, 255), width=int(4 * k))
    d.rounded_rectangle((44 * k, 14 * k, 108 * k, 100 * k), 8 * k, fill=(235, 225, 200, 255), outline=(40, 40, 60, 255), width=int(4 * k))


def skull(d, S):
    k = S / 128
    d.ellipse((22 * k, 14 * k, 106 * k, 92 * k), fill=(220, 210, 230, 255))
    d.rounded_rectangle((40 * k, 80 * k, 88 * k, 112 * k), 6 * k, fill=(220, 210, 230, 255))
    d.ellipse((38 * k, 44 * k, 58 * k, 64 * k), fill=(40, 20, 40, 255))
    d.ellipse((70 * k, 44 * k, 90 * k, 64 * k), fill=(40, 20, 40, 255))


icon("ic_up.png", up)
icon("ic_bolt.png", bolt)
icon("ic_info.png", info)
icon("ic_pause.png", pause)
icon("ic_gear.png", gear)
icon("ic_cards.png", cards)
icon("ic_skull.png", skull)


def pip(name, fill):
    im = canvas(40, 40)
    S = 40 * SS
    ImageDraw.Draw(im).polygon([(S / 2, 2 * SS), (S - 2 * SS, S / 2), (S / 2, S - 2 * SS), (2 * SS, S / 2)], fill=fill, outline=(20, 16, 10, 255), width=3 * SS)
    save(im, name)


pip("pip_on.png", (255, 214, 90, 255))
pip("pip_off.png", (60, 60, 70, 255))

# ── 화살표(대상 지정) ──
im = canvas(64, 64)
ImageDraw.Draw(im).polygon([(8 * SS, 8 * SS), (40 * SS, 32 * SS), (8 * SS, 56 * SS), (22 * SS, 32 * SS)], fill=(255, 255, 255, 255))
save(im, "chevron.png")
im = canvas(128, 128)
ImageDraw.Draw(im).polygon([(10 * SS, 10 * SS), (120 * SS, 64 * SS), (10 * SS, 118 * SS), (36 * SS, 64 * SS)], fill=(255, 255, 255, 255))
save(im, "arrowhead.png")

im = canvas(256, 256)
S = 256 * SS
d = ImageDraw.Draw(im)
d.ellipse((20 * SS, 20 * SS, S - 20 * SS, S - 20 * SS), outline=(255, 255, 255, 255), width=8 * SS)
d.ellipse((70 * SS, 70 * SS, S - 70 * SS, S - 70 * SS), outline=(255, 255, 255, 170), width=4 * SS)
for a in range(4):
    ang = a * math.pi / 2 + math.pi / 4
    cx = cy = S / 2
    d.line((cx + math.cos(ang) * 96 * SS, cy + math.sin(ang) * 96 * SS, cx + math.cos(ang) * 126 * SS, cy + math.sin(ang) * 126 * SS), fill=(255, 255, 255, 255), width=10 * SS)
save(im, "reticle.png")

im = canvas(300, 110)
W, H = 300 * SS, 110 * SS
paste_grad(im, (0, 0, W, H), (255, 236, 170, 255), (190, 110, 30, 255), 26 * SS)
paste_grad(im, (6 * SS, 6 * SS, W - 6 * SS, H - 6 * SS), (90, 50, 20, 255), (40, 20, 10, 255), 22 * SS)
save(im, "btn_end.png")

im = canvas(160, 160)
S = 160 * SS
d = ImageDraw.Draw(im)
d.ellipse((0, 0, S - 1, S - 1), fill=(255, 226, 150, 255))
d.ellipse((7 * SS, 7 * SS, S - 7 * SS, S - 7 * SS), fill=(20, 22, 34, 255))
save(im, "portrait_frame.png")
im = canvas(160, 160)
ImageDraw.Draw(im).ellipse((7 * SS, 7 * SS, 160 * SS - 7 * SS, 160 * SS - 7 * SS), fill=(255, 255, 255, 255))
save(im, "circle.png")

# ── 전투 HUD(카제나식 배치 · 우리 그림) ──
# 왼쪽 위 · 아래 무리 뒤에 까는 남색 그늘 — 왼쪽이 짙고 오른쪽 · 위아래로 사라진다(상자 대신)
fw, fh = 256, 128
fade = Image.new("RGBA", (fw, fh))
px = fade.load()
for y in range(fh):
    vy = 1 - abs(y - (fh - 1) / 2) / ((fh - 1) / 2)
    vy = min(1.0, vy * 2.2)
    for x in range(fw):
        hx = 1 - x / (fw - 1)
        a = (hx ** 1.25) * (vy ** 1.2)
        px[x, y] = (8, 12, 26, int(235 * a))
fade.save(os.path.join(OUT, "hud_fade.png"))

# 마름모(채움 · 테) — 적 즉시 행동 · 예고 · AP
im = canvas(128, 128)
S = 128 * SS
ImageDraw.Draw(im).polygon([(S / 2, 3 * SS), (S - 3 * SS, S / 2), (S / 2, S - 3 * SS), (3 * SS, S / 2)], fill=(255, 255, 255, 255))
save(im, "diamond.png")
im = canvas(128, 128)
d = ImageDraw.Draw(im)
d.polygon([(S / 2, 2 * SS), (S - 2 * SS, S / 2), (S / 2, S - 2 * SS), (2 * SS, S / 2)], fill=(255, 255, 255, 255))
d.polygon([(S / 2, 12 * SS), (S - 12 * SS, S / 2), (S / 2, S - 12 * SS), (12 * SS, S / 2)], fill=(0, 0, 0, 0))
save(im, "diamond_rim.png")

# 둥근 단추 — 남색 + 금테(턴 종료 · 오른쪽 위 단추)
im = canvas(160, 160)
S = 160 * SS
d = ImageDraw.Draw(im)
d.ellipse((0, 0, S - 1, S - 1), fill=(243, 212, 140, 255))
d.ellipse((5 * SS, 5 * SS, S - 5 * SS, S - 5 * SS), fill=(14, 20, 40, 255))
g = Image.new("RGBA", (S, S))
gp = ImageDraw.Draw(g)
for i in range(40):
    t = i / 39
    r = S * 0.46 * (1 - t) + 1
    cy = S / 2 - S * 0.12 * t
    gp.ellipse((S / 2 - r, cy - r, S / 2 + r, cy + r), fill=lerp((14, 20, 40), (40, 56, 100), t) + (255,))
m = Image.new("L", (S, S), 0)
ImageDraw.Draw(m).ellipse((5 * SS, 5 * SS, S - 5 * SS, S - 5 * SS), fill=255)
im.paste(g, (0, 0), m)
d = ImageDraw.Draw(im)
d.ellipse((11 * SS, 11 * SS, S - 11 * SS, S - 11 * SS), outline=(243, 212, 140, 90), width=2 * SS)
save(im, "btn_round.png")

# 띠 — 가운데가 짙고 양 끝으로 사라지는 남색(턴 · 승리 · 패배)
bw, bh = 512, 64
band = Image.new("RGBA", (bw, bh))
px = band.load()
for x in range(bw):
    hx = 1 - abs(x - (bw - 1) / 2) / ((bw - 1) / 2)
    a = min(1.0, hx * 1.8) ** 1.4
    for y in range(bh):
        px[x, y] = (8, 12, 28, int(225 * a))
band.save(os.path.join(OUT, "band.png"))
line = Image.new("RGBA", (bw, 4))
px = line.load()
for x in range(bw):
    hx = 1 - abs(x - (bw - 1) / 2) / ((bw - 1) / 2)
    a = min(1.0, hx * 1.6) ** 1.6
    for y in range(4):
        px[x, y] = (255, 255, 255, int(255 * a))
line.save(os.path.join(OUT, "band_line.png"))
print("ok")

# 세로 그늘 — 아래가 짙고 위로 사라진다(큰 카드 글 밑 · 위 그늘). 위아래 뒤집어 쓴다
gv = Image.new("RGBA", (8, 128))
px = gv.load()
for y in range(128):
    t = y / 127
    for x in range(8):
        px[x, y] = (255, 255, 255, int(255 * (t ** 1.6)))
gv.save(os.path.join(OUT, "grad_v.png"))
print("grad ok")


# 더미 보기 탭 — 뽑을(카드 + 위 화살표) · 버린(카드 + 아래 화살표) · 닫기(×)
def pile_up(d, S):
    k = S / 128
    d.rounded_rectangle((30 * k, 14 * k, 98 * k, 114 * k), 10 * k, outline=(240, 236, 226, 255), width=int(8 * k))
    d.polygon([(64 * k, 34 * k), (86 * k, 62 * k), (72 * k, 62 * k), (72 * k, 90 * k), (56 * k, 90 * k), (56 * k, 62 * k), (42 * k, 62 * k)], fill=(240, 236, 226, 255))


def pile_down(d, S):
    k = S / 128
    d.rounded_rectangle((30 * k, 14 * k, 98 * k, 114 * k), 10 * k, outline=(240, 236, 226, 255), width=int(8 * k))
    d.polygon([(64 * k, 94 * k), (86 * k, 66 * k), (72 * k, 66 * k), (72 * k, 38 * k), (56 * k, 38 * k), (56 * k, 66 * k), (42 * k, 66 * k)], fill=(240, 236, 226, 255))


def close_x(d, S):
    k = S / 128
    d.line((22 * k, 22 * k, 106 * k, 106 * k), fill=(240, 236, 226, 255), width=int(9 * k))
    d.line((106 * k, 22 * k, 22 * k, 106 * k), fill=(240, 236, 226, 255), width=int(9 * k))


icon("ic_pile_up.png", pile_up)
icon("ic_pile_down.png", pile_down)
icon("ic_close.png", close_x)
print("pile icons ok")


# 비스듬한 띠 조각(초상 띠 · 고학년 띠) — 평행사변형, 왼쪽 아래 · 오른쪽 위가 기운다. 마스크 · 테로 쓴다
im = canvas(280, 80)
d = ImageDraw.Draw(im)
d.polygon([(22 * SS, 0), (280 * SS - 1, 0), (258 * SS, 80 * SS - 1), (0, 80 * SS - 1)], fill=(255, 255, 255, 255))
save(im, "slant.png")


def menu(d, S):
    k = S / 128
    for y in (34, 64, 94):
        d.rounded_rectangle((20 * k, (y - 6) * k, 108 * k, (y + 6) * k), 5 * k, fill=(240, 236, 226, 255))


def check(d, S):
    k = S / 128
    d.line((22 * k, 66 * k, 52 * k, 96 * k), fill=(240, 236, 226, 255), width=int(14 * k))
    d.line((48 * k, 98 * k, 108 * k, 32 * k), fill=(240, 236, 226, 255), width=int(14 * k))


def spark(d, S):
    k = S / 128
    d.polygon([(64 * k, 6 * k), (76 * k, 52 * k), (122 * k, 64 * k), (76 * k, 76 * k), (64 * k, 122 * k), (52 * k, 76 * k), (6 * k, 64 * k), (52 * k, 52 * k)], fill=(255, 236, 170, 255))


icon("ic_menu.png", menu)
icon("ic_check.png", check)
icon("ic_spark.png", spark)
print("slant ok")


def clock(d, S):
    k = S / 128
    d.ellipse((14 * k, 14 * k, 114 * k, 114 * k), outline=(240, 236, 226, 255), width=int(9 * k))
    d.line((64 * k, 64 * k, 64 * k, 32 * k), fill=(240, 236, 226, 255), width=int(9 * k))
    d.line((64 * k, 64 * k, 88 * k, 76 * k), fill=(240, 236, 226, 255), width=int(9 * k))


def auto(d, S):
    k = S / 128
    d.arc((16 * k, 16 * k, 112 * k, 112 * k), 200, 520, fill=(240, 236, 226, 255), width=int(9 * k))
    d.polygon([(98 * k, 14 * k), (112 * k, 46 * k), (80 * k, 44 * k)], fill=(240, 236, 226, 255))
    d.polygon([(52 * k, 42 * k), (52 * k, 86 * k), (88 * k, 64 * k)], fill=(240, 236, 226, 255))


def party(d, S):
    k = S / 128
    for cx, cy, r in ((40, 52, 15), (88, 52, 15), (64, 40, 18)):
        d.ellipse(((cx - r) * k, (cy - r) * k, (cx + r) * k, (cy + r) * k), fill=(240, 236, 226, 255))
    d.rounded_rectangle((20 * k, 72 * k, 60 * k, 112 * k), 14 * k, fill=(240, 236, 226, 255))
    d.rounded_rectangle((68 * k, 72 * k, 108 * k, 112 * k), 14 * k, fill=(240, 236, 226, 255))
    d.rounded_rectangle((42 * k, 64 * k, 86 * k, 116 * k), 16 * k, fill=(240, 236, 226, 255))


icon("ic_clock.png", clock)
icon("ic_auto.png", auto)
icon("ic_party.png", party)
print("more icons ok")
