# 기본 스킬 카드의 SD 꼬마 전신(원작 SD 일정 그림 ScheduleStory) — 사도 몸 자리 재기(2026-10-09). Docs/카드그림.md
#   python "Tools~/cardart_sdbox.py"   → Tools~/cardart_sdbox.json {그림: [왼, 위, 오른, 아래] (0~1, 원본 기준)}
# SD 일정 그림 135장은 같은 마을 배경 위에 사도 하나가 서 있다 — 135장의 칸별 중앙값을 배경으로 보고, 배경과 다른 덩어리(가장 큰 것)를 몸으로 잡는다.
# cardpic.bake 가 이 상자로 몸 높이를 같은 몫(SD_BODY)으로 맞추고 발을 효과 글 상자 바로 위(SD_FEET)에 둔다(카드마다 크기 · 자리가 같게).
import glob, json, os
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = r"C:\projects\볼제나\assets\cardart_src"
OUT = os.path.join(HERE, "cardart_sdbox.json")
W, H = 250, 350   # 재는 크기(원본 1000×1400 의 1/4)

files = sorted(glob.glob(os.path.join(SRC, "*", "schedule__*.png")))
arrs = [np.asarray(Image.open(f).convert("RGB").resize((W, H), Image.BILINEAR), dtype=np.int16) for f in files]
bg = np.median(np.stack(arrs), axis=0)
out = {}
from scipy import ndimage
for f, a in zip(files, arrs):
    diff = np.abs(a - bg).sum(axis=2) > 60
    diff[: int(H * 0.15)] = False   # 하늘 · 나무 꼭대기(배경 차이가 큰 곳)
    diff[int(H * 0.92):] = False
    diff = ndimage.binary_opening(diff, iterations=1)
    lab, n = ndimage.label(diff)
    if n == 0:
        continue
    sizes = ndimage.sum(diff, lab, range(1, n + 1))
    k = int(np.argmax(sizes)) + 1
    # 가장 큰 덩어리 + 그 상자 가까이 있는 덩어리(머리 장식 · 무기)
    ys, xs = np.nonzero(lab == k)
    x0, x1, y0, y1 = xs.min(), xs.max(), ys.min(), ys.max()
    for j in range(1, n + 1):
        if j == k or sizes[j - 1] < sizes[k - 1] * 0.05:
            continue
        yy, xx = np.nonzero(lab == j)
        if xx.max() >= x0 - 10 and xx.min() <= x1 + 10 and yy.max() >= y0 - 10 and yy.min() <= y1 + 10:
            x0, x1, y0, y1 = min(x0, xx.min()), max(x1, xx.max()), min(y0, yy.min()), max(y1, yy.max())
    key = os.path.basename(os.path.dirname(f)) + "/" + os.path.basename(f)[:-4]
    out[key] = [round(x0 / W, 4), round(y0 / H, 4), round((x1 + 1) / W, 4), round((y1 + 1) / H, 4)]
json.dump(out, open(OUT, "w", encoding="utf-8"), indent=0)
hs = [v[3] - v[1] for v in out.values()]
print("SD 몸 상자", len(out), "· 몸 높이 몫 최소", min(hs), "중간", sorted(hs)[len(hs) // 2], "최대", max(hs))
