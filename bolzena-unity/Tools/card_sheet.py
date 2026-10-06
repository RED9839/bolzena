# -*- coding: utf-8 -*-
"""카드 몸짓 견주기 — 원작 영상 시트(기본공격 · 저학년)와 우리 몸짓(-cardsheet 프레임)을 한 장에 위아래로.
  python Tools/card_sheet.py C:/projects/bolzena-unity-tmp/card_sheets [사도 …]
→ <폴더>/<사도>_기본공격.png(원작 기본공격 / 우리 Attack1 · Attack2) · <사도>_저학년.png(원작 저학년 / 우리 Skill1)
   · _card_summary.tsv(사도 · 몸짓 · 튼 애니 · 우리 길이 ms · 원작 시트 길이 s)
"""
import os, sys, glob
from PIL import Image, ImageDraw, ImageFont

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
VID = "C:/projects/bolzena-unity-tmp/ult_videos"
TILE = (200, 113)          # 원작 시트 칸
OURS = (200, 70)           # 우리 프레임(싸움터 가운데 띠 512×180)
COLS = 10
VID_NAME = {"마에스트로2호": "마에스트로 2호", "시온더다크불릿": "시온 더 다크불릿"}   # 영상 폴더 이름이 띄어 쓴 사도


def font(sz):
    for f in ["C:/Windows/Fonts/malgun.ttf", "C:/Windows/Fonts/arial.ttf"]:
        if os.path.exists(f):
            return ImageFont.truetype(f, sz)
    return ImageFont.load_default()


F = font(18)


def ours_grid(fdir, label):
    fs = sorted(glob.glob(os.path.join(fdir, "*.jpg")))
    anim = open(os.path.join(fdir, "_anim.txt"), encoding="utf-8").read().strip() if os.path.exists(os.path.join(fdir, "_anim.txt")) else "?"
    rows = max(1, (len(fs) + COLS - 1) // COLS)
    im = Image.new("RGB", (OURS[0] * COLS, 30 + rows * (OURS[1] + 18)), (20, 20, 26))
    d = ImageDraw.Draw(im)
    d.text((6, 4), f"우리 {label} — {anim} · {len(fs) * 100}ms", fill=(255, 220, 120), font=F)
    for i, f in enumerate(fs):
        x, y = (i % COLS) * OURS[0], 30 + (i // COLS) * (OURS[1] + 18)
        d.text((x + 4, y), f"{i / 10:.1f}s", fill=(255, 200, 80), font=font(13))
        im.paste(Image.open(f).convert("RGB").resize(OURS), (x, y + 16))
    return im, anim, len(fs) * 100


def orig(h, name):
    p = os.path.join(VID, h, name + "_sheet.png")
    if not os.path.exists(p):
        return None
    im = Image.open(p).convert("RGB")
    w = TILE[0] * COLS
    return im.resize((w, int(im.size[1] * w / im.size[0])))


def stack(parts, title):
    parts = [p for p in parts if p is not None]
    W = max(p.size[0] for p in parts)
    H = 34 + sum(p.size[1] + 8 for p in parts)
    out = Image.new("RGB", (W, H), (10, 10, 14))
    ImageDraw.Draw(out).text((6, 6), title, fill=(255, 255, 255), font=F)
    y = 34
    for p in parts:
        out.paste(p, (0, y)); y += p.size[1] + 8
    return out


def main():
    root = sys.argv[1]
    heroes = sys.argv[2:] or sorted(os.listdir(os.path.join(root, "_cards")))
    rows = [["사도", "몸짓", "튼 애니", "우리 ms", "원작 시트"]]
    for h in heroes:
        cdir = os.path.join(root, "_cards", h)
        if not os.path.isdir(cdir):
            continue
        vh = h.replace("_", "(", 1) + ")" if "_" in h and not os.path.isdir(os.path.join(VID, h)) else h
        vh = VID_NAME.get(h, vh)
        parts = []
        for m in ["Attack1", "Attack2"]:
            g, an, ms = ours_grid(os.path.join(cdir, m), m)
            parts.append(g); rows.append([h, m, an, ms, "기본공격"])
        o = orig(vh, "기본공격")
        if o is not None:
            parts.insert(0, o)
        stack(parts, f"{h} — 위: 원작 기본공격 · 아래: 우리 Attack1 · Attack2").save(os.path.join(root, f"{h}_기본공격.png"))
        g, an, ms = ours_grid(os.path.join(cdir, "Skill1"), "Skill1")
        rows.append([h, "Skill1", an, ms, "저학년"])
        stack([orig(vh, "저학년"), g], f"{h} — 위: 원작 저학년 · 아래: 우리 Skill1").save(os.path.join(root, f"{h}_저학년.png"))
    with open(os.path.join(root, "_card_summary.tsv"), "w", encoding="utf-8") as f:
        f.write("\n".join("\t".join(map(str, r)) for r in rows) + "\n")
    print(f"{len(heroes)}명 → {root}")


if __name__ == "__main__":
    main()
