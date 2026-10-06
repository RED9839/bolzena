# -*- coding: utf-8 -*-
"""원작 미리보기 영상에서 타격 시각 찾기 — 맞은 적이 빨갛게 번쩍이는(원작 피격 틴트) 프레임.
프레임마다 「피해 숫자 색」 픽셀(빨강 높고 초록 중간 · 파랑 낮음, 진한 테두리 옆) 수를 세고, 직전 0.2초 최소보다 크게 늘어난 때를 새 숫자로 본다.
  python Tools/video_hits.py <사도> [고학년|저학년|기본공격]      → 시각 목록 · 곡선 그림(<영상폴더>/<사도>/<종류>_hits.png)
  python Tools/video_hits.py --all                               → video_hits.tsv(135명 고학년)
"""
import csv, os, sys
import cv2
import numpy as np
sys.stdout.reconfigure(encoding="utf-8", errors="replace")
ROOT = r"C:\projects\bolzena-unity-tmp\ult_videos"


def curve(path):
    cap = cv2.VideoCapture(path)
    fps = cap.get(cv2.CAP_PROP_FPS) or 30
    vals = []
    while True:
        ok, f = cap.read()
        if not ok: break
        b, g, r = [f[..., i].astype(np.int16) for i in range(3)]
        # 피해 숫자 — 주황 · 노랑(r 높고 g 중간~높고 b 낮음) 진한 색, 화면 위 2/3
        # 맞은 적이 빨갛게 번쩍(원작 피격 틴트) — 진한 빨강(r 높고 g · b 낮음). 피해 숫자가 없는 영상도 있어 이쪽이 더 믿을 만하다
        m = (r > 170) & (g < 70) & (b < 80) & (r - g > 120)
        vals.append(int(m.sum()))
    cap.release()
    return np.array(vals, float), fps


def hits(vals, fps):
    if len(vals) == 0: return []
    win = max(2, int(fps * 0.2))
    out, last = [], -1e9
    thr = max(40.0, np.percentile(vals, 90) * 0.25)
    for i in range(win, len(vals)):
        base = vals[i - win:i].min()
        if vals[i] - base > thr and vals[i] >= vals[i - 1] and (i / fps - last) > 0.12:
            if i + 1 < len(vals) and vals[i + 1] > vals[i]: continue
            out.append(round(i / fps * 1000)); last = i / fps
    return out


def one(hero, kind="고학년", draw=True):
    p = os.path.join(ROOT, hero, kind + ".mp4")
    if not os.path.exists(p): return None
    v, fps = curve(p)
    hs = hits(v, fps)
    if draw:
        W, H = max(400, len(v) * 3), 200
        img = np.full((H, W, 3), 20, np.uint8)
        mx = max(1, v.max())
        for i in range(1, len(v)):
            cv2.line(img, ((i - 1) * 3, int(H - v[i - 1] / mx * (H - 20))), (i * 3, int(H - v[i] / mx * (H - 20))), (80, 200, 255), 1)
        for t in hs:
            x = int(t / 1000 * fps * 3); cv2.line(img, (x, 0), (x, H), (60, 60, 255), 1)
        cv2.imencode(".png", img)[1].tofile(os.path.join(ROOT, hero, kind + "_hits.png"))
    return {"ms": round(len(v) / fps * 1000), "hits": hs}


def main():
    if sys.argv[1] == "--all":
        rows = [["사도", "영상ms", "타수", "첫타ms", "끝타ms", "시각들"]]
        for h in sorted(os.listdir(ROOT)):
            if not os.path.isdir(os.path.join(ROOT, h)) or h.startswith("_"): continue
            r = one(h, "고학년", draw=True)
            if r is None: continue
            hs = r["hits"]
            rows.append([h, r["ms"], len(hs), hs[0] if hs else "", hs[-1] if hs else "", ",".join(map(str, hs))])
        with open(os.path.join(ROOT, "video_hits.tsv"), "w", encoding="utf-8", newline="") as fo:
            csv.writer(fo, delimiter="\t").writerows(rows)
        print(f"{len(rows) - 1}명 → video_hits.tsv")
    else:
        print(one(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else "고학년"))


if __name__ == "__main__":
    main()
