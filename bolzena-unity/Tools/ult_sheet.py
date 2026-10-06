# -*- coding: utf-8 -*-
"""고학년 프레임 시트 — Bolzena.exe -battle -ultaudit -ultsheet -captures <폴더> 가 남긴 <폴더>/_frames/<사도>/<ms>.jpg · <사도>.tsv 를
사도마다 한 장(<폴더>/<사도>.png)으로 붙인다. 한 줄 10칸(100ms 마다), 칸 아래 그 100ms 동안 일어난 일, 피해 칸은 빨간 테 · 이펙트 충격 칸은 노란 테.
  python Tools/ult_sheet.py C:/projects/bolzena-unity-tmp/ult_sheets [사도,…]
"""
import glob, os, sys
from concurrent.futures import ProcessPoolExecutor
from PIL import Image, ImageDraw, ImageFont
sys.stdout.reconfigure(encoding="utf-8", errors="replace")

W, H, C, LINES = 320, 180, 10, 5
FONT = r"C:\Windows\Fonts\malgun.ttf"
KIND_KO = {"hit": "", "fx": "fx", "impact": "", "snd": "소리", "voice": "목소리", "move": "이동"}
COL = {"hit": (255, 110, 110), "impact": (255, 220, 90), "fx": (170, 210, 255), "snd": (180, 180, 180), "voice": (200, 170, 255), "move": (140, 255, 160)}


def build(args):
    root, hero = args
    tsv = os.path.join(root, hero + ".tsv")
    frames = sorted(glob.glob(os.path.join(root, "_frames", hero, "*.jpg")))
    if not frames or not os.path.exists(tsv): return None
    head, evs = {}, []
    for line in open(tsv, encoding="utf-8"):
        p = line.rstrip("\n").split("\t")
        if p[0].startswith("#"): head[p[0][1:]] = p[1] if len(p) > 1 else ""
        elif len(p) >= 3:
            try: evs.append((int(p[0]), p[1], p[2]))
            except ValueError: pass
    f = ImageFont.truetype(FONT, 13)
    fb = ImageFont.truetype(FONT, 22)
    cellH = H + 16 + LINES * 15
    rows = (len(frames) + C - 1) // C
    top = 64
    sheet = Image.new("RGB", (W * C, top + rows * cellH), (14, 14, 20))
    d = ImageDraw.Draw(sheet)
    d.text((10, 6), f"{head.get('hero', hero)} · 「{head.get('ult', '')}」 · 원작 타수 {head.get('origHits', '-')} · 우리 표시 {head.get('shownHits', '-')}(엔진 {head.get('engineHits', '-')})"
           f" · 이동 {head.get('move', '-')} · 몸짓 {head.get('motionMs', '-')}ms · 끝 {head.get('endMs', '-')}ms · 충격↔타격 최대 {head.get('syncMaxMs', '-')}ms · {head.get('mode', '')}",
           fill=(245, 235, 210), font=fb)
    d.text((10, 36), "빨간 테 = 피해(타격) · 노란 테 = 원작 이펙트 충격 · 점: 초록 사도 · 빨강 적 · 노란 고리 = 원작 이펙트가 틀린 자리 · 칸 아래: 그 100ms 동안 일어난 일", fill=(170, 170, 180), font=f)
    for i, fr in enumerate(frames):
        ms = int(os.path.splitext(os.path.basename(fr))[0])
        x, y = (i % C) * W, top + (i // C) * cellH
        im = Image.open(fr).convert("RGB").resize((W - 4, H - 4))
        sheet.paste(im, (x + 2, y + 2))
        win = [e for e in evs if ms <= e[0] < ms + 100]
        kinds = {e[1] for e in win}
        if "hit" in kinds: d.rectangle([x, y, x + W - 1, y + H - 1], outline=(255, 60, 60), width=4)
        if "impact" in kinds: d.rectangle([x + 4, y + 4, x + W - 5, y + H - 5], outline=(255, 210, 40), width=3)
        d.text((x + 6, y + H - 2), f"{ms}ms", fill=(255, 255, 255), font=f)
        # 자리 점 — 사도(초록) · 적(빨강) 몸 가운데, 원작 이펙트가 틀린 자리(노랑 고리 · 이름 앞 글자). 뷰포트 0~1(아래가 0)
        def dot(vx, vy, col, r):
            px, py = x + 2 + vx * (W - 4), y + 2 + (1 - vy) * (H - 4)
            if x <= px <= x + W and y <= py <= y + H: d.ellipse([px - r, py - r, px + r, py + r], outline=col, width=2)
            return px, py
        for e in win:
            if e[1] not in ("pt", "pos"): continue
            p = e[2].split(" ")
            try: vx, vy = float(p[-2]), float(p[-1])
            except (ValueError, IndexError): continue
            if e[1] == "pt": dot(vx, vy, (80, 255, 120) if p[0] == "사도" else (255, 80, 80), 4)
            else:
                px, py = dot(vx, vy, (255, 230, 60), 7)
                d.text((px + 6, py - 6), p[0][-14:], fill=(255, 230, 60), font=f)
        lines = []
        for e in win:
            if e[1] in ("pt", "pos"): continue
            t = e[2].replace("hero/", "").replace("voice:", "")
            lines.append((COL.get(e[1], (200, 200, 200)), f"{KIND_KO.get(e[1], e[1])} {t}".strip()[:44]))
        for j, (c, t) in enumerate(lines[:LINES]):
            d.text((x + 4, y + H + 14 + j * 15), t, fill=c, font=f)
        if len(lines) > LINES: d.text((x + W - 40, y + H + 14 + (LINES - 1) * 15), f"+{len(lines) - LINES + 1}", fill=(255, 255, 255), font=f)
    out = os.path.join(root, hero + ".png")
    sheet.save(out, optimize=True)
    return out


def main():
    root = sys.argv[1] if len(sys.argv) > 1 else r"C:\projects\bolzena-unity-tmp\ult_sheets"
    heroes = sys.argv[2].split(",") if len(sys.argv) > 2 else sorted(os.listdir(os.path.join(root, "_frames")))
    with ProcessPoolExecutor(6) as ex:
        n = sum(1 for r in ex.map(build, [(root, h) for h in heroes]) if r)
    print(f"시트 {n}장 → {root}")


if __name__ == "__main__":
    main()
