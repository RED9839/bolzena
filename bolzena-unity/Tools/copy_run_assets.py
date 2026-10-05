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
    # PMA → 곧은 알파 + 번짐 채움(runui Tools~/spine_straight.py) — 그냥 rgb ÷ a 로 나누면 가장자리에 흰 · 빨간 점 · 흰 테두리가 생긴다
    sys.path.insert(0, "C:/projects/bolzena-runui/Tools~")
    import spine_straight
    spine_straight.convert(src, dst)


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


def audio(job):
    # 원작 소리는 Ogg Opus 라 유니티가 못 읽는다 — 풀어서 WAV 로(빌드는 유니티가 Vorbis 로 다시 줄인다)
    src, dst = job
    if os.path.exists(dst):
        return 0
    import soundfile as sf
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    data, sr = sf.read(src)
    sf.write(dst + ".tmp.wav", data, sr, subtype="PCM_16")
    os.replace(dst + ".tmp.wav", dst)
    return 1


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
    # 1-c) 스탠딩 스파인 135명(spine/standing/<그림 키 · 웹판 키 · 한글 이름> → Resources/Spine/st_<그림 키>) — 고학년 컷인 · 사도 화면이 실제로 재생한다
    #   (Normal 스킨 · Idle_1 — 덧스킨은 입히지 않는다). 폴더 이름이 한글인 사도가 많다
    sroot = f"{SRC}/spine/standing"
    have = set(os.listdir(sroot))
    for h in roster:
        art = h.get("art")
        src = next((n for n in (art, h["key"], h["ko"]) if n and n in have), None)
        if art and src:
            jobs.append((f"{sroot}/{src}", "st_" + art))
    for e in os.listdir(f"{SRC}/spine/enemy"):
        jobs.append((f"{SRC}/spine/enemy/{e}", e))
    # 2-a) 원작 적 스파인 둘째 묶음(monsterspine — 목도룡 · 길어용 · 동석 …). spine/enemy 에 같은 이름이 있으면 그것을 둔다
    for e in os.listdir(f"{SRC}/monsterspine"):
        jobs.append((f"{SRC}/monsterspine/{e}", e))
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

    # 1-b) 고학년 소리 — 사도 135명 모두(시범 여섯만 있던 것을 넓힌다): 효과음 <키>_ultimate* · 목소리 ultimate*
    #   전투가 Sfx/hero/<키>/<키>_ultimate… (시전 · _hit) 와 Voice/<키>/ultimate… 를 찾는다(BattleDirector.HeroSfx · UltCutin)
    ajobs = []
    for h in roster:
        art = h.get("art")
        if not art:
            continue
        sd, vd = f"{SRC}/sfx/hero/{art}", f"{SRC}/voice/{art}"
        if os.path.isdir(sd):
            fs = [f for f in os.listdir(sd) if f.endswith(".ogg")]
            # 원작 파일 이름이 틀린 사도(마리 maire_ultimate…) — 키 이름으로 바로잡는다
            ult = [f for f in fs if "_ultimate" in f]
            # 고학년 효과음이 없는 사도(이프리트) — 스킬 효과음으로 대신(전투 HeroSfx 가 ultimate → spskill 로 떨어진다)
            pick = ult or [f for f in fs if "_spskill" in f or "_skill" in f]
            for f in pick:
                name = art + f[f.index("_"):] if not f.startswith(art + "_") else f
                ajobs.append((f"{sd}/{f}", f"{DST}/Sfx/hero/{art}/{name[:-4]}.wav"))
        if os.path.isdir(vd):
            fs = [f for f in os.listdir(vd) if f.endswith(".ogg")]
            # 고학년 목소리가 없는 사도(디아나) — 외침(shout)으로 대신(컷인이 ultimate → shout 순으로 찾는다)
            pick = [f for f in fs if f.startswith("ultimate")] or [f for f in fs if f.startswith("shout")]
            for f in pick:
                ajobs.append((f"{vd}/{f}", f"{DST}/Voice/{art}/{f[:-4]}.wav"))
    with ProcessPoolExecutor(max_workers=24) as ex:
        na = sum(ex.map(audio, ajobs, chunksize=16))
    print("고학년 소리", na, "새로 ·", len(ajobs), "개")

    # 2-b) 원작 적 정지 아이콘(monster/icon_*.png → Resources/Art/Monster) — 스킨이 없는 적의 안전판 · 적 도감
    m = 0
    for f in sorted(os.listdir(f"{SRC}/monster")):
        if f.endswith(".png"):
            dst = f"{DST}/Art/Monster/{f}"
            if not os.path.exists(dst):
                cp(f"{SRC}/monster/{f}", dst, (256, 256))
                m += 1
    print("적 아이콘", m, "새로")

    # 3-b) 판 화면 그림 — runui 의 복사기를 이 프로젝트로
    subprocess.run([sys.executable, os.path.join(RUNUI, "Tools~", "copy_assets.py"), PROJ], check=True)


if __name__ == "__main__":
    main()
