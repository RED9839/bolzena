# -*- coding: utf-8 -*-
"""고학년 말고 나머지 원작 이펙트 — 카드(평타 · 센 공격 · 스킬 · 시그니처) · 공용(회복 · 실드 · 격파 · 쓰러짐 · 신탁) 추출.

웹판 tools/extract-fx.py(고학년만)의 변환 함수를 그대로 빌려 쓰고(읽기만 — 웹판 저장소는 고치지 않는다),
고르는 규칙만 여기 둔다. 원본은 웹판이 adb 로 받아 둔 assets/_fxraw(prefabeffectunit · prefabeffecthit · 공용 번들 · 아틀라스).
UnityPy 로 이펙트 프리팹만 읽는다 — 암호화된 데이터 테이블은 건드리지 않는다.

  python Tools/fx_extract_more.py --project C:\\projects\\bolzena-fx-test            전부(사도 카드 이펙트 + 공용)
  python Tools/fx_extract_more.py --project <경로> --only erpin,vela            (동시 일꾼은 메모리로 정한다 — 합계 32GB 안)

나오는 것(쓰는 프로젝트의 Assets/BolzenaFxData/Src/fx2 — 원작 에셋이라 git 에 넣지 않는다):
  index.json  { heroes: { 사도키: { name, attack:[], power:[], skill:[], sig:[] } }, common: { 갈래: [이름…] }, effects: { 이름: {hero, dur, n} } }
  <그림이름>/fx.json · <그림이름>/*.png · _shared/*.png · _common/fx.json
FxImport.ImportAll 이 Src/fx 와 함께 읽는다.
필요: Python 3 · UnityPy · Pillow · numpy
"""
import argparse, hashlib, importlib.util, json, os, re, shutil, sys, time
from concurrent.futures import ProcessPoolExecutor

for _s in (sys.stdout, sys.stderr):
    try: _s.reconfigure(encoding="utf-8", errors="replace")
    except Exception: pass

WEB = r"C:\projects\볼제나"


def load_x():
    spec = importlib.util.spec_from_file_location("extract_fx", os.path.join(WEB, "tools", "extract-fx.py"))
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


X = load_x()

# 카드 · 공용에서 빼는 것 — 고학년(따로 있다) · 어사이드 · 등장 · 걷기 · 표정 · 쓰러짐 연출 · 로비 · 미니게임
NOT_CARD = re.compile(r"^(ultimate|ult_|ult\d|ulitmate|aside|spawn|move|emoji|die|fakedie|idle|victory|lobby|storybattle|minigame|halo|lamp|renewa|vividivineskin)")
POWER = re.compile(r"^(attack_?2|attack2|power)")
SKILL = re.compile(r"^(skill|personal)")
SIG = re.compile(r"signaturecard")
MAX_PER = 12   # 갈래마다 이만큼만(티그 slash 14개 같은 것) — 재생기도 한 번에 8개까지만 튼다

# 공용 — 원작에 「공용 회복 · 실드」 프리팹은 없다. 증강(augment — 어느 사도에게나 붙는 강화)의 이펙트가 가장 공용에 가깝다.
# 갈래마다 후보를 다 뽑아 두고, 재생기 표(FxRules.COMMON)가 그중 몇을 고른다. 시험 시트(-fxsheet common)로 눈으로 고른다
COMMON = {
    "heal":    ["fx_augment_112_heal", "fx_augment_170_heal", "fx_hilde_heal_1", "fx_jubee_heal_1", "fx_aurora_skill1_heal_1", "fx_cuee_skill_heal_1", "fx_carren_skill_heal_1"],
    "shield":  ["fx_augment_145_shield", "fx_augment_barrier_1", "fx_ed_shield_1", "fx_bana_skill_shield_1", "fx_gabia_skill_shield_1", "fx_snorky_skill_shield_1", "fx_authority_shield"],
    "shieldHit": ["fx_augment_barrier_hit", "fx_ed_shieldhit_1"],
    "buff":    ["fx_common_equalallyattackpower", "fx_lion_skill_buff_1", "fx_rude_skill_buff_1", "fx_laika_buff_1", "fx_kishya_buff_1", "fx_polan_skill_buff_1"],
    "debuff":  ["fx_lion_skill_debuff_1", "fx_vivi_skill_debuff_1", "fx_benibeni_debuff_1", "fx_bana_attack_debuff_1"],
    "break":   ["fx_e0_groggy_loop_1", "fx_rufo_ultimate_stun", "fx_velvet_ultimate_stun", "fx_bana_skill_broken_1"],
    "kill":    ["fx_kidian_kill", "fx_e0_die_smoke_1", "fx_momo_die_spark_1"],
    "oracle":  ["fx_vividivine_skill_1_buff_default", "fx_ui_vividivine_ultimate_1", "fx_epica_ultimate_buff_start", "fx_epica_ultimate_buff_top", "fx_haleysane_medal_buff"],
    "revive":  ["fx_common_waitrevive"],
}


def log(m): print(m, flush=True)


def grab(n, names):
    """웹판 pick 의 grab 과 같은 거르기 — 스킨판 · 미니게임 빼고, _default 만 있으면 그것, 성격판은 한 갈래."""
    got = [f for f in names if f.startswith(f"fx_{n}_")]
    keep = [f for f in got if not X.SKIP.search(f)]
    keep += [f for f in got if f.endswith("_default") and f[:-8] not in got]
    out, seen = [], set()
    for f in sorted(set(k for k in keep if " " not in k), key=lambda f: (X.MOODS.index(m.group(1)) if (m := X.MOOD.search(f)) else -1, f)):
        base = X.MOOD.sub("_", f).removesuffix("_default")
        if base in seen: continue
        seen.add(base); out.append(f)
    return sorted(out)


def groups(n, unit, hit):
    g = {"attack": [], "power": [], "skill": [], "sig": []}
    for f in grab(n, unit) + [h for h in grab(n, hit) if h not in unit]:
        w = f[len(f"fx_{n}_"):]
        if NOT_CARD.search(w): continue
        if SIG.search(w): g["sig"].append(f)
        elif SKILL.search(w): g["skill"].append(f)
        elif POWER.search(w): g["power"].append(f)
        else: g["attack"].append(f)
    return {k: v[:MAX_PER] for k, v in g.items()}


# ── 일꾼(프로세스마다 Ctx 하나) ──
# 메모리: UnityPy 는 이펙트마다 공용 번들(76MB, 풀면 수백 MB)을 다시 올린다. 다 쓴 env 는 순환 참조라 gc 를 불러야 풀리고,
# Ctx.tex 는 그림을 계속 쥔다 — 이펙트 하나마다 그림을 캐시 폴더에 바로 쓰고 Ctx.tex 를 비운다. 일꾼은 몇 개 하고 새로 뜬다.
# 동시 일꾼 수는 메모리로 정한다(2026-10-05 22개로 돌려 PC 가 멈췄다 — 일꾼마다 수 GB): 먼저 일꾼 하나로 몇 개 해 보고
# 일꾼 하나의 최대 메모리를 재서 병렬 수 = 합계 한도(32GB) ÷ (그 값 × 1.3). 돌다가 합계가 한도를 넘을 기세면 동시에 맡기는 수를 줄인다.
_ctx = None
_cache = None
MEM_LIMIT_GB = 32
PROBE = 10


def mem_gb():
    """이 프로세스가 커밋한 메모리(GB) — Windows PrivateUsage."""
    try:
        import ctypes
        from ctypes import wintypes
        class PMC(ctypes.Structure):
            _fields_ = [("cb", wintypes.DWORD), ("PageFaultCount", wintypes.DWORD), ("PeakWorkingSetSize", ctypes.c_size_t),
                        ("WorkingSetSize", ctypes.c_size_t), ("QuotaPeakPagedPoolUsage", ctypes.c_size_t), ("QuotaPagedPoolUsage", ctypes.c_size_t),
                        ("QuotaPeakNonPagedPoolUsage", ctypes.c_size_t), ("QuotaNonPagedPoolUsage", ctypes.c_size_t),
                        ("PagefileUsage", ctypes.c_size_t), ("PeakPagefileUsage", ctypes.c_size_t), ("PrivateUsage", ctypes.c_size_t)]
        c = PMC(); c.cb = ctypes.sizeof(PMC)
        k32 = ctypes.windll.kernel32
        k32.GetCurrentProcess.restype = wintypes.HANDLE
        f = ctypes.windll.psapi.GetProcessMemoryInfo
        f.argtypes = [wintypes.HANDLE, ctypes.c_void_p, wintypes.DWORD]
        if not f(k32.GetCurrentProcess(), ctypes.byref(c), c.cb): return 0.0
        return c.PrivateUsage / 2 ** 30
    except Exception:
        return 0.0


def _init(raw, cache):
    global _ctx, _cache
    _ctx = X.Ctx(os.path.join(raw, X.DEP), [os.path.join(raw, "effectatlases", f) for f in X.ATLASES])
    _cache = cache


def tkey(k): return hashlib.md5(repr(k).encode()).hexdigest()


def _work(job):
    import gc
    path, name, owner = job
    try:
        fx = X.effect(_ctx, path, owner)
        err = None
    except Exception as ex:
        fx, err = None, str(ex)[:160]
    rec = {"name": name, "err": err, "fx": None, "tex": {}}
    if fx is not None:
        for e in fx["em"]:
            k = e["tex"]; h = tkey(k)
            t = _ctx.tex[k]
            png = os.path.join(_cache, "tex", h + ".png")
            if not os.path.exists(png):
                tmp = f"{png}.{os.getpid()}.tmp.png"   # 일꾼끼리 같은 그림을 동시에 쓸 수 있다 — 먼저 쓴 쪽이 이긴다
                (t["img"].convert("RGB") if t["opaque"] else t["img"]).save(tmp)
                try: os.replace(tmp, png)
                except OSError:
                    if not os.path.exists(png): raise
                    os.remove(tmp)
            rec["tex"][h] = {"name": t["name"], "opaque": t["opaque"]}
            e["tex"] = h
        rec["fx"] = fx
    _ctx.tex.clear()                     # 그림을 쥐지 않는다(다음 이펙트가 같은 그림을 쓰면 다시 읽는다)
    gc.collect()                         # 다 쓴 UnityPy env(순환 참조)를 바로 놓는다
    p = os.path.join(_cache, "fx", name + ".json")
    json.dump(rec, open(p + ".tmp", "w", encoding="utf-8"), ensure_ascii=False, separators=(",", ":")); os.replace(p + ".tmp", p)
    return name, mem_gb()


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--project", required=True)
    ap.add_argument("--raw", default=os.path.join(WEB, "assets", "_fxraw"))
    ap.add_argument("--cache", default=os.path.join("C:/projects", "bolzena-fx-tmp", "fx2cache"), help="이펙트마다 결과를 여기 쓴다 — 다시 돌리면 이미 한 것은 건너뛴다")
    ap.add_argument("--only", default="")
    ap.add_argument("--jobs", type=int, default=0, help="0 = 메모리로 정한다(합계 32GB 안)")
    ap.add_argument("--no-common", action="store_true")
    ap.add_argument("--mem-limit", type=float, default=32, help="일꾼 메모리 합계 한도(GB) — 기본 32(게임을 켜 둬도 안전한 값)")
    a = ap.parse_args()
    global MEM_LIMIT_GB
    MEM_LIMIT_GB = a.mem_limit
    out = os.path.join(a.project, "Assets", "BolzenaFxData", "Src", "fx2")
    os.makedirs(os.path.join(a.cache, "fx"), exist_ok=True); os.makedirs(os.path.join(a.cache, "tex"), exist_ok=True)
    unit_dir, hit_dir = os.path.join(a.raw, "prefabeffectunit"), os.path.join(a.raw, "prefabeffecthit")
    unit, hit = sorted(os.listdir(unit_dir)), sorted(os.listdir(hit_dir))
    art = X.roster()
    only = {s.strip() for s in a.only.split(",") if s.strip()}
    jobs, heroes, common = [], {}, {}
    seen = set()

    def job(f, owner):
        if f in seen: return True
        p = os.path.join(unit_dir, f)
        if not os.path.exists(p): p = os.path.join(hit_dir, f)
        if not os.path.exists(p): return False
        seen.add(f); jobs.append((p, f, owner)); return True

    for key, name in art.items():
        if only and name not in only and key not in only: continue
        n = X.ALIAS.get(name, name)
        if n in X.NO_FX: continue
        g = groups(n, unit, hit)
        if n != name:   # 고학년만 다른 철자(beinibeni)인 사도 — 카드 쪽은 본 이름(benibeni)
            for k, v in groups(name, unit, hit).items(): g[k] = (g[k] + [f for f in v if f not in g[k]])[:MAX_PER]
        if not any(g.values()): continue
        heroes[key] = dict(name=name, **g)
        for l in g.values():
            for f in l: job(f, name)
    if not a.no_common:
        for k, l in COMMON.items():
            common[k] = [f for f in l if job(f, "_common")]

    owner_of = {j[1]: j[2] for j in jobs}
    todo = [j for j in jobs if not os.path.exists(os.path.join(a.cache, "fx", j[1] + ".json"))]
    log(f"사도 {len(heroes)}명 · 공용 {sum(len(v) for v in common.values())}개 · 이펙트 {len(jobs)}개(남은 것 {len(todo)}) ")
    t0 = time.time()
    peak = 0.0
    from concurrent.futures import FIRST_COMPLETED, wait
    def run(batch, jobs):
        nonlocal peak
        limit = jobs
        done_n = 0
        with ProcessPoolExecutor(jobs, initializer=_init, initargs=(a.raw, a.cache), max_tasks_per_child=12) as ex:
            it = iter(batch); live = set()
            while True:
                while len(live) < limit:
                    j = next(it, None)
                    if j is None: break
                    live.add(ex.submit(_work, j))
                if not live: break
                fin, live = wait(live, return_when=FIRST_COMPLETED)
                for f in fin:
                    name, gb = f.result()
                    peak = max(peak, gb); done_n += 1
                    if gb * limit > MEM_LIMIT_GB and limit > 1:
                        limit = max(1, int(MEM_LIMIT_GB // (gb * 1.3)))
                        log(f"  ! 일꾼 메모리 {gb:.1f}GB — 동시 {limit} 로 줄임")
                    if done_n % 50 == 0: log(f"  {done_n}/{len(batch)} · {time.time() - t0:.0f}s · 일꾼 메모리 최대 {peak:.2f}GB · 동시 {limit}")
    if todo:
        nj = a.jobs
        if nj <= 0:
            run(todo[:PROBE], 1)       # 재 보기 — 일꾼 하나
            todo = todo[PROBE:]
            nj = max(1, min((os.cpu_count() or 4) - 4, int(MEM_LIMIT_GB // max(0.5, peak * 1.3))))
            log(f"  재 봄: 일꾼 하나 최대 {peak:.2f}GB → 동시 {nj}(합계 한도 {MEM_LIMIT_GB}GB)")
        if todo: run(todo, nj)
        log(f"  일꾼 메모리 최대 {peak:.2f}GB")

    # 캐시에서 모은다
    effects, owner, tex, fails = {}, {}, {}, []
    for (_, name, o) in jobs:
        rec = json.load(open(os.path.join(a.cache, "fx", name + ".json"), encoding="utf-8"))
        if rec["err"]: fails.append(f"{name}: {rec['err']}"); continue
        fx = rec["fx"]
        if not fx["em"]: fails.append(f"{name}: 이미터 없음"); continue
        effects[name] = fx; owner[name] = o
        for h, t in rec["tex"].items():
            cur = tex.setdefault(h, dict(t, users=set()))
            cur["users"].add(o)

    # 텍스처 — 둘 이상이 쓰면 _shared, 하나만 쓰면 그 폴더(웹판과 같은 규칙)
    tex_path, used = {}, {}
    nbytes = 0
    for k, t in tex.items():
        folder = "_shared" if len(t["users"]) > 1 else sorted(t["users"])[0]
        base = re.sub(r"[^A-Za-z0-9_\-]", "_", t["name"]) or "tex"
        fn = f"{folder}/{base}.png"
        if fn in used and used[fn] != k: fn = f"{folder}/{base}_{hashlib.md5(str(k).encode()).hexdigest()[:6]}.png"
        used[fn] = k
        dst = os.path.join(out, fn)
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        if not os.path.exists(dst): shutil.copyfile(os.path.join(a.cache, "tex", k + ".png"), dst)
        nbytes += os.path.getsize(dst)
        tex_path[k] = fn
    per = {}
    for f, fx in effects.items():
        for e in fx["em"]: e["tex"] = tex_path[e["tex"]]
        per.setdefault(owner[f], {})[f] = fx
    for o, fxs in per.items():
        os.makedirs(os.path.join(out, o), exist_ok=True)
        json.dump(fxs, open(os.path.join(out, o, "fx.json"), "w", encoding="utf-8"), ensure_ascii=False, separators=(",", ":"))
    # 실패한 이름은 색인에서 뺀다
    for h in heroes.values():
        for k in ("attack", "power", "skill", "sig"): h[k] = [f for f in h[k] if f in effects]
    common = {k: [f for f in v if f in effects] for k, v in common.items()}
    idx = {"heroes": {k: v for k, v in heroes.items() if any(v[g] for g in ("attack", "power", "skill", "sig"))},
           "common": common,
           "effects": {f: {"hero": owner[f], "dur": fx["dur"], "n": len(fx["em"])} for f, fx in effects.items()},
           "_meta": {"note": "bolzena-fx/Tools/fx_extract_more.py — 카드 · 공용 이펙트(고학년은 Src/fx)"}}
    json.dump(idx, open(os.path.join(out, "index.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=0)
    log(f"\n이펙트 {len(effects)}개 · 실패 {len(fails)} · 텍스처 {len(tex_path)}장 {nbytes / 1048576:.1f}MB · {time.time() - t0:.0f}s → {out}")
    cov = {g: sum(1 for h in idx["heroes"].values() if h[g]) for g in ("attack", "power", "skill", "sig")}
    log(f"사도 {len(idx['heroes'])}명 — 갈래별 있는 사도 {cov}")
    for f in fails[:30]: log("  ! " + f)


if __name__ == "__main__":
    main()
