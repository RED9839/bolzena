# 볼제나 편성 통계 — 전투 하나 HP 합 · 턴 피해 기댓값(적_상세.md §1-3 잣대)
import json, glob, os, sys, statistics as st, collections

ROOT = sys.argv[1] if len(sys.argv) > 1 else r'C:\projects\bolzena-content-v2\world\villages'
PACK = None  # {n: x} 일반전 마릿수 배율 — None 이면 배율 없음
ELITE_PACK = None
if len(sys.argv) > 2:
    PACK = {int(k): float(v) for k, v in (p.split(':') for p in sys.argv[2].split(','))}
if len(sys.argv) > 3:
    ELITE_PACK = {int(k): float(v) for k, v in (p.split(':') for p in sys.argv[3].split(','))}

ENEMY_HP = 0.85; FLOOR_HP = {1: 2.0, 2: 4.3}; FIGHT_HP = {1: 1.5, 2: 1.15}; ELITE_HP = 1.5; BOSS_HP = {1: 1.0, 2: 0.6}
FLOOR_DMG = {1: 2.1, 2: 2.35}; ENEMY_DMG = 0.92; BOSS_DMG = {1: 1.05, 2: 1.15}
PARTY = 2200

def scale(k, kind):
    hp = ENEMY_HP * FLOOR_HP[k] * (BOSS_HP[k] if kind == 'boss' else 1) * (ELITE_HP if kind == 'elite' else 1) * (FIGHT_HP[k] if kind == 'fight' else 1)
    dmg = ENEMY_DMG * FLOOR_DMG[k] * (BOSS_DMG[k] if kind == 'boss' else 1)
    return hp, dmg

E = {}
V = []
for f in sorted(glob.glob(os.path.join(ROOT, '*.json'))):
    if os.path.basename(f).startswith('_'): continue
    d = json.load(open(f, encoding='utf-8'))
    for e in d['enemies']: E[e['id']] = e
    for v in d['villages']: V.append((os.path.basename(f), v))

def hitv(it):
    t = it.get('t')
    if t in ('attack', 'back'): return it.get('v', 0)
    if t == 'multi': return it.get('v', 0) * max(1, it.get('n', 1))
    if t == 'attackAll': return it.get('v', 0) * 2
    return 0

def exp_hit(e):
    its = e.get('intents') or []
    tw = sum(it.get('w', 1) for it in its) or 1
    return sum(hitv(it) * it.get('w', 1) for it in its) / tw

def max_hit(e):
    its = e.get('intents') or []
    return max([hitv(it) for it in its] + [0])

def fight(ids, k, kind):
    hs, ds = scale(k, kind)
    n = len(ids)
    px = 1
    if kind == 'fight' and PACK: px = PACK.get(n, 1)
    if kind == 'elite' and ELITE_PACK: px = ELITE_PACK.get(n, 1)
    hp = sum(E[i]['hp'] for i in ids) * hs * px
    dm = sum(exp_hit(E[i]) for i in ids) * ds * px
    mx = sum(max_hit(E[i]) for i in ids) * ds * px
    return hp, dm, mx

def q(xs):
    xs = sorted(xs)
    return f'{xs[0]:.0f}/{st.median(xs):.0f}/{xs[-1]:.0f}'

res = collections.defaultdict(list)
cnt = collections.defaultdict(collections.Counter)
same = collections.Counter()
for fn, v in V:
    for fi, fl in enumerate(v['floors']):
        k = fi + 1
        for ti, tier in enumerate(fl['pools']):
            for p in tier:
                res[(k, 'fight')].append(fight(p, k, 'fight'))
                res[(k, 'fight', ti)].append(fight(p, k, 'fight'))
                cnt[(k, 'fight')][len(p)] += 1
                if len(set(p)) < len(p): same[(k, 'fight')] += 1
        for p in fl['elites']:
            res[(k, 'elite')].append(fight(p, k, 'elite'))
            cnt[(k, 'elite')][len(p)] += 1
        for b in fl['boss']:
            res[(k, 'boss')].append(fight([b], k, 'boss'))

for key in sorted(res, key=str):
    r = res[key]
    hp = [x[0] for x in r]; dm = [x[1] for x in r]; mx = [x[2] for x in r]
    print(key, len(r), 'HP', q(hp), 'turn %.1f%%' % (st.median(dm) / PARTY * 100), 'max %.1f%%' % (st.median(mx) / PARTY * 100),
          dict(cnt.get(key[:2], {})) if len(key) == 2 else '', 'same', same.get(key[:2], '') if len(key) == 2 else '')
for k in (1, 2):
    nf = st.median([x[0] for x in res[(k, 'fight')]])
    ne = st.median([x[0] for x in res[(k, 'elite')]])
    nb = st.median([x[0] for x in res[(k, 'boss')]])
    print(f'{k}층 일반:정예:보스 = 1 : {ne / nf:.2f} : {nb / nf:.2f}')
