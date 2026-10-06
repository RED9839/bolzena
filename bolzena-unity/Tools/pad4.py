# 4의 배수 맞추기 — 웹(WebGL)은 가로 · 세로가 4의 배수가 아닌 그림을 DXT 로 못 넣고 압축 안 된 RGBA32 로 넣는다
# (유니티가 원본 크기를 보고 형식을 정한다 — 가져오기 뒤에 늘려도 소용없다. bolzena-unity-tmp/web_size/result.md).
# 그래서 원본 PNG 를 투명 픽셀로 4의 배수까지 채운다. 여러 번 돌려도 같다(이미 4의 배수면 그대로).
#   · 스프라이트(Art/Monster 적 아이콘 202×202 · Art 의 스킬 아이콘 몇 장) — 사방 고르게(가운데 기준 스프라이트라 그림 자리가 그대로)
#   · 칸을 위에서부터 픽셀로 자르는 시트(Fx/Sheets · BolzenaFxData/Src/baked) — 오른쪽 · 아래에만(칸 좌표가 그대로)
# python Tools/pad4.py [--dry]
import glob, os, sys
from PIL import Image

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
dry = "--dry" in sys.argv

JOBS = [
    ("Assets/Bolzena/Resources/Art/**/*.png", "center"),
    ("Assets/Bolzena/Resources/Fx/Sheets/*.png", "topleft"),
    ("Assets/BolzenaFxData/Src/baked/**/*.png", "topleft"),
]

n = 0
for pat, mode in JOBS:
    for f in sorted(glob.glob(os.path.join(ROOT, pat), recursive=True)):
        with Image.open(f) as im:
            w, h = im.size
            if w % 4 == 0 and h % 4 == 0:
                continue
            W, H = (w + 3) // 4 * 4, (h + 3) // 4 * 4
            src = im.convert("RGBA")
        x0, y0 = ((W - w) // 2, (H - h) // 2) if mode == "center" else (0, 0)
        out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        out.paste(src, (x0, y0))
        n += 1
        print("%s %dx%d → %dx%d (%s)" % (os.path.relpath(f, ROOT), w, h, W, H, mode))
        if not dry:
            out.save(f)
print("[pad4] %d장 %s" % (n, "(시험 — 저장 안 함)" if dry else "채움"))
