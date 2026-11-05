# 적 편성 · 수치를 원작 범위로(2026-10-31) — 정예전 정예만 · 전체 공격 → 단일 · 일반전 1~5마리 · 튀는 값
# 쓰기: python transform.py <원본 villages 폴더> <쓸 폴더> <정예 HP 배율> — 지금 world/villages 는 _backup/enemy_orig_20261031/villages · 1.95 로 만든 것
import json, glob, os, sys, re, collections, copy

SRC, DST = sys.argv[1], sys.argv[2]
ELITE_F = float(sys.argv[3]) if len(sys.argv) > 3 else 1.0
BOSS_DMG = {1: 1.05, 2: 1.15}
REF = 95   # 볼제나 일반 1타 중앙(적_상세.md §7-1 잣대)
# 원작 등급 90% 값(1타 · 한 수, 일반 1타 중앙 100% = 1) — 적_상세.md §7-1
CAP = {'boss': (3.4, 4.0), 'elite': (4.0, 4.5), 'n': (2.5, 2.8)}
HITS = ('attack', 'back', 'multi', 'attackAll')
# 1층 약 세기 혼자로 두기엔 기믹이 무거운 적(2026-11-01) — 받는 피해를 깎는 껍질(고정 1 · -15%×3) · HP 600 에 실드 깨면 반격
HEAVY_SOLO = {'golem', 'oldtree', 'gluttonbear'}

files = {}
for f in sorted(glob.glob(os.path.join(SRC, '*.json'))):
    b = os.path.basename(f)
    if b.startswith('_'): continue
    raw = open(f, 'rb').read()
    files[b] = (json.loads(raw.decode('utf-8')), b'\r\n' in raw)
E = {e['id']: e for d, _ in files.values() for e in d['enemies']}

log = collections.defaultdict(list)

# ── 층 · 등급 ──
floor_of = {}
for b, (d, _) in files.items():
    for v in d['villages']:
        for fi, fl in enumerate(v['floors']):
            for i in fl['boss']: floor_of[i] = fi + 1
def grade(e):
    if e.get('boss') or e.get('clone'): return 'boss'
    return 'elite' if e['id'].endswith('_elite') else 'n'
def boss_floor(e):
    if e['id'] in floor_of: return floor_of[e['id']]
    m = re.match(r'(\d)층 보스', e.get('blurb', ''))
    return int(m.group(1)) if m else 2

# ── 2. 전체 공격 → 단일(같은 피해: v × FOE_ALL_X 2) · 3/5. 원작 등급 90% 안으로 ──
def walk(x, fn):
    if isinstance(x, dict):
        if x.get('t') in HITS: fn(x)
        for v in x.values(): walk(v, fn)
    elif isinstance(x, list):
        for v in x: walk(v, fn)

for e in E.values():
    g = grade(e)
    dmgf = BOSS_DMG[boss_floor(e)] if g == 'boss' else 1.0
    c1 = int(CAP[g][0] * REF / dmgf); cn = int(CAP[g][1] * REF / dmgf)
    def fix(it, e=e, c1=c1, cn=cn):
        if it['t'] == 'attackAll':
            v0 = it['v']; it['t'] = 'attack'; it['v'] = v0 * 2
            log['all'].append((e['id'], it.get('say'), v0, it['v']))
        v0 = it['v']
        if it['t'] == 'multi':
            n = max(1, it.get('n', 1))
            v = min(v0, c1, cn // n)
        else:
            v = min(v0, c1)
        if v != v0:
            it['v'] = v
            log['cap'].append((e['id'], it.get('say'), it['t'], v0, v, it.get('n', 1)))
    # 키 차례를 지키며 t 를 바꾼다(dict 는 그대로 — t 값만)
    walk(e, fix)

# 글 — 파티에 주는 「전체」 피해 말을 단일 꼴로(적 전체 · 파티 전체 상태는 그대로)
TEXT = [('로 전체를 칩니다', '로 크게 칩니다'), ('으로 전체를 칩니다', '으로 크게 칩니다'), ('전체를 찍습니다', '크게 찍습니다'),
        ('(전체 공격 · ', '(큰 한 방 · '), ('전체 폭격', '큰 폭격'), ('전체 피해', '큰 피해'), ('전체 진압 사격', '일제 진압 사격'), ('다음 차례에 전체 경보', '다음 차례에 경보 사격'),
        ('전체를 칩니다', '크게 칩니다'), ('전체를 쏘고', '크게 쏘고'), ('전체를 치고', '크게 치고'), ('전체를 날립니다', '크게 날립니다'), ('전체 물벼락', '큰 물벼락'), ('(전체 공격)', '(큰 공격)'), ('전체 경보', '경보 사격')]
def retext(x):
    if isinstance(x, dict):
        for k, v in x.items():
            if isinstance(v, str) and k in ('blurb', 'say', 'desc'):
                s = v
                for a, b2 in TEXT: s = s.replace(a, b2)
                if s != v: log['text'].append((k, v[:60], s[:60])); x[k] = s
            else: retext(v)
    elif isinstance(x, list):
        for v in x: retext(v)
for e in E.values(): retext(e)

# ── 1. 정예 HP ──
elite_ids = set()
for d, _ in files.values():
    for v in d['villages']:
        for fl in v['floors']:
            for p in fl['elites']:
                for i in p:
                    if i.endswith('_elite'): elite_ids.add(i)
for i in sorted(elite_ids):
    e = E[i]; h0 = e['hp']; e['hp'] = int(round(h0 * ELITE_F / 10.0)) * 10
    log['ehp'].append((i, h0, e['hp']))

# ── 편성 ──
def hp(i): return E[i]['hp']
def heavy(p): return max(p, key=lambda i: (hp(i), i))
def light(p): return min(p, key=lambda i: (hp(i), i))
def uniq(seq):
    out = []
    for x in seq:
        if x not in out: out.append(x)
    return out

for b, (d, _) in files.items():
    for v in d['villages']:
        for fi, fl in enumerate(v['floors']):
            P = fl['pools']
            new = [[], [], []]
            used = []
            def add(t, f):
                # 같은 세기에 똑같은 싸움이 이미 있으면 그대로 두지 않는다(호출부가 대안을 준다)
                new[t].append(f)
            # 약(2마리 다섯) → 1 · 1 · 2(같은 종) · 2(같은 종) · 2(그대로)
            w = P[0]
            s0 = [heavy(w[0])]; s1 = [heavy(w[1])]
            if s1 == s0: s1 = [light(w[1])]
            add(0, s0); add(0, s1)
            a = [w[2][0], w[2][0]]; bb = [w[3][1], w[3][1]]
            if bb == a: bb = [w[3][0], w[3][0]]
            add(0, a); add(0, bb); add(0, list(w[4]))
            # 중(2~3마리 다섯) → 1 · 1 · 2(같은 종) · 3 · 4(가벼운 종 셋 + 하나)
            m = P[1]
            def solo(p):
                for c in (heavy(p), light(p)) + tuple(p):
                    if [c] not in new[0] + new[1] + new[2]: return [c]
                return [heavy(p)]
            add(1, solo(m[0])); add(1, solo(m[1]))
            add(1, [m[2][0], m[2][0]])
            add(1, list(m[3]) if len(m[3]) == 3 else [m[3][0], m[3][1], m[3][1]])
            l4 = light(m[4]); o4 = heavy(m[4])
            add(1, [l4, l4, l4, o4])
            # 강(3마리 다섯) → 3(그대로) · 3(같은 종 둘) · 4 · 5 · 5
            s = P[2]
            add(2, list(s[0]))
            add(2, [s[1][0], s[1][0], s[1][1]])
            l2 = sorted(s[2], key=lambda i: (hp(i), i)); add(2, [l2[0], l2[0], l2[1], l2[1]])
            l3 = sorted(s[3], key=lambda i: (hp(i), i)); add(2, [l3[0], l3[0], l3[0], l3[1], l3[1]])
            l4s = sorted(s[4], key=lambda i: (hp(i), i)); add(2, [l4s[0], l4s[0], l4s[1], l4s[1], l4s[2]])
            # (2026-11-01) 1층 약 세기의 혼자 싸움에서 기믹이 무거운 적은 중 세기로 옮기고, 그 자리를 약 세기 짝 중 가벼운 적 혼자로 채운다
            if fi == 0:
                for k, f in enumerate(new[0]):
                    if len(f) != 1 or f[0] not in HEAVY_SOLO: continue
                    solos = {g[0] for t in (0, 1) for g in new[t] if len(g) == 1}
                    cand = sorted({i for g in new[0] if len(g) > 1 for i in g if not E[i].get('counters')}, key=lambda i: (hp(i), i))
                    pick = next((i for i in cand if i not in solos), cand[0])
                    new[0][k] = [pick]; new[1].append(f)
                    log['move'].append((b, f[0], '약 → 중', '약 자리', pick))
            log['pool'].append((b, fi + 1, [len(f) for t in new for f in t]))
            fl['pools'] = new
            # 정예 — 정예만: 셋은 혼자, 하나는 서로 다른 정예 둘
            els = [p[0] if p[0].endswith('_elite') else next(i for i in p if i.endswith('_elite')) for p in fl['elites']]
            kinds = uniq(els)
            solo = [[k] for k in kinds[:3]]
            while len(solo) < 3: solo.append([kinds[len(solo) % len(kinds)]])
            pair = [kinds[0], kinds[1]] if len(kinds) >= 2 else [kinds[0], kinds[0]]
            if len(kinds) >= 3: pair = [kinds[1], kinds[2]]
            fl['elites'] = solo + [pair]
            log['elite'].append((b, fi + 1, fl['elites']))

os.makedirs(DST, exist_ok=True)
for b, (d, crlf) in files.items():
    s = json.dumps(d, ensure_ascii=False, indent=2) + '\n'
    if crlf: s = s.replace('\n', '\r\n')
    open(os.path.join(DST, b), 'wb').write(s.encode('utf-8'))

for k in ('all', 'cap', 'text', 'ehp', 'pool', 'move', 'elite'):
    print(f'== {k} {len(log[k])}')
    for x in log[k][: (400 if k in ('cap', 'ehp', 'elite', 'pool', 'move') else 30)]: print('  ', x)
