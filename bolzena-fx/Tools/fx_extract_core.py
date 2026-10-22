# -*- coding: utf-8 -*-
"""(2026-10-15 볼제나 판 — 웹판 tools/extract-fx.py 를 복사해 고친 것. 웹판은 그대로 둔다.
  고침: 중심점에 세로 크기 비율(sy) 반영 · 원본 값(rm 그리는 법 · ra 정렬 · simW 시뮬 공간 · pv 원본 중심점 · sz3 축별 크기 · shn 셰이더 이름) 덧붙임)
궁극기 이펙트 추출기 — prefab/prefabeffectunit 의 fx_<사도>_ultimate_* 를 웹에서 돌릴 만큼만 풀어 둔다.

이펙트 번들 하나에는 ParticleSystem 이 20개쯤 든다. 재질·텍스처는 공용 번들 assetdata/unmanaged/assetbundles 에
있어서 둘을 한 환경에 올려야 렌더러 → 재질 → 텍스처가 이어진다.
모듈을 다 옮기지 않는다 — 방출(버스트·초당)·모양·처음 값·수명에 따른 크기·색·시트 애니메이션·속도·힘만 대충 옮긴다.
셰이더 그래프(디졸브·노이즈·UV 흐름)는 못 옮긴다. 주 텍스처 하나에 색만 곱한다.

  python tools/extract-fx.py                     기기에서 받아(이미 받은 것은 assets/_fxraw 재사용) 전부 푼다
  python tools/extract-fx.py --only erpin,amelia 몇 명만 (색인은 그 몇 명만 고쳐 쓴다)
  python tools/extract-fx.py --pull              기기에서 다시 받는다

나오는 것: assets/fx/index.json · assets/fx/<이름>/fx.json · assets/fx/<이름>/*.png · assets/fx/_shared/*.png
adb 로 복사해 UnityPy 로 읽기만 한다. 암호화된 table 은 건드리지 않는다. 꺼낸 것은 저장소에 넣지 않는다(assets/ 는 gitignore).
필요: adb (앱플레이어 것), UnityPy, Pillow
"""
import argparse, hashlib, json, math, os, re, shutil, subprocess, sys
from PIL import Image, ImageChops

PKG = "com.epidgames.trickcalrevive"
BASE = f"/sdcard/Android/data/{PKG}/files/Packages"
ENV = dict(os.environ)
ENV["MSYS_NO_PATHCONV"] = "1"
ENV.pop("ELECTRON_RUN_AS_NODE", None)
ADB_CANDIDATES = [
    r"C:\Program Files\Netease\MuMuPlayer\nx_main\adb.exe",
    r"C:\Program Files\Netease\MuMu Player 12\shell\adb.exe",
    r"D:\Program Files\Netease\MuMuPlayer\nx_main\adb.exe",
]
PORTS = ["127.0.0.1:16384", "127.0.0.1:16416", "127.0.0.1:7555", "127.0.0.1:5555"]
ROOT = "C:/projects/볼제나"   # (10-15) 볼제나 판 — 웹판 자료(js/data/artmap.js · assets/_fxraw)를 읽는다
DEP = "assetbundles"          # 공용 번들 — 재질·텍스처·메시가 다 여기 있다
MAXTEX = 512
ATLASES = ("fx_pack_sprite", "fx_unit_sprite", "fx_pack_common_seq")   # effectatlases — 스프라이트 이미터의 그림
# 그림 이름과 이펙트 이름이 다른 사도 — 게임 쪽 철자를 따른다
ALIAS = {"benibeni": "beinibeni", "renewaawaken": "renewa"}
NO_FX = {"ui"}   # fx_ui_* 는 화면(UI) 이펙트라 우이 것이 아니다
MOODS = ["jolly", "cool", "mad", "naive", "gloomy", "resonance"]
MOOD = re.compile(r"_(" + "|".join(MOODS) + r")(?=_|$)")
SKIP = re.compile(r"storybattle|minigame|_skin\d*($|_)|_default$")

for _s in (sys.stdout, sys.stderr):
    try: _s.reconfigure(encoding="utf-8", errors="replace")
    except Exception: pass


def log(m): print(m, flush=True)


def adb(exe, dev, *a, timeout=900):
    return subprocess.run([exe, "-s", dev, *a], env=ENV, capture_output=True, timeout=timeout)


def find_device(exe, serial):
    for dev in ([serial] if serial else PORTS):
        subprocess.run([exe, "connect", dev], env=ENV, capture_output=True, timeout=30)
        p = adb(exe, dev, "shell", f"ls {BASE}/prefab/prefabeffectunit | head -1", timeout=30)
        if p.returncode == 0 and p.stdout.strip(): return dev
    return None


def pull(raw, args):
    """공용 번들과 prefabeffectunit 폴더를 통째로 받는다 — 하나씩 받는 것보다 훨씬 빠르다(130MB 몇 초)."""
    have = all(os.path.exists(os.path.join(raw, f)) for f in (DEP, "prefabeffectunit", "effectatlases"))
    if have and not args.pull: return
    exe = args.adb or next((p for p in ADB_CANDIDATES if os.path.exists(p)), None) or shutil.which("adb")
    if not exe: sys.exit("adb 를 못 찾았습니다. --adb 로 앱플레이어의 adb.exe 를 알려 주세요")
    dev = find_device(exe, args.serial)
    if not dev: sys.exit("트릭컬이 깔린 기기를 못 찾았습니다. 앱플레이어를 켜고 게임을 한 번 실행해 두세요")
    log(f"기기 {dev} — 받는 중")
    os.makedirs(raw, exist_ok=True)
    for src, dst in ((f"{BASE}/assetdata/unmanaged/{DEP}", os.path.join(raw, DEP)), (f"{BASE}/prefab/prefabeffectunit", raw), (f"{BASE}/effectatlases", raw)):
        p = adb(exe, dev, "pull", src, dst)
        if p.returncode != 0: sys.exit(f"복사 실패: {p.stderr.decode('utf-8', 'ignore')[:300]}")


def roster():
    """js/data/artmap.js 의 art 칸 — 사도 키 → 그림 이름."""
    src = open(os.path.join(ROOT, "js/data/artmap.js"), encoding="utf-8").read()
    art = src[src.index('"art"'):]
    return dict(re.findall(r'"([^"]+)":\s*"([a-z0-9_]+)"', art))


def pick(name, files):
    """그 사도의 궁극기 이펙트. 없으면 스킬 이펙트로 메운다(트릭컬의 고학년 스킬 연출이 스파인 안에 든 사도들)."""
    n = ALIAS.get(name, name)
    def grab(pat):
        got = [f for f in files if re.match(rf"^fx_{re.escape(n)}_{pat}", f)]
        keep = [f for f in got if not SKIP.search(f)]
        # 기본판 없이 _default 만 있으면 그것을 쓴다
        keep += [f for f in got if f.endswith("_default") and f[:-8] not in got]
        # 성격(cool·gloomy·jolly·mad·naive·resonance)마다 따로 있는 판은 한 갈래만 — 앞의 것을 고른다
        out, seen = [], set()
        for f in sorted(set(k for k in keep if " " not in k), key=lambda f: (MOODS.index(m.group(1)) if (m := MOOD.search(f)) else -1, f)):
            base = MOOD.sub("_", f).removesuffix("_default")
            if base in seen: continue
            seen.add(base); out.append(f)
        return sorted(out)
    if n in NO_FX: return [], None
    # 마지막은 평타·어사이드·등장 말고 그 사도의 아무 이펙트(마요처럼 이름에 갈래가 없는 사도)
    for pat, src in (("ultimate", "ult"), ("ult_", "ult"), ("skill(?!_signature)", "skill"), ("personal", "skill"),
                     ("(?!attack|aside|spawn|move|emoji|storybattle)", "other")):
        got = grab(pat)
        if got: return got, src
    return [], None


# ── 커브·그라디언트 ──
def hermite(keys, t):
    if not keys: return 0.0
    if t <= keys[0]["time"]: return keys[0]["value"]
    if t >= keys[-1]["time"]: return keys[-1]["value"]
    for a, b in zip(keys, keys[1:]):
        if a["time"] <= t <= b["time"]:
            dt = b["time"] - a["time"]
            if dt <= 0: return b["value"]
            s = (t - a["time"]) / dt
            m0, m1 = a["outSlope"] * dt, b["inSlope"] * dt
            if not (math.isfinite(m0) and math.isfinite(m1)): return a["value"]   # 계단 키
            s2, s3 = s * s, s * s * s
            return (2*s3 - 3*s2 + 1) * a["value"] + (s3 - 2*s2 + s) * m0 + (-2*s3 + 3*s2) * b["value"] + (s3 - s2) * m1
    return keys[-1]["value"]


def r3(v):
    if isinstance(v, float):
        if not math.isfinite(v): return 0
        if v == 0: return 0
        return float(f"{v:.3g}")
    if isinstance(v, (list, tuple)): return [r3(x) for x in v]
    return v


def mm_range(c):
    """MinMaxCurve → [작은, 큰]. 커브면 그 커브가 닿는 범위."""
    st, s, lo = c["minMaxState"], c["scalar"], c["minScalar"]
    if st == 0: return [s, s]
    if st == 3: return sorted([lo, s])
    vals = [hermite(c["maxCurve"]["m_Curve"], i / 8) * s for i in range(9)]
    if st == 2: vals += [hermite(c["minCurve"]["m_Curve"], i / 8) * s for i in range(9)]
    return [min(vals), max(vals)]


def mm_mean(c):
    a, b = mm_range(c)
    if c["minMaxState"] in (1, 2):
        vals = [hermite(c["maxCurve"]["m_Curve"], i / 8) * c["scalar"] for i in range(9)]
        if c["minMaxState"] == 2: vals = [(v + hermite(c["minCurve"]["m_Curve"], i / 8) * c["scalar"]) / 2 for i, v in enumerate(vals)]
        return sum(vals) / len(vals)
    return (a + b) / 2


def mm_keys(c, n=5):
    """수명에 걸친 값 → [[t, v]…]. 상수면 null."""
    st, s = c["minMaxState"], c["scalar"]
    if st in (0, 3): return None
    out = []
    for i in range(n):
        t = i / (n - 1)
        v = hermite(c["maxCurve"]["m_Curve"], t) * s
        if st == 2: v = (v + hermite(c["minCurve"]["m_Curve"], t) * s) / 2
        out.append([t, v])
    return r3(out)


def grad(g):
    """Gradient → (색 키 [[t,r,g,b]…], 알파 키 [[t,a]…])."""
    nc, na = max(1, g["m_NumColorKeys"]), max(1, g["m_NumAlphaKeys"])
    ck = [[g[f"ctime{i}"] / 65535, g[f"key{i}"]["r"], g[f"key{i}"]["g"], g[f"key{i}"]["b"]] for i in range(nc)]
    ak = [[g[f"atime{i}"] / 65535, g[f"key{i}"]["a"]] for i in range(na)]
    return sorted(ck), sorted(ak)


def grad_at(g, t):
    ck, ak = grad(g)
    def lerp(keys, t):
        if t <= keys[0][0]: return keys[0][1:]
        for a, b in zip(keys, keys[1:]):
            if a[0] <= t <= b[0]:
                s = (t - a[0]) / ((b[0] - a[0]) or 1)
                return [x + (y - x) * s for x, y in zip(a[1:], b[1:])]
        return keys[-1][1:]
    return lerp(ck, t) + lerp(ak, t)


def col(c): return [c["r"], c["g"], c["b"], c["a"]]


def start_colors(m):
    st = m["minMaxState"]
    if st == 0: return [col(m["maxColor"])], False
    if st == 2: return [col(m["minColor"]), col(m["maxColor"])], True
    if st in (1, 4): return [grad_at(m["maxGradient"], t) for t in (0, .33, .66, 1)], False
    return [grad_at(m["minGradient"], t) for t in (0, .5, 1)] + [grad_at(m["maxGradient"], t) for t in (0, .5, 1)], False


# ── 변환 ──
def qmat(q):
    x, y, z, w = q["x"], q["y"], q["z"], q["w"]
    return [[1 - 2*(y*y + z*z), 2*(x*y - z*w), 2*(x*z + y*w)],
            [2*(x*y + z*w), 1 - 2*(x*x + z*z), 2*(y*z - x*w)],
            [2*(x*z - y*w), 2*(y*z + x*w), 1 - 2*(x*x + y*y)]]


def euler(e):
    """유니티 오일러(도, ZXY 순) → 행렬."""
    rx, ry, rz = (math.radians(e[k]) for k in ("x", "y", "z"))
    cx, sx, cy, sy, cz, sz = math.cos(rx), math.sin(rx), math.cos(ry), math.sin(ry), math.cos(rz), math.sin(rz)
    X = [[1, 0, 0], [0, cx, -sx], [0, sx, cx]]
    Y = [[cy, 0, sy], [0, 1, 0], [-sy, 0, cy]]
    Z = [[cz, -sz, 0], [sz, cz, 0], [0, 0, 1]]
    return mul(Y, mul(X, Z))


def mul(a, b): return [[sum(a[i][k] * b[k][j] for k in range(3)) for j in range(3)] for i in range(3)]
def mv(a, v): return [sum(a[i][k] * v[k] for k in range(3)) for i in range(3)]
def diag(s): return [[s[0], 0, 0], [0, s[1], 0], [0, 0, s[2]]]


# 원작 카메라는 20° 내려다본다 — 이펙트 뿌리가 대개 X 로 20° 기울어 있고, 바닥 데칼은 X 90° 로 누워 있다.
# 그래서 3D → 화면은 z 를 버리지 않고 화면 y = y·cos + z·sin 으로 떨군다(누운 고리가 납작한 타원으로 보이게)
CAM = math.radians(20)
CY, SY = math.cos(CAM), math.sin(CAM)


def view(v): return [v[0], CY * v[1] + SY * v[2]]


def rows(A):
    """3×3 → 화면 두 줄 [xx, xy, xz, yx, yy, yz]."""
    return [A[0][0], A[0][1], A[0][2]] + [CY * A[1][j] + SY * A[2][j] for j in range(3)]


SHAPES = {0: "sphere", 1: "sphere", 2: "hemi", 3: "hemi", 4: "cone", 7: "cone", 8: "cone", 9: "cone", 5: "box", 15: "box", 16: "box",
          10: "circle", 11: "circle", 12: "edge", 17: "donut", 18: "rect", 6: "box", 13: "box", 14: "box", 19: "rect", 20: "rect"}


def shrink(img):
    w, h = img.size
    if max(w, h) <= MAXTEX: return img
    k = MAXTEX / max(w, h)
    return img.resize((max(1, round(w * k)), max(1, round(h * k))), Image.LANCZOS)


def entry(name, img):
    if img.mode != "RGBA": img = img.convert("RGBA")
    a = img.getchannel("A").getextrema()
    return {"name": name, "img": shrink(img), "opaque": a[0] >= 250, "users": set()}


class Ctx:
    def __init__(self, dep_path, atlas_paths):
        import UnityPy
        self.UnityPy = UnityPy
        self.dep_bytes = open(dep_path, "rb").read()
        # 스프라이트 아틀라스 — [Atlas_*] 이미터는 재질이 비어 있고(FX_empty_add) UV 모듈의 스프라이트가 그림이다.
        # 스프라이트는 아틀라스를 늦게 묶어(m_SpriteAtlas 가 비었다) 렌더 키로 아틀라스 칸을 찾는다
        self.extra = [open(p, "rb").read() for p in atlas_paths]
        self.atlas = {}
        sheets = {}
        for p in atlas_paths:
            env = UnityPy.load(p)
            for o in env.objects:
                if o.type.name == "Texture2D": sheets[o.path_id] = o
            for o in env.objects:
                if o.type.name != "SpriteAtlas": continue
                for key, v in o.read_typetree()["m_RenderDataMap"]:
                    k = tuple(key[0][f"data[{i}]"] for i in range(4)) + (key[1],)
                    self.atlas[k] = (v["texture"]["m_PathID"], v["textureRect"], v["textureRectOffset"], v["settingsRaw"])
        self.sheets = sheets
        self.sheet_img = {}
        self.tex = {}       # 열쇠 → {name, img, opaque, users:set}
        self.blend = {}     # shader path_id → (src, dst, srcName, dstName)
        self.mesh = {}
        self.last_mask = None   # 마지막 material() 이 찾은 가리개 그림 열쇠
        self.last_sg = None     # 마지막 material() 이 만든 셰이더 그래프 재질 값

    def shader_blend(self, sh):
        pid = sh.path_id
        if pid not in self.blend:
            try:
                d = sh.deref().read_typetree()["m_ParsedForm"]
                st = d["m_SubShaders"][0]["m_Passes"][0]["m_State"]["rtBlend0"]
                self.blend[pid] = (st["srcBlend"]["val"], st["destBlend"]["val"], st["srcBlend"]["name"], st["destBlend"]["name"], d["m_Name"])
            except Exception:
                self.blend[pid] = None
        return self.blend[pid]

    def texture(self, pp, bake=None):
        key = ("t", pp.path_id, bake[0] if bake else None)
        if key not in self.tex:
            t = pp.deref().read()
            img, name = t.image, t.m_Name
            if bake: img = bake[1](shrink(img.convert("RGBA"))); name += "_" + bake[0]
            self.tex[key] = entry(name, img)
        return key, self.tex[key]

    def sprite_img(self, pp):
        """스프라이트 하나 → 원래 크기의 그림(아틀라스에서 잘라 원래 자리에 둔다)."""
        sd = pp.deref().read_typetree()
        rk = sd["m_RenderDataKey"]
        k = tuple(rk[0][f"data[{i}]"] for i in range(4)) + (rk[1],)
        rect = sd["m_Rect"]
        if k in self.atlas:
            tid, tr, off, raw = self.atlas[k]
            if tid not in self.sheet_img: self.sheet_img[tid] = self.sheets[tid].read().image.convert("RGBA")
            sheet = self.sheet_img[tid]
            x, y, w, h = tr["x"], tr["y"], tr["width"], tr["height"]
            top = sheet.height - (y + h)
            cut = sheet.crop((round(x), round(top), round(x + w), round(top + h)))
            rot = (raw >> 2) & 0xF
            if rot == 1: cut = cut.transpose(Image.FLIP_LEFT_RIGHT)
            elif rot == 2: cut = cut.transpose(Image.FLIP_TOP_BOTTOM)
            elif rot == 3: cut = cut.transpose(Image.ROTATE_180)
            elif rot == 4: cut = cut.transpose(Image.ROTATE_90)
            W, H = max(1, round(rect["width"])), max(1, round(rect["height"]))
            img = Image.new("RGBA", (W, H))
            img.paste(cut, (round(off["x"]), round(H - off["y"] - cut.height)))
            return sd["m_Name"], img
        return sd["m_Name"], pp.deref().read().image

    def sprites(self, pps):
        """UV 모듈의 스프라이트들 → 가로 띠 한 장(여럿이면 시트처럼 칸을 나눈다)."""
        key = ("s",) + tuple(p.path_id for p in pps)
        if key not in self.tex:
            imgs = [self.sprite_img(p) for p in pps]
            if len(imgs) == 1:
                self.tex[key] = entry(imgs[0][0], imgs[0][1])
            else:
                W = max(i.width for _, i in imgs); H = max(i.height for _, i in imgs)
                strip = Image.new("RGBA", (W * len(imgs), H))
                for j, (_, i) in enumerate(imgs): strip.paste(i, (j * W + (W - i.width) // 2, (H - i.height) // 2))
                self.tex[key] = entry(imgs[0][0] + f"_x{len(imgs)}", strip)
        return key, self.tex[key]

    def mesh_extent(self, pp):
        """메시 → [반폭 x·y·z, 가운데 x·y·z, 판 매핑 여부]."""
        if not pp or not pp.path_id: return None
        pid = pp.path_id
        if pid not in self.mesh:
            try:
                d = pp.deref().read_typetree()
                e = d["m_LocalAABB"]["m_Extent"]; c = d["m_LocalAABB"]["m_Center"]
                self.mesh[pid] = [e["x"], e["y"], e["z"], c["x"], c["y"], c["z"], uv_planar(pp.deref().read())]
            except Exception:
                self.mesh[pid] = None
        return self.mesh[pid]


    def mesh_data(self, pp):
        """(10-15) 메시 몸 — 꼭짓점 xyz · UV · 삼각형 · 꼭짓점 알파(있으면). 휘는 칼빛 · 띠를 판 대신 그대로 그리려고."""
        if not pp or not pp.path_id: return None
        key = ("md", pp.path_id)
        if key not in self.mesh:
            try:
                from UnityPy.helpers.MeshHelper import MeshHandler
                h = MeshHandler(pp.deref().read()); h.process()
                V, U = h.m_Vertices or [], h.m_UV0 or []
                T = [i for sub in h.get_triangles() for tri in sub for i in tri]
                A = [c[3] for c in (h.m_Colors or [])]
                ok = 4 < len(V) <= MESH_MAX_V and len(U) == len(V) and T
                self.mesh[key] = {"v": V, "u": U, "t": T, "a": A if len(A) == len(V) and min(A) < 0.99 else None} if ok else None
            except Exception:
                self.mesh[key] = None
        return self.mesh[key]


MESH_MAX_V = 3000

# 셰이더 그래프 재현(10-15) — 원작 셰이더 이름 → 우리 셰이더 갈래. 값 · 그림 이름은 원작 재질 속성 그대로
SG_KIND = {"Shader Graphs/FX_Master": "master", "Custom/FX_Master_Mask": "master", "Shader Graphs/FX_TEX_Tint": "tint", "Custom/FX_TEX_Tint_Mask": "tint",
           "Shader Graphs/FX_TEX_Tint_Color": "tint_color", "Shader Graphs/FX_Simple_Dissolve_Step": "dstep", "Shader Graphs/FX_Move_UV_Mask": "moveuv",
           "Shader Graphs/FX_Simple_Dissolve_Smoothstep": "dsmooth", "Shader Graphs/FX_UV_Offset_ab": "uvoff",
           "Shader Graphs/FX_TEX_Tint_Front": "tint", "Shader Graphs/FX_TEX_Tint_Back": "tint_back", "Shader Graphs/FX_Toon_Explosion_01": "toon",
           "Shader Graphs/FX_Toon_Dissolve_RB": "toon_rb", "Shader Graphs/FX_Pixel": "pixel", "Shader Graphs/FX_Simple_Dissolve_Gradient": "dgrad",
           "Shader Graphs/FX_Water_Polar": "water", "Shader Graphs/FX_Shockwave": "shock", "Shader Graphs/FX_Slash_Dissolve_Color": "slash",
           "Shader Graphs/FX_Slash_Dissolve_01": "slash01", "Shader Graphs/FX_Dissolve_Edge": "dedge",
           "Custom/FX_Water_Polar_Mask": "water", "Shader Graphs/FX_Toon_Dissolve_RB_Outline": "toon_ol",
           "Shader Graphs/FX_UV_Move_Dissolve_1": "uvmd", "Shader Graphs/FX_Master_Trail": "trail"}
# (10-16) 원작 재질 속성 이름이 우리 셰이더와 다른 갈래 — 뽑을 때 바꿔 넘긴다
SG_RENAME = {"_MainTex": "_Main_Tex", "_MaskTex": "_Mask_Tex", "_NoiseTex": "_Noise_Tex", "_MainTex_Alpha": "_Main_Tex_Alpha", "_MainTex_Rotate": "_Main_Tex_Rotate",
             "_MainTex_Tilling": "_Main_Tex_Tiling", "_MainPanner": "_UV_Offset", "_NoisePanner": "_Noise_Panner", "_NoisePower": "_Noise_Mul",
             "_MainAlphaSwitch": "_Main_Tex_Alpha", "_MainPower": "_Main_Tex_Power", "_MainIntensity": "_Main_Tex_Mul", "_MainRotate": "_Main_Tex_Rotate"}
SG_TEX = ("_MainTex", "_MaskTex", "_NoiseTex", "_Main_Tex", "_Mask_Tex", "_Dissolve_Tex", "_Color_Tex", "_Noise_Tex", "_Texture2D", "_Alpha_Tex", "_Distort_Tex", "_Color", "_Dissolve_Tex_Add", "_Main_Mask")
SG_FLOATS = ("_Use_Custom2", "_Main_Tex_Gray", "_Main_Tex_Pow", "_Main_Tex_Alpha", "_Main_FlatColor", "_Main_Tex_Rotation", "_Dissolve_Smooth",
             "_Use_Dissovlve_Tex", "_Dissolve_Tex_Rotation", "_Color_Tex_Rotation", "_Color_Gra", "_Color_Pow", "_Color_Affect", "_Use_Noise",
             "_Noise_Polar", "_Mask_NoiseAffect", "_Mask_Tex_Rotation", "_SrcBlend", "_DstBlend",
             "_Main_Tex_Mul", "_Main_Tex_Rotate", "_Color_Tex_Mul", "_Color_Tex_Rotate", "_Main_Tex_Power", "_Main_Tex_Color",
             "_Pixel", "_OpacitySaturation", "_Noise_Mul", "_Main_Tex_Scale", "_Main_Tex_RGBA_R",
             "_Alpha_Tex_Rotation", "_Color_Rotate", "_Mask_Rotate", "_Alpha_Int", "_Alpha_Pow", "_Color_Power", "_Mask_Pow", "_Mask_Str",
             "_Use_DissolveTex", "_Use_AlphaTex", "_Affected_Noise", "_Main_Alpha", "_Add_Main",
             "_MainTex_Alpha", "_MainTex_Rotate", "_MainAlphaSwitch", "_MainPower", "_MainIntensity", "_MainRotate", "_NoisePower",
             "_Edge", "Vector1_37492e6f4f974286867e7ab21900492e", "_Dissolve_Mask_Mul", "_Dissolve_Mask_L_R", "_Main_Tex_Move_Offest")
SG_VECS = ("_Main_Tex_Panning", "_Color_Tiling", "_Noise_Panner", "_Main_Tex_Tiling", "_UV_Offset", "_MinMaxSmoothStepDissolve", "_Noise_Polar_Radial_Length",
           "_Alpha_Panning", "_Noise_Panning", "_Color_Offset", "_Tiling", "_Offset", "_Tiling_D", "_Offset_D", "_MainTex_Tilling", "_MainPanner", "_NoisePanner", "Color_3f2a675068714c89b0bea054e216b557")   # 이보다 큰 메시는 옮기지 않는다(장 크기)


def uv_planar(mesh):
    """UV 가 자리의 일차식인가 — 판(쪼갠 판 포함)이면 사각형 한 장으로 그려도 그림이 맞는다.
    나무·고리·띠처럼 UV 를 메시 따라 펼친 것은 사각형에 얹으면 그라디언트 띠가 각진 덩어리로 보인다."""
    try:
        import numpy as np
        from UnityPy.helpers.MeshHelper import MeshHandler
        h = MeshHandler(mesh); h.process()
        V, U = h.m_Vertices, h.m_UV0
        if not V or not U or len(V) != len(U): return True
        if len(V) <= 4: return True
        A = np.c_[np.array(V, float)[:, :3], np.ones(len(V))]
        B = np.array(U, float)[:, :2]
        X, *_ = np.linalg.lstsq(A, B, rcond=None)
        err = np.abs(A @ X - B).max()
        return bool(err < 0.03 * max(1e-6, float(np.ptp(B, axis=0).max())))
    except Exception:
        return True


def hard_edge(img, sheet, add):
    """칸 가장자리가 차 있나(평균 0.3 넘게) — 사각형 한 장으로 그리면 네모난 테두리가 보인다."""
    import numpy as np
    px = np.asarray(img.convert("RGBA"), float) / 255
    a = px[..., 3] * (px[..., :3].max(-1) if add or px[..., 3].min() > .98 else 1)
    tx, ty = (sheet["tx"], sheet["ty"]) if sheet else (1, 1)
    H, W = a.shape; h, w = max(1, H // ty), max(1, W // tx)
    for j in range(ty):
        for i in range(tx):
            c = a[j * h:(j + 1) * h, i * w:(i + 1) * w]
            if c.size and max(c[0].mean(), c[-1].mean(), c[:, 0].mean(), c[:, -1].mean()) > .3: return True
    return False


def toon(im, hot, mid):
    """툰 폭발 — R 이 빛, 알파가 모양. 밝은 쪽은 뜨거운 색, 그늘은 본색 두 톤으로."""
    r, g, b, a = im.split()
    if a.getextrema()[0] >= 250:      # 알파 없는 판 — 검은 바탕을 지운다
        a = Image.eval(Image.merge("RGB", (r, g, b)).convert("L"), lambda v: min(255, v * 3))
    lit = r.point(lambda v: 255 if v > 150 else (0 if v < 110 else (v - 110) * 255 // 40))
    hi = Image.new("RGB", im.size, tuple(int(x * 255) for x in hot))
    lo = Image.new("RGB", im.size, tuple(int(x * 255) for x in mid))
    out = Image.composite(hi, lo, lit).convert("RGBA")
    out.putalpha(a.point(lambda v: 255 if v > 90 else v * 255 // 90))
    return out


def alum(im):
    """밝기 → 알파, 색은 그대로(셰이더 그래프 재현용 — R 을 알파로 쓰는 재질도 그대로 맞게)."""
    r, g, b, _ = im.split()
    return Image.merge("RGBA", (r, g, b, ImageChops.lighter(ImageChops.lighter(r, g), b)))


def lum(im):
    """밝기 → 알파. 색은 가장 밝은 채널로 나눠 살린다(불꽃 그림이 검은 바탕째 덮이지 않게)."""
    r, g, b, _ = im.split()
    mx = ImageChops.lighter(ImageChops.lighter(r, g), b)
    k = mx.point(lambda v: min(255, 255 * 255 // v) if v else 0)
    return Image.merge("RGBA", (*[ImageChops.multiply(c, k) for c in (r, g, b)], mx))


def dissolve(im):
    """디졸브 — 밝기 0.4~0.8 을 문턱으로 어두운 쪽을 지운다."""
    l = im.convert("L").point(lambda v: 0 if v < 100 else (255 if v > 205 else (v - 100) * 255 // 105))
    return Image.composite(im.convert("RGB"), Image.new("RGB", im.size), l).convert("RGBA")


NOISE = re.compile(r"noise|distort", re.I)
TINT_KEYS = ("_TintColor", "_Color", "_Main_Color", "_MainColor", "_BaseColor", "_Color_1")
MAIN_KEYS = ("_Main_Tex", "_MainTex", "_BaseMap", "_Texture2D")
# 주 그림이 노이즈면 마스크 칸 그림으로 대신 그린다(material 참고) — 기본 끔(None).
#   10-14 시도: 마스크를 주 그림으로 쓰면 원작 모양이 안 나옴(노이즈 합성 셰이더 효과라) — 실비아 석상 · 우이(기억) 클로버 등 44개를 살려 캡처해 보니
#   모양이 다르고 에르핀(왕도) 구체는 속이 탁한 올리브 · 회색이 돼 되돌렸다. 켜려면 사도 전용 마스크 이름 정규식(예: re.compile(r"^FX_Silvia_Statue_"))
MASK_AS_MAIN = None


def material(ctx, rend_obj, hero, ps_obj, emname):
    """재질 → (텍스처 열쇠, 더하기 여부, 색, 셰이더 이름, 스프라이트 수). 못 읽으면 None."""
    ctx.last_mask = None; ctx.last_sg = None
    r = rend_obj.read()
    if not r.m_Materials: return None
    pp = r.m_Materials[0]
    if not pp.path_id: return None
    try: mo = pp.deref()
    except Exception: return None
    md = mo.read_typetree()
    mat = mo.read()
    props = md["m_SavedProperties"]
    fl = dict(props["m_Floats"])
    cols = dict(props["m_Colors"])
    texpp = {}
    for k, v in mat.m_SavedProperties.m_TexEnvs:
        if v.m_Texture and v.m_Texture.path_id: texpp[k] = v.m_Texture
    main = next((texpp[k] for k in MAIN_KEYS if k in texpp), None)
    if main is None:
        main = next((v for k, v in texpp.items() if k not in ("_Dissolve_Tex", "_Noise_Tex", "_Distort_Tex", "_Mask_Tex", "_Color_Tex")), None)
    # 섞는 법 — 셰이더 패스의 블렌드, 재질 속성에 묶였으면 그 값, 그것도 없으면 이름
    add = None; shname = ""
    b = ctx.shader_blend(mat.m_Shader) if mat.m_Shader and mat.m_Shader.path_id else None
    if b:
        src, dst, sn, dn, shname = b
        if dn and dn != "<noninit>" and dn in fl: dst = fl[dn]
        add = dst == 1
    else:
        nm = ((md["m_Name"] or "") + " " + emname).lower()
        add = ("add" in nm) and not re.search(r"_ab\b|\[atlas_ab\]|alpha", nm)
    # 셰이더 그래프 몇 가지는 텍스처를 그대로 보여 주지 않는다 — 비슷하게 보이도록 그림을 미리 구워 둔다
    bake = None; over = None
    if "Toon_Explosion" in shname:
        def c3(k, d): c = cols.get(k); return [c["r"], c["g"], c["b"]] if c else d
        mid = c3("_Main_Color", [1, .3, 0]); hot = [min(1, x * fl.get("_Main_Color_Mul", 4)) for x in mid]
        bake = ("toon", lambda im: toon(im, hot, mid))
        # 불로 피었다가 연기로 식으며 사라진다(원작은 꼭짓점 알파로 디졸브를 민다)
        over = {"add": False, "tint": [1, 1, 1, 1], "colOL": [[0, 1, 1, 1], [0.18, 1, 1, 1], [0.42, .5, .42, .38], [1, .3, .26, .24]],
                "alphaOL": [[0, 1], [0.55, .9], [1, 0]]}
    nspr = 0
    try:
        uv = ps_obj.read().UVModule
        sp = [x.sprite for x in uv.sprites if x.sprite and x.sprite.path_id] if uv.enabled and uv.mode == 1 else []
    except Exception: sp = []
    try:
        if sp: tkey, tx = ctx.sprites(sp); nspr = len(sp)
        elif main is not None:
            tkey, tx = ctx.texture(main, bake)
            # 디졸브 셰이더 + 알파 없는 회색 그림 — 문턱을 넘는 밝은 쪽만 보인다(원 그라디언트 → 고리)
            if not bake and "Dissolve" in shname and tx["opaque"]:
                tkey, tx = ctx.texture(main, ("dslv", dissolve))
        else: return None
    except Exception: return None
    # 주 그림이 노이즈인데 모양은 마스크 칸이 정하는 셰이더 그래프(실비아 석상 · 우이(기억) 클로버 · 에르핀(왕도) 구체 바람 …, 2026-10-14) —
    #   마스크(없으면 알파) 칸 그림을 주 그림으로 쓴다. 노이즈 흐름은 버리고, 색은 아래 재질 색 · 틴트 · 색 램프 그대로
    if MASK_AS_MAIN and not sp and NOISE.search(tx["name"]) and "Distort" not in shname:
        mk = next((texpp[k] for k in ("_Mask_Tex", "_Alpha_Tex") if k in texpp), None)
        if mk is not None:
            try:
                k2, t2 = ctx.texture(mk)
                if MASK_AS_MAIN.match(t2["name"]): main, tkey, tx = mk, k2, t2
            except Exception: pass
    # 노이즈 · 일그러뜨림 그림은 다른 그림을 흔드는 재료다 — 그대로 그리면 회색 네모가 된다
    if not sp and (NOISE.search(tx["name"]) or "Distort" in shname): return None
    # 알파 없는 텍스처(검은 바탕) — 셰이더가 밝기를 알파로 쓴다. 더하기면 그대로, 알파 섞기면 밝기를 알파로 구워 둔다
    # (10-15) FX_TEX_Tint 계열은 _Main_Tex_Alpha = 0 이면 그림 알파 대신 밝기를 투명도로 쓴다(784 재질 중 520) — 알파가 있는 그림이라도 밝기로 구워야
    #   어두운 바탕이 회색 판으로 남지 않는다(디아나(왕년) 고학년 회색 기둥 · 바람 띠)
    #   → 해 보니(10-15) 네르(빡침) 고학년 거대한 흰 십자 · 요미 · 우로스 네모 판이 생겨 되돌렸다(알파를 이미 쓰는 그림까지 밝기로 바꾸면 바탕이 판으로 남는다). 끔
    tint_lum = False
    if (tx["opaque"] or tint_lum) and not add and not bake:
        tkey, tx = ctx.texture(main, ("lum", lum)) if not sp else (tkey, tx)
        if sp: add = True
    tint = [1, 1, 1, 1]
    # Mobile/Particles/* 는 재질 색을 안 쓴다(그림 × 입자 색) — 남은 _Color 를 곱하면 알리스 불꽃이 갈색이 된다
    for k in (() if shname.startswith("Mobile/Particles") else TINT_KEYS):
        if k in cols:
            c = cols[k]; tint = [c["r"], c["g"], c["b"], c["a"]]
            if k == "_TintColor": tint = [x * 2 for x in tint[:3]] + [min(1, tint[3] * 2)]   # 옛 파티클 셰이더는 ×2
            break
    mulv = fl.get("_Main_Tex_Mul") or fl.get("_Intensity") or 1
    if 0 < mulv < 8: tint = [x * mulv for x in tint[:3]] + [tint[3]]
    # 색 램프(_Color_Tex)로 칠하는 셰이더 — 램프의 밝은 쪽 색을 곱해 흉내 낸다
    if "_Color_Tex" in texpp and main is not texpp["_Color_Tex"]:
        try:
            ramp = ctx.texture(texpp["_Color_Tex"])[1]["img"]
            px = ramp.getpixel((int(ramp.width * 0.7), ramp.height // 2))
            tint = [tint[0] * px[0] / 255, tint[1] * px[1] / 255, tint[2] * px[2] / 255, tint[3]]
        except Exception: pass
    if tint[3] < 0.05: tint[3] = 1     # 셰이더 그래프 색의 알파는 대개 안 쓰인다(0 으로 두면 안 보인다)
    tint = [max(0, min(4, x)) for x in tint[:3]] + [max(0, min(1, tint[3]))]
    tx["users"].add(hero)
    # (10-15) 셰이더 그래프 재현(SG) — 원작 GLSL(꾸러미 안 GLES3 소스)을 읽어 같은 셈을 하는 유니티 셰이더(Bolzena/FxMaster)에 넘길 재질 값 · 그림.
    #   그림은 굽지 않은 원본(열쇠 ("t", pid, None)), _ST 는 [sx, sy, ox, oy], 섞기는 _SrcBlend/_DstBlend
    sgk = SG_KIND.get(shname or "")
    if sgk:
        try:
            sg = {"k": sgk, "tex": {}, "st": {}, "f": {}, "v": {}}
            # (10-16) 아틀라스 스프라이트 이미터 — 주 그림은 잘라 낸 스프라이트 띠(입자 UV 가 이미 그 칸을 가리킨다). 전엔 셰이더 그래프를 통째로 건너뛰어 단순 판이 됐다(네르(빡침) 평타 연보라 판)
            if sp:
                sg["tex"]["_Main_Tex"] = tkey; sg["st"]["_Main_Tex"] = [1, 1, 0, 0]
            for tn, tv in md["m_SavedProperties"]["m_TexEnvs"]:
                if tn not in SG_TEX or tn not in texpp: continue
                if sp and tn in ("_Main_Tex", "_Texture2D", "_MainTex"): continue
                k2, t2 = ctx.texture(texpp[tn])
                if t2["opaque"]: k2, t2 = ctx.texture(texpp[tn], ("alum", alum))   # 알파 없는 단일 채널 그림 — 원작은 밝기가 알파(검은 바탕이 비친다). 안 하면 검은 판(10-16 비비 스킬)
                t2["users"].add(hero)
                sg["tex"][tn] = k2
                sc, of = tv.get("m_Scale", {}), tv.get("m_Offset", {})
                sg["st"][tn] = r3([sc.get("x", 1), sc.get("y", 1), of.get("x", 0), of.get("y", 0)])
            for k in SG_FLOATS:
                if k in fl: sg["f"][k] = r3(fl[k])
            for k in SG_VECS:
                if k in cols: c = cols[k]; sg["v"][k] = r3([c["r"], c["g"], c["b"], c["a"]])
            for d0 in ("tex", "st", "f", "v"):
                sg[d0] = {SG_RENAME.get(k, k): v for k, v in sg[d0].items()}
            if "_Texture2D" in sg["tex"]: sg["tex"]["_Main_Tex"] = sg["tex"].pop("_Texture2D"); sg["st"].pop("_Texture2D", None)   # 툰 폭발은 그림 이름만 다르다
            if "_Main_Tex" in sg["tex"]: ctx.last_sg = sg
            if ctx.last_sg is not None and sgk == "toon": over = None   # 툰 폭발 흉내(색 · 알파 곡선 덧입힘)는 원작 셰이더 재현이면 안 쓴다
        except Exception: pass
    # (10-15) 가리개 그림(_Mask_Tex) — 셰이더 그래프가 주 그림 알파에 곱한다(원통 · 돔 메시의 위아래를 지움 — 디아나(왕년) 고학년 회색 기둥). 주 그림으로 쓴 것이면 빼고
    mk = texpp.get("_Mask_Tex")
    if mk is not None and not sp and mk is not main:
        try:
            mkey, mtx = ctx.texture(mk)
            mtx["users"].add(hero); ctx.last_mask = mkey
        except Exception: pass
    asp = tx["img"].height / tx["img"].width * nspr if nspr else 0
    return tkey, add, tint, shname, nspr, asp, over


def emitter(ctx, ps_obj, rend, world, hero, name):
    d = ps_obj.read_typetree()
    im, em, sh = d["InitialModule"], d["EmissionModule"], d["ShapeModule"]
    rd = rend.read_typetree()
    if not rd.get("m_Enabled", 1) and not _ui_now: return None   # 화면 층 장은 렌더러를 꺼 두고 UI 파티클 스크립트가 그린다 — 꺼졌어도 읽는다(UI_RECT)
    bursts = []
    for bu in em["m_Bursts"] if em["enabled"] else []:
        n = mm_mean(bu["countCurve"])
        if n <= 0 or bu.get("probability", 1) <= 0: continue
        cyc = bu.get("cycleCount", 1) or 1
        bursts.append(r3([bu["time"], n, min(cyc, 20), bu.get("repeatInterval", 0.01)]))
    rate = mm_mean(em["rateOverTime"]) if em["enabled"] else 0
    if not bursts and rate <= 0: return None
    m = material(ctx, rend, hero, ps_obj, name)
    if not m: return None
    tex, add, tint, shname, nspr, asp, over = m
    mask_key = ctx.last_mask
    sg_val = ctx.last_sg
    M, pos, lossy = world
    e = {"tex": tex, "add": 1 if add else 0, "tint": r3(tint)}
    if mask_key is not None: e["mask"] = mask_key
    if sg_val is not None: e["sg"] = sg_val; e["tint"] = [1, 1, 1, 1]   # 셰이더 그래프 재현은 재질 값을 셰이더가 쓴다 — 옛 근사 틴트(재질 색을 입자 색에 곱함)를 빼야 검게 안 나온다(10-16 비비 스킬)
    e["dur"] = r3(d["lengthInSec"]); e["loop"] = 1 if d["looping"] else 0
    e["delay"] = r3(mm_range(d["startDelay"]))
    e["life"] = r3(mm_range(im["startLifetime"]))
    e["speed"] = r3(mm_range(im["startSpeed"]))
    e["size"] = r3(mm_range(im["startSize"]))
    if im.get("size3D"):
        sx, sy = mm_mean(im["startSize"]), mm_mean(im["startSizeY"])
        if sx > 0 and abs(sy / sx - 1) > 0.02: e["sy"] = r3(sy / sx)
    rot = mm_range(im["startRotation"])
    if rot != [0, 0]: e["rot"] = r3(rot)
    cs, lerp = start_colors(im["startColor"])
    e["col"] = r3(cs)
    if lerp: e["colLerp"] = 1
    if im.get("gravityModifier"):
        g = mm_mean(im["gravityModifier"])
        if g: e["grav"] = r3(g * 9.81)
    if rate > 0: e["rate"] = r3(rate)
    if bursts: e["bursts"] = bursts
    e["max"] = int(im.get("maxNumParticles", 1000))
    # 모양 — 모양 자체의 자리·회전·크기까지 행렬에 접어 넣는다
    SM = M; spos = [0, 0, 0]
    if sh["enabled"]:
        kind = SHAPES.get(sh["type"], "sphere")
        rad = sh["radius"]["value"]
        s = {"k": kind, "r": r3(rad)}
        if kind == "cone": s["a"] = r3(sh["angle"]); s["len"] = r3(sh.get("length", 0))
        if sh["type"] in (1, 3, 7, 9, 11, 15, 16): s["t"] = 0
        else: s["t"] = r3(sh.get("radiusThickness", 1))
        arc = sh["arc"]["value"] if isinstance(sh.get("arc"), dict) else 360
        if arc < 359.9: s["arc"] = r3(arc)
        if kind == "donut": s["dr"] = r3(sh.get("donutRadius", 0.2))
        if sh.get("randomDirectionAmount"): s["rnd"] = r3(sh["randomDirectionAmount"])
        if sh.get("sphericalDirectionAmount"): s["sph"] = r3(sh["sphericalDirectionAmount"])
        if sh.get("randomPositionAmount"): s["jit"] = r3(sh["randomPositionAmount"])
        e["shape"] = s
        sc = sh.get("m_Scale", {"x": 1, "y": 1, "z": 1})
        SM = mul(M, mul(euler(sh["m_Rotation"]), diag([sc["x"], sc["y"], sc["z"]])))
        spos = [sh["m_Position"][k] for k in ("x", "y", "z")]
    # 방출 공간 → 화면(유니티 x·y, z 버림). m 은 [xx, xy, xz, yx, yy, yz] — 3D 방향을 2D 로 떨군다
    off = mv(M, spos)
    e["pos"] = r3(view([pos[i] + off[i] for i in range(3)]))
    e["m"] = r3(rows(SM))
    mt = r3(rows(M))
    if mt != e["m"]: e["mt"] = mt      # 수명 속도·힘은 모양 회전을 안 탄다
    sm = d.get("scalingMode", 0)
    e["sc"] = r3([lossy[0], lossy[1]] if sm == 0 else [1, 1])
    # 수명에 따른 크기·색
    sz = d["SizeModule"]
    if sz["enabled"]:
        k = mm_keys(sz["curve"])
        if k: e["sizeOL"] = k
        elif sz["curve"]["minMaxState"] in (0, 3): e["sizeK"] = r3(mm_mean(sz["curve"]))
    cm = d["ColorModule"]
    if cm["enabled"]:
        g = cm["gradient"]
        gg = g["maxGradient"] if g["minMaxState"] in (1, 3) else None
        if gg:
            ck, ak = grad(gg)
            if not all(abs(c[1] - 1) < .01 and abs(c[2] - 1) < .01 and abs(c[3] - 1) < .01 for c in ck): e["colOL"] = r3(ck)
            if not all(a[1] > .99 for a in ak): e["alphaOL"] = r3(ak)
        elif g["minMaxState"] == 0:
            c = col(g["maxColor"]); e["tint"] = r3([e["tint"][i] * c[i] for i in range(4)])
    # 회전 속도(라디안/초)
    rm = d["RotationModule"]
    if rm["enabled"]:
        w = mm_range(rm["curve"])
        if w != [0, 0]: e["spin"] = r3(w)
    # 속도·힘
    vm = d["VelocityModule"]
    if vm["enabled"]:
        v = [mm_mean(vm[k]) for k in ("x", "y", "z")]
        if any(v): e["vel"] = r3(v); e["velW"] = 1 if vm.get("inWorldSpace") else 0
        rad = mm_mean(vm["radial"]) if "radial" in vm else 0
        if rad: e["radial"] = r3(rad)
        spm = mm_mean(vm["speedModifier"]) if "speedModifier" in vm else 1
        if abs(spm - 1) > .01: e["spdMul"] = r3(spm)
    fm = d["ForceModule"]
    if fm["enabled"]:
        f = [mm_mean(fm[k]) for k in ("x", "y", "z")]
        if any(f): e["force"] = r3(f); e["forceW"] = 1 if fm.get("inWorldSpace") else 0
    cv = d["ClampVelocityModule"]
    if cv["enabled"]:
        e["limit"] = r3(mm_mean(cv["magnitude"])); e["damp"] = r3(cv.get("dampen", 0))
        if cv.get("drag") and mm_mean(cv["drag"]): e["drag"] = r3(mm_mean(cv["drag"]))
    # 시트 애니메이션 — 셀 하나를 고르는 아틀라스(상수 프레임)도 이것으로 된다
    uv = d["UVModule"]
    if asp and abs(asp - 1) > .02: e["asp"] = r3(asp)    # 스프라이트는 제 가로세로 비를 지킨다
    if uv["enabled"] and ((uv.get("mode", 0) == 0 and uv["tilesX"] * uv["tilesY"] > 1) or nspr > 1):
        tx, ty = (nspr, 1) if nspr > 1 else (uv["tilesX"], uv["tilesY"])
        a = {"tx": tx, "ty": ty}
        if uv["animationType"] == 1:
            a["row"] = 1 if uv.get("rowMode", 0) == 1 else uv.get("rowIndex", 0) + 2   # 1 = 줄 무작위, 2+ = 그 줄
        fot = uv["frameOverTime"]
        if fot["minMaxState"] in (0, 3): a["f"] = r3(mm_range(fot))
        else: a["fk"] = mm_keys(fot, 9)
        sf = mm_range(uv["startFrame"])
        if sf != [0, 0]: a["sf"] = r3(sf)
        if uv.get("cycles", 1) != 1: a["cyc"] = r3(uv["cycles"])
        if uv.get("timeMode", 0) == 2: a["fps"] = r3(uv.get("fps", 30))
        e["sheet"] = a
    # 그리는 법 — 0 판, 1 늘인 판, 2 눕힌 판, 4 메시
    # 그리는 법 — 0 판, 1 늘인 판, 2 눕힌 판, 4 메시. 정렬 1·2(World·Local)면 판이 이펙트와 같이 돈다(원작 대부분)
    mode, align = rd.get("m_RenderMode", 0), rd.get("m_RenderAlignment", 0)
    if mode == 1:
        e["stretch"] = r3([rd.get("m_LengthScale", 2), rd.get("m_VelocityScale", 0)])
    elif mode == 2: e["flat"] = 1
    elif mode in (0, 4):
        ext = ctx.mesh_extent(rend.read().m_Mesh) if mode == 4 else None
        ex = ext or [.5, .5, 0, 0, 0, 0]
        if align in (1, 2):
            # 판(메시)의 x·y 축을 화면에 떨궈 둔다 — 눕힌 고리는 납작한 타원이 된다
            hx, hy, hz = max(ex[0], 1e-3), ex[1], ex[2]
            ay = [0, hy * 2, 0] if hy >= hz else [0, 0, hz * 2]
            R = M if sm == 0 else mul(M, diag([1 / max(1e-6, abs(lossy[0])), 1 / max(1e-6, abs(lossy[1])), 1]))
            u = mv(R, [hx * 2, 0, 0]); w = mv(R, ay)
            e["quad"] = r3(view(u) + view(w))
            pv = rd.get("m_Pivot") or {}
            if pv.get("x") or pv.get("y"):
                # 원작 pivot 은 입자 크기 비율(축마다) — 세로 크기 비율(sy, 아래에서 quad 에 접는다)도 중심점에 곱해야 늘인 판이 제자리에 선다(10-15 디아나(왕년) 불기둥)
                syk = e.get("sy", 1)
                o_ = mv(R, [pv.get("x", 0), pv.get("y", 0) * syk, 0]); e["pivot"] = r3(view(o_))
            e.pop("sc", None)
        elif ext:
            e["ext"] = r3([ext[0] * 2, max(ext[1], ext[2] * .35) * 2])
        if ext and sorted(ext[:3])[0] > 0.1 * max(ext[:3]): e["m3d"] = 1    # 입체 메시(원통·나선…) — 판 하나로는 못 옮겨 옅게 그린다
        elif ext and not ext[6] and hard_edge(ctx.tex[tex]["img"], e.get("sheet"), add):
            e["m3d"] = 1    # 납작해도 UV 를 펼친 메시(나무·띠)에 가장자리가 찬 그림 — 사각형에 얹으면 각진 덩어리가 된다
        # (10-15) 메시 몸 — 3D 꼭짓점(세로 크기 비율 sy 는 판이 고른 세로 축 y 또는 z 에 곱함)과 화면 투영 행렬 A(2×3 = view ∘ R).
        #   렌더러가 입자 돌림(z)을 메시 평면에서 먼저 하고 A 로 떨군다 — 원작 칼빛은 눕힌 메시를 제 평면에서 빠르게 돌려(초당 6~19 라디안) 몸을 감싸는 가로 타원이 된다
        if mode == 4 and ext:
            md = ctx.mesh_data(rend.read().m_Mesh)
            if md:
                syk = e.get("sy", 1)
                yax = ex[1] >= ex[2]
                if im.get("size3D"):
                    # (10-15) 축별 크기(size3D) — 메시는 x · y · z 를 따로 곱한다(비비(신성) 레이저: x 0 · y 27 · z 5 — 한 축 크기만 쓰면 0 이라 사라졌다). 크기 칸은 1
                    s3 = [mm_mean(im["startSize"]), mm_mean(im["startSizeY"]), mm_mean(im["startSizeZ"])]
                    V3 = [[x * s3[0], y * s3[1], z * s3[2]] for x, y, z in (v[:3] for v in md["v"])]
                    e["size"] = [1, 1]; e.pop("sy", None)
                else:
                    V3 = [[x, y * (syk if yax else 1), z * (1 if yax else syk)] for x, y, z in (v[:3] for v in md["v"])]
                if align in (1, 2):
                    R = M if sm == 0 else mul(M, diag([1 / max(1e-6, abs(lossy[0])), 1 / max(1e-6, abs(lossy[1])), 1]))
                    A = [R[0][0], R[0][1], R[0][2], CY * R[1][0] + SY * R[2][0], CY * R[1][1] + SY * R[2][1], CY * R[1][2] + SY * R[2][2]]
                else:
                    A = [1, 0, 0, 0, 1, 0]
                # (10-15) 시작 3D 회전 X · Y(rotation3D) — 유니티 순서 Y · X · Z 중 Z 는 렌더러가 입자마다 메시 평면에서 돌리니, Y · X 만 투영 행렬에 접는다
                #   (비비(신성) 레이저: YZ 평면 메시를 Y 로 −108° 돌려 세운다 — 안 접으면 옆면만 보여 사라졌다)
                if im.get("rotation3D"):
                    rx, ry = mm_mean(im["startRotationX"]), mm_mean(im["startRotationY"])
                    if abs(rx) > 1e-4 or abs(ry) > 1e-4:
                        cx_, sx_, cy_, sy_ = math.cos(rx), math.sin(rx), math.cos(ry), math.sin(ry)
                        RX = [[1, 0, 0], [0, cx_, -sx_], [0, sx_, cx_]]
                        RY = [[cy_, 0, sy_], [0, 1, 0], [-sy_, 0, cy_]]
                        RYX = mul(RY, RX)
                        A2 = [A[0:3], A[3:6]]
                        A = [sum(A2[r][k] * RYX[k][c] for k in range(3)) for r in range(2) for c in range(3)]
                e["mesh"] = {"v3": r3([c for q in V3 for c in q]), "A": r3(A), "u": r3([c for uv in md["u"] for c in uv[:2]]), "t": md["t"]}
                if md["a"]: e["mesh"]["a"] = r3(md["a"])
    # (10-15) 사용자 데이터(Custom1 · Custom2) — 셰이더 그래프 FX_Master 가 입자마다 읽는다. 원작 칼빛: Custom1.xy = 주 그림 UV 밀기(그어지듯 드러남),
    #   Custom1.w = 밝기 배율(네르 칼빛 15 → 0 — 파란 입자 색이 흰빛으로 빛난 까닭). 성분마다 수명 곡선 [[t, v]…] 또는 상수
    cdm = d.get("CustomDataModule") or {}
    if cdm.get("enabled") and ("FX_Master" in (shname or "") or SG_KIND.get(shname or "")):
        for i, key in ((0, "c1"), (1, "c2")):
            if cdm.get(f"mode{i}") == 2:   # 색 모드 — 시작 색 한 가지(툰 폭발 불빛 색 · 원작은 Custom2 = 색)
                cg = cdm.get(f"color{i}")
                if cg:
                    cs, _ = start_colors(cg)
                    c0 = cs[0] if cs else [1, 1, 1, 1]
                    e[key] = r3([c0[0], c0[1], c0[2], c0[3] if len(c0) > 3 else 1])
                continue
            if cdm.get(f"mode{i}") != 1: continue
            comps = []
            for c in range(4):
                mc = cdm.get(f"vector{i}_{c}")
                if mc is None: comps.append(0); continue
                k = mm_keys(mc, 9)
                comps.append(k if k else r3(mm_mean(mc)))
            e[key] = comps
    fp = rd.get("m_Flip") or {}
    if fp.get("x"): e["flipU"] = r3(fp["x"])
    if e.get("sy") and "quad" in e:
        q = e["quad"]; e["quad"] = r3([q[0], q[1], q[2] * e["sy"], q[3] * e["sy"]]); del e["sy"]
    if over:
        for k_, v_ in over.items(): e[k_] = (1 if v_ else 0) if k_ == "add" else v_
    e["order"] = [rd.get("m_SortingOrder", 0), -rd.get("m_SortingFudge", 0)]
    # 원본 값 — 렌더러가 짐작 대신 쓸 수 있게(Codex 검토 10-15 「추출기에 빠진 필드」)
    e["rm"] = mode; e["ra"] = align
    if d.get("moveWithTransform", 0) == 1 or d.get("simulationSpace", 0) == 1: e["simW"] = 1   # 월드 시뮬 — 방출 뒤 원점을 따라가지 않는다
    pvr = rd.get("m_Pivot") or {}
    if any(pvr.get(k) for k in ("x", "y", "z")): e["pv"] = r3([pvr.get("x", 0), pvr.get("y", 0), pvr.get("z", 0)])
    if im.get("size3D"): e["sz3"] = r3([mm_mean(im["startSize"]), mm_mean(im["startSizeY"]), mm_mean(im.get("startSizeZ", im["startSize"]))])
    if shname: e["shn"] = shname
    return e


# (10-16) 원작 ParticleSystem 그대로(유니티 기본 파티클로 다시 조립) — 직렬화 값을 거의 통째로 넘긴다.
#   꺼진 모듈 · 상수 곡선의 곡선 · 오브젝트 참조는 뺀다(크기). FX_PSRAW=1 일 때만
PSRAW = os.environ.get("FX_PSRAW") == "1"
KEEP_ALWAYS = ("InitialModule", "EmissionModule")


def _fin(x):
    """무한 · NaN 을 JSON 이 읽는 수로(원작 수명 무한 = 아주 큰 수)."""
    if isinstance(x, float) and not math.isfinite(x): return 0 if x != x else (1e5 if x > 0 else -1e5)
    return r3(x) if isinstance(x, float) else x


def _compact(v):
    if isinstance(v, dict):
        if set(v.keys()) >= {"m_FileID", "m_PathID"}: return None   # 오브젝트 참조
        if "minMaxState" in v and "scalar" in v:        # MinMaxCurve
            st = v["minMaxState"]; o = {"minMaxState": st, "scalar": _fin(v["scalar"]), "minScalar": _fin(v.get("minScalar", 0))}
            if st in (1, 2): o["maxCurve"] = _compact(v["maxCurve"])
            if st == 2: o["minCurve"] = _compact(v["minCurve"])
            return o
        if "minMaxState" in v and "maxColor" in v:      # MinMaxGradient
            st = v["minMaxState"]; o = {"minMaxState": st, "maxColor": v["maxColor"], "minColor": v.get("minColor")}
            if st in (1, 3, 4): o["maxGradient"] = v["maxGradient"]
            if st == 3: o["minGradient"] = v["minGradient"]
            return o
        out = {}
        for k, x in v.items():
            if isinstance(x, dict) and "enabled" in x and not x["enabled"] and k.endswith("Module") and k not in KEEP_ALWAYS: continue
            c = _compact(x)
            if c is not None: out[k] = c
        return out
    if isinstance(v, list): return [_compact(x) for x in v]
    if isinstance(v, float): return _fin(v)
    return v


# (10-18 2차) 게임이 단계(시작 · 유지 · 끝)를 켜 가며 트는 장(CustomEffectCondition — RootStart · RootLoop · RootEnd) — 유지 · 끝 단계가 꺼진 채 저장돼
#   꺼진 오브젝트를 건너뛰는 walk 가 빈 장으로 뽑았다(비비 수은 쉴드 fx_vivi_skill_barrier_1). stage 를 주면 그 단계만 켠 것으로 읽는다
STAGES = ("Root_Start", "Root_Loop", "Root_End")
# (10-18 2차) 화면(UI) 층 장 — 마디가 RectTransform 이라 Transform 만 읽던 walk 가 빈 장으로 뽑았다. 좌표는 원작 캔버스 픽셀 그대로(쓰는 쪽이 화면 단위로 옮긴다).
#   다른 fx_ui_* 장(신탁 표 후보 fx_ui_vividivine_ultimate_1 등)이 새로 살아나지 않게 이름으로 좁힌다 — 비비(신성) 고학년 화면 레이저
UI_RECT = re.compile(r"^fx_ui_vividivine_ultimate_attack_")
_ui_now = False   # 지금 읽는 장이 UI_RECT 인가(emitter 가 본다)


def effect(ctx, path, hero, stage=None):
    UnityPy = ctx.UnityPy
    env = UnityPy.load(ctx.dep_bytes, *ctx.extra, open(path, "rb").read())
    name = os.path.basename(path)
    global _ui_now
    _ui_now = bool(UI_RECT.search(name))
    own = None
    for o in env.objects:
        # 공용 번들에도 AssetBundle 이 있다 — 이름이 prefab/ 으로 시작하는 쪽이 이펙트
        if o.type.name == "AssetBundle" and o.read_typetree()["m_Name"].startswith("prefab/"): own = o.assets_file.name; break
    objs = [o for o in env.objects if o.assets_file.name == own]
    T, G, PS, RD = {}, {}, {}, {}
    for o in objs:
        t = o.type.name
        if t == "Transform" or (t == "RectTransform" and UI_RECT.search(name)): T[o.path_id] = o.read_typetree()
        elif t == "GameObject": G[o.path_id] = o.read_typetree()
        elif t == "ParticleSystem": PS[o.read_typetree()["m_GameObject"]["m_PathID"]] = o
        elif t == "ParticleSystemRenderer": RD[o.read_typetree()["m_GameObject"]["m_PathID"]] = o
    out = []
    def walk(tid, M, pos, lossy, active):
        t = T.get(tid)
        if not t: return
        gid = t["m_GameObject"]["m_PathID"]
        g = G.get(gid, {})
        # 맨 위(프리팹 뿌리)는 꺼진 채 저장된 것이 있다 — 게임이 띄울 때 켠다
        if stage is not None and g.get("m_Name", "") in STAGES: active = active and g.get("m_Name", "") in stage
        else: active = active and (g.get("m_IsActive", 1) or t["m_Father"]["m_PathID"] == 0)
        lp, lr, ls = t["m_LocalPosition"], t["m_LocalRotation"], t["m_LocalScale"]
        p = mv(M, [lp["x"], lp["y"], lp["z"]])
        pos2 = [pos[i] + p[i] for i in range(3)]
        M2 = mul(M, mul(qmat(lr), diag([ls["x"], ls["y"], ls["z"]])))
        lossy2 = [lossy[0] * ls["x"], lossy[1] * ls["y"]]
        if active and gid in PS and gid in RD:
            e = emitter(ctx, PS[gid], RD[gid], (M2, pos2, lossy2), hero, g.get("m_Name", ""))
            if e:
                e["n"] = g.get("m_Name", "")
                if PSRAW:
                    psd = PS[gid].read_typetree(); rdd = RD[gid].read_typetree()
                    subs = []
                    for sb in (psd.get("SubModule", {}).get("subEmitters") or []) if psd.get("SubModule", {}).get("enabled") else []:
                        pid = (sb.get("emitter") or {}).get("m_PathID", 0)
                        subs.append([pid, sb.get("type", 0), sb.get("properties", 0), r3(sb.get("emitProbability", 1))])
                    e["ps"] = _compact(psd); e["ps_sub"] = subs; e["ps_id"] = PS[gid].path_id
                    rd = _compact(rdd)
                    for k in ("m_Materials", "m_StaticBatchInfo", "m_LightProbeVolumeOverride", "m_ProbeAnchor", "m_StaticBatchRoot", "m_Mesh", "m_Mesh1", "m_Mesh2", "m_Mesh3", "m_MaskInteraction"): rd.pop(k, None)
                    if _ui_now: rd["m_Enabled"] = 1   # 우리는 렌더러로 그린다
                    e["rd"] = rd
                    e["xf"] = {"p": r3(pos2), "M": r3([c for row in M2 for c in row]), "s": r3(lossy2)}
                    if rdd.get("m_RenderMode") == 4:
                        md = ctx.mesh_data(RD[gid].read().m_Mesh)
                        if md: e["rmesh"] = {"v": r3([c for q in md["v"] for c in q[:3]]), "u": r3([c for uv in md["u"] for c in uv[:2]]), "t": md["t"]}
                e["i"] = len(out)
                out.append(e)
        for c in t["m_Children"]: walk(c["m_PathID"], M2, pos2, lossy2, active)
    I = [[1, 0, 0], [0, 1, 0], [0, 0, 1]]
    for tid, t in T.items():
        if t["m_Father"]["m_PathID"] == 0: walk(tid, I, [0, 0, 0], [1, 1], True)
    out.sort(key=lambda e: (e["order"][0], e["order"][1], e["i"]))
    for e in out: del e["order"], e["i"]
    dur = 0
    for e in out:
        emit_end = e["delay"][1] + (min(e["dur"], 1.5) if e["loop"] else e["dur"])
        dur = max(dur, emit_end + e["life"][1])
    return {"dur": r3(min(dur, 6.0)), "em": out}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--raw", default=os.path.join(ROOT, "assets", "_fxraw"))
    ap.add_argument("--out", default=os.path.join(ROOT, "assets", "fx"))
    ap.add_argument("--only", default="")
    ap.add_argument("--pull", action="store_true")
    ap.add_argument("--adb", default=None)
    ap.add_argument("--serial", default=None)
    a = ap.parse_args()
    pull(a.raw, a)
    src = os.path.join(a.raw, "prefabeffectunit")
    files = sorted(os.listdir(src))
    art = roster()
    only = {s.strip() for s in a.only.split(",") if s.strip()}
    ctx = Ctx(os.path.join(a.raw, DEP), [os.path.join(a.raw, "effectatlases", f) for f in ATLASES])
    heroes, effects, owner = {}, {}, {}
    for key, name in art.items():
        if only and name not in only and key not in only: continue
        got, how = pick(name, files)
        if not got: continue
        ok = []
        for f in got:
            try: fx = effect(ctx, os.path.join(src, f), name)
            except Exception as ex:
                log(f"  ! {f}: {ex}"); continue
            if not fx["em"]: continue
            effects[f] = fx; owner[f] = name; ok.append(f)
        if ok:
            heroes[key] = {"name": name, "ult": ok}
            if how != "ult": heroes[key]["from"] = how
        log(f"{key}({name}) {how or '-'} {len(ok)}/{len(got)}")

    # 텍스처 — 둘 이상이 쓰면 _shared, 하나만 쓰면 그 사도 폴더. 512px 넘으면 줄인다
    os.makedirs(a.out, exist_ok=True)
    tex_path, used_names = {}, {}
    nbytes = 0
    for pid, t in ctx.tex.items():
        if not t["users"]: continue
        folder = "_shared" if len(t["users"]) > 1 else sorted(t["users"])[0]
        base = re.sub(r"[^A-Za-z0-9_\-]", "_", t["name"]) or "tex"
        fn = f"{folder}/{base}.png"
        if fn in used_names and used_names[fn] != pid:
            fn = f"{folder}/{base}_{hashlib.md5(str(pid).encode()).hexdigest()[:6]}.png"
        used_names[fn] = pid
        img = t["img"]
        if t["opaque"]: img = img.convert("RGB")
        dst = os.path.join(a.out, fn)
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        img.save(dst, optimize=True)
        nbytes += os.path.getsize(dst)
        tex_path[pid] = fn

    # 사도별 fx.json — 색인은 가볍게, 이펙트 본문은 그 사도를 처음 쓸 때 받는다
    per = {}
    for f, fx in effects.items():
        for e in fx["em"]:
            e["tex"] = tex_path[e["tex"]]
            if "mask" in e:
                mp = tex_path.get(e.pop("mask"))
                if mp: e["mask"] = mp
            if "sg" in e:
                e["sg"]["tex"] = {k: tex_path[v] for k, v in e["sg"]["tex"].items() if v in tex_path}
        per.setdefault(owner[f], {})[f] = fx
    for name, fxs in per.items():
        os.makedirs(os.path.join(a.out, name), exist_ok=True)
        json.dump(fxs, open(os.path.join(a.out, name, "fx.json"), "w", encoding="utf-8"), ensure_ascii=False, separators=(",", ":"))

    ip = os.path.join(a.out, "index.json")
    idx = {"heroes": {}, "effects": {}}
    if only and os.path.exists(ip):
        idx = json.load(open(ip, encoding="utf-8"))
        for k in list(idx["heroes"]):
            if k in heroes:
                for f in idx["heroes"][k]["ult"]: idx["effects"].pop(f, None)
    idx["heroes"].update(heroes)
    for f, fx in effects.items():
        idx["effects"][f] = {"hero": owner[f], "dur": fx["dur"], "n": len(fx["em"])}
    idx["_meta"] = {"note": "tools/extract-fx.py 가 만든다. 이펙트 본문은 <hero>/fx.json", "unit": "유니티 단위 — 화면 픽셀은 fx-burst.js 의 ppu 로 곱한다"}
    json.dump(idx, open(ip, "w", encoding="utf-8"), ensure_ascii=False, separators=(",", ":"))

    n_ult = sum(1 for h in idx["heroes"].values() if "from" not in h)
    log(f"\n사도 {len(art)}명 중 {len(idx['heroes'])}명 (궁극기 {n_ult} · 스킬로 메움 {len(idx['heroes']) - n_ult})")
    log(f"이펙트 {len(effects)}개 · 텍스처 {len(tex_path)}장 {nbytes / 1024 / 1024:.1f}MB")
    miss = [k for k, n in art.items() if k not in idx["heroes"]]
    if miss: log("없음: " + " ".join(miss))


if __name__ == "__main__":
    main()
