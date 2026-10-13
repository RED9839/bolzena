# -*- coding: utf-8 -*-
"""모션 확인 캡처의 고학년 장들을 사도마다 한 장짜리 시트로 — Tools/motionlab_shots.sh(-motionlab-shots) 가 남긴
<폴더>/<번호>_<사도>_5고학년_<nn>.png · <번호>_<사도>_5고학년.tsv(nn · 몸짓 시작에서 ms · t|hit) 를 <폴더>/_sheets/<번호>_<사도>_5고학년.png 로 붙인다.
한 줄 5칸, 칸 아래 시각(몸짓 시작 = 컷인 끝에서 ms), 타격 순간 칸은 빨간 테 · 「타격」.
  python Tools/motionlab_sheet.py [폴더=C:/projects/bolzena-unity/Captures/motionlab] [사도,…]
"""
import glob, os, sys
from PIL import Image, ImageDraw, ImageFont
sys.stdout.reconfigure(encoding="utf-8", errors="replace")

W, H, C = 384, 216, 5
FONT = r"C:\Windows\Fonts\malgun.ttf"


def read_tsv(path):
    head, rows = {}, []
    for line in open(path, encoding="utf-8"):
        p = line.rstrip("\n").split("\t")
        if not p or not p[0]: continue
        if p[0].startswith("#"): head[p[0][1:]] = p[1] if len(p) > 1 else ""
        elif len(p) >= 3:
            try: rows.append((p[0], int(float(p[1])), p[2]))
            except ValueError: pass
    return head, rows


def build(root, tsv):
    base = os.path.basename(tsv)[:-4]
    head, rows = read_tsv(tsv)
    cells = [(nn, ms, kind, os.path.join(root, f"{base}_{nn}.png")) for nn, ms, kind in rows]
    cells = [c for c in cells if os.path.exists(c[3])]
    if not cells: return None
    f = ImageFont.truetype(FONT, 16)
    fb = ImageFont.truetype(FONT, 24)
    cellH = H + 26
    top = 44
    rows_n = (len(cells) + C - 1) // C
    sheet = Image.new("RGB", (W * C, top + rows_n * cellH), (14, 14, 20))
    d = ImageDraw.Draw(sheet)
    nh = sum(1 for c in cells if c[2] == "hit")
    d.text((10, 8), f"{base} · {head.get('hero', '')} · {len(cells)}장(0.25초마다 + 타격 {nh}) · 시각 = 컷인 끝(몸짓 시작)에서 · 빨간 테 = 타격 순간",
           fill=(245, 235, 210), font=fb)
    for i, (nn, ms, kind, path) in enumerate(cells):
        x, y = (i % C) * W, top + (i // C) * cellH
        im = Image.open(path).convert("RGB")
        im.thumbnail((W - 6, H - 6))
        sheet.paste(im, (x + (W - im.width) // 2, y + (H - im.height) // 2))
        hit = kind == "hit"
        if hit: d.rectangle([x + 1, y + 1, x + W - 2, y + H - 2], outline=(255, 80, 80), width=4)
        d.text((x + 8, y + H + 2), f"{nn} · {ms}ms{' · 타격' if hit else ''}", fill=(255, 130, 130) if hit else (210, 210, 220), font=f)
    out_dir = os.path.join(root, "_sheets")
    os.makedirs(out_dir, exist_ok=True)
    out = os.path.join(out_dir, base + ".png")
    sheet.save(out)
    return out


def main():
    root = sys.argv[1] if len(sys.argv) > 1 else r"C:/projects/bolzena-unity/Captures/motionlab"
    pick = set(sys.argv[2].split(",")) if len(sys.argv) > 2 else None
    tsvs = sorted(glob.glob(os.path.join(root, "*_5고학년.tsv")))
    if pick: tsvs = [t for t in tsvs if any(f"_{h}_" in os.path.basename(t) for h in pick)]
    if not tsvs: print("고학년 시각표(*_5고학년.tsv)가 없다 —", root); return
    for t in tsvs:
        out = build(root, t)
        print(out or f"장 없음 — {os.path.basename(t)}")


if __name__ == "__main__":
    main()
