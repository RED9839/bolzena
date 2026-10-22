# 원작 영상 ↔ 우리 동작을 같은 시각으로 나란히(2026-10-15) — 위 줄 원작, 아래 줄 우리, 0.1초마다(같은 ms)
#   python Tools/vs_orig.py <motionlab 캡처 폴더(seq/ 있음)> [출력 폴더]
#   평타 = 기본공격 영상 0 ~ 경계ms, 강공 = 기본공격 경계ms ~, 스킬 = 저학년 영상, 고학년 = 고학년 영상. 우리 시각 0 = 동작 시작(고학년은 컷인 끝)
import sys, os, re, glob, csv, shutil, tempfile
import cv2
from PIL import Image, ImageDraw, ImageFont
cap_dir = sys.argv[1]; out = sys.argv[2] if len(sys.argv) > 2 else os.path.join(cap_dir, '_vs')
os.makedirs(out, exist_ok=True)
VID = r'C:/projects/bolzena-unity-tmp/ult_videos'
idx = {r['사도']: r for r in csv.DictReader(open(os.path.join(VID, 'video_index.tsv'), encoding='utf-8-sig'), delimiter='\t')}
try: font = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 14)
except Exception: font = ImageFont.load_default()
CLIP = {'1평타': '기본공격', '2강공': '기본공격', '3스킬': '저학년', '5고학년': '고학년'}
tmp = tempfile.mkdtemp()
def orig_frames(hero, label, times):
    clip = CLIP[label]; src = os.path.join(VID, hero, clip + '.mp4')
    if not os.path.exists(src): return {}
    off = 0
    if label == '2강공':
        try: off = int(idx.get(hero, {}).get('기본공격_경계ms') or 0)
        except ValueError: off = 0
    dst = os.path.join(tmp, 'v.mp4'); shutil.copyfile(src, dst)
    cap = cv2.VideoCapture(dst); res = {}
    for t in times:
        cap.set(cv2.CAP_PROP_POS_MSEC, off + t)
        ok, fr = cap.read()
        if ok: res[t] = Image.fromarray(cv2.cvtColor(fr, cv2.COLOR_BGR2RGB))
    cap.release(); return res
CW, CH, PER = 220, 200, 12
made = 0
for d in sorted(glob.glob(os.path.join(cap_dir, 'seq', '*'))):
    m = re.match(r'\d+_(.+)_(1평타|2강공|3스킬|5고학년)$', os.path.basename(d))
    if not m: continue
    hero, label = m.group(1), m.group(2)
    ours = {int(os.path.basename(f)[:-4]): f for f in glob.glob(os.path.join(d, '*.jpg'))}
    if not ours: continue
    times = sorted(ours)
    orig = orig_frames(hero, label, times)
    pages = [times[i:i + PER] for i in range(0, len(times), PER)]
    for pi, page in enumerate(pages):
        S = Image.new('RGB', (len(page) * CW + 60, 2 * CH + 40), (16, 18, 26)); dr = ImageDraw.Draw(S)
        dr.text((4, 2), f'{hero} {label}  위: 원작 · 아래: 우리  ({pi + 1}/{len(pages)})', fill=(255, 220, 90), font=font)
        dr.text((4, 20 + CH // 2), '원작', fill=(200, 200, 200), font=font); dr.text((4, 20 + CH + CH // 2), '우리', fill=(200, 200, 200), font=font)
        for k, t in enumerate(page):
            x = 60 + k * CW
            if t in orig:
                o = orig[t]; o.thumbnail((CW, CH)); S.paste(o, (x + (CW - o.width) // 2, 20))
            ou = Image.open(ours[t]).convert('RGB'); W0, H0 = ou.size
            ou = ou.crop((int(W0 * 0.10), int(H0 * 0.16), int(W0 * 0.60), int(H0 * 0.92)))   # 사도 · 앞 적만(원작 미리보기 구도에 가깝게)
            ou.thumbnail((CW, CH)); S.paste(ou, (x + (CW - ou.width) // 2, 20 + CH))
            dr.text((x + 3, 22), f'{t / 1000:.1f}s', fill=(255, 255, 0), font=font)
        S.save(os.path.join(out, f'{os.path.basename(d)}_{pi + 1}.png')); made += 1
print('나란히', made, '장 →', out)
