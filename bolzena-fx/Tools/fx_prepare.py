# -*- coding: utf-8 -*-
"""볼제나 연출 자료 준비 — 웹판 원작 에셋(읽기만)을 유니티 프로젝트가 가져올 수 있는 꼴로 옮긴다.

유니티가 못 읽는 것만 여기서 푼다(나머지 변환은 에디터 스크립트 Bolzena.Fx.EditorTools.FxImport.ImportAll):
  · 이펙트  assets/fx/**  (png · fx.json · index.json)        → <프로젝트>/Assets/BolzenaFxData/Src/fx/        그대로 복사
  · 구운 판 assets/fx-baked/<이름>/*.webp                       → Src/baked/<이름>/*.png   (webp → png, 색인의 post 를 미리 칠한다)
  · 효과음  assets/sfx/**.ogg (Ogg Opus)                          → Resources/BolzenaAudio/sfx/**.wav
  · 목소리  assets/voice/<사도>/*.ogg                             → Resources/BolzenaAudio/voice/<사도>/*.wav
  · 소리 색인 → Resources/BolzenaAudio/audio_index.json — 파일마다 세기 맞춤(g) · 앞의 고요(lead) · 첫 마루(onset) · 길이,
    목소리 갈래, 사도 키 → 소리 · 목소리 폴더 (웹판 js/sfx.js 의 leadOf · onsetOf 를 미리 잰다)

  python Tools/fx_prepare.py --project C:\\projects\\bolzena-fx-test --heroes 에르핀,아멜리아
  python Tools/fx_prepare.py --project <경로> --heroes all            모든 사도(소리 · 목소리까지 — 크다)
  python Tools/fx_prepare.py --project <경로> --no-audio              이펙트만

이펙트(--fx)는 늘 전부 옮긴다(43MB). 소리는 공용 갈래(js/data/sfx-map.js 에 적힌 것) + 고른 사도의 폴더 + --monsters 의 적 폴더만.
옮긴 것은 원작 에셋이라 git 에 넣지 않는다(Assets/BolzenaFxData 를 .gitignore 에).
필요: Python 3 · Pillow · numpy · soundfile
"""
import argparse, json, os, re, shutil, sys

for _s in (sys.stdout, sys.stderr):
    try: _s.reconfigure(encoding="utf-8", errors="replace")
    except Exception: pass

WEB = r"C:\projects\볼제나"
VOICE_CATS = {"victory", "pleasure", "joy", "basicattack", "shout", "anger", "spskill", "ultimate", "hit", "surprise", "sorry",
              "die", "defeat", "sorrow", "spawn", "greeting", "powerattack", "skillcast"}


def log(m): print(m, flush=True)


def copy(src, dst):
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    if not os.path.exists(dst) or os.path.getsize(dst) != os.path.getsize(src):
        shutil.copyfile(src, dst)


def artmap(web):
    src = open(os.path.join(web, "js/data/artmap.js"), encoding="utf-8").read()
    art = src[src.index('"art"'):]
    return dict(re.findall(r'"([^"]+)":\s*"([a-z0-9_]+)"', art))


def standing_dirs(web):
    try:
        m = json.load(open(os.path.join(web, "assets/spine/manifest.json"), encoding="utf-8"))
        return {k: v["atlas"].replace(".atlas", "").lower() for k, v in (m.get("standing") or {}).items()}
    except Exception:
        return {}


# ── 이펙트 ──
def prep_fx(web, out):
    src = os.path.join(web, "assets", "fx")
    dst = os.path.join(out, "Src", "fx")
    n = 0
    for root, _, files in os.walk(src):
        for f in files:
            if not (f.endswith(".png") or f.endswith(".json")): continue
            rel = os.path.relpath(os.path.join(root, f), src)
            copy(os.path.join(root, f), os.path.join(dst, rel))
            n += 1
    log(f"이펙트 파일 {n}개 → {dst}")


def post_sheet(im, post):
    """웹판 fx-burst.js postSheet — 어두운 칸 → 주황 반투명(dark), 흰 칸 → 주황(white)."""
    import numpy as np
    from PIL import Image
    px = np.asarray(im.convert("RGBA")).astype(np.float32)
    r, g, b, a = px[..., 0], px[..., 1], px[..., 2], px[..., 3]
    mx = np.maximum(np.maximum(r, g), b); mn = np.minimum(np.minimum(r, g), b); l = mx / 255
    on = a > 0
    D, Wt = post.get("dark"), post.get("white")
    if D:
        x = np.clip(1 - l / D["below"], 0, 1); w = x * x * (3 - 2 * x) * on
        k = D["base"] + l * D["gain"]
        for c, t in ((r, D["tint"][0]), (g, D["tint"][1]), (b, D["tint"][2])):
            c += (np.minimum(255, t * k) - c) * w
        a *= 1 - w * (1 - D["alpha"])
    if Wt:
        sat = (mx - mn) / np.maximum(mx, 1)
        w = np.clip((Wt["sat"] - sat) / Wt["sat"], 0, 1) * np.clip((l - Wt["above"]) / (1 - Wt["above"]), 0, 1) * Wt["amt"] * on
        for c, t in ((r, Wt["tint"][0]), (g, Wt["tint"][1]), (b, Wt["tint"][2])):
            c += (t * l - c) * w
    out = np.stack([r, g, b, a], -1)
    return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8), "RGBA")


def prep_baked(web, out):
    from PIL import Image
    src = os.path.join(web, "assets", "fx-baked")
    dst = os.path.join(out, "Src", "baked")
    idx = json.load(open(os.path.join(src, "index.json"), encoding="utf-8"))
    os.makedirs(dst, exist_ok=True)
    for name, e in idx["effects"].items():
        for p in e["pages"]:
            png = os.path.join(dst, os.path.splitext(p["file"])[0] + ".png")
            if os.path.exists(png): continue
            os.makedirs(os.path.dirname(png), exist_ok=True)
            im = Image.open(os.path.join(src, p["file"])).convert("RGBA")
            if e.get("post"): im = post_sheet(im, e["post"])
            im.save(png, optimize=False)
    json.dump(idx, open(os.path.join(dst, "index.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    log(f"구운 판 {len(idx['effects'])}개 → {dst}")


# ── 소리 ──
def measure(data, sr):
    """웹판 sfx.js 의 onsetOf · leadOf — 첫 채널로 잰다(초)."""
    import numpy as np
    x = data if data.ndim == 1 else data[:, 0]
    x = x.astype(np.float64)
    n = max(1, round(sr * 0.02)); k = len(x) // n
    onset = 0.0
    if k > 0:
        rr = (x[:k * n].reshape(k, n) ** 2).sum(1)
        top = rr.max()
        if top > 0:
            i = int(np.argmax(rr >= top * 0.36)); onset = i * n / sr
    ax = np.abs(x); top = ax.max() if len(ax) else 0
    i = int(np.argmax(ax >= top * 0.03)) if top > 0 else 0
    lead = min(onset, max(0.0, i / sr - 0.004))
    return round(lead, 4), round(onset, 4)


def wav(src, dst):
    import soundfile as sf
    if os.path.exists(dst):
        data, sr = sf.read(dst)
    else:
        data, sr = sf.read(src)
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        sf.write(dst, data, sr, subtype="PCM_16")
    return data, sr


def sfx_map_paths(web):
    src = open(os.path.join(web, "js/data/sfx-map.js"), encoding="utf-8").read()
    out = set()
    for body in re.findall(r"\bf:\s*\[([^\]]*)\]", src):
        out.update(re.findall(r'"([^"]+)"', body))
    return sorted(out)


def prep_audio(web, out, heroes, monsters, art, stand):
    sfx_src = os.path.join(web, "assets", "sfx")
    voice_src = os.path.join(web, "assets", "voice")
    res = os.path.join(out, "Resources", "BolzenaAudio")
    sidx = json.load(open(os.path.join(sfx_src, "index.json"), encoding="utf-8"))
    vidx = json.load(open(os.path.join(voice_src, "index.json"), encoding="utf-8"))
    want = set(sfx_map_paths(web))
    hero_dirs = {}
    for k in heroes:
        a = art.get(k)
        if a: hero_dirs[k] = a.lower()
    for d in set(hero_dirs.values()):
        want.update(x for x in sidx if x.startswith(f"hero/{d}/") and x.count("/") == 2)
    for m in monsters:
        want.update(x for x in sidx if x.startswith(f"monster/{m}/") and x.count("/") == 2)
    sfx = {}
    for key in sorted(want):
        p = os.path.join(sfx_src, key + ".ogg")
        if not os.path.exists(p): continue
        data, sr = wav(p, os.path.join(res, "sfx", key + ".wav"))
        lead, onset = measure(data, sr)
        e = sidx.get(key, {})
        sfx[key] = {"g": e.get("g", 1), "lead": lead, "onset": onset, "d": round(len(data) / sr, 3)}
    log(f"효과음 {len(sfx)}개")
    voice, vdirs = {}, {}
    for k in heroes:
        dirs = [stand.get(k), art.get(k, "").lower()]
        d = next((x for x in dirs if x and x in vidx and vidx[x].get("base")), None)
        if not d: continue
        vdirs[k] = d
        if d in voice: continue
        cats = {}
        for c, files in vidx[d]["base"].items():
            files = [f for f in files if not re.search(r"_skin\d", f)]
            keep = c in VOICE_CATS
            for f in files:
                if keep:
                    wav(os.path.join(voice_src, f), os.path.join(res, "voice", os.path.splitext(f)[0] + ".wav"))
                    cats.setdefault(c, []).append(os.path.splitext(f)[0])
        voice[d] = cats
    log(f"목소리 {len(voice)}명 {sum(len(v) for c in voice.values() for v in c.values())}개")
    index = {"sfx": sfx, "voice": voice, "heroSfx": hero_dirs, "heroVoice": vdirs, "art": {k: v.lower() for k, v in art.items()}}
    os.makedirs(res, exist_ok=True)
    json.dump(index, open(os.path.join(res, "audio_index.json"), "w", encoding="utf-8"), ensure_ascii=False)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--web", default=WEB)
    ap.add_argument("--project", required=True)
    ap.add_argument("--heroes", default="", help="쉼표 · all")
    ap.add_argument("--monsters", default="fairymobcloserange,fairymoblongrange,curburus")
    ap.add_argument("--no-audio", action="store_true")
    ap.add_argument("--no-fx", action="store_true")
    a = ap.parse_args()
    out = os.path.join(a.project, "Assets", "BolzenaFxData")
    art = artmap(a.web)
    heroes = list(art) if a.heroes == "all" else [h.strip() for h in a.heroes.split(",") if h.strip()]
    if not a.no_fx:
        prep_fx(a.web, out)
        prep_baked(a.web, out)
    if not a.no_audio:
        prep_audio(a.web, out, heroes, [m for m in a.monsters.split(",") if m], art, standing_dirs(a.web))
    log("끝 — 이제 유니티에서 Bolzena.Fx.EditorTools.FxImport.ImportAll")


if __name__ == "__main__":
    main()
