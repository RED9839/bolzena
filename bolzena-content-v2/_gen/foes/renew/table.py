# 마을마다 리뉴얼 적 한 줄 표(문서용) — python table.py > 표.md
import json, os
ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..'))
NAMES = {"furry": "수인 부락", "spirit": "정령산", "dragon": "용족 동굴(불길과 물길의 터)", "erpien": "에르피엔", "ghost": "유령 늪", "monatium": "모나티엄"}
for v in ["furry", "spirit", "dragon", "erpien", "ghost", "monatium"]:
    d = json.load(open(os.path.join(ROOT, '_gen', 'foes', 'villages', v + '.json'), encoding='utf-8'))
    used = set()
    for fl in d['villages'][0]['floors']:
        for t in fl['pools']:
            for g in t: used.update(g)
        for g in fl['elites']: used.update(g)
    print(f"### {NAMES[v]}\n\n| 적 | 등급 | HP · 강인도 | 기믹(정보창 첫 줄 = blurb) |\n|---|---|---|---|")
    for e in d['enemies']:
        if 'clone' in e or e['id'] not in used and not e['id'].endswith('_pupil'): continue
        el = '엘리트' if e['id'].endswith('_elite') else '일반'
        gim = ' · '.join([c['name'] for c in e.get('counters', [])] + [p['name'] for p in e.get('passives', [])] + (['국면'] if e.get('phase') else []) + ([f"행동 카운트 {e['rush']}"] if 'rush' in e else [])
                     + [f"{it['say']}(실드 있을 때)" for it in e['intents'] if it.get('if', {}).get('selfBlock')]
                     + [f"{it['say']}(카드 삼키기)" for it in e['intents'] if it['t'] == 'seize']
                     + ([f"{e['open']['say']}(첫 턴)"] if e.get('open') else [])
                     + [f"{it['say']}(소환)" for it in e['intents'] if it['t'] == 'summon'])
        print(f"| {e['name']} `{e['id']}` | {el} | {e['hp']} · {e.get('tough')} | **{gim or '—'}** — {e['blurb']} |")
    print()
