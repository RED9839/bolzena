# 한 판(판 화면 + 전투)에 쓰는 원작 에셋을 더 복사한다(원본은 읽기만 · 복사본은 gitignore).
#   python Tools/copy_run_assets.py
# 1) 사도 135명의 전투 SD 스파인(spine/ingame/<웹판 키> → Resources/Spine/<그림 키>) · 초상 · 스킬 아이콘(카드 그림)
# 2) 원작 적 스파인 전부(spine/enemy/<이름> → Resources/Spine/<이름>)
# 3) 판 화면(com.bolzena.runui) — 사도 표(roster.json, 웹판에서 뽑음) · 배경 · 아이콘 · 미니미 · 스탠딩(로비 · 상점 · NPC)
#    · 사도 스탠딩 렌더(RunArt/Standing — 상세 · 목록 · 시작 카드) · 고유 카드 아이콘(RunArt/Skill — runui cardart.json 표)
# 시범 여섯(copy_assets.py)과 같은 규칙: 스파인 그림은 PMA → 곧은 알파. 목소리 · 사도 효과음 · 스탠딩(컷인)은 무거워 시범 여섯만.
import json, os, shutil, subprocess, sys
from concurrent.futures import ProcessPoolExecutor
import numpy as np
from PIL import Image

SRC = r"C:\projects\볼제나\assets"
PROJ = r"C:\projects\bolzena-unity"
DST = os.path.join(PROJ, "Assets", "Bolzena", "Resources")
RUNUI = r"C:\projects\bolzena-runui"
ROSTER = os.path.join(PROJ, "Assets", "Resources", "RunUI", "roster.json")


def cp(src, dst, size=None):
    if os.path.exists(dst) or not os.path.exists(src):
        return
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    if size:
        im = Image.open(src)
        im.thumbnail(size, Image.LANCZOS)
        im.save(dst)
    else:
        shutil.copyfile(src, dst)


def straight(src, dst):
    im = np.asarray(Image.open(src).convert("RGBA")).astype(np.float32)
    a = im[..., 3:4]
    rgb = np.where(a > 0, im[..., :3] * 255.0 / np.maximum(a, 1), 0)
    Image.fromarray(np.concatenate([np.clip(rgb, 0, 255), a], axis=-1).astype(np.uint8), "RGBA").save(dst)


def spine(job):
    sd, name = job
    dd = f"{DST}/Spine/{name}"
    if os.path.isdir(dd) or not os.path.isdir(sd):
        return None
    os.makedirs(dd + ".tmp", exist_ok=True)
    for f in os.listdir(sd):
        s = os.path.join(sd, f)
        if f.endswith(".skel"):
            shutil.copyfile(s, f"{dd}.tmp/{f}.bytes")
        elif f.endswith(".atlas"):
            shutil.copyfile(s, f"{dd}.tmp/{f}.txt")
        elif f.endswith(".png"):
            straight(s, f"{dd}.tmp/{f}")
    os.replace(dd + ".tmp", dd)   # 다 된 것만 보이게(유니티가 반쯤 된 폴더를 읽지 않게)
    return name


def main():
    # 3-a) 사도 표 — 웹판 cardbook · artmap 에서(node)
    if not os.path.exists(ROSTER):
        subprocess.run(["node", os.path.join(RUNUI, "Tools~", "build_roster.mjs"), ROSTER.replace("\\", "/")], check=True)
    roster = json.load(open(ROSTER, encoding="utf-8"))["heroes"]

    jobs = []
    for h in roster:
        art = h.get("art")
        if art:
            jobs.append((f"{SRC}/spine/ingame/{h['key']}", art))
    for e in os.listdir(f"{SRC}/spine/enemy"):
        jobs.append((f"{SRC}/spine/enemy/{e}", e))
    with ProcessPoolExecutor(max_workers=24) as ex:
        done = [n for n in ex.map(spine, jobs) if n]
    print("스파인", len(done), "새로")

    n = 0
    for h in roster:
        art = h.get("art")
        if not art:
            continue
        cp(f"{SRC}/heroicons/{art}.png", f"{DST}/Art/{art}.png", (256, 256))
        for ic in [f"icon_admissionskill_{art}", f"icon_graduateskill_{art}", f"aside_skill_{art}_1", f"aside_skill_{art}_2", f"aside_skill_{art}_3"]:
            cp(f"{SRC}/skillicons/{ic}.png", f"{DST}/Art/{ic}.png")
        n += 1
    print("초상 · 스킬 아이콘", n, "명")

    # 3-b) 판 화면 그림 — runui 의 복사기를 이 프로젝트로
    subprocess.run([sys.executable, os.path.join(RUNUI, "Tools~", "copy_assets.py"), PROJ], check=True)


if __name__ == "__main__":
    main()
