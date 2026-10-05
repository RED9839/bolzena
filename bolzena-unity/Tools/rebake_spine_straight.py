# -*- coding: utf-8 -*-
"""본 게임 스파인 그림을 곧은 알파로 다시 굽는다 — runui Tools~/spine_straight.py(낮은 알파는 이웃 색과 섞고 a=0 은 번짐으로 채움).
전에는 rgb ÷ a 로만 풀어 가장자리에 흰 · 빨간 점과 흰 테두리가 생겼다(2026-10 사용자 제보).

  python Tools/rebake_spine_straight.py            Resources/Spine 의 모든 png(전투 SD · 적 · 스탠딩 st_* · 미니미)를 원본에서 다시
  python Tools/rebake_spine_straight.py --dry      짝만 찾아 본다

짝 찾기: 대상 폴더의 <atlas>.atlas.txt 와 내용이 같은 원본 assets/spine/**/<atlas>.atlas 의 폴더 → 같은 이름 png.
굽기 전 원본(지금 Resources/Spine)은 --backup 폴더로 통째로 복사해 둔다. 작업자는 메모리를 재어(그림 하나 ≈ 수백 MB) 합계 한도 안에서.
"""
import argparse, hashlib, os, shutil, sys, time
from concurrent.futures import ProcessPoolExecutor

sys.stdout.reconfigure(encoding="utf-8", errors="replace")
sys.path.insert(0, r"C:\projects\bolzena-runui\Tools~")
SRCS = [r"C:\projects\볼제나\assets\spine", r"C:\projects\볼제나\assets\monsterspine"]
DST = r"C:\projects\bolzena-unity\Assets\Bolzena\Resources\Spine"


def h(p): return hashlib.md5(open(p, "rb").read()).hexdigest()


def index():
    idx = {}
    for top in SRCS:
      for root, _, files in os.walk(top):
        for f in files:
            if f.endswith(".atlas"):
                idx.setdefault((f.lower(), h(os.path.join(root, f))), root)
    return idx


def work(job):
    import spine_straight
    src, dst = job
    spine_straight.convert(src, dst + ".tmp.png")
    os.replace(dst + ".tmp.png", dst)
    return dst


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--dry", action="store_true")
    ap.add_argument("--jobs", type=int, default=8)
    ap.add_argument("--rest", action="store_true", help="이 스크립트보다 새로 쓴 그림은 건너뛴다(끊긴 뒤 이어하기)")
    ap.add_argument("--backup", default=r"C:\projects\bolzena-unity-tmp\spine_backup_pma_div")
    a = ap.parse_args()
    idx = index()
    jobs, miss = [], []
    for d in sorted(os.listdir(DST)):
        dd = os.path.join(DST, d)
        if not os.path.isdir(dd): continue
        atl = [f for f in os.listdir(dd) if f.endswith(".atlas.txt")]
        if not atl: continue
        src_dir = idx.get((atl[0][:-4].lower(), h(os.path.join(dd, atl[0]))))
        if src_dir is None: miss.append(d); continue
        for f in os.listdir(dd):
            if a.rest and f.endswith(".png") and os.path.getmtime(os.path.join(dd, f)) > os.path.getmtime(__file__): continue
            if f.endswith(".png") and os.path.exists(os.path.join(src_dir, f)):
                jobs.append((os.path.join(src_dir, f), os.path.join(dd, f)))
    print(f"폴더 짝 {len(os.listdir(DST)) - len(miss)} · 못 찾음 {len(miss)} {miss[:10]} · 그림 {len(jobs)}")
    if a.dry: return
    if not os.path.exists(a.backup):
        t = time.time()
        shutil.copytree(DST, a.backup, ignore=shutil.ignore_patterns("*.meta"))
        print(f"백업 → {a.backup} ({time.time() - t:.0f}s)")
    t = time.time()
    with ProcessPoolExecutor(a.jobs, max_tasks_per_child=20) as ex:
        for i, _ in enumerate(ex.map(work, jobs, chunksize=2)):
            if (i + 1) % 100 == 0: print(f"  {i + 1}/{len(jobs)} · {time.time() - t:.0f}s", flush=True)
    print(f"끝 {len(jobs)}장 · {time.time() - t:.0f}s")


if __name__ == "__main__":
    main()
