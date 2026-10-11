# 적 리뉴얼 현황 통계 — 클론 제외 적의 패시브 · 수 · 강인도 · 글 길이(00_기준.md §2). python stats.py <villages 폴더> [x = 적마다 한 줄]
import json,glob,collections,statistics as st,sys
rows=[]
for f in sorted(glob.glob(sys.argv[1]+'/*.json')):
    if '_그림' in f: continue
    d=json.load(open(f,encoding='utf-8'))
    vid=d['villages'][0]['id']
    for e in d['enemies']:
        if 'clone' in e: continue
        el = e['id'].endswith('_elite')
        its=e.get('intents',[])+ (e.get('phase',{}) or {}).get('intents',[])
        types=collections.Counter(i['t'] for i in e.get('intents',[]))
        rows.append(dict(v=vid,id=e['id'],name=e['name'],el=el,hp=e['hp'],tough=e.get('tough'),
          ni=len(e.get('intents',[])),nt=len(types),types=types,np=len(e.get('passives',[])),nc=len(e.get('counters',[])),
          pon=[p['on'] for p in e.get('passives',[])],phase='phase' in e,rare=bool(e.get('rare')),
          addc=[i.get('id') for i in its if i['t'] in('addCard','reshuffle')],
          charge=any(i['t']=='charge' for i in its), blen=len(e.get('blurb','')),
          maxhit=max([i.get('v',0)*(i.get('n',1) if i['t']=='multi' else 2 if i['t']=='attackAll' else 1) for i in e.get('intents',[]) if i['t'] in('attack','back','multi','attackAll')] or [0])))
def summ(rs,lab):
    if not rs: return
    print(f"## {lab} n={len(rs)}")
    for k in ['hp','tough','ni','nt','np','nc','blen','maxhit']:
        vals=[r[k] for r in rs if r[k] is not None]
        print(f"  {k}: min {min(vals)} med {st.median(vals)} mean {st.mean(vals):.1f} max {max(vals)}")
    print("  passive on:",collections.Counter(p for r in rs for p in r['pon']).most_common())
    print("  broken-only passives:", sum(1 for r in rs if r['pon'] and all(p=='broken' for p in r['pon'])), " no non-broken gimmick:", sum(1 for r in rs if not [p for p in r['pon'] if p!='broken'] and r['nc']==0))
    print("  intent types:",collections.Counter(t for r in rs for t in r['types']).most_common())
    print("  addCard users:",sum(1 for r in rs if r['addc']), collections.Counter(c for r in rs for c in r['addc']).most_common())
    print("  phase:",sum(r['phase'] for r in rs)," charge:",sum(r['charge'] for r in rs), " rare:", sum(r['rare'] for r in rs))
summ([r for r in rows if not r['el']],'일반')
summ([r for r in rows if r['el']],'엘리트')
print()
for v in sorted(set(r['v'] for r in rows)):
    rs=[r for r in rows if r['v']==v]
    print(v, len(rs), 'el',sum(r['el'] for r in rs), 'hp med',st.median([r['hp'] for r in rs if not r['el']]), 'tough', collections.Counter(r['tough'] for r in rs), 'np mean %.1f'%st.mean([r['np'] for r in rs]))
if len(sys.argv)>2:
  for r in rows:
    print(r['v'],r['id'],r['name'],'E' if r['el'] else '-',r['hp'],r['tough'],r['ni'],dict(r['types']),r['pon'],r['nc'],r['addc'],r['blen'])
