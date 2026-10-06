# -*- coding: utf-8 -*-
"""고학년 시트 전후 나란히 — <전>/<사도>.png 와 <후>/<사도>.png 를 왼쪽 · 오른쪽으로 붙여 <후>/<사도>_compare.png
  python Tools/ult_sheet_compare.py C:/projects/bolzena-unity-tmp/ult_sheets C:/projects/bolzena-unity-tmp/ult_sheets_after
"""
import os, sys
from concurrent.futures import ProcessPoolExecutor
from PIL import Image, ImageDraw, ImageFont
sys.stdout.reconfigure(encoding="utf-8", errors="replace")
HALF = 1600


def one(args):
    a, b, out = args
    if not (os.path.exists(a) and os.path.exists(b)): return None
    A, Bm = Image.open(a).convert("RGB"), Image.open(b).convert("RGB")
    A = A.resize((HALF, round(A.height * HALF / A.width)))
    Bm = Bm.resize((HALF, round(Bm.height * HALF / Bm.width)))
    im = Image.new("RGB", (HALF * 2 + 12, max(A.height, Bm.height) + 34), (10, 10, 14))
    d = ImageDraw.Draw(im)
    f = ImageFont.truetype(r"C:\Windows\Fonts\malgunbd.ttf", 22)
    d.text((10, 4), "고치기 전", fill=(255, 200, 200), font=f)
    d.text((HALF + 22, 4), "고친 뒤", fill=(200, 255, 200), font=f)
    im.paste(A, (0, 34)); im.paste(Bm, (HALF + 12, 34))
    im.save(out, optimize=True)
    return out


def main():
    before, after = sys.argv[1], sys.argv[2]
    jobs = []
    for f in os.listdir(after):
        if f.endswith(".png") and not f.endswith("_compare.png") and "_" + f.split("_")[-1] != f and not f[:3].isdigit():
            jobs.append((os.path.join(before, f), os.path.join(after, f), os.path.join(after, f[:-4] + "_compare.png")))
    with ProcessPoolExecutor(6) as ex:
        n = sum(1 for r in ex.map(one, jobs) if r)
    print(f"전후 {n}장 → {after}/<사도>_compare.png")


if __name__ == "__main__":
    main()
