# -*- coding: utf-8 -*-
"""사람 기록에서 장비 · 교주 카드마다 제시 · 고름 · 구매 · 낸 수를 센다(2026-10-13 교주 카드 · 장비 리뉴얼).
    python 사람.py [기록 폴더 C:/projects/bolzena-records] > 사람.tsv
열: id · 상점 제시 · 상점 구매 · 전리품 제시 · 전리품 고름 · 이벤트 제시 · 이벤트 고름 · 끝 덱(판) · 낸 수 · 덱에 있던 싸움
"""
import json, glob, os, sys, re, collections
DIR = sys.argv[1] if len(sys.argv) > 1 else 'C:/projects/bolzena-records'
C = collections.defaultdict(lambda: collections.Counter())
def base(i): return re.sub(r'[\^~+]', '', i.split('@')[0]).lstrip('+')
for f in glob.glob(os.path.join(DIR, '*.json')):
    d = json.load(open(f, encoding='utf-8'))
    shop = []
    for p in d.get('picks') or []:
        k = p.get('k')
        if k == 'shop':
            shop = p.get('offer') or []
            for o in shop: C[base(o)]['shopOffer'] += 1
        elif k == 'buy' and p.get('pick'):
            C[base(p['pick'])]['buy'] += 1
        elif k == 'equip':
            for o in p.get('offer') or []: C[base(o)]['lootOffer'] += 1
            if p.get('pick'): C[base(p['pick'])]['loot'] += 1
        elif k in ('ev.card', 'ev.equip'):
            for o in p.get('offer') or []: C[base(o)]['evOffer'] += 1
            if p.get('pick'): C[base(p['pick'])]['ev'] += 1
    seen = set()
    for c in d.get('deck') or []:
        b = base(c['id'])
        if b.startswith('n_') and b not in seen: seen.add(b); C[b]['deckRuns'] += 1
    arts = d.get('arts') or {}
    for slots in (arts.values() if isinstance(arts, dict) else []):
        for b in set((slots or {}).values()): C[b]['endEquip'] += 1   # 끝날 때 낀 장비(사도 · 칸 → id)
    for fi in d.get('fights') or []:
        inDeck = {base(x) for x in fi.get('deck') or []}
        for b in inDeck:
            if b.startswith('n_'): C[b]['fightsIn'] += 1
        for t in fi.get('log') or []:
            for x in t.get('c') or []:
                b = base(x)
                if b.startswith('n_'): C[b]['played'] += 1
cols = ['shopOffer', 'buy', 'lootOffer', 'loot', 'evOffer', 'ev', 'deckRuns', 'endEquip', 'played', 'fightsIn']
print('id\t' + '\t'.join(cols))
for k in sorted(C):
    if k.startswith('n_') or k.startswith('eq_'):
        print(k + '\t' + '\t'.join(str(C[k][c]) for c in cols))
