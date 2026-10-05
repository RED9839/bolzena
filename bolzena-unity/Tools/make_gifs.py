# Captures/frames/<이름>/*.jpg → Captures/<이름>.gif (15fps, 800x450). python Tools/make_gifs.py
import os, glob
from PIL import Image

ROOT = r"C:\projects\bolzena-unity\Captures"
for d in sorted(glob.glob(os.path.join(ROOT, "frames", "*"))):
    fs = sorted(glob.glob(os.path.join(d, "*.jpg")))
    if len(fs) < 3:
        continue
    name = os.path.basename(d)
    frames = [Image.open(f).convert("RGB").resize((560, 315), Image.LANCZOS) for f in fs]
    pal = [f.quantize(colors=200, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.FLOYDSTEINBERG) for f in frames]
    out = os.path.join(ROOT, f"gif_{name}.gif")
    pal[0].save(out, save_all=True, append_images=pal[1:], duration=66, loop=0, optimize=True)
    print(name, len(fs), "frames ->", out, os.path.getsize(out) // 1024, "KB")
