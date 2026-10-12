# -*- coding: utf-8 -*-
"""두 데이터 폴더의 장비 · 교주 카드를 id 로 견줘 바뀐 id 만 적는다(리뉴얼 확인용).
    python 차이.py <전 폴더(world 를 품은)> <뒤 폴더>"""
import json, os, sys
def items(root):
    o = {}
    for f in ('equips/무기', 'equips/방어구', 'equips/장신구', 'neutral/교주카드'):
        d = json.load(open(os.path.join(root, 'world', f + '.json'), encoding='utf-8'))
        for x in d.get('equips') or d.get('cards'): o[x['id']] = x
    return o
a, b = items(sys.argv[1]), items(sys.argv[2])
ch = [k for k in a if json.dumps(a[k], sort_keys=True) != json.dumps(b.get(k), sort_keys=True)]
print('바뀜', len(ch), ' '.join(ch)); print('새로', [k for k in b if k not in a]); print('빠짐', [k for k in a if k not in b])
