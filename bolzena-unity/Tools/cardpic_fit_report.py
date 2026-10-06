# 원작 그림 카드 그림 자리 표(2026-10-07 「카드 이미지가 너무 붕 뜬 것」) — 전후 시트 로그([PicSheet] 줄)를 모아 카드마다 자리를 잰다.
#   python Tools/cardpic_fit_report.py <출력.tsv> <로그 …>
#   로그 = 전투(-battle -cardpicsheet) · 판(-demo-picsheet) 실행의 player.log, 전(-oldpicfit) · 후 둘 다.
# 잰 값(그림 창 높이에 대한 위에서부터의 몫):
#   top = 위 글 · 칩 끝(전투 0.26 · 판 0.203) · deco = 효과 판 장식 선 · 그림 = 불투명 내용 자리(사물 · SD 는 알파 경계, 아이콘은 판)
#   빈칸 = deco − 그림 아래 · 겹침 = top − 그림 위 · 채움 = 그림 높이 / (deco − top)
#   붕 뜸 = 빈칸 > 0.05 또는 채움 < 0.6(가로 폭 한도에 걸린 넓은 그림 · 창을 덮는 장면 그림은 빼고)
# 전(before) 자리는 로그에 없으니 옛 굽기 규칙(runui Tools~/cardpic.py _place · CardView IconY/IconS · W.Card 판 134)으로 다시 계산한다.
import csv, os, re, sys, collections
sys.path.insert(0, r"C:\projects\bolzena-runui\Tools~")
import cardpic
from PIL import Image

TOP = {"battle": 0.26, "board": 56 / 276}


def old_obj_rect(p):
    """옛 굽기(364×512 한 장)에서 사물 내용 자리(창 몫)."""
    src = os.path.join(cardpic.SRC, *p["file"].split("/")) + ".png"
    im = Image.open(src).convert("RGBA")
    bb = im.getchannel("A").point(lambda a: 255 if a > 16 else 0).getbbox()
    w, h = bb[2] - bb[0], bb[3] - bb[1]
    cat = os.path.basename(p["file"]).split("__")[0]
    W, H = cardpic.OUT_W, cardpic.OUT_H
    bw, bh, cy = (W * 0.96, H * 0.56, 0.36) if cat == "btn" else (W * 0.80, H * 0.48, 0.34)
    k = min(bw / w, bh / h)
    ww, hh = round(w * k), round(h * k)
    x, y = round((W - ww) / 2), round(H * cy - hh / 2)
    return (x / W, y / H, ww / W, hh / H), cat, (w, h)


def old_icon_rect(screen):
    if screen == "battle":
        ah = 2.7 - 0.07
        c = (ah / 2 - 0.27) / ah
        s = 1.06 / ah
        return (0.5 - s * (2.63 / 1.83) / 2, c - s / 2, s * (2.63 / 1.83), s)
    c = (140 - 2 - 22) / 276
    s = 134 / 276
    return (0.5 - s * 276 / 196 / 2, c - s / 2, s * 276 / 196, s)


def main():
    out, logs = sys.argv[1], sys.argv[2:]
    picks = cardpic.load()
    rows = {}
    for lg in logs:
        for line in open(lg, encoding="utf-8", errors="replace"):
            if not line.startswith("[PicSheet] battle\t") and not line.startswith("[PicSheet] board\t"):
                continue
            f = line.rstrip("\n").split("\t")
            screen = f[0].split()[1]
            tag, s, cid, art = f[1], f[2], f[3], f[4]
            bottom = float(f[5])
            r = tuple(float(v) for v in f[6:10])
            cap = f[10] == "1"
            rows[(screen, tag, cid)] = dict(screen=screen, tag=tag, set=s, id=cid, art=art, deco=bottom, rect=r, cap=cap)
    res = []
    for (screen, tag, cid), d in sorted(rows.items()):
        p = picks.get(cid, {})
        kind, cat, src = "icon", "icon", ""
        if p.get("file"):
            if cardpic.is_obj(p):
                kind = "obj"
                orc, cat, wh = old_obj_rect(p)
                src = f"{wh[0]}x{wh[1]}"
            else:
                kind, cat = "cover", os.path.basename(p["file"]).split("__")[0]
        r = d["rect"]
        if tag == "before" or r[3] == 0:
            if kind == "obj":
                r = orc
            elif kind == "icon":
                r = old_icon_rect(screen)
        top = TOP[screen]
        if kind == "cover":
            gap = over = fill = 0.0
            floaty = False
        else:
            gap = d["deco"] - (r[1] + r[3])
            over = top - r[1]
            fill = r[3] / max(1e-3, d["deco"] - top)
            floaty = gap > 0.05 or (fill < 0.6 and r[2] < 0.9)   # 가로가 넓어 폭 한도(92%)에 걸린 것은 덜 채워도 뜬 것이 아니다
        res.append(dict(screen=screen, tag=tag, id=cid, kind=kind, cat=cat, src=src, art=d["art"], deco=round(d["deco"], 3),
                        top=round(r[1], 3), bottom=round(r[1] + r[3], 3), width=round(r[2], 3), height=round(r[3], 3),
                        gap=round(gap, 3), overlap=round(over, 3), fill=round(fill, 2), capped=int(d["cap"]), floating=int(floaty)))
    with open(out, "w", encoding="utf-8", newline="") as fo:
        w = csv.DictWriter(fo, fieldnames=list(res[0].keys()), delimiter="\t")
        w.writeheader()
        w.writerows(res)
    summ = collections.defaultdict(lambda: collections.Counter())
    for r in res:
        k = (r["screen"], r["tag"], r["kind"])
        summ[k]["n"] += 1
        summ[k]["floating"] += r["floating"]
        summ[k]["capped"] += r["capped"]
        summ[k]["overlap"] += int(r["overlap"] > 0.01)
        summ[k]["gap_sum"] += r["gap"]
        summ[k]["fill_sum"] += r["fill"]
    for k, c in sorted(summ.items()):
        n = c["n"]
        print(f"{k[0]:6} {k[1]:6} {k[2]:5} 장 {n:3}  붕뜸 {c['floating']:3}  글칸겹침 {c['overlap']:3}  상한 {c['capped']:3}  빈칸평균 {c['gap_sum']/n:.3f}  채움평균 {c['fill_sum']/n:.2f}")


if __name__ == "__main__":
    main()
