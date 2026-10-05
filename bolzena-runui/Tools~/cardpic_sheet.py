# 사도 카드 그림 대조 시트 — 사도 한 명의 고유 · 생성 카드(이름 · 종류 · 글)와 원작 그림 후보를 한 장에 모은다.
#   python "Tools~/cardpic_sheet.py" <출력 폴더> [사도 id …]
# 사람(또는 에이전트)이 시트를 보고 카드마다 어울리는 그림을 골라 Tools~/cardpic_picks.json 에 적는다(Docs/카드그림.md).
# 후보: story(액자 일러스트) · btn(싸우는 SD) · cg(만화 컷) · schedule(SD 전신 액자) · aside(애장품) · present(선물) · skill(원작 스킬 아이콘 128)
# 쓰지 않는 것: head(대화창 얼굴) · profile · clone · skin(사도 없는 방 배경) · bg · icon(초상 — 시작 카드는 스탠딩)
# 그림마다 10% 눈금(가로 · 세로 0~10)을 그어 두었다 — 자르기 자리(얼굴 가운데)를 눈금으로 적는다.
import glob, json, os, sys
from PIL import Image, ImageDraw, ImageFont

SRC = r"C:\projects\볼제나\assets\cardart_src"
HEROES = r"C:\projects\bolzena-content-v2\heroes"
DRAFT = r"C:\projects\bolzena-content-v2\_ref\사도카드그림.json"
CATS = ["story", "btn", "cg", "schedule", "aside", "present", "skill"]
F = lambda s, b=False: ImageFont.truetype(r"C:\Windows\Fonts\malgunbd.ttf" if b else r"C:\Windows\Fonts\malgun.ttf", s)


def heroes():
    """사도 id → (사도 정의, 카드 목록, 그림 폴더)"""
    draft = json.load(open(DRAFT, encoding="utf-8"))["cards"]
    out = {}
    for f in sorted(glob.glob(os.path.join(HEROES, "*", "*.json"))):
        d = json.load(open(f, encoding="utf-8"))
        h = d["heroes"][0]
        folder = next((draft[c["id"]].split("/")[0] for c in d["cards"] if c["id"] in draft), None)
        out[h["id"]] = (h, d["cards"], folder)
    return out


def fx_text(fx):
    parts = []
    for e in fx or []:
        k = e.get("k")
        bits = [k]
        for key in ("ratio", "v", "id", "target", "pct", "all", "not"):
            if key in e:
                bits.append(f"{key}={e[key]}")
        if "fx" in e:
            bits.append("{" + fx_text(e["fx"]) + "}")
        parts.append(" ".join(str(b) for b in bits))
    return " · ".join(parts)


def cands(folder):
    fs = []
    for cat in CATS:
        fs += sorted(p for p in glob.glob(os.path.join(SRC, folder, cat + "__*.png")))
    return fs


def grid(im):
    d = ImageDraw.Draw(im, "RGBA")
    w, h = im.size
    for i in range(1, 10):
        x, y = w * i / 10, h * i / 10
        d.line([(x, 0), (x, h)], fill=(255, 255, 255, 60) if i != 5 else (255, 80, 80, 90), width=1)
        d.line([(0, y), (w, y)], fill=(255, 255, 255, 60) if i != 5 else (255, 80, 80, 90), width=1)
        d.text((x + 2, 1), str(i), fill=(255, 255, 0, 230), font=F(13, True), stroke_width=2, stroke_fill=(0, 0, 0))
        d.text((2, y + 1), str(i), fill=(255, 255, 0, 230), font=F(13, True), stroke_width=2, stroke_fill=(0, 0, 0))
    return im


def sheet(hid, h, cards, folder, outdir):
    st = set(h.get("starter", []))
    mine = [c for c in cards if c["id"] not in st]
    W = 2400
    lines = [(f"{hid}  ({h.get('race')} · {h.get('nature')} · {h.get('role')})  폴더 {folder}  — 키워드 {(h.get('keyword') or {}).get('name', '')}", F(30, True), (255, 230, 150))]
    if h.get("blurb"):
        lines.append(("   " + h["blurb"], F(20), (200, 205, 225)))
    for c in mine:
        tag = "고유" if c.get("unique") else "생성"
        lines.append((f"[{c['id']}] {c['name']}  — {tag} {c.get('type')} {c.get('cost')}코", F(25, True), (255, 255, 255)))
        sub = "      " + (c.get("blurb") or "") + "   |  " + fx_text(c.get("fx"))
        lines.append((sub[:170], F(19), (170, 200, 240)))
    th = sum(f.size + 10 for _, f, _ in lines) + 20
    fs = cands(folder) if folder else []
    TH = 330
    tiles = []
    for i, p in enumerate(fs):
        im = Image.open(p).convert("RGBA")
        cat = os.path.basename(p).split("__")[0]
        H = TH if cat not in ("skill",) else 150
        t = im.resize((max(1, round(im.width * H / im.height)), H), Image.LANCZOS)
        bg = Image.new("RGBA", t.size, (70, 70, 80, 255))
        bg.alpha_composite(t)
        if cat != "skill":
            grid(bg)
        stem = os.path.basename(p)[:-4].split("__")[1]
        label = f"{chr(65 + i) if i < 26 else 'a' + chr(65 + i - 26)} {cat} {stem.replace('Character_', '').replace('ScheduleStory_', '').replace('CG_', '').replace('MainLobby_', '')}"
        tiles.append((bg.convert("RGB"), label))
    # 줄 바꿈 배치
    rows, row, x = [], [], 0
    for t, lab in tiles:
        tw = max(t.width, 200)
        if x + tw > W - 20 and row:
            rows.append(row); row, x = [], 0
        row.append((t, lab)); x += tw + 16
    if row:
        rows.append(row)
    rh = [max(t.height for t, _ in r) + 30 for r in rows]
    out = Image.new("RGB", (W, th + sum(rh) + 20), (24, 26, 36))
    d = ImageDraw.Draw(out)
    y = 12
    for txt, f, col in lines:
        d.text((16, y), txt, font=f, fill=col); y += f.size + 10
    y = th
    for r, hgt in zip(rows, rh):
        x = 16
        for t, lab in r:
            d.text((x, y), lab[:max(8, max(t.width, 200) // 9)], font=F(16, True), fill=(255, 220, 120))
            out.paste(t, (x, y + 24))
            x += max(t.width, 200) + 16
        y += hgt
    os.makedirs(outdir, exist_ok=True)
    out.save(os.path.join(outdir, f"{hid}.jpg"), quality=85)
    return [os.path.basename(p)[:-4] for p in fs]


if __name__ == "__main__":
    outdir = sys.argv[1]
    hs = heroes()
    want = sys.argv[2:] or list(hs)
    index = {}
    for hid in want:
        h, cards, folder = hs[hid]
        index[hid] = {"folder": folder, "cands": {(chr(65 + i) if i < 26 else "a" + chr(65 + i - 26)): n for i, n in enumerate(sheet(hid, h, cards, folder, outdir))}}
    json.dump(index, open(os.path.join(outdir, "_index.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    print(len(want), "장")
