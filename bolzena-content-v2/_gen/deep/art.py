import os as _os, sys as _sys
if not _os.environ.get("BZ_OLD_GEN"): _sys.exit("[중단] 옛 생성기입니다(2026-10-11 엘다인 리뉴얼). heroes/** 는 _gen/eldain 이 씁니다. 꼭 돌려야 하면 BZ_OLD_GEN=1")
# 깊이 리뉴얼 묶음의 카드 그림 짝 다시 맞추기 — python _gen/deep/art.py <사도키,사도키,…> [--low 사도키=카드id,…]
# 1) 백업(리뉴얼 전)과 지금 카드 이름을 견줘, 이름이 바뀌었거나 새로 생긴 · 없어진 카드의 손 짝(cardpic_picks.json — fill 이 아닌 줄)을 지운다
#    → cardart_fill.py 가 카드 종류 · 이름 규칙(기본 공격 = 스탠딩 · 기본 스킬 = SD · 고유 = 원작 그림 재고)으로 다시 고른다.
# 2) 원작 저학년 칸(cardart_low.json)은 그 사도의 저학년 카드 id 와 새 이름으로 맞춘다(--low 로 id 를 바꿀 수 있다).
# 3) cardart_fill.py → cardart_check.py. 굽기(copy_run_assets.py)는 유니티 잠금이 비었을 때 따로.
import json, os, subprocess, sys, glob, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
RUNUI = r'C:\projects\bolzena-runui\Tools~'
BACK = r'C:\projects\_backup\heroes_villages_before_deep_20261009\heroes'
NOW = r'C:\projects\bolzena-content-v2\heroes'
heroes = sys.argv[1].split(',')
low_over = {}
if '--low' in sys.argv:
    for kv in sys.argv[sys.argv.index('--low') + 1].split(','):
        h, cid = kv.split('='); low_over[h] = cid

def load(root, hid):
    f = [p for p in glob.glob(os.path.join(root, '*', hid + '.json'))]
    return {c['id']: c for c in json.load(open(f[0], encoding='utf-8'))['cards']} if f else {}

picks_p = os.path.join(RUNUI, 'cardpic_picks.json'); low_p = os.path.join(RUNUI, 'cardart_low.json')
picks = json.load(open(picks_p, encoding='utf-8')); low = json.load(open(low_p, encoding='utf-8'))
dropped, lowfix = [], []
for hid in heroes:
    old, new = load(BACK, hid), load(NOW, hid)
    for cid in set(old) | set(new):
        if not cid.startswith(hid + '_') or cid[len(hid) + 1:].count('_'):   # 다른 사도(에르핀_왕도 등) 카드는 건드리지 않는다
            if not (cid.startswith(hid + '_') and cid[len(hid) + 1:] in ('cake', 'bite', 'dream', 'red')): continue
        o, n = old.get(cid), new.get(cid)
        row = picks['cards'].get(cid)
        if row and not row.get('fill') and (n is None or o is None or o['name'] != n['name']):
            del picks['cards'][cid]; dropped.append(cid)
    # 저학년 칸
    keys = [k for k in low['cards'] if k.startswith(hid + '_u') and k[len(hid) + 2:].isdigit()]
    want = low_over.get(hid) or (keys[0] if keys else None)
    for k in keys:
        if k != want: del low['cards'][k]
    if want and want in new:
        if low['cards'].get(want) != new[want]['name']: lowfix.append(f"{want}={new[want]['name']}")
        low['cards'][want] = new[want]['name']
json.dump(picks, open(picks_p, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
json.dump(low, open(low_p, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
print('손 짝 지움', len(dropped), ' '.join(dropped))
print('저학년 칸', ' '.join(lowfix) or '그대로')
env = dict(os.environ, PYTHONIOENCODING='utf-8')
r = subprocess.run([sys.executable, os.path.join(RUNUI, 'cardart_fill.py')], capture_output=True, text=True, encoding='utf-8', env=env, cwd=os.path.dirname(RUNUI))
print(r.stdout[-1200:], r.stderr[-800:])
r = subprocess.run([sys.executable, os.path.join(RUNUI, 'cardart_check.py'), '--all'], capture_output=True, text=True, encoding='utf-8', env=env, cwd=os.path.dirname(RUNUI))
out = r.stdout.splitlines()
for h in heroes:
    for i, l in enumerate(out):
        if l.split('\t')[0][3:] == h: print(l); [print(x) for x in out[i + 1:i + 14] if x.startswith('     ')]
print('\n'.join(out[-3:]))
