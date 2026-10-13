# -*- coding: utf-8 -*-
"""이펙트 그림마다 가장자리 흐림 폭 표 — FxRun 이 입자 판의 가장자리를 걷어 낼 폭(EDGE 0.05 대신)을 그림마다 고른다(2026-10-13).

원작 입자 그림 26% 는 바깥 1픽셀까지 알파가 차 있어(꽉 찬 불빛 · 잘린 칼빛) 판 가장자리 0.05 만 흐리면 네모로 잘려 보였다.
이미터가 쓰는 (그림, 시트 칸 수, 더하기 여부)마다 칸 바깥 1픽셀의 「보이는 세기」를 잰다:
  · 알파 섞기 = 알파 · 더하기 = 밝기 × 알파(검은 테두리는 더해도 안 보인다) · 알파 없는 그림을 알파 섞기로 = 밝기(셰이더가 밝기를 알파로 읽는다)
  · 세기 b ≤ 8 이면 그대로(0.05) — 표에 안 적는다
  · 그 위면 폭 = 0.25 × b/255 + 0.02 (0.05 ~ 0.25) — 가장자리 세기를 0 까지 내리는 기울기를 같게. 꽉 찬(255) 그림이 상한 0.25
  · 세기 32 를 넘고 칸 안 세기의 97% 이상이 칸에 내접한 타원 안에 있으면(둥근 불빛이 네모로 잘린 것) 타원으로 걷는다 — 폭을 음수로 적는다
알파 없는 그림(RGB · L · 투명 없는 P)을 알파 섞기로 쓰는 이미터는 lum 1 — 셰이더가 밝기를 알파로 읽는다(더하기는 그대로 둔다).

출력: <프로젝트>/Assets/BolzenaFxData/Resources/BolzenaFx/fx_edge.txt — 줄마다 「tex<TAB>tx<TAB>ty<TAB>add<TAB>폭<TAB>lum」.
런타임(FxRun.EdgeOf)이 Resources 에서 읽는다 — FxLibrary.asset · Effects 를 다시 굽지 않아도 된다(FxImport 를 돌릴 필요 없음).
  python Tools/fx_edge.py --project C:\\projects\\bolzena-unity
필요: Python 3 · Pillow · numpy
"""
import argparse, glob, json, os, sys
import numpy as np
from PIL import Image

for _s in (sys.stdout, sys.stderr):
    try: _s.reconfigure(encoding="utf-8", errors="replace")
    except Exception: pass

DEFAULT = 0.05
FLOOR = 8           # 이 세기 이하면 가장자리가 비었다고 본다
CAP = 0.25
ELLIPSE_IN = 0.97   # 세기의 이 몫 이상이 내접 타원 안이면 타원으로 걷는다
ELLIPSE_B = 32      # 타원은 가장자리가 이만큼은 찬 그림만(살짝 찬 그림은 네모 0.05 로 충분 — 모서리를 덜 깎는다)
# 잰 값 대신 손으로 정한 폭(그림, 칸 x, 칸 y, 더하기) — 바깥 1픽셀은 옅어도 그 바로 안쪽이 꽉 찬 그림(2026-10-14):
#   불벽 1(FX_Seq_Fire_Wall_01 — 불꽃 밑동이 칸 아래끝 3~5픽셀 위까지 흰색, 바깥 줄은 34) — 벨라 스킬 회색 연기 아랫변이 수평으로 잘렸다 → 타원 0.25
OVERRIDE = {("_shared/FX_Seq_Fire_Wall_01_5x5.png", 5, 5, 0): -0.25}


def has_alpha(im):
    return im.mode in ("RGBA", "LA", "PA") or (im.mode == "P" and "transparency" in im.info) or (im.mode in ("RGB", "L") and "transparency" in im.info)


def uses(src):
    """(dir, tex, tx, ty, add) → 쓰는 이펙트 이름들 — fx(고학년) · fx2(카드 · 공용)의 모든 이미터"""
    out = {}
    for d in ("fx", "fx2"):
        for p in glob.glob(os.path.join(src, d, "*", "fx.json")):
            for name, body in json.load(open(p, encoding="utf-8")).items():
                for e in body.get("em", []):
                    t = e.get("tex")
                    if not t: continue
                    sh = e.get("sheet") or {}
                    key = (d, t, max(1, int(sh.get("tx", 1) or 1)), max(1, int(sh.get("ty", 1) or 1)), 1 if e.get("add") else 0)
                    out.setdefault(key, set()).add(name)
    return out


def measure(path, tx, ty, add):
    """(가장자리 세기 b, 타원 안 몫, 알파 없음)"""
    im = Image.open(path)
    alpha = has_alpha(im)
    a = np.asarray(im.convert("RGBA"), dtype=np.float32)
    lum = a[:, :, :3].max(axis=2)
    if add: s = lum * a[:, :, 3] / 255.0
    elif alpha: s = a[:, :, 3]
    else: s = lum
    H, W = s.shape
    cw, ch = W // tx, H // ty
    if cw < 2 or ch < 2: return 0.0, 1.0, not alpha
    yy, xx = np.mgrid[0:ch, 0:cw]
    r = np.hypot((xx + 0.5) / cw * 2 - 1, (yy + 0.5) / ch * 2 - 1)
    inside = r <= 1
    b = 0.0; tot = 0.0; ins = 0.0
    for j in range(ty):
        for i in range(tx):
            c = s[j * ch:(j + 1) * ch, i * cw:(i + 1) * cw]
            b = max(b, float(c[0].max()), float(c[-1].max()), float(c[:, 0].max()), float(c[:, -1].max()))
            tot += float(c.sum()); ins += float(c[inside].sum())
    return b, (ins / tot if tot > 0 else 1.0), not alpha


def width(b, ell):
    if b <= FLOOR: return DEFAULT
    w = round(min(CAP, max(DEFAULT, CAP * b / 255 + 0.02)), 3)
    return -w if ell >= ELLIPSE_IN and b > ELLIPSE_B else w


def build(src):
    rows = {}
    for (d, t, tx, ty, add), names in sorted(uses(src).items()):
        p = os.path.join(src, d, t)
        if not os.path.exists(p): continue
        b, ell, noalpha = measure(p, tx, ty, add)
        rows[(d, t, tx, ty, add)] = dict(b=b, ell=ell, noalpha=noalpha, w=OVERRIDE.get((t, tx, ty, add), width(b, ell)), lum=1 if noalpha and not add else 0, names=names)
    return rows


def write(rows, out):
    # 런타임 열쇠에는 fx · fx2 구분이 없다(이미터는 TexPath 만 든다) — 겹치면 넓은 쪽 · lum 은 하나라도
    tab = {}
    for (d, t, tx, ty, add), r in rows.items():
        k = (t, tx, ty, add)
        w, lum = r["w"], r["lum"]
        if k in tab:
            w0, l0 = tab[k]
            w = w if abs(w) >= abs(w0) else w0
            lum = max(lum, l0)
        tab[k] = (w, lum)
    lines = [f"{t}\t{tx}\t{ty}\t{add}\t{w:g}\t{lum}" for (t, tx, ty, add), (w, lum) in sorted(tab.items()) if w != DEFAULT or lum]
    os.makedirs(os.path.dirname(out), exist_ok=True)
    with open(out, "w", encoding="utf-8", newline="\n") as f:
        f.write("# fx_edge — Tools/fx_edge.py 가 만든다(손대지 말 것). tex\ttx\tty\tadd\t폭(음수 = 타원)\tlum(밝기를 알파로)\n")
        f.write("\n".join(lines) + "\n")
    return len(lines)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--project", default=r"C:\projects\bolzena-unity")
    a = ap.parse_args()
    data = os.path.join(a.project, "Assets", "BolzenaFxData")
    rows = build(os.path.join(data, "Src"))
    n = write(rows, os.path.join(data, "Resources", "BolzenaFx", "fx_edge.txt"))
    bad = sum(1 for r in rows.values() if r["b"] > FLOOR)
    ell = sum(1 for r in rows.values() if r["w"] < 0)
    lum = sum(1 for r in rows.values() if r["lum"])
    print(f"[fx_edge] 이미터 짝 {len(rows)} · 가장자리 찬 짝 {bad}(타원 {ell}) · 밝기→알파 {lum} · 표 {n}줄")


if __name__ == "__main__":
    main()
