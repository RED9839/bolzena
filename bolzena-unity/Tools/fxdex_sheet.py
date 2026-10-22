# 장 도감 시트 — Captures/fxdex 의 <번호>_<사도>_fxNN_<갈래>_<a|b>.png + _done.txt(fxNN 줄)를 사도마다 이름 붙은 시트로(한 장 24개씩)
import sys, os, re, glob
from PIL import Image, ImageDraw, ImageFont
d = sys.argv[1]; out = os.path.join(d, '_sheets'); os.makedirs(out, exist_ok=True)
try: font = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 15)
except Exception: font = ImageFont.load_default()
info = {}   # (번호, fxNN) → 설명
cur = None
for line in open(os.path.join(d, '_done.txt'), encoding='utf-8'):
    m = re.match(r'== (\d+) ', line)
    if m: cur = m.group(1); continue
    if line.startswith('fx') and cur:
        p = line.rstrip('\n').split('\t'); info[(cur, p[0])] = p
files = sorted(glob.glob(os.path.join(d, '*_fx*_a.png')))
groups = {}
for f in files:
    m = re.match(r'(\d+)_(.+)_(fx\d+)_(\w+)_a\.png', os.path.basename(f))
    if m: groups.setdefault((m.group(1), m.group(2)), []).append((m.group(3), f))
W, H, LAB, COLS, PER = 320, 180, 22, 3, 24
for (no, ko), parts in groups.items():
    parts.sort()
    for page in range(0, len(parts), PER):
        chunk = parts[page:page + PER]; rows = (len(chunk) + COLS - 1) // COLS
        S = Image.new('RGB', (COLS * (2 * W + 10), rows * (H + LAB + 6) + 30), (18, 20, 28)); dr = ImageDraw.Draw(S)
        dr.text((6, 6), f'{no} {ko} — 장 도감 {page // PER + 1} (시전자 발밑 · 제 크기 · 0.3초 | 0.9초)', fill=(255, 220, 90), font=font)
        for k, (fx, fa) in enumerate(chunk):
            x = (k % COLS) * (2 * W + 10); y = 30 + (k // COLS) * (H + LAB + 6)
            p = info.get((no, fx), [fx, '?', '?', '?', '?'])
            dr.text((x + 4, y + 2), f'{fx} [{p[1]}] {p[2].replace("fx_", "")}  {p[3]}  {p[4] if len(p) > 4 else ""}', fill=(220, 230, 255), font=font)
            for j, suf in enumerate(('_a.png', '_b.png')):
                fp = fa[:-6] + suf
                if os.path.exists(fp): S.paste(Image.open(fp).convert('RGB').resize((W, H)), (x + j * W, y + LAB))
        S.save(os.path.join(out, f'{no}_{ko}_장도감_{page // PER + 1}.png'))
print('시트', len(os.listdir(out)), '장 →', out)
