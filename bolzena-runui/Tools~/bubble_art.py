# 사도 말풍선(HeroBubble D1) 그림 — 원작 Common_TalkBubble_1 · Common_TalkBubbleTail_1 에 두꺼운 갈색 테를 둘러 RunArt/Ui 로 굽는다.
#   copy_assets.py 가 부른다(원작 복사본이라 git 에 넣지 않는다). 따로 돌리기: python "Tools~/bubble_art.py" <유니티 프로젝트>
#   bubble_body.png — 몸통(원작의 작은 꼬리 · 그림자는 걷고 흰 몸 + 갈색 테), 9칸 테두리는 화면이 BODY_BORDER 로 자른다
#   bubble_tail.png — 꼬리(테 포함, 몸통 뒤에 깐다) · bubble_tail_fill.png — 같은 판의 흰 속(몸통 위에 덮어 이음매 테를 지운다)
import os, sys
import numpy as np
from PIL import Image, ImageFilter

SRC = r"C:\projects\볼제나\assets\iconsrc\raw_common"
INK = (74, 46, 34)          # Theme.BubbleRim 과 같은 값(#4A2E22)
BODY_RIM = 15               # 원본 410px 기준 테 굵기 — 화면에서 9칸 배율(BODY_PPU)로 줄면 4px 안팎
TAIL_UP = 4                 # 꼬리는 46×32 로 작아 4배로 키워 굽는다
TAIL_RIM = 22


def _dilate(alpha, r):
    import cv2
    k = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (2 * r + 1, 2 * r + 1))
    out = cv2.dilate(alpha, k)
    return cv2.GaussianBlur(out, (0, 0), 0.8)


def _rimmed(rgba, r, pad):
    a = np.array(rgba)
    h, w = a.shape[:2]
    big = np.zeros((h + 2 * pad, w + 2 * pad, 4), np.uint8)
    big[pad:pad + h, pad:pad + w] = a
    alpha = big[..., 3]
    rim = _dilate(alpha, r)
    out = np.zeros_like(big)
    out[..., 0], out[..., 1], out[..., 2] = INK
    out[..., 3] = rim
    fill = big.astype(np.float32)
    fa = fill[..., 3:4] / 255.0
    res = out.astype(np.float32)
    res[..., :3] = res[..., :3] * (1 - fa) + fill[..., :3] * fa
    res[..., 3] = np.maximum(res[..., 3], fill[..., 3])
    return Image.fromarray(res.clip(0, 255).astype(np.uint8)), Image.fromarray(big)


def body(dst):
    im = Image.open(os.path.join(SRC, "Common_TalkBubble_1.png")).convert("RGBA")
    a = np.array(im).astype(np.int32)
    lum = a[..., :3].mean(-1)
    keep = (a[..., 3] > 110) & (lum > 175)          # 흰 몸만(아래 그림자 · 테 빼고)
    # 원작 몸통의 작은 꼬리(아래 왼쪽) 걷기 — 가운데 기둥의 바닥보다 아래는 지운다
    cols = np.where(keep.any(0))[0]
    mid = (cols[0] + cols[-1]) // 2
    bottom = np.where(keep[:, mid])[0].max()
    keep[bottom + 1:, :] = False
    # 왼쪽 반은 꼬리 자국이 남는다 — 오른쪽 반을 뒤집어 덮어 좌우를 맞춘다
    keep[:, :mid] = keep[:, np.minimum(2 * mid - np.arange(mid), keep.shape[1] - 1)]
    rows = np.where(keep.any(1))[0]; cols = np.where(keep.any(0))[0]
    a8 = np.zeros(a.shape, np.uint8)
    a8[..., :3] = 255          # 속은 흰색 한 가지(원작 회색 물결은 9칸으로 늘면 얼룩진다)
    a8[..., 3] = np.where(keep, 255, 0)
    cut = Image.fromarray(a8).crop((cols[0], rows[0], cols[-1] + 1, rows[-1] + 1))
    cut = Image.fromarray(np.array(cut)).filter(ImageFilter.SMOOTH)
    rimmed, _ = _rimmed(cut, BODY_RIM, BODY_RIM + 2)
    rimmed.save(os.path.join(dst, "bubble_body.png"))
    return rimmed.size


def tail(dst):
    im = Image.open(os.path.join(SRC, "Common_TalkBubbleTail_1.png")).convert("RGBA")
    im = im.resize((im.width * TAIL_UP, im.height * TAIL_UP), Image.LANCZOS)
    a = np.array(im); a[..., :3] = 255
    im = Image.fromarray(a)
    rimmed, fill = _rimmed(im, TAIL_RIM, TAIL_RIM + 2)
    rimmed.save(os.path.join(dst, "bubble_tail.png"))
    fill.save(os.path.join(dst, "bubble_tail_fill.png"))
    return rimmed.size


def make(proj):
    dst = os.path.join(proj, "Assets", "Resources", "RunArt", "Ui")
    os.makedirs(dst, exist_ok=True)
    if all(os.path.exists(os.path.join(dst, n)) for n in ("bubble_body.png", "bubble_tail.png", "bubble_tail_fill.png")):
        return False
    b = body(dst); t = tail(dst)
    print("말풍선 그림", b, t)
    return True


if __name__ == "__main__":
    make(sys.argv[1] if len(sys.argv) > 1 else r"C:\projects\bolzena-runui-test")
