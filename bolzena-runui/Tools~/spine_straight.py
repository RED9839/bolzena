# 원작 스파인 그림(PMA — atlas 의 pma:true)을 곧은 알파로 푼다 — 유니티 Linear 색 공간에서 바래지 않게(재질은 Straight Alpha · Resources/RunUI/SpineStraight.mat).
#   그냥 rgb / a 로 나누면 두 가지 테두리가 생긴다(2026-10 사용자 제보 「흰 테두리 · 번짐」):
#     · 알파가 아주 낮은 가장자리 픽셀은 8비트 양자화 오차가 수십 배로 부풀어 원색(빨강 · 흰색) 점이 된다 → 이중선형 걸름이 이웃으로 번진다
#     · a = 0 픽셀은 색이 0(검정)이라 가장자리를 걸를 때 어둡게 섞인다
#   그래서: a ≥ T 는 rgb / a 그대로, 0 < a < T 는 rgb / a 와 「이웃의 믿을 만한 색(번짐 · bleed)」을 a / T 비율로 섞고,
#   a = 0 은 이웃 색으로 채운다(알파는 그대로 0) — Spine 의 「Bleed」 내보내기와 같은 일.
#   python "Tools~/spine_straight.py" <원본 png> <대상 png>   (copy_assets.py 가 불러 쓴다)
import sys
import numpy as np
from PIL import Image

T = 24          # 이 알파 아래는 색을 믿지 않는다
GROW = 6        # 번짐을 몇 픽셀까지 넓히나(이중선형 · 축소 걸름에 넉넉히)


def _shift(x, dy, dx):
    out = np.zeros_like(x)
    h, w = x.shape[:2]
    ys, yd = (slice(dy, h), slice(0, h - dy)) if dy >= 0 else (slice(0, h + dy), slice(-dy, h))
    xs, xd = (slice(dx, w), slice(0, w - dx)) if dx >= 0 else (slice(0, w + dx), slice(-dx, w))
    out[yd, xd] = x[ys, xs]
    return out


def to_straight(arr):
    """arr: HxWx4 uint8 PMA → HxWx4 uint8 곧은 알파(번짐 채움)."""
    im = arr.astype(np.float32)
    a = im[..., 3]
    rgb = im[..., :3]
    raw = np.where(a[..., None] > 0, np.clip(rgb * 255.0 / np.maximum(a[..., None], 1), 0, 255), 0)
    known = a >= T
    col = np.where(known[..., None], raw, 0).astype(np.float32)
    have = known.astype(np.float32)
    bleed = col.copy()
    filled = known.copy()
    for _ in range(GROW):
        s = np.zeros_like(col)
        n = np.zeros_like(have)
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                if dy == 0 and dx == 0:
                    continue
                s += _shift(bleed * filled[..., None], dy, dx)
                n += _shift(filled.astype(np.float32), dy, dx)
        new = (~filled) & (n > 0)
        if not new.any():
            break
        bleed[new] = s[new] / n[new][:, None]
        filled = filled | new
    out = np.where(known[..., None], raw, 0)
    low = (a > 0) & ~known
    w = (a / T)[..., None]
    mix = np.where(filled[..., None], w * raw + (1 - w) * bleed, raw)
    out = np.where(low[..., None], mix, out)
    zero = (a == 0) & filled
    out = np.where(zero[..., None], bleed, out)
    return np.concatenate([np.clip(out, 0, 255), a[..., None]], axis=-1).astype(np.uint8)


def convert(src, dst):
    arr = np.asarray(Image.open(src).convert("RGBA"))
    Image.fromarray(to_straight(arr), "RGBA").save(dst)


if __name__ == "__main__":
    convert(sys.argv[1], sys.argv[2])
