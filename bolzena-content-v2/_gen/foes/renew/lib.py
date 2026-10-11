# 적 리뉴얼 생성기 공용(2026-10-10) — 마을 파일 <마을>.py 가 FOES · NEW · POOLS 를 주고 build() 를 부른다.
#   FOES: 원본에 있는 클론 아닌 적 id → 새 칸(hp · tough · blurb · intents · counters · passives · phase · open · rush …)
#   NEW : 마을판으로 나눈 새 적 id → 칸 전부(name · row · nature 포함) + art(바탕 그림 id)
#   POOLS: 층마다 {"pools": [약, 중, 강], "elites": [...]}
#   DROP: 이 마을 파일에서 뺄 적 id(다른 마을판으로 옮겨 아무도 안 쓰는 것)
# 결과는 _gen/foes/villages/<마을>.json(원본 사본에 클론 아닌 적 · 판 조합만). 적용은 apply.py.
import json, os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
KEYS = ('hp', 'tough', 'pick', 'blurb', 'open', 'rush', 'intents', 'phase', 'counters', 'passives', 'tied', 'soul', 'toughTaken')

def P(name, on, do, **kw):
    d = {"name": name, "on": on, "do": do}; d.update(kw); return d

def scale(FOES, dmg=1.0, hp=1.0, elites=True):
    """마을 세기 맞춤 — 치는 수(공격 · 관통 · 다단 · 전체 · 여는 수 · 판 바뀐 수)의 v × dmg, HP × hp. 패시브 · 카운터의 수는 그대로."""
    for k, f in FOES.items():
        if not elites and k.endswith('_elite'): continue
        f['hp'] = int(round(f['hp'] * hp / 10.0)) * 10
        its = list(f.get('intents', [])) + ([f['open']] if f.get('open') else []) + list((f.get('phase') or {}).get('intents', []))
        for it in its:
            if it['t'] in ('attack', 'back', 'multi', 'attackAll') and 'v' in it: it['v'] = int(round(it['v'] * dmg / 5.0)) * 5

def _fill(e, f):
    keep = {k: e[k] for k in ('id', 'name', 'row', 'nature', 'weak') if k in e}
    for k in ('name', 'row', 'nature', 'weak'):
        if k in f: keep[k] = f[k]
    e.clear(); e.update(keep)
    e['pick'] = 'shuffle'
    for k in KEYS:
        if k in f and f[k] not in (None, [], {}): e[k] = f[k]

PATCH = {}   # 원본 적을 통째로 다시 쓰지 않고 몇 칸만 고칠 때: id → 함수(e)

def build(village, FOES, POOLS, NEW=None, DROP=(), foes_only=False, src=None):
    src = src or os.path.join(ROOT, 'world', 'villages', village + '.json')
    d = json.load(open(src, encoding='utf-8'))
    out = []
    for e in d['enemies']:
        if 'clone' not in e and e['id'] in DROP: continue
        if 'clone' not in e and e['id'] in FOES: _fill(e, FOES[e['id']])
        if 'clone' not in e: e.pop('rare', None)   # 희귀종 덧붙임 없앰(사용자 2026-10-10)
        if 'clone' not in e and e['id'] in PATCH: PATCH[e['id']](e)
        out.append(e)
    for nid, f in (NEW or {}).items():
        e = {"id": nid}; _fill(e, f); out.append(e)
    d['enemies'] = out
    if not foes_only:
        for fl, p in zip(d['villages'][0]['floors'], POOLS):
            fl['pools'] = p['pools']; fl['elites'] = p['elites']
    return d

BAL_PATH = os.path.join(HERE, 'balance.json')

def balance(d):
    """밸런스 손질(2026-10-10 적 수치 재조정) — balance.json 의 종류별(일반 · 엘리트) · 적별 hp · dmg 배율을 클론 아닌 적에 곱한다.
    생성기 FOES 값은 그대로 두고 마지막에 한 번 — 생성기를 다시 돌려도 같은 결과. 치는 수(공격 · 관통 · 다단 · 전체)의 v 만 곱한다(scale 과 같은 규칙)."""
    if not os.path.exists(BAL_PATH): return d
    bal = json.load(open(BAL_PATH, encoding='utf-8'))
    kinds, ids = bal.get('kind', {}), bal.get('id', {})
    for e in d['enemies']:
        if 'clone' in e: continue
        k = kinds.get('elite' if e['id'].endswith('_elite') else 'fight', {})
        o = ids.get(e['id'], {})
        hp, dmg = k.get('hp', 1) * o.get('hp', 1), k.get('dmg', 1) * o.get('dmg', 1)
        if hp != 1 and 'hp' in e: e['hp'] = int(round(e['hp'] * hp / 10.0)) * 10
        if dmg != 1:
            its = list(e.get('intents', [])) + ([e['open']] if e.get('open') else []) + list((e.get('phase') or {}).get('intents', []))
            for it in its:
                if it.get('t') in ('attack', 'back', 'multi', 'attackAll') and 'v' in it: it['v'] = max(5, int(round(it['v'] * dmg / 5.0)) * 5)
    return d

def main(village, FOES, POOLS, NEW=None, DROP=()):
    a = sys.argv[1:]
    if a[:1] == ['--foes-only']:
        out = a[1]; d = build(village, FOES, POOLS, NEW, DROP, foes_only=True)
    elif a[:1] == ['--src']:   # 다른 원본(측정 사본)에서 만들기: --src <in.json> <out.json>
        d = build(village, FOES, POOLS, NEW, DROP, src=a[1]); out = a[2]
    else:
        out = os.path.join(ROOT, '_gen', 'foes', 'villages', village + '.json'); d = build(village, FOES, POOLS, NEW, DROP)
    balance(d)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    json.dump(d, open(out, 'w', encoding='utf-8'), ensure_ascii=False, indent=2)
    print('→', out)
    return d
