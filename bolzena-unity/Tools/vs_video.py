# 원작 영상 ↔ 우리 동작 나란히 동영상(2026-10-15 「끊어 보지 말고 흐름으로」) — 왼쪽 원작, 오른쪽 우리, 30fps · 같은 시간축
#   python Tools/vs_video.py <motionlab 캡처 폴더(seq/ — -motionlab-seqfps 30 으로 찍은 것)> [출력 폴더]
#   동작마다 <사도>_<동작>.mp4 + 사도마다 모든 동작을 이은 <사도>_전체.mp4. 원작이 짧으면 마지막 장면에서 멈춤
import sys, os, re, glob, csv, shutil, tempfile
import cv2, numpy as np
from PIL import Image, ImageDraw, ImageFont
cap_dir = sys.argv[1]; out = sys.argv[2] if len(sys.argv) > 2 else os.path.join(cap_dir, '_video')
os.makedirs(out, exist_ok=True)
VID = r'C:/projects/bolzena-unity-tmp/ult_videos'
idx = {r['사도']: r for r in csv.DictReader(open(os.path.join(VID, 'video_index.tsv'), encoding='utf-8-sig'), delimiter='\t')}
font = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 18)
CLIP = {'1평타': '기본공격', '2강공': '기본공격', '3스킬': '저학년', '5고학년': '고학년'}
FPS, H, OW, UW = 30, 432, 444, 768          # 원작 칸 444×432 · 우리 칸 768×432
tmp = tempfile.mkdtemp()
def orig_reader(hero, label):
    src = os.path.join(VID, hero, CLIP[label] + '.mp4')
    if not os.path.exists(src): return None, 0, 0
    dst = os.path.join(tmp, f'{abs(hash(src))}.mp4'); shutil.copyfile(src, dst)
    cap = cv2.VideoCapture(dst)
    off = 0; dur = cap.get(cv2.CAP_PROP_FRAME_COUNT) / max(1, cap.get(cv2.CAP_PROP_FPS)) * 1000
    b = idx.get(hero, {}).get('기본공격_경계ms') or ''
    if label in ('1평타', '2강공') and b.isdigit():
        if label == '2강공': off = int(b); dur -= int(b)
        else: dur = int(b)
    return cap, off, dur
def frame_of(cap, ms):
    cap.set(cv2.CAP_PROP_POS_MSEC, ms); ok, fr = cap.read()
    return cv2.cvtColor(fr, cv2.COLOR_BGR2RGB) if ok else None
heroes = {}
for d in sorted(glob.glob(os.path.join(cap_dir, 'seq', '*'))):
    m = re.match(r'\d+_(.+)_(1평타|2강공|3스킬|5고학년)$', os.path.basename(d))
    if not m: continue
    hero, label = m.groups()
    ours = sorted((int(os.path.basename(f)[:-4]), f) for f in glob.glob(os.path.join(d, '*.jpg')))
    if len(ours) < 3: continue
    cap, off, odur = orig_reader(hero, label)
    total = max(ours[-1][0], odur) + 300
    tmpout = os.path.join(tmp, 'o.mp4')
    vw = cv2.VideoWriter(tmpout, cv2.VideoWriter_fourcc(*'mp4v'), FPS, (OW + UW, H + 40))
    last_o = None; j = 0
    for k in range(int(total / 1000 * FPS)):
        t = k * 1000 / FPS
        S = Image.new('RGB', (OW + UW, H + 40), (16, 18, 26)); dr = ImageDraw.Draw(S)
        if cap is not None and t <= odur:
            fo = frame_of(cap, off + t)
            if fo is not None: last_o = fo
        if last_o is not None:
            o = Image.fromarray(last_o); o.thumbnail((OW, H)); S.paste(o, ((OW - o.width) // 2, 40))
        while j + 1 < len(ours) and ours[j + 1][0] <= t: j += 1
        u = Image.open(ours[j][1]).convert('RGB').resize((UW, H)); S.paste(u, (OW, 40))
        dr.text((8, 8), f'{hero} {label}   왼쪽 원작 · 오른쪽 우리   {t / 1000:4.1f}s', fill=(255, 220, 90), font=font)
        vw.write(cv2.cvtColor(np.asarray(S), cv2.COLOR_RGB2BGR))
    vw.release()
    name = f'{hero}_{label}.mp4'; shutil.move(tmpout, os.path.join(out, name))
    heroes.setdefault(hero, []).append(os.path.join(out, name))
    print('영상', name, f'{total / 1000:.1f}s')
for hero, files in heroes.items():   # 사도마다 이어 붙이기
    tmpout = os.path.join(tmp, 'all.mp4'); vw = None
    for f in files:
        src = os.path.join(tmp, 'in.mp4'); shutil.copyfile(f, src); c = cv2.VideoCapture(src)
        while True:
            ok, fr = c.read()
            if not ok: break
            if vw is None: vw = cv2.VideoWriter(tmpout, cv2.VideoWriter_fourcc(*'mp4v'), FPS, (fr.shape[1], fr.shape[0]))
            vw.write(fr)
        c.release()
    if vw: vw.release(); shutil.move(tmpout, os.path.join(out, f'{hero}_전체.mp4'))
print('→', out)
