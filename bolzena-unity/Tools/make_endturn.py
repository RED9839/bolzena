# 턴 종료 단추 그림 — 코드로 직접 그린다(원작 · 카제나 그림 없음). 4배로 그려 줄여 가장자리를 매끈하게.
# python Tools/make_endturn.py  ->  Assets/Bolzena/Resources/UI/EndTurn/*.png
#
#   메달(기본 안 A · 모래시계 안 B 가 같이 쓴다) — 반지름 1 = 그림 반폭
#     disc      남색 원판(가운데 밝고 가장자리 어둡게 · 위쪽 윤기)
#     rim       금속 금 테(빗면 명암 · 안팎 가는 어두운 선 · 네 귀 징)
#     dial      안쪽 눈금 고리(흰색 — 코드가 성격 색으로 물들인다)
#     gem       테 꼭대기 작은 마름모 보석(흰색 — 성격 색)
#     arrow     「다음 턴」 — 시계 방향 고리 화살 + 가운데 ▶ (흰색 + 구운 그림자)
#     hourglass 모래시계(흰색 + 구운 그림자)
#     halo      테 바깥 빛 고리(가산 — 맥동)
#     sweep     테를 도는 빛 꼬리(가산 — 끝내라는 신호 · 자동 전투)
#     tag       아래 이름표 판(남색 + 금 테)
#   알약(안 C)
#     pill      남색 알약 몸통 + 금 테
#     pill_line 안쪽 가는 선(흰색 — 성격 색)
#     chev      ▶▶ 겹 화살(흰색 + 구운 그림자)
import math, os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

OUT = r"C:\projects\bolzena-unity\Assets\Bolzena\Resources\UI\EndTurn"
os.makedirs(OUT, exist_ok=True)
SS = 4


def save(arr, name, size):
    """arr — float RGBA (H, W, 4) 0..1 를 size 로 줄여 저장."""
    a = np.clip(arr, 0, 1)
    # 미리 곱한 알파로 줄여야 가장자리에 검은 테가 안 낀다
    pm = a.copy()
    pm[..., :3] *= pm[..., 3:4]
    im = Image.fromarray((pm * 255 + 0.5).astype(np.uint8), "RGBA").resize(size, Image.LANCZOS)
    p = np.asarray(im).astype(np.float32) / 255
    al = p[..., 3:4]
    rgb = np.where(al > 1e-4, p[..., :3] / np.maximum(al, 1e-4), 0)
    out = np.concatenate([np.clip(rgb, 0, 1), al], -1)
    Image.fromarray((out * 255 + 0.5).astype(np.uint8), "RGBA").save(os.path.join(OUT, name + ".png"))
    print("  ", name, size)


def grid(n, extent=1.0):
    """n×n 격자 — x 오른쪽 · y 위쪽, [-extent, extent]."""
    c = (np.arange(n) + 0.5) / n * 2 - 1
    x, y = np.meshgrid(c * extent, -c * extent)
    return x, y


def smooth(edge0, edge1, v):
    t = np.clip((v - edge0) / (edge1 - edge0), 0, 1)
    return t * t * (3 - 2 * t)


def band(r, r0, r1, aa):
    return smooth(r0 - aa, r0 + aa, r) * (1 - smooth(r1 - aa, r1 + aa, r))


def rgba(h, w):
    return np.zeros((h, w, 4), np.float32)


def over(dst, rgb, alpha):
    """dst 위에 색 rgb(3 또는 H×W×3)를 alpha(H×W) 로 얹는다."""
    rgb = np.broadcast_to(np.asarray(rgb, np.float32), dst[..., :3].shape)
    a = alpha[..., None]
    da = dst[..., 3:4]
    oa = a + da * (1 - a)
    dst[..., :3] = np.where(oa > 1e-5, (rgb * a + dst[..., :3] * da * (1 - a)) / np.maximum(oa, 1e-5), 0)
    dst[..., 3:4] = oa


def lerp3(a, b, t):
    a = np.asarray(a, np.float32); b = np.asarray(b, np.float32)
    return a + (b - a) * t[..., None]


GOLD_DARK = (0.42, 0.27, 0.09)
GOLD_MID = (0.80, 0.60, 0.27)
GOLD_LIGHT = (1.00, 0.93, 0.68)
NAVY_C = (0.13, 0.18, 0.33)
NAVY_E = (0.035, 0.05, 0.12)

N = 512 * SS
AA = 2.0 / N   # 반 픽셀쯤(격자 단위 — 범위 2 / N)

# ── 메달 ──
x, y = grid(N)
r = np.hypot(x, y)
ang = np.arctan2(y, x)

# 원판 — 테 안쪽까지(0.86)
disc = rgba(N, N)
t = np.clip(r / 0.86, 0, 1) ** 1.6
col = lerp3(NAVY_C, NAVY_E, t)
# 위쪽 윤기 — 위로 치우친 타원
sheen = np.clip(1 - np.hypot(x / 0.75, (y - 0.42) / 0.38), 0, 1) ** 2 * 0.22
col = col + sheen[..., None] * np.array([0.55, 0.65, 0.9], np.float32)
# 가장자리 안쪽 그늘(테 밑 그림자)
col = col * (1 - 0.45 * smooth(0.66, 0.86, r))[..., None]
over(disc, col, 1 - smooth(0.86 - AA, 0.86 + AA, r))
save(disc, "disc", (512, 512))

# 금 테 — 0.80 ~ 0.97, 빗면 명암(빛은 왼쪽 위)
rim = rgba(N, N)
R0, R1 = 0.80, 0.97
tt = np.clip((r - R0) / (R1 - R0), 0, 1)            # 0 안쪽 · 1 바깥
nr = np.sin((tt - 0.5) * math.pi)                   # 볼록한 빗면 — 안쪽은 안을, 바깥은 밖을 본다
lx, ly = -0.55, 0.83
facing = (np.cos(ang) * lx + np.sin(ang) * ly)      # 테의 이 자리가 빛 쪽인가
shade = 0.5 + 0.42 * nr * facing + 0.12 * (1 - np.abs(nr))
spec = np.clip(nr * facing, 0, 1) ** 6 * 0.9
c1 = lerp3(GOLD_DARK, GOLD_MID, np.clip(shade * 1.4, 0, 1))
c2 = lerp3(GOLD_MID, GOLD_LIGHT, np.clip((shade - 0.55) * 2.4, 0, 1))
col = np.where((shade > 0.55)[..., None], c2, c1) + spec[..., None] * np.array([1, 0.95, 0.8], np.float32) * 0.5
# 결 — 가는 동심 선 몇 줄(금속 느낌)
col = col * (1 - 0.06 * (0.5 + 0.5 * np.sin(tt * 40)))[..., None]
over(rim, col, band(r, R0, R1, AA))
# 바깥 · 안쪽 어두운 가는 선(테를 판에서 떼어 낸다)
over(rim, (0.12, 0.07, 0.02), band(r, R1 - 0.004, R1 + 0.014, AA) * 0.9)
over(rim, (0.10, 0.06, 0.02), band(r, R0 - 0.014, R0 + 0.004, AA) * 0.85)
# 안쪽 밝은 실선(빗면 위 반사)
over(rim, GOLD_LIGHT, band(r, R0 + 0.022, R0 + 0.032, AA) * (0.35 + 0.4 * np.clip(-facing, 0, 1)))
# 네 귀 징 — 대각선 자리에 작은 둥근 금 단추
for a in (45, 135, 225, 315):
    cx, cy = math.cos(math.radians(a)) * 0.885, math.sin(math.radians(a)) * 0.885
    d = np.hypot(x - cx, y - cy)
    over(rim, (0.14, 0.08, 0.02), 1 - smooth(0.045 - AA, 0.045 + AA, d))
    sh = np.clip(0.55 + 0.6 * ((x - cx) * lx + (y - cy) * ly) / 0.04, 0, 1)
    over(rim, lerp3(GOLD_MID, GOLD_LIGHT, sh), 1 - smooth(0.034 - AA, 0.034 + AA, d))
save(rim, "rim", (512, 512))

# 눈금 고리(흰색 — 코드가 성격 색으로)
dial = rgba(N, N)
over(dial, (1, 1, 1), band(r, 0.745, 0.758, AA) * 0.9)
deg = (np.degrees(ang) + 360) % 360
for k in range(24):
    a0 = k * 15
    long = k % 6 == 0
    dd = np.abs(((deg - a0 + 180) % 360) - 180)       # 도 단위 거리
    w = 1.6 if long else 0.9
    rad_in = 0.665 if long else 0.70
    m = (1 - smooth(w - 0.25, w + 0.25, dd)) * band(r, rad_in, 0.745, AA)
    over(dial, (1, 1, 1), m * (1 if long else 0.65))
save(dial, "dial", (512, 512))

# 테 꼭대기 보석(마름모) — 흰색 + 위 밝은 면
G = 160 * SS
gx, gy = grid(G)
gem = rgba(G, G)
dia = np.abs(gx) + np.abs(gy)
over(gem, (0.05, 0.05, 0.08), 1 - smooth(0.92 - 4 / G, 0.92 + 4 / G, dia))
face = np.where(gy > 0, 1.0, 0.72) * np.where(gx < 0, 1.0, 0.86)
over(gem, np.stack([face] * 3, -1), 1 - smooth(0.74 - 4 / G, 0.74 + 4 / G, dia))
over(gem, (1, 1, 1), (1 - smooth(0.0, 0.18, np.hypot(gx + 0.22, gy - 0.25))) * 0.9)
save(gem, "gem", (160, 160))


def glyph_from_draw(draw_fn, size, shadow=(0.0, -0.035), blur=0.02):
    """PIL 로 흰 글리프를 그리고, 아래로 어두운 그림자를 구워 넣는다."""
    n = size * SS
    m = Image.new("L", (n, n), 0)
    draw_fn(ImageDraw.Draw(m), n)
    mask = np.asarray(m).astype(np.float32) / 255
    sm = Image.fromarray((mask * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(blur * n))
    sm = np.asarray(sm).astype(np.float32) / 255
    dx, dy = int(shadow[0] * n / 2), int(-shadow[1] * n / 2)
    sm = np.roll(np.roll(sm, dy, 0), dx, 1)
    g = rgba(n, n)
    over(g, (0, 0, 0), sm * 0.7)
    over(g, (1, 1, 1), mask)
    return g


def P(n, x, y):
    """[-1,1] 좌표(y 위) → 픽셀."""
    return ((x + 1) / 2 * n, (1 - y) / 2 * n)


def arc_poly(n, cx, cy, rad, a0, a1, steps=120):
    return [P(n, cx + rad * math.cos(math.radians(a0 + (a1 - a0) * i / steps)), cy + rad * math.sin(math.radians(a0 + (a1 - a0) * i / steps))) for i in range(steps + 1)]


def draw_arrow(d, n):
    # 시계 방향 고리 화살 — 위 오른쪽(110°)에서 시작해 290° 돌아 왼쪽 위(180°)에서 머리
    R, W = 0.50, 0.15
    a0, a1 = 112, 112 - 268
    outer = arc_poly(n, 0, 0, R + W / 2, a0, a1)
    inner = arc_poly(n, 0, 0, R - W / 2, a1, a0)
    d.polygon(outer + inner, fill=255)
    # 시작 끝은 둥글게
    sx, sy = R * math.cos(math.radians(a0)), R * math.sin(math.radians(a0))
    d.ellipse([P(n, sx - W / 2, sy + W / 2), P(n, sx + W / 2, sy - W / 2)], fill=255)
    # 머리 — 끝 자리에서 시계 방향 접선으로
    e = math.radians(a1)
    px, py = R * math.cos(e), R * math.sin(e)
    tx, ty = math.sin(e), -math.cos(e)               # 시계 방향 접선
    rx, ry = math.cos(e), math.sin(e)
    hw, hl = 0.21, 0.27
    tip = P(n, px + tx * hl, py + ty * hl)
    b1 = P(n, px + rx * hw - tx * 0.02, py + ry * hw - ty * 0.02)
    b2 = P(n, px - rx * hw - tx * 0.02, py - ry * hw - ty * 0.02)
    d.polygon([tip, b1, b2], fill=255)
    # 가운데 ▶(다음)
    s = 0.21
    d.polygon([P(n, -s * 0.62, s), P(n, -s * 0.62, -s), P(n, s * 0.95, 0)], fill=255)


def draw_hourglass(d, n):
    w, h, cap = 0.40, 0.56, 0.08
    # 위 · 아래 받침
    d.rounded_rectangle([P(n, -w - 0.08, h + cap), P(n, w + 0.08, h)], radius=int(0.03 * n), fill=255)
    d.rounded_rectangle([P(n, -w - 0.08, -h), P(n, w + 0.08, -h - cap)], radius=int(0.03 * n), fill=255)
    # 유리 — 테두리만(두께 t)
    t = 0.075
    outer = [P(n, -w, h), P(n, w, h), P(n, 0.07, 0.0), P(n, w, -h), P(n, -w, -h), P(n, -0.07, 0.0)]
    d.polygon(outer, fill=255)
    wi, hi = w - t * 1.25, h - t * 0.6
    inner = [P(n, -wi, hi), P(n, wi, hi), P(n, 0.02, 0.0), P(n, wi, -hi), P(n, -wi, -hi), P(n, -0.02, 0.0)]
    d.polygon(inner, fill=0)
    # 모래 — 위에 조금 · 아래에 더미 · 가운데 실
    d.polygon([P(n, -wi * 0.55, hi * 0.45), P(n, wi * 0.55, hi * 0.45), P(n, 0.0, 0.05)], fill=255)
    d.polygon([P(n, -wi + 0.01, -hi), P(n, wi - 0.01, -hi), P(n, 0.0, -hi * 0.42)], fill=255)
    d.rectangle([P(n, -0.012, 0.03), P(n, 0.012, -hi * 0.45)], fill=255)


def draw_chev(d, n):
    for ox in (-0.30, 0.22):
        s = 0.42
        d.polygon([P(n, ox - s * 0.55, s), P(n, ox - s * 0.05, s), P(n, ox + s * 0.75, 0), P(n, ox - s * 0.05, -s), P(n, ox - s * 0.55, -s), P(n, ox + s * 0.25, 0)], fill=255)


save(glyph_from_draw(draw_arrow, 384), "arrow", (384, 384))
save(glyph_from_draw(draw_hourglass, 384), "hourglass", (384, 384))
save(glyph_from_draw(draw_chev, 256, blur=0.03), "chev", (256, 256))

# 빛 고리(가산) — 그림 반폭 1.4 = 메달 반지름 1 기준으로 그린다(코드는 메달 지름 × 1.4 로 놓는다)
H = 384 * SS
hx, hy = grid(H, 1.4)
hr = np.hypot(hx, hy)
halo = rgba(H, H)
ga = np.exp(-((hr - 0.98) / 0.13) ** 2) * (hr > 0.8) + np.exp(-((hr - 0.98) / 0.05) ** 2) * 0.5
ga = ga * (1 - smooth(1.25, 1.4, hr))
over(halo, (1, 1, 1), np.clip(ga, 0, 1))
save(halo, "halo", (384, 384))

# 테를 도는 빛 꼬리(가산) — 머리는 위(90°), 꼬리는 시계 반대 방향 뒤로 110°
sweep = rgba(N, N)
behind = (np.degrees(ang) - 90) % 360                # 머리에서 반시계로 얼마나 뒤인가
tail = np.clip(1 - behind / 110, 0, 1) ** 2 * (behind < 110)
head = np.exp(-(np.minimum(behind, 360 - behind) / 6) ** 2)
prof = np.exp(-((r - 0.885) / 0.035) ** 2)
over(sweep, (1, 1, 1), np.clip((tail * 0.8 + head) * prof, 0, 1))
save(sweep, "sweep", (512, 512))


def capsule(xx, yy, hw, hh):
    """가로 알약의 부호 거리(안쪽 음수) — 반폭 hw · 반높이 hh."""
    qx = np.maximum(np.abs(xx) - (hw - hh), 0)
    return np.hypot(qx, yy) - hh


# 이름표 판 — 320×88, 좌우 끝 금 마름모
TW, TH = 320 * SS, 88 * SS
tx_ = (np.arange(TW) + 0.5) / TW * 2 - 1
ty_ = -((np.arange(TH) + 0.5) / TH * 2 - 1)
X, Y = np.meshgrid(tx_ * (TW / TH), ty_)            # 높이 기준 단위(반높이 1)
aa = 2.0 / TH
tag = rgba(TH, TW)
hwT = TW / TH - 0.08
sd = capsule(X, Y, hwT * 0.86, 0.74)
over(tag, lerp3((0.12, 0.16, 0.30), (0.05, 0.07, 0.15), np.clip((0.74 - Y) / 1.48, 0, 1)), (1 - smooth(-aa, aa, sd)) * 0.96)
over(tag, GOLD_MID, (1 - smooth(-aa, aa, np.abs(sd + 0.04) - 0.045)) * 0.95)
for sx in (-1, 1):
    dd = np.abs(X - sx * (hwT * 0.86 + 0.02)) + np.abs(Y)
    over(tag, (0.12, 0.07, 0.02), 1 - smooth(0.36 - aa, 0.36 + aa, dd))
    over(tag, lerp3(GOLD_MID, GOLD_LIGHT, np.clip(Y * 2 + 0.5, 0, 1)), 1 - smooth(0.26 - aa, 0.26 + aa, dd))
save(tag, "tag", (320, 88))

# 알약 — 760×232, 몸통 남색 · 금 빗면 테
PW, PH = 760 * SS, 232 * SS
px_ = (np.arange(PW) + 0.5) / PW * 2 - 1
py_ = -((np.arange(PH) + 0.5) / PH * 2 - 1)
X, Y = np.meshgrid(px_ * (PW / PH), py_)
aa = 2.0 / PH
pill = rgba(PH, PW)
hwP = PW / PH - 0.06
sd = capsule(X, Y, hwP, 0.94)
body = lerp3(NAVY_C, NAVY_E, np.clip((0.9 - Y) / 1.8, 0, 1) ** 0.8)
body = body + (np.clip(1 - np.hypot(X / (hwP * 0.9), (Y - 0.55) / 0.4), 0, 1) ** 2 * 0.18)[..., None] * np.array([0.55, 0.65, 0.9], np.float32)
over(pill, body, 1 - smooth(-aa, aa, sd))
# 테 — 바깥에서 0.14 안쪽까지, 위가 밝게
rt = np.clip(-sd / 0.14, 0, 1)
nr = np.sin((0.5 - rt) * math.pi)
shade = 0.52 + 0.45 * nr * np.clip(Y * 0.9 + 0.3, -1, 1)
c1 = lerp3(GOLD_DARK, GOLD_MID, np.clip(shade * 1.4, 0, 1))
c2 = lerp3(GOLD_MID, GOLD_LIGHT, np.clip((shade - 0.55) * 2.4, 0, 1))
col = np.where((shade > 0.55)[..., None], c2, c1)
over(pill, col, (1 - smooth(-aa, aa, sd)) * smooth(-0.14 - aa, -0.14 + aa, sd))
over(pill, (0.12, 0.07, 0.02), (1 - smooth(-aa, aa, np.abs(sd + 0.005) - 0.012)) * 0.9)
over(pill, (0.10, 0.06, 0.02), (1 - smooth(-aa, aa, np.abs(sd + 0.15) - 0.012)) * 0.8)
save(pill, "pill", (760, 232))

pline = rgba(PH, PW)
over(pline, (1, 1, 1), (1 - smooth(-aa, aa, np.abs(sd + 0.215) - 0.012)) * 0.9)
save(pline, "pill_line", (760, 232))
print("끝 —", OUT)
